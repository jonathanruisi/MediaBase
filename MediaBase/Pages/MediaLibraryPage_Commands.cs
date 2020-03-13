using System;

using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using JLR.Utility.UWP.Dialogs;

namespace MediaBase
{
	public sealed partial class MediaLibraryPage
	{
		#region Private Methods
		private void InitializeCommands()
		{
			// MediaLibraryNewFolderCommand
			Commands.MediaLibraryNewFolderCommand.MediaLibraryNewFolder.CanExecuteRequested +=
				MediaLibraryNewFolder_CanExecuteRequested;
			Commands.MediaLibraryNewFolderCommand.MediaLibraryNewFolder.ExecuteRequested +=
				MediaLibraryNewFolder_ExecuteRequested;
			FlyoutItemMediaLibraryNewFolder.Command =
				Commands.MediaLibraryNewFolderCommand.MediaLibraryNewFolder;
			FlyoutItemMediaLibraryFolderNewFolder.Command =
				Commands.MediaLibraryNewFolderCommand.MediaLibraryNewFolder;

			// MediaLibraryImportFilesCommand
			Commands.MediaLibraryImportFilesCommand.MediaLibraryImportFiles.CanExecuteRequested +=
				MediaLibraryImportFiles_CanExecuteRequested;
			Commands.MediaLibraryImportFilesCommand.MediaLibraryImportFiles.ExecuteRequested +=
				MediaLibraryImportFiles_ExecuteRequested;
			FlyoutItemMediaLibraryImportFiles.Command =
				Commands.MediaLibraryImportFilesCommand.MediaLibraryImportFiles;
			FlyoutItemMediaLibraryFolderImportFiles.Command =
				Commands.MediaLibraryImportFilesCommand.MediaLibraryImportFiles;

			// MediaLibraryImportFolderCommand
			Commands.MediaLibraryImportFolderCommand.MediaLibraryImportFolder.CanExecuteRequested +=
				MediaLibraryImportFolder_CanExecuteRequested;
			Commands.MediaLibraryImportFolderCommand.MediaLibraryImportFolder.ExecuteRequested +=
				MediaLibraryImportFolder_ExecuteRequested;
			FlyoutItemMediaLibraryImportFolder.Command =
				Commands.MediaLibraryImportFolderCommand.MediaLibraryImportFolder;
			FlyoutItemMediaLibraryFolderImportFolder.Command =
				Commands.MediaLibraryImportFolderCommand.MediaLibraryImportFolder;

			// MediaLibraryRemoveItemCommand
			Commands.MediaLibraryRemoveItemCommand.MediaLibraryRemoveItem.CanExecuteRequested +=
				MediaLibraryRemoveItem_CanExecuteRequested;
			Commands.MediaLibraryRemoveItemCommand.MediaLibraryRemoveItem.ExecuteRequested +=
				MediaLibraryRemoveItem_ExecuteRequested;
			FlyoutItemMediaLibraryFolderRemove.Command =
				Commands.MediaLibraryRemoveItemCommand.MediaLibraryRemoveItem;
			FlyoutItemMediaLibraryItemRemove.Command =
				Commands.MediaLibraryRemoveItemCommand.MediaLibraryRemoveItem;

			// MediaLibraryRemoveSelectedCommand
			Commands.MediaLibraryRemoveSelectedCommand.MediaLibraryRemoveSelected.CanExecuteRequested +=
				MediaLibraryRemoveSelected_CanExecuteRequested;
			Commands.MediaLibraryRemoveSelectedCommand.MediaLibraryRemoveSelected.ExecuteRequested +=
				MediaLibraryRemoveSelected_ExecuteRequested;
			FlyoutItemMediaLibraryFolderRemoveSelected.Command =
				Commands.MediaLibraryRemoveSelectedCommand.MediaLibraryRemoveSelected;
			FlyoutItemMediaLibraryItemRemoveSelected.Command =
				Commands.MediaLibraryRemoveSelectedCommand.MediaLibraryRemoveSelected;
			FlyoutItemMediaLibraryRemoveSelected.Command =
				Commands.MediaLibraryRemoveSelectedCommand.MediaLibraryRemoveSelected;

			// MediaLibraryRemoveAllCommand
			Commands.MediaLibraryRemoveAllCommand.MediaLibraryRemoveAll.CanExecuteRequested +=
				MediaLibraryRemoveAll_CanExecuteRequested;
			Commands.MediaLibraryRemoveAllCommand.MediaLibraryRemoveAll.ExecuteRequested +=
				MediaLibraryRemoveAll_ExecuteRequested;
			FlyoutItemMediaLibraryFolderRemoveAll.Command =
				Commands.MediaLibraryRemoveAllCommand.MediaLibraryRemoveAll;
			FlyoutItemMediaLibraryItemRemoveAll.Command =
				Commands.MediaLibraryRemoveAllCommand.MediaLibraryRemoveAll;
			FlyoutItemMediaLibraryRemoveAll.Command =
				Commands.MediaLibraryRemoveAllCommand.MediaLibraryRemoveAll;

			// MediaLibraryRenameItemCommand
			Commands.MediaLibraryRenameItemCommand.MediaLibraryRenameItem.CanExecuteRequested +=
				MediaLibraryRenameItem_CanExecuteRequested;
			Commands.MediaLibraryRenameItemCommand.MediaLibraryRenameItem.ExecuteRequested +=
				MediaLibraryRenameItem_ExecuteRequested;
			FlyoutItemMediaLibraryFolderRename.Command =
				Commands.MediaLibraryRenameItemCommand.MediaLibraryRenameItem;
			FlyoutItemMediaLibraryItemRename.Command =
				Commands.MediaLibraryRenameItemCommand.MediaLibraryRenameItem;
		}
		#endregion

