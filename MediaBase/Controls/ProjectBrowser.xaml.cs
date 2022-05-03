using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using MediaBase.ViewModel;
using MediaBase.ViewModel.Base;

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

namespace MediaBase.Controls
{
    public sealed partial class ProjectBrowser : UserControl
    {
        #region Properties
        public Project ViewModel => (Project)DataContext;
        #endregion

        #region Constructor
        public ProjectBrowser()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<Project>();
            InitializeCommands();
        }
        #endregion

        #region Event Handlers
        private void ProjectBrowserTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (!(args.InvokedItem is MBMediaSource mediaSource))
                return;

            ViewModel.ActiveMediaSource = mediaSource;
        }

        private void ProjectBrowserTreeView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is TreeViewItem item)
                ViewModel.ActiveNode = item.DataContext as ViewModelNode;
            else
                ViewModel.ActiveNode = ViewModel.MediaLibrary;

            e.Handled = true;
        }
        #endregion

        #region Commands
        private void InitializeCommands()
        {
            ViewModel.ProjectNewFolder.CanExecuteRequested += ProjectNewFolder_CanExecuteRequested;
            ViewModel.ProjectNewFolder.ExecuteRequested += ProjectNewFolder_ExecuteRequested;

            ViewModel.ProjectImportFiles.CanExecuteRequested += ProjectImportFiles_CanExecuteRequested;
            ViewModel.ProjectImportFiles.ExecuteRequested += ProjectImportFiles_ExecuteRequested;

            ViewModel.ProjectImportFolder.CanExecuteRequested += ProjectImportFolder_CanExecuteRequested;
            ViewModel.ProjectImportFolder.ExecuteRequested += ProjectImportFolder_ExecuteRequested;

            ViewModel.ProjectRemoveItem.CanExecuteRequested += ProjectRemoveItem_CanExecuteRequested;
            ViewModel.ProjectRemoveItem.ExecuteRequested += ProjectRemoveItem_ExecuteRequested;

            ViewModel.ProjectRemoveSelected.CanExecuteRequested += ProjectRemoveSelected_CanExecuteRequested;
            ViewModel.ProjectRemoveSelected.ExecuteRequested += ProjectRemoveSelected_ExecuteRequested;

            ViewModel.ProjectRemoveAll.CanExecuteRequested += ProjectRemoveAll_CanExecuteRequested;
            ViewModel.ProjectRemoveAll.ExecuteRequested += ProjectRemoveAll_ExecuteRequested;

            ViewModel.ProjectRenameItem.CanExecuteRequested += ProjectRenameItem_CanExecuteRequested;
            ViewModel.ProjectRenameItem.ExecuteRequested += ProjectRenameItem_ExecuteRequested;
        }

        private void ProjectNewFolder_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectImportFiles_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectImportFolder_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectRemoveItem_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectRemoveSelected_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectRemoveAll_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectRenameItem_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectNewFolder_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectImportFiles_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectImportFolder_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectRemoveItem_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectRemoveSelected_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectRemoveAll_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectRenameItem_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }
        #endregion
    }
}