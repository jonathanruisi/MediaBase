using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;

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
using Windows.System;

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

        #region Event Handlers (UserControl)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            messenger.Register<RequestMessage<bool>, string>(this, "AreSystemBrowserNodesSelected", (r, m) =>
            {
                m.Reply(((SystemBrowser)r).SystemBrowserTreeView.SelectedNodes.Any());
            });

            messenger.Register<CollectionRequestMessage<TreeViewNode>, string>(this, "GetSelectedSystemBrowserNodes", (r, m) =>
            {
                foreach (var node in ((SystemBrowser)r).SystemBrowserTreeView.SelectedNodes)
                {
                    m.Reply(node);
                }
            });

            messenger.Register<GeneralInfoMessage<TreeViewNode>, string>(this, "SetSelectedSystemBrowserNode", (r, m) =>
            {
                if (m.Info != null)
                    ((SystemBrowser)r).SystemBrowserTreeView.SelectedNode = m.Info;
                else
                {
                    ((SystemBrowser)r).SystemBrowserTreeView.SelectedNode = null;
                    ((SystemBrowser)r).SystemBrowserTreeView.SelectedNodes.Clear();
                }
            });

            messenger.Register<GeneralActionMessage, string>(this, "ClearSystemBrowserSelection", (r, m) =>
            {
                ((SystemBrowser)r).SystemBrowserTreeView.SelectedNodes.Clear();
            });

            // Project Manager's ActiveSystemBrowserNode property changed
            messenger.Register<PropertyChangedMessage<TreeViewNode>>(this, (r, m) =>
            {
                if (m.Sender != ViewModel && m.PropertyName != nameof(ViewModel.ActiveSystemBrowserNode))
                    return;

                if (m.NewValue.Content is StorageFile file &&
                    (file.ContentType.Contains("image") ||
                     file.ContentType.Contains("video")))
                {
                    ((SystemBrowser)r).SystemBrowserTreeView.SelectedNode = m.NewValue;
                }
                else
                {
                    ((SystemBrowser)r).SystemBrowserTreeView.SelectedNode = null;
                }
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
            if (args.InvokedItem is TreeViewNode node)
            {
                ViewModel.ActiveSystemBrowserNode = node;
                if (node.Content is StorageFolder)
                {
                    node.IsExpanded = !node.IsExpanded;
                }
                else if (node.Content is StorageFile file)
                {
                    ViewModel.SetActiveMediaSourceFromNonProjectFile(file);
                }
            }
            else
            {
                ViewModel.ActiveSystemBrowserNode = null;
            }
        }

        private void SystemBrowserTreeView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element && element.DataContext is TreeViewNode node)
                ViewModel.ActiveSystemBrowserNode = node;
            else
                ViewModel.ActiveSystemBrowserNode = null;

            e.Handled = true;
        }
        #endregion

        #region Private Methods
        private async void InitializeTreeView()
        {
            // Add the user's desktop folder to the browser
            var desktopFolder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            var desktopNode = new GroupableTreeViewNode
            {
                Content = desktopFolder,
                IsExpanded = false,
                HasUnrealizedChildren = true
            };
            SystemBrowserTreeView.RootNodes.Add(desktopNode);

            // Add the user's personal folder to the browser
            var personalFolder = await StorageFolder.GetFolderFromPathAsync(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var personalNode = new GroupableTreeViewNode
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
                var driveNode = new GroupableTreeViewNode
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
                var newNode = new GroupableTreeViewNode
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

                var newNode = new GroupableTreeViewNode { Content = file };
                node.Children.Add(newNode);
            }
        }
        #endregion
    }

    public sealed class GroupableTreeViewNode : TreeViewNode
    {
        /// <summary>
        /// Gets or sets a value where each bit represents a group.
        /// </summary>
        /// <remarks>
        /// The meaning of "group" is arbitrary and has no effect on the
        /// functionality of this object.
        /// </remarks>
        public int GroupFlags
        {
            get => (int)GetValue(GroupFlagsProperty);
            set => SetValue(GroupFlagsProperty, value);
        }

        public static readonly DependencyProperty GroupFlagsProperty =
            DependencyProperty.Register("GroupFlags",
                                        typeof(int),
                                        typeof(GroupableTreeViewNode),
                                        new PropertyMetadata(0));
    }
}