		#region Event Handlers (CanExecute)
		private void MediaLibraryNewFolder_CanExecuteRequested(XamlUICommand sender,
															   CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = ActiveNode is MediaTreeFolder;
		}

		private void MediaLibraryImportFiles_CanExecuteRequested(XamlUICommand sender,
																 CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = ActiveNode is MediaTreeFolder;
		}

		private void MediaLibraryImportFolder_CanExecuteRequested(XamlUICommand sender,
																  CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = ActiveNode is MediaTreeFolder;
		}

		private void MediaLibraryRemoveItem_CanExecuteRequested(XamlUICommand sender,
																CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = ActiveNode?.Depth > 0;
		}

		private void MediaLibraryRemoveSelected_CanExecuteRequested(XamlUICommand sender,
																	CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = TreeViewMediaLibrary.SelectedNodes.Count > 0;
		}

		private void MediaLibraryRemoveAll_CanExecuteRequested(XamlUICommand sender,
															   CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = ActiveProject?.MediaLibrary.Children.Count > 0;
		}

		private void MediaLibraryRenameItem_CanExecuteRequested(XamlUICommand sender,
																CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = ActiveNode?.Depth > 0;
		}
		#endregion

		#region Event Handlers (Execute)
		private async void MediaLibraryNewFolder_ExecuteRequested(XamlUICommand sender,
																  ExecuteRequestedEventArgs args)
		{
			// Prompt user for the name of the new project
			var dlg = new TextPromptDialog
			{
				Title             = "New Folder",
				PromptText        = "Enter a name for the new folder",
				PrimaryButtonText = "OK",
				CloseButtonText   = "Cancel"
			};

			var dlgResult = await dlg.ShowAsync();
			if (dlgResult == ContentDialogResult.Primary)
			{
				ActiveNode.Children.Add(new MediaTreeFolder {Name = dlg.Text});
				ActiveProject.HasUnsavedChanges = true;
			}
		}

