using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using JLR.Utility.UWP.Dialogs;

namespace MediaBase
{
	public sealed partial class MainPage
	{
		#region Private Methods
		private void InitializeCommands()
		{
			// ProjectNewCommand
			Commands.ProjectNewCommand.ProjectNew.CanExecuteRequested +=
				ProjectNew_CanExecuteRequested;
			Commands.ProjectNewCommand.ProjectNew.ExecuteRequested +=
				ProjectNew_ExecuteRequested;
			FlyoutItemProjectNew.Command =
				Commands.ProjectNewCommand.ProjectNew;

			// ProjectOpenCommand
			Commands.ProjectOpenCommand.ProjectOpen.CanExecuteRequested +=
				ProjectOpen_CanExecuteRequested;
			Commands.ProjectOpenCommand.ProjectOpen.ExecuteRequested +=
				ProjectOpen_ExecuteRequested;
			FlyoutItemProjectOpen.Command =
				Commands.ProjectOpenCommand.ProjectOpen;

			// ProjectSaveCommand
			Commands.ProjectSaveCommand.ProjectSave.CanExecuteRequested +=
				ProjectSave_CanExecuteRequested;
			Commands.ProjectSaveCommand.ProjectSave.ExecuteRequested +=
				ProjectSave_ExecuteRequested;
			FlyoutItemProjectSave.Command =
				Commands.ProjectSaveCommand.ProjectSave;

			// ProjectSaveAsCommand
			Commands.ProjectSaveAsCommand.ProjectSaveAs.CanExecuteRequested +=
				ProjectSaveAs_CanExecuteRequested;
			Commands.ProjectSaveAsCommand.ProjectSaveAs.ExecuteRequested +=
				ProjectSaveAs_ExecuteRequested;
			FlyoutItemProjectSaveAs.Command =
				Commands.ProjectSaveAsCommand.ProjectSaveAs;
		}

		private async Task<bool> ProceedAfterPromptToSaveChanges()
		{
			if (ActiveProject == null || !ActiveProject.HasUnsavedChanges)
				return true;

			var dlg = new ContentDialog
			{
				Title               = "Unsaved Changes",
				Content             = $"Save changes to {ActiveProject.Name}?",
				PrimaryButtonText   = "Yes",
				SecondaryButtonText = "No",
				CloseButtonText     = "Cancel"
			};

			var dlgResult = await dlg.ShowAsync();

			switch (dlgResult)
			{
				case ContentDialogResult.None:
					return false;
				case ContentDialogResult.Primary:
					Commands.ProjectSaveCommand.ProjectSave.Command.Execute(null);
					break;
			}

			return true;
		}

		private async Task<StorageFile> PromptSaveLocation()
		{
			var picker = new FileSavePicker
			{
				SuggestedStartLocation = PickerLocationId.Desktop,
				CommitButtonText       = "Save",
				SuggestedFileName      = ActiveProject.Name
			};
			picker.FileTypeChoices.Add("MediaBase Project Files", new List<string> {".mbp"});
			return await picker.PickSaveFileAsync();
		}
		#endregion

		#region Event Handlers (CanExecute)
		private void ProjectNew_CanExecuteRequested(XamlUICommand sender,
													CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = true;
		}

		private void ProjectOpen_CanExecuteRequested(XamlUICommand sender,
													 CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = true;
		}

		private void ProjectSave_CanExecuteRequested(XamlUICommand sender,
													 CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = ActiveProject?.HasUnsavedChanges == true;
		}

		private void ProjectSaveAs_CanExecuteRequested(XamlUICommand sender,
													   CanExecuteRequestedEventArgs args)
		{
			args.CanExecute = ActiveProject != null &&
							  (ActiveProject.StorageFile != null || ActiveProject.HasUnsavedChanges);
		}
		#endregion

		#region Event Handlers (Execute)
		private async void ProjectNew_ExecuteRequested(XamlUICommand sender,
													   ExecuteRequestedEventArgs args)
		{
			// Prompt user to save unsaved changes (if applicable)
			if (await ProceedAfterPromptToSaveChanges() == false)
				return;

			var dlg = new TextPromptDialog
			{
				Title             = "New Project",
				PromptText        = "Enter a name for the new project",
				PrimaryButtonText = "OK",
				CloseButtonText   = "Cancel"
			};

			var dlgResult = await dlg.ShowAsync();
			if (dlgResult == ContentDialogResult.Primary)
				ActiveProject = new Project {Name = dlg.Text};
		}

		private async void ProjectOpen_ExecuteRequested(XamlUICommand sender,
														ExecuteRequestedEventArgs args)
		{
			// Prompt user to save unsaved changes (if applicable)
			if (await ProceedAfterPromptToSaveChanges() == false)
				return;

			ActiveProject = null;
			var picker = new FileOpenPicker
			{
				ViewMode               = PickerViewMode.Thumbnail,
				SuggestedStartLocation = PickerLocationId.Desktop,
				CommitButtonText       = "Open",
				FileTypeFilter         = {".mbp"}
			};

			var file = await picker.PickSingleFileAsync();
			if (file == null)
				return;

			ActiveProject = await Project.Load(file);

			var title = AppTitleText;
			var mediaFiles = ActiveProject.GetAllMediaFiles<MediaTreeFile>() as MediaTreeFile[] ??
							 ActiveProject.GetAllMediaFiles<MediaTreeFile>().ToArray();

			// Get each file from its storage location
			for (var i = 0; i < mediaFiles.Length; i++)
			{
				await mediaFiles[i].GetFileFromPathAsync();
				AppTitleText = $"{title} - Loading ({mediaFiles.Length - i} files remaining)";
			}

			AppTitleText = title;
		}

		private async void ProjectSave_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			if (ActiveProject.StorageFile == null)
				ActiveProject.StorageFile = await PromptSaveLocation();
			ActiveProject.Save();
		}

		private async void ProjectSaveAs_ExecuteRequested(XamlUICommand sender,
														  ExecuteRequestedEventArgs args)
		{
			ActiveProject.StorageFile = await PromptSaveLocation();
			ActiveProject.Save();
		}
		#endregion
	}
}