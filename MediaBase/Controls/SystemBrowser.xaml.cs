using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using JLR.Utility.WinUI;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace MediaBase.Controls
{
    public sealed partial class SystemBrowser : UserControl
    {
        #region Properties
        public ProjectManager ViewModel => (ProjectManager)DataContext;
        #endregion

        #region Constructor
        public SystemBrowser()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ProjectManager>();

            InitializeTreeView();
        }
        #endregion

        #region Private Methods
        private async void InitializeTreeView()
        {
            // Add the user's desktop folder to the browser
            var desktopFolder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            var desktopNode = new TreeViewNode
            {
                Content = desktopFolder,
                IsExpanded = false,
                HasUnrealizedChildren = true
            };
            SystemBrowserTreeView.RootNodes.Add(desktopNode);

            // Add the user's personal folder to the browser
            var personalFolder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var personalNode = new TreeViewNode
            {
                Content = personalFolder,
                IsExpanded = false,
                HasUnrealizedChildren = true
            };
            SystemBrowserTreeView.RootNodes.Add(personalNode);

            // Add all logical drives to the browser
            var drives = Environment.GetLogicalDrives();
            foreach (var drive in drives)
            {
                var driveFolder = await StorageFolder.GetFolderFromPathAsync(drive);
                var driveNode = new TreeViewNode
                {
                    Content = driveFolder,
                    IsExpanded = false,
                    HasUnrealizedChildren = true
                };

                SystemBrowserTreeView.RootNodes.Add(driveNode);
            }
        }

        private static async Task FillTreeNode(TreeViewNode node)
        {
            if (node.Content is not StorageFolder folder)
                return;

            var items = await folder.GetItemsAsync();
            if (items.Count == 0)
                return;

            foreach (var subFolder in items.OfType<StorageFolder>())
            {
                var newNode = new TreeViewNode
                {
                    Content = subFolder,
                    HasUnrealizedChildren = true
                };

                node.Children.Add(newNode);
            }

            foreach (var file in items.OfType<StorageFile>())
            {
                var extension = file.GetFileExtension();
                var contentType = file.ContentType.ToLower();

                if (extension != "mbw" &&
                    extension != "mbp" &&
                    !contentType.Contains("image") &&
                    !contentType.Contains("video"))
                    continue;

                var newNode = new TreeViewNode { Content = file };
                node.Children.Add(newNode);
            }
        }
        #endregion

        #region Event Handlers (UserControl)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.SystemBrowserSelectedNodesFunction = new Func<IList<TreeViewNode>>(() =>
            {
                return SystemBrowserTreeView.SelectedNodes;
            });
        }
        #endregion

        #region Event Handlers (TreeView)
        private async void SystemBrowserTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (args.Node.HasUnrealizedChildren)
                await FillTreeNode(args.Node);
        }

        private void SystemBrowserTreeView_Collapsed(TreeView sender, TreeViewCollapsedEventArgs args)
        {
            args.Node.Children.Clear();
            args.Node.HasUnrealizedChildren = true;
        }

        private void SystemBrowserTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is not TreeViewNode node)
                return;

            if (node.Content is StorageFolder)
            {
                node.IsExpanded = !node.IsExpanded;
            }
        }

        private void SystemBrowserTreeView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            
        }
        #endregion
    }
}