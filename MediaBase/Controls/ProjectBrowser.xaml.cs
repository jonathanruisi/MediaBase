using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

using JLR.Utility.WinUI.Dialogs;
using JLR.Utility.WinUI.Messaging;
using JLR.Utility.WinUI.ViewModel;

using MediaBase.Dialogs;
using MediaBase.ViewModel;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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

        private void ProjectSelectMultipleCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ViewModel != null && ViewModel.IsActive;
        }

        private void ToolsCategoryActionCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            // TODO: Move all possible CanExecute/Execute handlers into the Project class itself
            args.CanExecute = true;
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

            var files = await picker.PickMultipleFilesAsync();
            var fileCount = files.Count;
            if (fileCount == 0)
                return;

            // Send message to user that importing has begun
            var messenger = App.Current.Services.GetService<IMessenger>();
            messenger.Send(new SetInfoBarMessage
            {
                Title = "Importing Files",
                Message = "Please wait...",
                Severity = InfoBarSeverity.Informational,
                IsCloseable = false
            });

            foreach (var file in files)
            {
                if (file.ContentType.Contains("image"))
                {
                    var imageFile = new ImageFile(file);
                    //await imageFile.ReadPropertiesFromFileAsync();
                    ViewModel.ActiveNode.Children.Add(imageFile);
                }
                else if (file.ContentType.Contains("video"))
                {
                    var videoFile = new VideoFile(file);
                    //await videoFile.ReadPropertiesFromFileAsync();
                    ViewModel.ActiveNode.Children.Add(videoFile);
                }
            }

            // Send message to user summarizing import results
            messenger.Send(new SetInfoBarMessage
            {
                Title = "Done",
                Message = $"Imported {fileCount} file{(fileCount != 1 ? "s" : "")}",
                Severity = InfoBarSeverity.Success,
                IsCloseable = true
            });
        }

        private async void ProjectImportFolderCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var picker = new FolderPicker
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

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null)
                return;

            var messenger = App.Current.Services.GetService<IMessenger>();
            int fileCount = 0, folderCount = 0;
            await AddFolder(folder, (MediaFolder)ViewModel.ActiveNode);

            // Send message to user summarizing import results
            var message = new StringBuilder();
            message.Append($"Imported {folderCount} folder{(folderCount != 1 ? "s" : "")} and ");
            message.Append($"{fileCount} file{(fileCount != 1 ? "s" : "")}");
            messenger.Send(new SetInfoBarMessage
            {
                Title = "Done",
                Message = message.ToString(),
                Severity = InfoBarSeverity.Success,
                IsCloseable = true
            });

            // Local function to add a StorageFile to a MediaFolder node
            void AddFile(StorageFile sourceFile, MediaFolder destinationFolder)
            {
                if (!ViewModel.MediaFileExtensions.Contains($".{sourceFile.Name.Split('.').Last()}"))
                    return;

                fileCount++;
                /*messenger.Send(new SetInfoBarMessage
                {
                    Title = "Importing File",
                    Message = sourceFile.Name,
                    Severity = InfoBarSeverity.Informational,
                    IsCloseable = false
                });*/

                if (sourceFile.ContentType.Contains("image"))
                {
                    var imageFile = new ImageFile(sourceFile);
                    //await imageFile.ReadPropertiesFromFileAsync();
                    destinationFolder.Children.Add(imageFile);
                }
                else if (sourceFile.ContentType.Contains("video"))
                {
                    var videoFile = new VideoFile(sourceFile);
                    //await videoFile.ReadPropertiesFromFileAsync();
                    destinationFolder.Children.Add(videoFile);
                }
            }

            // Local function to recursively add a storage folder to a ViewModel folder node
            async Task AddFolder(StorageFolder sourceFolder, MediaFolder destinationFolder)
            {
                messenger.Send(new SetInfoBarMessage
                {
                    Title = "Importing Folder",
                    Message = sourceFolder.Path,
                    Severity = InfoBarSeverity.Informational,
                    IsCloseable = false
                });

                var items = await sourceFolder.GetItemsAsync();

                if (items.Count == 0)
                    return;

                folderCount++;
                var newFolder = new MediaFolder(sourceFolder.DisplayName);
                destinationFolder.Children.Add(newFolder);

                foreach (var item in items)
                {
                    if (item is StorageFile file)
                        AddFile(file, newFolder);
                    else if (item is StorageFolder subfolder)
                        await AddFolder(subfolder, newFolder);
                }
            }
        }

        private void ProjectRemoveItemCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            ViewModel.MediaLibrary.Remove(ViewModel.ActiveNode);
            ViewModel.ActiveNode = null;
        }

        private void ProjectRemoveSelectedCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var nodesToRemove = ProjectBrowserTreeView.SelectedNodes.ToArray();
            foreach (var node in nodesToRemove)
            {
                ViewModel.MediaLibrary.Remove(node.Content as ViewModelNode);
            }
        }

        private void ProjectRemoveAllCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            ViewModel.MediaLibrary.Children.Clear();
        }

        private async void ProjectRenameItemCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var dlg = new TextPromptDialog
            {
                Title = "Rename Item",
                PromptText = "Enter a different name for the item",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                Text = ViewModel.ActiveNode.Name,
                XamlRoot = Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.ActiveNode.Name = dlg.Text;
            }
        }

        private void ProjectSelectMultipleCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (ProjectBrowserTreeView.SelectionMode == TreeViewSelectionMode.Single)
                ProjectBrowserTreeView.SelectionMode = TreeViewSelectionMode.Multiple;
            else
                ProjectBrowserTreeView.SelectionMode = TreeViewSelectionMode.Single;
        }

        private async void ToolsCategoryActionCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var messenger = App.Current.Services.GetService<IMessenger>();
            ViewModel.ActiveMediaSource = null;

            var dlg = new BatchActionDialog
            {
                Title = "Perform Batch Action",
                PrimaryButtonText = "Execute",
                CloseButtonText = "Cancel",
                XamlRoot = Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            // TODO: Do this category thing better so that each category can be assigned a unique action
            var itemsToProcess = new List<MBMediaSource>();
            if (dlg.ActOnCategory1 && dlg.Category1Count > 0)
            {
                itemsToProcess.AddRange(ViewModel.MediaLibrary.DepthFirstEnumerable().OfType<MBMediaSource>().Where(x => x.IsCategory1));
            }

            if (dlg.ActOnCategory2 && dlg.Category2Count > 0)
            {
                itemsToProcess.AddRange(ViewModel.MediaLibrary.DepthFirstEnumerable().OfType<MBMediaSource>().Where(x => x.IsCategory2));
            }

            if (dlg.ActOnCategory3 && dlg.Category3Count > 0)
            {
                itemsToProcess.AddRange(ViewModel.MediaLibrary.DepthFirstEnumerable().OfType<MBMediaSource>().Where(x => x.IsCategory3));
            }

            if (dlg.ActOnCategory4 && dlg.Category4Count > 0)
            {
                itemsToProcess.AddRange(ViewModel.MediaLibrary.DepthFirstEnumerable().OfType<MBMediaSource>().Where(x => x.IsCategory4));
            }

            // Process items based on selected action
            var currentItem = 1;
            switch (dlg.Action)
            {
                case BatchAction.Delete:
                    foreach (var item in itemsToProcess)
                    {
                        var message1 = $"Moving {item.Name} to the Recycle Bin (file {currentItem} of {itemsToProcess.Count})";
                        messenger.Send(new SetInfoBarMessage
                        {
                            Title = "Batch Operation Running",
                            Message = message1.ToString(),
                            Severity = InfoBarSeverity.Informational,
                            IsCloseable = false
                        });
                        ViewModel.MediaLibrary.Remove(item);
                        await (item as IMediaFile).File.DeleteAsync();
                        currentItem++;
                    }
                    break;

                case BatchAction.Copy:
                    foreach (var item in itemsToProcess)
                    {
                        var message2 = $"Copying {item.Name} to {dlg.TargetFolder.Path} (file {currentItem} of {itemsToProcess.Count})";
                        messenger.Send(new SetInfoBarMessage
                        {
                            Title = "Batch Operation Running",
                            Message = message2.ToString(),
                            Severity = InfoBarSeverity.Informational,
                            IsCloseable = false
                        });
                        await (item as IMediaFile).File.CopyAsync(dlg.TargetFolder);
                        currentItem++;
                    }
                    break;

                case BatchAction.Move:
                    foreach (var item in itemsToProcess)
                    {
                        var message3 = $"Moving {item.Name} to {dlg.TargetFolder.Path} (file {currentItem} of {itemsToProcess.Count})";
                        messenger.Send(new SetInfoBarMessage
                        {
                            Title = "Batch Operation Running",
                            Message = message3.ToString(),
                            Severity = InfoBarSeverity.Informational,
                            IsCloseable = false
                        });
                        await (item as IMediaFile).File.MoveAsync(dlg.TargetFolder);
                        currentItem++;
                    }
                    break;

                default:
                    var messageFail = $"Something went wrong during batch processing";
                    messenger.Send(new SetInfoBarMessage
                    {
                        Title = "Batch Operation Failed",
                        Message = messageFail.ToString(),
                        Severity = InfoBarSeverity.Error,
                        IsCloseable = true
                    });
                    return;
            }

            // Alert user that the batch operation is complete
            var summaryMessage = new StringBuilder();
            if (dlg.Action == BatchAction.Copy)
                summaryMessage.Append("Copied ");
            else if (dlg.Action is BatchAction.Delete or BatchAction.Move)
                summaryMessage.Append("Moved ");

            summaryMessage.Append(itemsToProcess.Count);
            summaryMessage.Append(" file");
            if (itemsToProcess.Count != 1)
                summaryMessage.Append('s');

            if (dlg.Action == BatchAction.Delete)
                summaryMessage.Append(" to the Recycle Bin");
            else if (dlg.Action is BatchAction.Copy or BatchAction.Move)
            {
                summaryMessage.Append(" to ");
                summaryMessage.Append(dlg.TargetFolder.Path);
            }

            messenger.Send(new SetInfoBarMessage
            {
                Title = "Batch Operation Complete",
                Message = summaryMessage.ToString(),
                Severity = InfoBarSeverity.Success,
                IsCloseable = true
            });
        }
        #endregion

        #region Event Handlers (TreeView)
        private void ProjectBrowserTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is not MBMediaSource mediaSource || mediaSource.IsReady == false)
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

        private async void ProjectBrowserTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (args.Item is not ViewModelNode node)
                return;

            await Project.LoadMediaFilesAsync(node);
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

            ViewModel.ProjectSelectMultipleCommand.CanExecuteRequested +=
                ProjectSelectMultipleCommand_CanExecuteRequested;
            ViewModel.ProjectSelectMultipleCommand.ExecuteRequested +=
                ProjectSelectMultipleCommand_ExecuteRequested;

            ViewModel.ToolsCategoryActionCommand.CanExecuteRequested +=
                ToolsCategoryActionCommand_CanExecuteRequested;
            ViewModel.ToolsCategoryActionCommand.ExecuteRequested +=
                ToolsCategoryActionCommand_ExecuteRequested;
        }
        #endregion
    }
}