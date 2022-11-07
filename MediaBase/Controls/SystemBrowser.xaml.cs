using System;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using JLR.Utility.WinUI;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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

            messenger.Register<CollectionRequestMessage<TreeViewNode>, string>(this, "GetGroupedSystemBrowserNodes", (r, m) =>
            {
                foreach (var rootNode in ((SystemBrowser)r).SystemBrowserTreeView.RootNodes)
                {
                    foreach (var node in rootNode.DepthFirstEnumerable().Where(x => x.Content is IGroupable groupable &&
                                                                                    groupable.GroupFlags != 0))
                    {
                        m.Reply(node);
                    }
                }
            });

            messenger.Register<GeneralMessage, string>(this, "ClearSystemBrowserSelection", (r, m) =>
            {
                ((SystemBrowser)r).SystemBrowserTreeView.SelectedNodes.Clear();
            });

            messenger.Register<GeneralMessage, string>(this, "CollapseAllTreeViewNodes", (r, m) =>
            {
                ((SystemBrowser)r).SystemBrowserTreeView.CollapseAllNodes();
            });

            // Project Manager's ActiveSystemBrowserNode property changed
            messenger.Register<PropertyChangedMessage<TreeViewNode>>(this, (r, m) =>
            {
                if (m.Sender != ViewModel || m.PropertyName != nameof(ViewModel.ActiveSystemBrowserNode))
                    return;

                if (m.NewValue.Content is MultimediaSource mediaSource &&
                    (mediaSource.ContentType == MediaContentType.Image ||
                     mediaSource.ContentType == MediaContentType.Video))
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
        private static async void SystemBrowserTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (!args.Node.HasUnrealizedChildren)
                return;

            await FillTreeNode(args.Node);

            foreach (var mediaSource in args.Node.Children.Select(x => x.Content).OfType<MultimediaSource>())
            {
                await mediaSource.MakeReady();
            }
        }

        private static void SystemBrowserTreeView_Collapsed(TreeView sender, TreeViewCollapsedEventArgs args)
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
                    node.IsExpanded = !node.IsExpanded;
                else if (node.Content is MultimediaSource mediaSource)
                    ViewModel.ActiveMediaSource = mediaSource;
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

                var newNode = new TreeViewNode { HasUnrealizedChildren = false };
                if (extension is "mbw" or "mbp")
                    newNode.Content = file;
                else if (contentType.Contains("image"))
                    newNode.Content = new ImageSource(Guid.Empty, new ImageFile(file) { Id = Guid.Empty });
                else if (contentType.Contains("video"))
                    newNode.Content = new VideoSource(Guid.Empty, new VideoFile(file) { Id = Guid.Empty });
                else continue;

                node.Children.Add(newNode);
            }
        }
        #endregion
    }
}