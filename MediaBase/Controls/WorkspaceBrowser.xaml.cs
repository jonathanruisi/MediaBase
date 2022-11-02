using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using CommunityToolkit.Mvvm.Messaging;

using JLR.Utility.WinUI;
using JLR.Utility.WinUI.ViewModel;

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
    public sealed partial class WorkspaceBrowser : UserControl
    {
        #region Properties
        public ProjectManager ViewModel => (ProjectManager)DataContext;
        #endregion

        #region Constructor
        public WorkspaceBrowser()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ProjectManager>();

            InitializeCommands();
        }
        #endregion

        #region Event Handlers (UserControl)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            messenger.Register<GeneralMessage, string>(this, "CollapseAllTreeViewNodes", (r, m) =>
            {
                ((WorkspaceBrowser)r).WorkspaceBrowserTreeView.CollapseAllNodes();
            });
        }
        #endregion

        #region Event Handlers (TreeView)
        private void WorkspaceBrowserTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is not ViewModelNode node)
            {
                ViewModel.ActiveWorkspaceBrowserNode = null;
                return;
            }

            ViewModel.ActiveWorkspaceBrowserNode = node;
            if (node is MultimediaSource multimediaSource)
                ViewModel.ActiveMediaSource = multimediaSource;
        }

        private void WorkspaceBrowserTreeView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is TreeViewItem item)
                ViewModel.ActiveWorkspaceBrowserNode = item.DataContext as ViewModelNode;
            else
                ViewModel.ActiveWorkspaceBrowserNode = null;

            e.Handled = true;
        }

        private async void WorkspaceBrowserTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (args.Item is not ViewModelNode node)
                return;

            await ProjectManager.MakeItemsReadyAsync(node);
        }
        #endregion

        #region Event Handlers (Commands - CanExecuteRequested)
        private void WorkspaceSelectMultipleCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ViewModel != null && ViewModel.IsActive;
        }
        #endregion

        #region Event Handlers (Commands - ExecuteRequested)
        private void WorkspaceSelectMultipleCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (WorkspaceBrowserTreeView.SelectionMode == TreeViewSelectionMode.Single)
                WorkspaceBrowserTreeView.SelectionMode = TreeViewSelectionMode.Multiple;
            else
                WorkspaceBrowserTreeView.SelectionMode = TreeViewSelectionMode.Single;
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            ViewModel.WorkspaceSelectMultipleCommand.CanExecuteRequested +=
                WorkspaceSelectMultipleCommand_CanExecuteRequested;
            ViewModel.WorkspaceSelectMultipleCommand.ExecuteRequested +=
                WorkspaceSelectMultipleCommand_ExecuteRequested;
        }
        #endregion
    }
}