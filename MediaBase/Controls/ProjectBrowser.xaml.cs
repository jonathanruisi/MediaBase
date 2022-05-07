using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using JLR.Utility.WinUI.Dialogs;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;

using WinRT.Interop;

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

        #region Event Handlers (Commands - CanExecuteRequested)
        private void ProjectNewFolderCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ViewModel != null && ViewModel.IsActive && ViewModel.ActiveNode is MediaFolder;
        }

        private void ProjectImportFilesCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ViewModel != null && ViewModel.IsActive && ViewModel.ActiveNode is MediaFolder;
        }

        private void ProjectImportFolderCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ViewModel != null && ViewModel.IsActive && ViewModel.ActiveNode is MediaFolder;
        }

        private void ProjectRemoveItemCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ViewModel != null && ViewModel.IsActive && ViewModel.ActiveNode?.Depth > 0;
        }

        private void ProjectRemoveSelectedCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ViewModel != null && ViewModel.IsActive && ProjectBrowserTreeView.SelectedNodes.Count > 0;
        }

        private void ProjectRemoveAllCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ViewModel != null && ViewModel.IsActive && ViewModel.MediaLibrary.Children.Count > 0;
        }

        private void ProjectRenameItemCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ViewModel != null && ViewModel.IsActive && ViewModel.ActiveNode?.Depth > 0;
        }
        #endregion

        #region Event Handlers (Commands - ExecuteRequested)
        private async void ProjectNewFolderCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var dlg = new TextPromptDialog
            {
                Title = "New Folder",
                PromptText = "Enter a name for the new folder",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.ActiveNode.Children.Add(new MediaFolder { Name = dlg.Text });
            }
        }

        private async void ProjectImportFilesCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                CommitButtonText = "Import"
            };

            foreach (var filter in ViewModel.MediaFileExtensions)
            {
                picker.FileTypeFilter.Add(filter);
            }

            InitializeWithWindow.Initialize(picker, App.WindowHandle);

            var messenger = App.Current.Services.GetService<IMessenger>();
            messenger.Send(new SetInfoBarMessage
            {
                Title = "Importing Files",
                Message = "Please wait...",
                Severity = InfoBarSeverity.Informational,
                IsCloseable = false
            });

            var files = await picker.PickMultipleFilesAsync();
            var fileCount = files.Count;
            if (fileCount == 0)
                return;

            foreach (var file in files)
            {
                if (file.ContentType.Contains("image"))
                {
                    var imageFile = new ImageFile(file);
                    await imageFile.LoadMediaPropertiesAsync();
                    ViewModel.ActiveNode.Children.Add(imageFile);
                }
                else if (file.ContentType.Contains("video"))
                {
                    var videoFile = new VideoFile(file);
                    await videoFile.LoadMediaPropertiesAsync();
                    ViewModel.ActiveNode.Children.Add(videoFile);
                }
            }

            messenger.Send(new SetInfoBarMessage
            {
                Title = "Done",
                Message = $"Imported {fileCount} file{(fileCount != 1 ? "s" : "")}",
                Severity = InfoBarSeverity.Success,
                IsCloseable = true
            });
        }

        private void ProjectImportFolderCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {

        }

        private void ProjectRemoveItemCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {

        }

        private void ProjectRemoveSelectedCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {

        }

        private void ProjectRemoveAllCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {

        }

        private void ProjectRenameItemCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {

        }
        #endregion

        #region Event Handlers (UserControl)
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

        #region Private Methods
        private void InitializeCommands()
        {
            ViewModel.ProjectNewFolderCommand.CanExecuteRequested +=
                ProjectNewFolderCommand_CanExecuteRequested;
            ViewModel.ProjectNewFolderCommand.ExecuteRequested +=
                ProjectNewFolderCommand_ExecuteRequested;

            ViewModel.ProjectImportFilesCommand.CanExecuteRequested +=
                ProjectImportFilesCommand_CanExecuteRequested;
            ViewModel.ProjectImportFilesCommand.ExecuteRequested +=
                ProjectImportFilesCommand_ExecuteRequested;

            ViewModel.ProjectImportFolderCommand.CanExecuteRequested +=
                ProjectImportFolderCommand_CanExecuteRequested;
            ViewModel.ProjectImportFolderCommand.ExecuteRequested +=
                ProjectImportFolderCommand_ExecuteRequested;

            ViewModel.ProjectRemoveItemCommand.CanExecuteRequested +=
                ProjectRemoveItemCommand_CanExecuteRequested;
            ViewModel.ProjectRemoveItemCommand.ExecuteRequested +=
                ProjectRemoveItemCommand_ExecuteRequested;

            ViewModel.ProjectRemoveSelectedCommand.CanExecuteRequested +=
                ProjectRemoveSelectedCommand_CanExecuteRequested;
            ViewModel.ProjectRemoveSelectedCommand.ExecuteRequested +=
                ProjectRemoveSelectedCommand_ExecuteRequested;

            ViewModel.ProjectRemoveAllCommand.CanExecuteRequested +=
                ProjectRemoveAllCommand_CanExecuteRequested;
            ViewModel.ProjectRemoveAllCommand.ExecuteRequested +=
                ProjectRemoveAllCommand_ExecuteRequested;

            ViewModel.ProjectRenameItemCommand.CanExecuteRequested +=
                ProjectRenameItemCommand_CanExecuteRequested;
            ViewModel.ProjectRenameItemCommand.ExecuteRequested +=
                ProjectRenameItemCommand_ExecuteRequested;
        }
        #endregion
    }
}