		private async void MediaLibraryImportFiles_ExecuteRequested(XamlUICommand sender,
																	ExecuteRequestedEventArgs args)
		{
			var picker = new FileOpenPicker
			{
				ViewMode               = PickerViewMode.Thumbnail,
				SuggestedStartLocation = PickerLocationId.Desktop,
				CommitButtonText       = "Import"
			};

			picker.FileTypeFilter.Add(".jpg");
			picker.FileTypeFilter.Add(".jpeg");
			picker.FileTypeFilter.Add(".png");
			picker.FileTypeFilter.Add(".bmp");
			picker.FileTypeFilter.Add(".avi");
			picker.FileTypeFilter.Add(".mov");
			picker.FileTypeFilter.Add(".wmv");
			picker.FileTypeFilter.Add(".mp4");
			picker.FileTypeFilter.Add(".mkv");

			var files = await picker.PickMultipleFilesAsync();
			if (files.Count == 0)
				return;

			foreach (var file in files)
			{
				if (file.ContentType.Contains("image"))
				{
					ActiveNode.Children.Add(new ImageFile(file));
					ActiveProject.HasUnsavedChanges = true;
				}
				else if (file.ContentType.Contains("video"))
				{
					ActiveNode.Children.Add(new VideoFile(file));
					ActiveProject.HasUnsavedChanges = true;
				}
			}
		}

		private async void MediaLibraryImportFolder_ExecuteRequested(XamlUICommand sender,
																	 ExecuteRequestedEventArgs args)
		{
			var picker = new FolderPicker
			{
				ViewMode               = PickerViewMode.Thumbnail,
				SuggestedStartLocation = PickerLocationId.Desktop,
				CommitButtonText       = "Import"
			};

			picker.FileTypeFilter.Add(".jpg");
			picker.FileTypeFilter.Add(".jpeg");
			picker.FileTypeFilter.Add(".png");
			picker.FileTypeFilter.Add(".bmp");
			picker.FileTypeFilter.Add(".avi");
			picker.FileTypeFilter.Add(".mov");
			picker.FileTypeFilter.Add(".wmv");
			picker.FileTypeFilter.Add(".mp4");
			picker.FileTypeFilter.Add(".mkv");

			var folder = await picker.PickSingleFolderAsync();
			if (folder == null)
				return;

			var files = await folder.GetFilesAsync();
			if (files.Count == 0)
				return;

			var mediaFolder = new MediaTreeFolder {Name = folder.DisplayName};
			foreach (var file in files)
			{
				if (file.ContentType.Contains("image"))
					mediaFolder.Children.Add(new ImageFile(file));
				else if (file.ContentType.Contains("video"))
					mediaFolder.Children.Add(new VideoFile(file));
			}

			ActiveNode.Children.Add(mediaFolder);
			ActiveProject.HasUnsavedChanges = true;
		}

		private void MediaLibraryRemoveItem_ExecuteRequested(XamlUICommand sender,
															 ExecuteRequestedEventArgs args)
		{
			if (ActiveNode.Parent.Children.Remove(ActiveNode))
				ActiveProject.HasUnsavedChanges = true;
		}

		private void MediaLibraryRemoveSelected_ExecuteRequested(XamlUICommand sender,
																 ExecuteRequestedEventArgs args)
		{
			if (TreeViewMediaLibrary.SelectedNodes.Count == 0)
				return;

			foreach (var node in TreeViewMediaLibrary.SelectedNodes)
			{
				ActiveProject.MediaLibrary.Remove(node.Content as MediaTreeNode);
			}

			ActiveProject.HasUnsavedChanges = true;
		}

		private void MediaLibraryRemoveAll_ExecuteRequested(XamlUICommand sender,
															ExecuteRequestedEventArgs args)
		{
			ActiveProject.MediaLibrary.Children.Clear();
			ActiveProject.HasUnsavedChanges = true;
		}

		private async void MediaLibraryRenameItem_ExecuteRequested(XamlUICommand sender,
																   ExecuteRequestedEventArgs args)
		{
			var dlg = new TextPromptDialog
			{
				Title             = "Rename Item",
				PromptText        = "Enter a different name for the item",
				Text              = ActiveNode.Name,
				PrimaryButtonText = "OK",
				CloseButtonText   = "Cancel"
			};

			var dlgResult = await dlg.ShowAsync();
			if (dlgResult == ContentDialogResult.Primary)
			{
				ActiveNode.Name                 = dlg.Text;
				ActiveProject.HasUnsavedChanges = true;
			}
		}
		#endregion
	}
}