using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using JLR.Utility.WinUI;
using JLR.Utility.WinUI.Dialogs;
using JLR.Utility.WinUI.Messaging;
using JLR.Utility.WinUI.ViewModel;

using MediaBase.Controls;
using MediaBase.Dialogs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;

using WinRT.Interop;

namespace MediaBase.ViewModel
{
    [ViewModelType("Workspace")]
    public sealed class ProjectManager : ViewModelElement
    {
        #region Constants
        public static readonly string DefaultName = "Workspace";
        public static readonly string DefaultTitle = "MediaBASE";
        public static readonly string WorkspaceFileExtension = "mbw";
        public static readonly string ProjectFileExtension = "mbp";
        #endregion

        #region Fields
        private StorageFile _file;
        private GroupableTreeViewNode _activeSystemBrowserNode;
        private ViewModelElement _activeWorkspaceBrowserNode;
        private MultimediaSource _activeMediaSource;
        private Marker _selectedMarker;
        private string _description;
        private bool _hasUnsavedChanges;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets a reference to the file in which
        /// this <see cref="ProjectManager"/> is stored.
        /// </summary>
        public StorageFile File
        {
            get => _file;
            set => SetProperty(ref _file, value);
        }

        /// <summary>
        /// Gets or sets a reference to the currently active system browser node.
        /// </summary>
        public GroupableTreeViewNode ActiveSystemBrowserNode
        {
            get => _activeSystemBrowserNode;
            set
            {
                if (SetProperty(ref _activeSystemBrowserNode, value, true))
                {
                    GeneralPreviousCommand.NotifyCanExecuteChanged();
                    GeneralNextCommand.NotifyCanExecuteChanged();
                    ToolsToggleGroup1Command.NotifyCanExecuteChanged();
                    ToolsToggleGroup2Command.NotifyCanExecuteChanged();
                    ToolsToggleGroup3Command.NotifyCanExecuteChanged();
                    ToolsToggleGroup4Command.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a reference to the currently active workspace browser node.
        /// </summary>
        public ViewModelElement ActiveWorkspaceBrowserNode
        {
            get => _activeWorkspaceBrowserNode;
            set
            {
                if (SetProperty(ref _activeWorkspaceBrowserNode, value))
                {
                    GeneralPreviousCommand.NotifyCanExecuteChanged();
                    GeneralNextCommand.NotifyCanExecuteChanged();
                    ToolsToggleGroup1Command.NotifyCanExecuteChanged();
                    ToolsToggleGroup2Command.NotifyCanExecuteChanged();
                    ToolsToggleGroup3Command.NotifyCanExecuteChanged();
                    ToolsToggleGroup4Command.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a reference to the currently active multimedia source.
        /// </summary>
        public MultimediaSource ActiveMediaSource
        {
            get => _activeMediaSource;
            set
            {
                if (_activeMediaSource != null)
                {
                    _activeMediaSource.IsSelected = false;
                    if (_activeMediaSource == ActiveWorkspaceBrowserNode)
                        ActiveWorkspaceBrowserNode = null;
                }

                SetProperty(ref _activeMediaSource, value);

                if (_activeMediaSource != null)
                {
                    _activeMediaSource.IsSelected = true;
                    if (_activeMediaSource.Parent != null)
                        ActiveWorkspaceBrowserNode = _activeMediaSource;
                }

                GeneralPreviousCommand.NotifyCanExecuteChanged();
                GeneralNextCommand.NotifyCanExecuteChanged();
                ToolsToggleGroup1Command.NotifyCanExecuteChanged();
                ToolsToggleGroup2Command.NotifyCanExecuteChanged();
                ToolsToggleGroup3Command.NotifyCanExecuteChanged();
                ToolsToggleGroup4Command.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets a reference to the currently selected marker.
        /// </summary>
        public Marker SelectedMarker
        {
            get => _selectedMarker;
            set
            {
                if (_selectedMarker != null)
                    _selectedMarker.IsSelected = false;

                SetProperty(ref _selectedMarker, value, true);

                if (_selectedMarker != null)
                    _selectedMarker.IsSelected = true;

                GeneralDeleteMarkerCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets a user-friendly description of the workspace.
        /// </summary>
        /// <remarks>
        /// This property is useful as a window title.
        /// </remarks>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// Gets a value indicating whether or not there are changes to
        /// any project in the workspace that have not been saved.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value, true);
        }

        [ViewModelCollection(nameof(Projects), nameof(Project), false, false, true)]
        public ObservableCollection<Project> Projects { get; }

        public ObservableCollection<string> TagDatabase { get; }

        public Dictionary<Guid, IMultimediaItem> MediaItemDictionary { get; }

        public Dictionary<Guid, IList<Guid>> MediaItemDependencyDictionary { get; }
        #endregion

        #region Commands
        // General
        public XamlUICommand GeneralPreviousCommand { get; private set; }
        public XamlUICommand GeneralNextCommand { get; private set; }
        public XamlUICommand GeneralDeleteMarkerCommand { get; private set; }

        // Project
        public XamlUICommand ProjectNewCommand { get; private set; }
        public XamlUICommand ProjectOpenCommand { get; private set; }
        public XamlUICommand ProjectSaveCommand { get; private set; }
        public XamlUICommand ProjectSaveAsCommand { get; private set; }
        public XamlUICommand ProjectCloseCommand { get; private set; }

        // Workspace
        public XamlUICommand WorkspaceOpenCommand { get; private set; }
        public XamlUICommand WorkspaceSaveCommand { get; private set; }
        public XamlUICommand WorkspaceSaveAsCommand { get; private set; }
        public XamlUICommand WorkspaceCloseCommand { get; private set; }
        public XamlUICommand WorkspaceNewFolderCommand { get; private set; }
        public XamlUICommand WorkspaceImportCommand { get; private set; }
        public XamlUICommand WorkspaceRemoveItemCommand { get; private set; }
        public XamlUICommand WorkspaceRemoveSelectedCommand { get; private set; }
        public XamlUICommand WorkspaceRenameItemCommand { get; private set; }
        public XamlUICommand WorkspaceSelectMultipleCommand { get; private set; }

        // Tools
        public XamlUICommand ToolsToggleGroup1Command { get; private set; }
        public XamlUICommand ToolsToggleGroup2Command { get; private set; }
        public XamlUICommand ToolsToggleGroup3Command { get; private set; }
        public XamlUICommand ToolsToggleGroup4Command { get; private set; }
        public XamlUICommand ToolsBatchActionCommand { get; private set; }

        // Editor
        public XamlUICommand EditorPlayCommand { get; private set; }
        public XamlUICommand EditorPauseCommand { get; private set; }
        public XamlUICommand EditorToggleLoopingCommand { get; private set; }
        public XamlUICommand EditorPreviousFrameCommand { get; private set; }
        public XamlUICommand EditorNextFrameCommand { get; private set; }
        public XamlUICommand EditorPreviousMarkerCommand { get; private set; }
        public XamlUICommand EditorNextMarkerCommand { get; private set; }
        public XamlUICommand EditorToggleActiveSelectionCommand { get; private set; }
        public XamlUICommand EditorAddTrackCommmand { get; private set; }
        public XamlUICommand EditorNewMarkerCommand { get; private set; }
        public XamlUICommand EditorNewKeyframeCommand { get; private set; }
        public XamlUICommand EditorCutSelectedCommand { get; private set; }
        public XamlUICommand EditorPlaybackRateDecreaseCommand { get; private set; }
        public XamlUICommand EditorPlaybackRateIncreaseCommand { get; private set; }
        public XamlUICommand EditorPlaybackRateNormalCommand { get; private set; }
        public XamlUICommand EditorTogglePanAndZoomLockCommand { get; private set; }
        public XamlUICommand EditorCenterFrameCommand { get; private set; }
        public XamlUICommand EditorFrameZoomFitCommand { get; private set; }
        public XamlUICommand EditorFrameZoomFullCommand { get; private set; }
        public XamlUICommand EditorTimelineZoomOutCommand { get; private set; }
        public XamlUICommand EditorTimelineZoomInCommand { get; private set; }
        #endregion

        #region Constructor
        public ProjectManager()
        {
            Name = DefaultName;
            _description = DefaultTitle;
            _activeSystemBrowserNode = null;
            _activeWorkspaceBrowserNode = null;
            _activeMediaSource = null;
            _selectedMarker = null;
            _hasUnsavedChanges = false;

            Projects = new ObservableCollection<Project>();
            TagDatabase = new ObservableCollection<string>();
            MediaItemDictionary = new Dictionary<Guid, IMultimediaItem>();
            MediaItemDependencyDictionary = new Dictionary<Guid, IList<Guid>>();

            Projects.CollectionChanged += Projects_CollectionChanged;

            InitializeCommands();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a new project and adds it to the workspace
        /// </summary>
        /// <param name="name">The name of the new project.</param>
        public void CreateNewProject(string name)
        {
            var newProject = new Project
            {
                IsActive = false,
                Name = name
            };
            newProject.IsActive = true;

            Projects.Add(newProject);
        }

        public async Task OpenWorkspaceFromFile(StorageFile file)
        {
            if (file.GetFileExtension() != WorkspaceFileExtension)
                throw new ArgumentException("Incorrect file type", nameof(file));

            // Reset singleton workspace and load from workspace file
            IsActive = false;
            File = file;

            XmlReader reader = null;
            try
            {
                reader = await GetXmlReaderForFileAsync(file);
                ReadXml(reader);
            }
            catch (Exception)
            {
                App.ShowMessageBoxAsync("Unable to read workspace file.", "Workspace File Error");
                return;
            }
            finally
            {
                reader.Close();
            }
            IsActive = true;

            // Open each project in the workspace
            foreach (var project in Projects)
            {
                if (await OpenProject(project) == false)
                    App.ShowMessageBoxAsync($"Unable to read project file: {project.Path}", "Project File Error");
            }
        }

        public async Task OpenProjectFromFile(StorageFile file)
        {
            if (file.GetFileExtension() != ProjectFileExtension)
                throw new ArgumentException("Incorrect file type", nameof(file));

            var project = new Project
            {
                File = file,
                Path = file.Path
            };

            foreach (var existingProject in Projects)
            {
                if (project.File.Path == existingProject.Path)
                {
                    App.ShowMessageBoxAsync("This project already exists in this workspace.", "Duplicate Project");
                    return;
                }
            }

            if (await OpenProject(project) == false)
            {
                App.ShowMessageBoxAsync($"Unable to read project file: {project.Path}", "Project File Error");
                return;
            }

            Projects.Add(project);
        }

        /// <summary>
        /// Saves the XML representation of this <see cref="ProjectManager"/>
        /// to the <see cref="StorageFile"/> pointed to by <see cref="File"/>.
        /// </summary>
        public async Task SaveAsync()
        {
            if (!HasUnsavedChanges)
                return;

            // Save each project in the workspace
            foreach (var project in Projects)
            {
                if (project.File == null || !project.File.IsAvailable)
                {
                    if (await project.PromptSaveLocation() == false)
                        return;
                }

                await project.SaveAsync();
            }

            // Save the workspace itself
            Name = File.DisplayName;
            if (await SaveAsync(File))
            {
                HasUnsavedChanges = false;
            }
        }

        /// <summary>
        /// Alerts the user that unsaved changes exist,
        /// asking whether or not to save those changes.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if the user chose either <b>Yes</b> or <b>No</b>,
        /// <b><c>false</c></b> if the user chose <b>Cancel</b>.
        /// </returns>
        public async Task<bool> PromptSaveChanges()
        {
            if (!IsActive || !HasUnsavedChanges)
                return true;

            var dlg = new ContentDialog
            {
                Title = "Unsaved Changes",
                Content = $"Save changes to workspace {Name}?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                CloseButtonText = "Cancel",
                XamlRoot = App.Window.Content.XamlRoot
            };

            var choice = await dlg.ShowAsync();

            // Cancel
            if (choice == ContentDialogResult.None)
                return false;

            // Yes
            if (choice == ContentDialogResult.Primary)
            {
                if (File == null || !File.IsAvailable)
                {
                    if (await PromptSaveLocation() == false)
                        return false;
                }

                await SaveAsync();
            }

            return true;
        }

        /// <summary>
        /// Prompts the user to choose a location and a
        /// filename to which the workspace will be saved.
        /// The <see cref="File"/> property will be updated
        /// if a file is chosen.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if a file is chosen,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        public async Task<bool> PromptSaveLocation()
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                CommitButtonText = "Save",
                SuggestedFileName = DefaultName
            };

            picker.FileTypeChoices.Add("MediaBase Workspace Files", new List<string> { ".mbw" });
            InitializeWithWindow.Initialize(picker, App.WindowHandle);

            var file = await picker.PickSaveFileAsync();
            if (file != null && file.IsAvailable)
            {
                File = file;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calls <see cref="IMultimediaItem.MakeReady"/> on
        /// every <see cref="IMultimediaItem"/> in the specified
        /// <see cref="ViewModelNode"/>. This operation is not
        /// recursive, therefore only the node's immediate children
        /// will be affected.
        /// </summary>
        /// <param name="node">
        /// The <see cref="ViewModelNode"/> to process.
        /// </param>
        public static async Task MakeItemsReadyAsync(ViewModelNode node)
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            int count = 0, errors = 0;
            foreach (var item in node.Children.OfType<IMultimediaItem>())
            {
                if (item.IsReady)
                    continue;

                if (await item.MakeReady() == false)
                    errors++;

                count++;
            }

            // Warn user if errors occurred
            if (errors > 0)
            {
                messenger.Send(new SetInfoBarMessage
                {
                    Title = "Media Error",
                    Message = $"Unable to prepare {errors} out of {count} item{(errors != 1 ? "s" : "")}.",
                    Severity = InfoBarSeverity.Warning,
                    IsCloseable = true
                });
            }
        }

        public (int index, int total) GetActiveMediaSourceIndexAndParentTotal()
        {
            if (IsActiveMediaSourceFromSystemBrowser)
            {
                var fileList = ActiveSystemBrowserNode.Parent.Children.Where(x => x.Content is StorageFile).ToList();
                return (fileList.IndexOf(ActiveSystemBrowserNode) + 1, fileList.Count);
            }

            var mediaList = ActiveMediaSource.Parent.Children.OfType<MultimediaSource>().ToList();
            return (mediaList.IndexOf(ActiveMediaSource) + 1, mediaList.Count);
        }

        public static MultimediaSource CreateMediaSourceFromFile(StorageFile file)
        {
            if (file?.ContentType.Contains("image") == true)
            {
                var imageFile = new ImageFile(file) { Id = Guid.Empty };
                var imageSource = new ImageSource(imageFile);
                return imageSource;
            }
            else if (file?.ContentType.Contains("video") == true)
            {
                var videoFile = new VideoFile(file) { Id = Guid.Empty };
                var videoSource = new VideoSource(videoFile);
                return videoSource;
            }

            return null;
        }
        #endregion

        #region Event Handlers (General)
        private void Projects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Projects.Count == 0)
                Description = DefaultTitle;
            else if (Projects.Count == 1)
                Description = Projects[0].Name;
            else
                Description = $"{Name}: {Projects.Count} Projects";

            if (IsActive)
                HasUnsavedChanges = true;
        }
        #endregion

        #region Event Handlers (Commands - CanExecuteRequested)
        private void GeneralPreviousCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = false;

            if (ActiveMediaSource == null)
                return;

            if (ActiveMediaSource == ActiveWorkspaceBrowserNode)
                args.CanExecute = ActiveWorkspaceBrowserNode != ActiveWorkspaceBrowserNode.Parent?.Children.First();
            else if (IsActiveMediaSourceFromSystemBrowser)
            {
                var index = ActiveSystemBrowserNode.Parent?.Children.IndexOf(ActiveSystemBrowserNode);
                args.CanExecute = index != null &&
                                  index > 0 &&
                                  ActiveSystemBrowserNode.Parent.Children[(int)index - 1].Content is StorageFile;
            }
        }

        private void GeneralNextCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = false;
            if (ActiveMediaSource == null)
                return;

            if (ActiveMediaSource == ActiveWorkspaceBrowserNode)
                args.CanExecute = ActiveWorkspaceBrowserNode != ActiveWorkspaceBrowserNode.Parent?.Children.Last();
            else if (IsActiveMediaSourceFromSystemBrowser)
            {
                var index = ActiveSystemBrowserNode.Parent?.Children.IndexOf(ActiveSystemBrowserNode);
                args.CanExecute = index != null &&
                                  index < ActiveSystemBrowserNode.Parent.Children.Count - 1 &&
                                  ActiveSystemBrowserNode.Parent.Children[(int)index + 1].Content is StorageFile;
            }    
        }

        private void GeneralDeleteMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = SelectedMarker != null;
        }

        private void ProjectNewCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive;
        }

        private void ProjectOpenCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive;
        }

        private void ProjectSaveCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = (ActiveWorkspaceBrowserNode is Project project && project.HasUnsavedChanges) ||
                              (Projects.Count == 1 && Projects[0].HasUnsavedChanges);
        }

        private void ProjectSaveAsCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveWorkspaceBrowserNode is Project || Projects.Count == 1;
        }

        private void ProjectCloseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveWorkspaceBrowserNode is Project || Projects.Count == 1;
        }

        private void WorkspaceOpenCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = true;
        }

        private void WorkspaceSaveCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = HasUnsavedChanges;
        }

        private void WorkspaceSaveAsCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive && Projects.Count > 0;
        }

        private void WorkspaceCloseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive;
        }

        private void WorkspaceNewFolderCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveWorkspaceBrowserNode is MediaFolder;
        }

        private void WorkspaceImportCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            if (ActiveWorkspaceBrowserNode is MediaFolder)
            {
                var request = Messenger.Send<RequestMessage<bool>, string>("AreSystemBrowserNodesSelected");
                args.CanExecute = request.HasReceivedResponse && request.Response;
            }
            else
            {
                args.CanExecute = false;
            }
        }

        private void WorkspaceRemoveItemCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveWorkspaceBrowserNode is not null and not Project;
        }

        private void WorkspaceRemoveSelectedCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = Projects.Any(x => x.DepthFirstEnumerable().Any(y => y.IsSelected && y is not Project));
        }

        private void WorkspaceRenameItemCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveWorkspaceBrowserNode is not null;
        }

        private void ToolsToggleGroupCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveMediaSource != null;
        }

        private void ToolsBatchActionCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveSystemBrowserNode != null;
        }
        #endregion

        #region Event Handlers (Commands - ExecuteRequested)
        private void GeneralPreviousCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (ActiveMediaSource == ActiveWorkspaceBrowserNode)
            {
                var index = ActiveMediaSource.Parent.Children.IndexOf(ActiveMediaSource);
                ActiveMediaSource = (MultimediaSource)ActiveMediaSource.Parent.Children[index - 1];
            }
            else if (IsActiveMediaSourceFromSystemBrowser)
            {
                var index = ActiveSystemBrowserNode.Parent.Children.IndexOf(ActiveSystemBrowserNode);
                ActiveSystemBrowserNode = (GroupableTreeViewNode)ActiveSystemBrowserNode.Parent.Children[index - 1];
                var mediaSource = CreateMediaSourceFromFile(ActiveSystemBrowserNode.Content as StorageFile);
                mediaSource.GroupFlags = ActiveSystemBrowserNode.GroupFlags;
                ActiveMediaSource = mediaSource;
            }
        }

        private void GeneralNextCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (ActiveMediaSource == ActiveWorkspaceBrowserNode)
            {
                var index = ActiveMediaSource.Parent.Children.IndexOf(ActiveMediaSource);
                ActiveMediaSource = (MultimediaSource)ActiveMediaSource.Parent.Children[index + 1];
            }
            else if (IsActiveMediaSourceFromSystemBrowser)
            {
                var index = ActiveSystemBrowserNode.Parent.Children.IndexOf(ActiveSystemBrowserNode);
                ActiveSystemBrowserNode = (GroupableTreeViewNode)ActiveSystemBrowserNode.Parent.Children[index + 1];
                var mediaSource = CreateMediaSourceFromFile(ActiveSystemBrowserNode.Content as StorageFile);
                mediaSource.GroupFlags = ActiveSystemBrowserNode.GroupFlags;
                ActiveMediaSource = mediaSource;
            }
        }

        private void GeneralDeleteMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (ActiveMediaSource.Markers.Contains(SelectedMarker))
                ActiveMediaSource.Markers.Remove(SelectedMarker);
        }

        private async void ProjectNewCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var dlg = new TextPromptDialog
            {
                Title = "New Project",
                PromptText = "Enter a name for the new project",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = App.Window.Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                CreateNewProject(dlg.Text);
            }
        }

        private async void ProjectOpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            StorageFile projectFile;
            if (args.Parameter == null)
            {
                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    CommitButtonText = "Open Project",
                    FileTypeFilter = { ".mbp" }
                };

                InitializeWithWindow.Initialize(picker, App.WindowHandle);

                projectFile = await picker.PickSingleFileAsync();
                if (projectFile == null || !projectFile.IsAvailable)
                    return;
            }
            else
            {
                projectFile = args.Parameter as StorageFile;
            }

            await OpenProjectFromFile(projectFile);
        }

        private async void ProjectSaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            Project project = null;
            if (ActiveWorkspaceBrowserNode is Project)
                project = ActiveWorkspaceBrowserNode as Project;
            else if (Projects.Count == 1)
                project = Projects[0];

            if (project.File == null || !project.File.IsAvailable)
            {
                if (await project.PromptSaveLocation() == false)
                    return;
            }

            await project.SaveAsync();
        }

        private async void ProjectSaveAsCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            Project project = null;
            if (ActiveWorkspaceBrowserNode is Project)
                project = ActiveWorkspaceBrowserNode as Project;
            else if (Projects.Count == 1)
                project = Projects[0];

            if (await project.PromptSaveLocation())
                await project.SaveAsync();
        }

        private async void ProjectCloseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            Project project = null;
            if (ActiveWorkspaceBrowserNode is Project)
                project = ActiveWorkspaceBrowserNode as Project;
            else if (Projects.Count == 1)
                project = Projects[0];

            if (await project.PromptSaveChanges() == false)
                return;

            if (ActiveMediaSource != null && ActiveMediaSource.Root == project)
                ActiveMediaSource = null;

            Messenger.Send<GeneralMessage, string>("CollapseAllTreeViewNodes");

            project.IsActive = false;
            CloseProject(project);
            Projects.Remove(project);
            ActiveWorkspaceBrowserNode = null;
        }

        private async void WorkspaceOpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            StorageFile workspaceFile;
            if (args.Parameter == null)
            {
                // Prompt user to save unsaved changes
                if (await PromptSaveChanges() == false)
                    return;

                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    CommitButtonText = "Open Workspace",
                    FileTypeFilter = { ".mbw" }
                };

                InitializeWithWindow.Initialize(picker, App.WindowHandle);

                workspaceFile = await picker.PickSingleFileAsync();
                if (workspaceFile == null || !workspaceFile.IsAvailable)
                    return;
            }
            else
            {
                workspaceFile = args.Parameter as StorageFile;
            }

            await OpenWorkspaceFromFile(workspaceFile);
        }

        private async void WorkspaceSaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (File == null || !File.IsAvailable)
            {
                if (await PromptSaveLocation() == false)
                    return;
            }

            await SaveAsync();
        }

        private async void WorkspaceSaveAsCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (await PromptSaveLocation())
                await SaveAsync();
        }

        private async void WorkspaceCloseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            // Prompt user to save unsaved changes
            if (await PromptSaveChanges() == false)
                return;

            if (ActiveMediaSource != null && ActiveMediaSource.Root != null)
                ActiveMediaSource = null;

            Messenger.Send<GeneralMessage, string>("CollapseAllTreeViewNodes");

            IsActive = false;
            IsActive = true;
        }

        private async void WorkspaceNewFolderCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var dlg = new TextPromptDialog
            {
                Title = "New Folder",
                PromptText = "Enter a name for the new folder",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = App.Window.Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ((MediaFolder)ActiveWorkspaceBrowserNode).Children.Add(new MediaFolder(dlg.Text));
            }
        }

        private async void WorkspaceImportCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (((MediaFolder)ActiveWorkspaceBrowserNode).Root is not Project parentProject)
                throw new Exception("Unable to find target project");

            int fileCount = 0, folderCount = 0;
            var folderList = new List<StorageFolder>();
            var fileList = new List<StorageFile>();

            // Get list of selected nodes in the System Browser
            var request = Messenger.Send<CollectionRequestMessage<TreeViewNode>, string>("GetSelectedSystemBrowserNodes");
            foreach (var node in request.Responses)
            {
                if (HasSelectedAncestor(node))
                    continue;

                if (node.Content is StorageFolder folder)
                    folderList.Add(folder);
                else if (node.Content is StorageFile file)
                    fileList.Add(file);
            }

            // Clear the SystemBrowser TreeView's SelectedNodes list
            Messenger.Send<GeneralMessage, string>("ClearSystemBrowserSelection");

            // Recursively import top-level folders
            foreach (var folder in folderList)
            {
                await AddFolder(folder, (MediaFolder)ActiveWorkspaceBrowserNode);
            }

            // Import all top-level files
            foreach (var file in fileList)
            {
                AddFile(file, (MediaFolder)ActiveWorkspaceBrowserNode);
            }

            // Import complete - display results
            var message = new StringBuilder();
            message.Append($"Imported {folderCount} folder{(folderCount != 1 ? "s" : "")} and ");
            message.Append($"{fileCount} file{(fileCount != 1 ? "s" : "")}");
            Messenger.Send(new SetInfoBarMessage
            {
                Title = "Done",
                Message = message.ToString(),
                Severity = InfoBarSeverity.Success,
                IsCloseable = true
            });

            // Make imported items ready
            await MakeItemsReadyAsync((MediaFolder)ActiveWorkspaceBrowserNode);

            bool HasSelectedAncestor(TreeViewNode node)
            {
                while (node.Parent != null)
                {
                    if (request.Responses.Contains(node.Parent))
                        return true;

                    node = node.Parent;
                }

                return false;
            }

            void AddFile(StorageFile sourceFile, MediaFolder destinationFolder)
            {
                if (parentProject.MediaFileDictionary.ContainsKey(sourceFile.Path))
                {
                    var mediaFile = (MediaFile)MediaItemDictionary[parentProject.MediaFileDictionary[sourceFile.Path]];
                    if (mediaFile is null) throw new Exception(
                        $"Database lookup error: Unable to find MediaFile associated with the file at {sourceFile.Path}");

                    if (mediaFile.ContentType == MediaContentType.Image)
                    {
                        fileCount++;
                        var imageSource = new ImageSource(mediaFile);
                        MediaItemDictionary.Add(imageSource.Id, imageSource);
                        destinationFolder.Children.Add(imageSource);
                    }
                    else if (mediaFile.ContentType == MediaContentType.Video)
                    {
                        fileCount++;
                        var videoSource = new VideoSource(mediaFile);
                        MediaItemDictionary.Add(videoSource.Id, videoSource);
                        destinationFolder.Children.Add(videoSource);
                    }
                }
                else if (sourceFile.ContentType.ToLower().Contains("image"))
                {
                    fileCount++;
                    var imageFile = new ImageFile(sourceFile);
                    MediaItemDictionary.Add(imageFile.Id, imageFile);
                    parentProject.MediaFileDictionary.Add(sourceFile.Path, imageFile.Id);

                    var imageSource = new ImageSource(imageFile);
                    destinationFolder.Children.Add(imageSource);
                }
                else if (sourceFile.ContentType.ToLower().Contains("video"))
                {
                    fileCount++;
                    var videoFile = new VideoFile(sourceFile);
                    MediaItemDictionary.Add(videoFile.Id, videoFile);
                    parentProject.MediaFileDictionary.Add(sourceFile.Path, videoFile.Id);

                    var videoSource = new VideoSource(videoFile);
                    destinationFolder.Children.Add(videoSource);
                }
            }

            async Task AddFolder(StorageFolder sourceFolder, MediaFolder destinationFolder)
            {
                Messenger.Send(new SetInfoBarMessage
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
                    else if (item is StorageFolder subFolder)
                        await AddFolder(subFolder, newFolder);
                }
            }
        }

        private void WorkspaceRemoveItemCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            ActiveWorkspaceBrowserNode.Parent.Remove(ActiveWorkspaceBrowserNode);
            ActiveWorkspaceBrowserNode = null;
        }

        private void WorkspaceRemoveSelectedCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var nodesToRemove = Projects
                .SelectMany(x => x.DepthFirstEnumerable()
                .Where(y => y.IsSelected && (y.Parent == null || !y.Parent.IsSelected)))
                .ToList();

            foreach (var node in nodesToRemove)
            {
                node.Parent.Remove(node);
            }
        }

        private async void WorkspaceRenameItemCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var dlg = new TextPromptDialog
            {
                Title = "Rename Item",
                PromptText = "Enter a different name for the item",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                Text = ActiveWorkspaceBrowserNode.Name,
                XamlRoot = App.Window.Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ActiveWorkspaceBrowserNode.Name = dlg.Text;
            }
        }

        private void ToolsToggleGroupCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (!int.TryParse((string)args.Parameter, out int group))
                return;

            if (ActiveMediaSource == ActiveWorkspaceBrowserNode)
            {
                ActiveMediaSource.ToggleGroupFlag(group);
                if (App.TestKeyStates(VirtualKey.Control, CoreVirtualKeyStates.Down))
                {
                    for (var i = ActiveMediaSource.Parent.Children.IndexOf(ActiveMediaSource) - 1; i >= 0; i--)
                    {
                        if (((MultimediaSource)ActiveMediaSource.Parent.Children[i]).CheckGroupFlag(group) != ActiveMediaSource.CheckGroupFlag(group))
                            ((MultimediaSource)ActiveMediaSource.Parent.Children[i]).ToggleGroupFlag(group);
                        else
                            break;
                    }
                }
            }
            else if (IsActiveMediaSourceFromSystemBrowser)
            {
                var activeSystemBrowserNode = ActiveSystemBrowserNode as GroupableTreeViewNode;
                activeSystemBrowserNode.ToggleGroupFlag(group);
                ActiveMediaSource.ToggleGroupFlag(group);   // For visual indication in the editor
                var newFlagValue = activeSystemBrowserNode.CheckGroupFlag(group);

                if (App.TestKeyStates(VirtualKey.Control, CoreVirtualKeyStates.Down))
                {
                    for (var i = activeSystemBrowserNode.Parent.Children.IndexOf(activeSystemBrowserNode) - 1; i >= 0; i--)
                    {
                        if (((IGroupable)ActiveSystemBrowserNode.Parent.Children[i]).CheckGroupFlag(group) != newFlagValue)
                            ((IGroupable)ActiveSystemBrowserNode.Parent.Children[i]).ToggleGroupFlag(group);
                        else
                            break;
                    }
                }
            }
        }

        private async void ToolsBatchActionCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            ActiveMediaSource = null;

            var groupedNodes = Messenger.Send<CollectionRequestMessage<GroupableTreeViewNode>, string>("GetGroupedSystemBrowserNodes").Responses;
            var group1Files = groupedNodes.Where(x => x.CheckGroupFlag(1)).Select(x => x.Content).Cast<StorageFile>().ToList();
            var group2Files = groupedNodes.Where(x => x.CheckGroupFlag(2)).Select(x => x.Content).Cast<StorageFile>().ToList();
            var group3Files = groupedNodes.Where(x => x.CheckGroupFlag(3)).Select(x => x.Content).Cast<StorageFile>().ToList();
            var group4Files = groupedNodes.Where(x => x.CheckGroupFlag(4)).Select(x => x.Content).Cast<StorageFile>().ToList();

            var dlg = new GroupActionDialog
            {
                Title = "Perform Batch Action",
                PrimaryButtonText = "Execute",
                CloseButtonText = "Cancel",
                Group1Count = group1Files.Count,
                Group2Count = group2Files.Count,
                Group3Count = group3Files.Count,
                Group4Count = group4Files.Count,
                XamlRoot = App.Window.Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            var filesToProcess = new List<StorageFile>();

            if (dlg.ActOnGroup1 && dlg.Group1Count > 0)
                filesToProcess.AddRange(group1Files);
            if (dlg.ActOnGroup2 && dlg.Group2Count > 0)
                filesToProcess.AddRange(group2Files);
            if (dlg.ActOnGroup3 && dlg.Group3Count > 0)
                filesToProcess.AddRange(group3Files);
            if (dlg.ActOnGroup4 && dlg.Group4Count > 0)
                filesToProcess.AddRange(group4Files);

            Messenger.Send<GeneralMessage, string>("CollapseAllTreeViewNodes");

            // Process the batch action
            var currentItem = 1;
            switch (dlg.Action)
            {
                case BatchAction.Delete:
                    foreach (var file in filesToProcess)
                    {
                        var message1 = $"Moving {file.DisplayName} to the Recycle Bin (file {currentItem} of {filesToProcess.Count})";
                        Messenger.Send(new SetInfoBarMessage
                        {
                            Title = "Batch Operation Running",
                            Message = message1.ToString(),
                            Severity = InfoBarSeverity.Informational,
                            IsCloseable = false
                        });
                        await file.DeleteAsync();
                        currentItem++;
                    }
                    break;

                case BatchAction.Copy:
                    foreach (var file in filesToProcess)
                    {
                        var message2 = $"Copying {file.DisplayName} to {dlg.TargetFolder.Path} (file {currentItem} of {filesToProcess.Count})";
                        Messenger.Send(new SetInfoBarMessage
                        {
                            Title = "Batch Operation Running",
                            Message = message2.ToString(),
                            Severity = InfoBarSeverity.Informational,
                            IsCloseable = false
                        });
                        await file.CopyAsync(dlg.TargetFolder);
                        currentItem++;
                    }
                    break;

                case BatchAction.Move:
                    foreach (var file in filesToProcess)
                    {
                        var message3 = $"Moving {file.DisplayName} to {dlg.TargetFolder.Path} (file {currentItem} of {filesToProcess.Count})";
                        Messenger.Send(new SetInfoBarMessage
                        {
                            Title = "Batch Operation Running",
                            Message = message3.ToString(),
                            Severity = InfoBarSeverity.Informational,
                            IsCloseable = false
                        });
                        await file.MoveAsync(dlg.TargetFolder);
                        currentItem++;
                    }
                    break;

                default:
                    var messageFail = $"Something went wrong during batch processing";
                    Messenger.Send(new SetInfoBarMessage
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

            summaryMessage.Append(filesToProcess.Count);
            summaryMessage.Append(" file");
            if (filesToProcess.Count != 1)
                summaryMessage.Append('s');

            if (dlg.Action == BatchAction.Delete)
                summaryMessage.Append(" to the Recycle Bin");
            else if (dlg.Action is BatchAction.Copy or BatchAction.Move)
            {
                summaryMessage.Append(" to ");
                summaryMessage.Append(dlg.TargetFolder.Path);
            }

            Messenger.Send(new SetInfoBarMessage
            {
                Title = "Batch Operation Complete",
                Message = summaryMessage.ToString(),
                Severity = InfoBarSeverity.Success,
                IsCloseable = true
            });
        }
        #endregion

        #region Method Overrides (ViewModelElement)
        protected override object HijackDeserialization(string propertyName, ref XmlReader reader)
        {
            if (propertyName == nameof(Project))
            {
                reader.MoveToFirstAttribute();
                var name = reader.ReadContentAsString();
                reader.MoveToElement();
                reader.ReadStartElement();
                var path = reader.ReadElementContentAsString();
                reader.ReadEndElement();

                return new Project(name) { Path = path };
            }

            return null;
        }

        protected override void HijackSerialization(string propertyName, object value, ref XmlWriter writer)
        {
            if (propertyName == nameof(Project))
            {
                if (value is not Project project)
                    throw new Exception("Argument passed to custom serializer could not be cast to Project");

                if (project.File == null || !project.File.IsAvailable)
                    throw new Exception("Project does not have an associated save file");

                writer.WriteStartElement(propertyName);
                writer.WriteAttributeString(nameof(project.Name), project.Name);
                writer.WriteElementString(nameof(project.Path), project.Path);
                writer.WriteEndElement();
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            // Media lookup request
            Messenger.Register<MediaLookupRequestMessage>(this, (r, m) =>
            {
                if (((ProjectManager)r).MediaItemDictionary.ContainsKey(m.Id))
                    m.Reply(((ProjectManager)r).MediaItemDictionary[m.Id]);
                else
                    m.Reply(null);
            });

            // Media item added/removed
            Messenger.Register<CollectionChangedMessage<ViewModelElement>, string>(this, nameof(ViewModelNode.Children), (r, m) =>
            {
                if (m.Sender is not MediaFolder folder)
                    return;

                if (m.Action is NotifyCollectionChangedAction.Remove or
                                NotifyCollectionChangedAction.Replace)
                {
                    foreach (var item in m.OldValue.OfType<IMultimediaItem>())
                    {
                        if (item is MultimediaSource source)
                        {
                            if (((ProjectManager)r).MediaItemDependencyDictionary.ContainsKey(source.SourceId))
                            {
                                ((ProjectManager)r).MediaItemDependencyDictionary[source.SourceId].Remove(item.Id);

                                if (((ProjectManager)r).MediaItemDependencyDictionary[source.SourceId].Count == 0)
                                    ((ProjectManager)r).MediaItemDependencyDictionary.Remove(source.SourceId);
                            }
                        }

                        if (((ProjectManager)r).MediaItemDictionary.ContainsKey(item.Id))
                            ((ProjectManager)r).MediaItemDictionary.Remove(item.Id);
                    }
                }

                if (m.Action is NotifyCollectionChangedAction.Add or
                                NotifyCollectionChangedAction.Replace)
                {
                    foreach (var item in m.NewValue.OfType<IMultimediaItem>())
                    {
                        if (!((ProjectManager)r).MediaItemDictionary.ContainsKey(item.Id))
                            ((ProjectManager)r).MediaItemDictionary.Add(item.Id, item);

                        if (item is MultimediaSource source)
                        {
                            if (((ProjectManager)r).MediaItemDependencyDictionary.ContainsKey(source.SourceId))
                                ((ProjectManager)r).MediaItemDependencyDictionary[source.SourceId].Add(item.Id);
                            else
                                ((ProjectManager)r).MediaItemDependencyDictionary.Add(source.SourceId, new List<Guid> { item.Id });
                        }
                    }
                }
            });

            // Tag added to media item
            Messenger.Register<CollectionChangedMessage<string>, string>(this, nameof(IMediaMetadata.Tags), (r, m) =>
            {
                foreach (var tag in m.NewValue)
                {
                    if (!((ProjectManager)r).TagDatabase.Contains(tag))
                        ((ProjectManager)r).TagDatabase.Add(tag);
                }
            });

            // Project unsaved changes
            Messenger.Register<PropertyChangedMessage<bool>>(this, (r, m) =>
            {
                if (m.Sender is Project && m.PropertyName == nameof(Project.HasUnsavedChanges))
                {
                    ((ProjectManager)r).HasUnsavedChanges = ((ProjectManager)r).Projects.Any(x => x.HasUnsavedChanges);
                }
            });
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            Projects.Clear();
            TagDatabase.Clear();
            MediaItemDictionary.Clear();
            MediaItemDependencyDictionary.Clear();
            Name = DefaultName;
            HasUnsavedChanges = false;
        }
        #endregion

        #region Private Properties
        private bool IsActiveMediaSourceFromSystemBrowser =>
            ActiveMediaSource.Parent == null &&
            ActiveMediaSource.Source is MediaFile file &&
            ActiveSystemBrowserNode.Content is StorageFile browserFile &&
            file.File == browserFile;
        #endregion

        #region Private Methods
        private async Task<bool> OpenProject(Project project)
        {
            if (project.File == null || !project.File.IsAvailable)
            {
                if (string.IsNullOrEmpty(project.Path))
                    return false;

                project.File = await StorageFile.GetFileFromPathAsync(project.Path);
            }

            if (project.File == null || !project.File.IsAvailable)
                return false;

            XmlReader reader = null;
            try
            {
                reader = await GetXmlReaderForFileAsync(project.File);
                project.ReadXml(reader);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                reader.Close();
            }
            project.IsActive = true;

            // Add file references to MediaItemDatabase
            foreach (var path in project.MediaFileDictionary.Keys)
            {
                StorageFile mediaFile;
                try
                {
                    mediaFile = await StorageFile.GetFileFromPathAsync(path);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }

                if (mediaFile.ContentType.ToLower().Contains("image"))
                {
                    var imageFile = new ImageFile(mediaFile) { Id = project.MediaFileDictionary[path] };
                    MediaItemDictionary.Add(imageFile.Id, imageFile);
                }
                else if (mediaFile.ContentType.ToLower().Contains("video"))
                {
                    var videoFile = new VideoFile(mediaFile) { Id = project.MediaFileDictionary[path] };
                    MediaItemDictionary.Add(videoFile.Id, videoFile);
                }
            }

            return true;
        }

        private void CloseProject(Project project)
        {
            foreach (var item in project.DepthFirstEnumerable().OfType<IMultimediaItem>())
            {
                if (item is MultimediaSource source)
                {
                    if (MediaItemDependencyDictionary.ContainsKey(source.SourceId))
                    {
                        MediaItemDependencyDictionary[source.SourceId].Remove(item.Id);

                        if (MediaItemDependencyDictionary[source.SourceId].Count == 0)
                            MediaItemDependencyDictionary.Remove(source.SourceId);
                    }
                }

                if (MediaItemDictionary.ContainsKey(item.Id))
                    MediaItemDictionary.Remove(item.Id);
            }
        }

        private void InitializeCommands()
        {
            // General: Previous
            GeneralPreviousCommand = new XamlUICommand
            {
                Label = "Previous",
                Description = "Previous",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xF0B0 }
            };

            GeneralPreviousCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Left,
                IsEnabled = true
            });

            // General: Next
            GeneralNextCommand = new XamlUICommand
            {
                Label = "Next",
                Description = "Next",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xF0AF }
            };

            GeneralNextCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Right,
                IsEnabled = true
            });

            // General: Delete Marker
            GeneralDeleteMarkerCommand = new XamlUICommand
            {
                Label = "Delete Marker",
                Description = "Delete currently selected marker",
                IconSource = new SymbolIconSource { Symbol = Symbol.Delete }
            };

            GeneralDeleteMarkerCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Delete,
                IsEnabled = true
            });

            // Project: New
            ProjectNewCommand = new XamlUICommand
            {
                Label = "New Project...",
                Description = "Begin a new project",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE81E }
            };

            // Project: Open
            ProjectOpenCommand = new XamlUICommand
            {
                Label = "Open Project...",
                Description = "Open an existing project",
                IconSource = new SymbolIconSource { Symbol = Symbol.OpenLocal }
            };

            ProjectOpenCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.O,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Project: Save
            ProjectSaveCommand = new XamlUICommand
            {
                Label = "Save Project",
                Description = "Save the current project",
                IconSource = new SymbolIconSource { Symbol = Symbol.SaveLocal }
            };

            // Project: Save As
            ProjectSaveAsCommand = new XamlUICommand
            {
                Label = "Save Project As...",
                Description = "Save the current project to a different file",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE792 }
            };

            // Project: Close
            ProjectCloseCommand = new XamlUICommand
            {
                Label = "Close Project",
                Description = "Close the current project",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE8BB }
            };

            // Workspace: Open
            WorkspaceOpenCommand = new XamlUICommand
            {
                Label = "Open Workspace...",
                Description = "Open an existing workspace",
                IconSource = new SymbolIconSource { Symbol = Symbol.Library }
            };

            // Workspace: Save
            WorkspaceSaveCommand = new XamlUICommand
            {
                Label = "Save Workspace",
                Description = "Save the current workspace",
                IconSource = new SymbolIconSource { Symbol = Symbol.SaveLocal }
            };

            WorkspaceSaveCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.S,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Workspace: Save As
            WorkspaceSaveAsCommand = new XamlUICommand
            {
                Label = "Save Workspace As...",
                Description = "Save the current workspace to a different file",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE792 }
            };

            WorkspaceSaveAsCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.S,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                IsEnabled = true
            });

            // Workspace: Close
            WorkspaceCloseCommand = new XamlUICommand
            {
                Label = "Close Workspace",
                Description = "Close the current workspace",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE8BB }
            };

            // Workspace: New Folder
            WorkspaceNewFolderCommand = new XamlUICommand
            {
                Label = "New Folder...",
                Description = "Create a new folder at this location",
                IconSource = new SymbolIconSource { Symbol = Symbol.NewFolder }
            };

            WorkspaceNewFolderCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.N,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Workspace: Import Item(s)
            WorkspaceImportCommand = new XamlUICommand
            {
                Label = "Import Selected",
                Description = "Import items selected in the System Browser",
                IconSource = new SymbolIconSource { Symbol = Symbol.Import }
            };

            // Workspace: Remove Item
            WorkspaceRemoveItemCommand = new XamlUICommand
            {
                Label = "Remove Item",
                Description = "Remove this item"
            };

            WorkspaceRemoveItemCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Delete,
                IsEnabled = true
            });

            // Workspace: Remove Selected
            WorkspaceRemoveSelectedCommand = new XamlUICommand
            {
                Label = "Remove Selected",
                Description = "Remove selected (checked) items"
            };

            // Workspace: Rename Item
            WorkspaceRenameItemCommand = new XamlUICommand
            {
                Label = "Rename...",
                Description = "Rename this item",
                IconSource = new SymbolIconSource { Symbol = Symbol.Rename }
            };

            // Workspace: Toggle Multiple Selection
            WorkspaceSelectMultipleCommand = new XamlUICommand
            {
                Label = "Toggle Multi-Select",
                Description = "Toggle multiple selection mode",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE762 }
            };

            // Tools: Batch Action
            ToolsBatchActionCommand = new XamlUICommand
            {
                Label = "Batch Action...",
                Description = "Perform an action on grouped items",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE781 }
            };

            ToolsBatchActionCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.A,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                IsEnabled = true
            });

            // Tools: Toggle Group 1
            ToolsToggleGroup1Command = new XamlUICommand
            {
                Label = "Group 1",
                Description = "Toggle item mark for Group 1",
                IconSource = new FontIconSource
                {
                    Glyph = "❶",
                    Foreground = new SolidColorBrush(Colors.Gold),
                    FontFamily = new FontFamily("Segoe UI")
                }
            };

            ToolsToggleGroup1Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.NumberPad1,
                IsEnabled = true
            });

            ToolsToggleGroup1Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.NumberPad1,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Tools: Toggle Group 2
            ToolsToggleGroup2Command = new XamlUICommand
            {
                Label = "Group 2",
                Description = "Toggle item mark for Group 2",
                IconSource = new FontIconSource
                {
                    Glyph = "❷",
                    Foreground = new SolidColorBrush(Colors.CornflowerBlue),
                    FontFamily = new FontFamily("Segoe UI")
                }
            };

            ToolsToggleGroup2Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.NumberPad2,
                IsEnabled = true
            });

            ToolsToggleGroup2Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.NumberPad2,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Tools: Toggle Group 3
            ToolsToggleGroup3Command = new XamlUICommand
            {
                Label = "Group 3",
                Description = "Toggle item mark for Group 3",
                IconSource = new FontIconSource
                {
                    Glyph = "❸",
                    Foreground = new SolidColorBrush(Colors.IndianRed),
                    FontFamily = new FontFamily("Segoe UI")
                }
            };

            ToolsToggleGroup3Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.NumberPad3,
                IsEnabled = true
            });

            ToolsToggleGroup3Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.NumberPad3,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Tools: Toggle Group 4
            ToolsToggleGroup4Command = new XamlUICommand
            {
                Label = "Group 4",
                Description = "Toggle item mark for Group 4",
                IconSource = new FontIconSource
                {
                    Glyph = "❹",
                    Foreground = new SolidColorBrush(Colors.ForestGreen),
                    FontFamily = new FontFamily("Segoe UI")
                }
            };

            ToolsToggleGroup4Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.NumberPad4,
                IsEnabled = true
            });

            ToolsToggleGroup4Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.NumberPad4,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Editor: Play
            EditorPlayCommand = new XamlUICommand
            {
                Label = "Play",
                Description = "Begin playback",
                IconSource = new SymbolIconSource { Symbol = Symbol.Play }
            };

            EditorPlayCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Space,
                IsEnabled = true
            });

            // Editor: Pause
            EditorPauseCommand = new XamlUICommand
            {
                Label = "Pause",
                Description = "Pause playback",
                IconSource = new SymbolIconSource { Symbol = Symbol.Pause }
            };

            EditorPauseCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Space,
                IsEnabled = true
            });

            // Editor: Toggle Looping
            EditorToggleLoopingCommand = new XamlUICommand
            {
                Label = "Loop Playback",
                Description = "Toggle looping of current media",
                IconSource = new SymbolIconSource { Symbol = Symbol.RepeatAll }
            };

            // Editor: Previous Frame
            EditorPreviousFrameCommand = new XamlUICommand
            {
                Label = "Previous Frame",
                Description = "Seek back one frame",
                IconSource = new SymbolIconSource { Symbol = Symbol.Previous }
            };

            EditorPreviousFrameCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Left,
                IsEnabled = true
            });

            // Editor: Next Frame
            EditorNextFrameCommand = new XamlUICommand
            {
                Label = "Next Frame",
                Description = "Seek forward one frame",
                IconSource = new SymbolIconSource { Symbol = Symbol.Next }
            };

            EditorNextFrameCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Right,
                IsEnabled = true
            });

            // Editor: Previous Marker
            EditorPreviousMarkerCommand = new XamlUICommand
            {
                Label = "Previous Marker",
                Description = "Seek to the previous marker",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE76B }
            };

            EditorPreviousMarkerCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Left,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Editor: Next Marker
            EditorNextMarkerCommand = new XamlUICommand
            {
                Label = "Next Marker",
                Description = "Seek to the next marker",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE76C }
            };

            EditorNextMarkerCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Right,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Editor: Toggle Active Selection
            EditorToggleActiveSelectionCommand = new XamlUICommand
            {
                Label = "Toggle Active Selection",
                Description = "Enable/disable timeline selection controls",
                IconSource = new SymbolIconSource { Symbol = Symbol.Highlight }
            };

            EditorToggleActiveSelectionCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.S,
                IsEnabled = true
            });

            // Editor: New Track
            EditorAddTrackCommmand = new XamlUICommand
            {
                Label = "Add Track",
                Description = "Add track to current media",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE92F }
            };

            // Editor: New Marker
            EditorNewMarkerCommand = new XamlUICommand
            {
                Label = "New Marker",
                Description = "Add new marker at current position",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE81B }
            };

            EditorNewMarkerCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.M,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Editor: New Keyframe
            EditorNewKeyframeCommand = new XamlUICommand
            {
                Label = "New Keyframe",
                Description = "Add new keyframe",
                IconSource = new SymbolIconSource { Symbol = Symbol.Permissions }
            };

            EditorNewKeyframeCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.K,
                IsEnabled = true
            });

            // Editor: Cut Selected
            EditorCutSelectedCommand = new XamlUICommand
            {
                Label = "Cut Selected",
                Description = "Add selection to cut list",
                IconSource = new SymbolIconSource { Symbol = Symbol.Trim }
            };

            // Editor: Playback Rate -
            EditorPlaybackRateDecreaseCommand = new XamlUICommand
            {
                Label = "Playback Rate -",
                Description = "Decrease playback rate",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xEC48 }
            };

            EditorPlaybackRateDecreaseCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Down,
                IsEnabled = true
            });

            // Editor: Playback Rate +
            EditorPlaybackRateIncreaseCommand = new XamlUICommand
            {
                Label = "Playback Rate +",
                Description = "Increase playback rate",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xEC4A }
            };

            EditorPlaybackRateIncreaseCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Up,
                IsEnabled = true
            });

            // Editor: Normal Playback Rate
            EditorPlaybackRateNormalCommand = new XamlUICommand
            {
                Label = "Normal Playback Rate",
                Description = "Normal playback rate",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xEC49 }
            };

            EditorPlaybackRateNormalCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Home,
                IsEnabled = true
            });

            // Editor: Lock Pan and Zoom
            EditorTogglePanAndZoomLockCommand = new XamlUICommand
            {
                Label = "Lock Pan and Zoom",
                Description = "Toggle pan and zoom lock",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE72E }
            };

            EditorTogglePanAndZoomLockCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.L,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Editor: Center Frame
            EditorCenterFrameCommand = new XamlUICommand
            {
                Label = "Center Frame",
                Description = "Center frame in window",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE799 }
            };

            EditorCenterFrameCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.C,
                IsEnabled = true
            });

            // Editor: Zoom Fit
            EditorFrameZoomFitCommand = new XamlUICommand
            {
                Label = "Zoom Fit",
                Description = "Zoom to fit current view",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE9A6 }
            };

            EditorFrameZoomFitCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Number0,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Editor: Zoom Full
            EditorFrameZoomFullCommand = new XamlUICommand
            {
                Label = "Zoom Full",
                Description = "Zoom to actual size",
                IconSource = new SymbolIconSource { Symbol = Symbol.FullScreen }
            };

            EditorFrameZoomFullCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Number1,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            // Editor: Zoom Out Timeline
            EditorTimelineZoomOutCommand = new XamlUICommand
            {
                Label = "Zoom Out Timeline",
                Description = "Increase the visible timeline range",
                IconSource = new SymbolIconSource { Symbol = Symbol.ZoomOut }
            };

            EditorTimelineZoomOutCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.PageDown,
                IsEnabled = true
            });

            // Editor: Zoom In Timeline
            EditorTimelineZoomInCommand = new XamlUICommand
            {
                Label = "Zoom In Timeline",
                Description = "Decrease the visible timeline range",
                IconSource = new SymbolIconSource { Symbol = Symbol.ZoomIn }
            };

            EditorTimelineZoomInCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.PageUp,
                IsEnabled = true
            });

            // General
            GeneralPreviousCommand.CanExecuteRequested +=
                GeneralPreviousCommand_CanExecuteRequested;
            GeneralPreviousCommand.ExecuteRequested +=
                GeneralPreviousCommand_ExecuteRequested;

            GeneralNextCommand.CanExecuteRequested +=
                GeneralNextCommand_CanExecuteRequested;
            GeneralNextCommand.ExecuteRequested +=
                GeneralNextCommand_ExecuteRequested;

            GeneralDeleteMarkerCommand.CanExecuteRequested +=
                GeneralDeleteMarkerCommand_CanExecuteRequested;
            GeneralDeleteMarkerCommand.ExecuteRequested +=
                GeneralDeleteMarkerCommand_ExecuteRequested;

            // Project
            ProjectNewCommand.CanExecuteRequested +=
                ProjectNewCommand_CanExecuteRequested;
            ProjectNewCommand.ExecuteRequested +=
                ProjectNewCommand_ExecuteRequested;

            ProjectOpenCommand.CanExecuteRequested +=
                ProjectOpenCommand_CanExecuteRequested;
            ProjectOpenCommand.ExecuteRequested +=
                ProjectOpenCommand_ExecuteRequested;

            ProjectSaveCommand.CanExecuteRequested +=
                ProjectSaveCommand_CanExecuteRequested;
            ProjectSaveCommand.ExecuteRequested +=
                ProjectSaveCommand_ExecuteRequested;

            ProjectSaveAsCommand.CanExecuteRequested +=
                ProjectSaveAsCommand_CanExecuteRequested;
            ProjectSaveAsCommand.ExecuteRequested +=
                ProjectSaveAsCommand_ExecuteRequested;

            ProjectCloseCommand.CanExecuteRequested +=
                ProjectCloseCommand_CanExecuteRequested;
            ProjectCloseCommand.ExecuteRequested +=
                ProjectCloseCommand_ExecuteRequested;

            // Workspace
            WorkspaceOpenCommand.CanExecuteRequested +=
                WorkspaceOpenCommand_CanExecuteRequested;
            WorkspaceOpenCommand.ExecuteRequested +=
                WorkspaceOpenCommand_ExecuteRequested;

            WorkspaceSaveCommand.CanExecuteRequested +=
                WorkspaceSaveCommand_CanExecuteRequested;
            WorkspaceSaveCommand.ExecuteRequested +=
                WorkspaceSaveCommand_ExecuteRequested;

            WorkspaceSaveAsCommand.CanExecuteRequested +=
                WorkspaceSaveAsCommand_CanExecuteRequested;
            WorkspaceSaveAsCommand.ExecuteRequested +=
                WorkspaceSaveAsCommand_ExecuteRequested;

            WorkspaceCloseCommand.CanExecuteRequested +=
                WorkspaceCloseCommand_CanExecuteRequested;
            WorkspaceCloseCommand.ExecuteRequested +=
                WorkspaceCloseCommand_ExecuteRequested;

            WorkspaceNewFolderCommand.CanExecuteRequested +=
                WorkspaceNewFolderCommand_CanExecuteRequested;
            WorkspaceNewFolderCommand.ExecuteRequested +=
                WorkspaceNewFolderCommand_ExecuteRequested;

            WorkspaceImportCommand.CanExecuteRequested +=
                WorkspaceImportCommand_CanExecuteRequested;
            WorkspaceImportCommand.ExecuteRequested +=
                WorkspaceImportCommand_ExecuteRequested;

            WorkspaceRemoveItemCommand.CanExecuteRequested +=
                WorkspaceRemoveItemCommand_CanExecuteRequested;
            WorkspaceRemoveItemCommand.ExecuteRequested +=
                WorkspaceRemoveItemCommand_ExecuteRequested;

            WorkspaceRemoveSelectedCommand.CanExecuteRequested +=
                WorkspaceRemoveSelectedCommand_CanExecuteRequested;
            WorkspaceRemoveSelectedCommand.ExecuteRequested +=
                WorkspaceRemoveSelectedCommand_ExecuteRequested;

            WorkspaceRenameItemCommand.CanExecuteRequested +=
                WorkspaceRenameItemCommand_CanExecuteRequested;
            WorkspaceRenameItemCommand.ExecuteRequested +=
                WorkspaceRenameItemCommand_ExecuteRequested;

            // Tools
            ToolsToggleGroup1Command.CanExecuteRequested +=
                ToolsToggleGroupCommand_CanExecuteRequested;
            ToolsToggleGroup1Command.ExecuteRequested +=
                ToolsToggleGroupCommand_ExecuteRequested;

            ToolsToggleGroup2Command.CanExecuteRequested +=
                ToolsToggleGroupCommand_CanExecuteRequested;
            ToolsToggleGroup2Command.ExecuteRequested +=
                ToolsToggleGroupCommand_ExecuteRequested;

            ToolsToggleGroup3Command.CanExecuteRequested +=
                ToolsToggleGroupCommand_CanExecuteRequested;
            ToolsToggleGroup3Command.ExecuteRequested +=
                ToolsToggleGroupCommand_ExecuteRequested;

            ToolsToggleGroup4Command.CanExecuteRequested +=
                ToolsToggleGroupCommand_CanExecuteRequested;
            ToolsToggleGroup4Command.ExecuteRequested +=
                ToolsToggleGroupCommand_ExecuteRequested;

            ToolsBatchActionCommand.CanExecuteRequested +=
                ToolsBatchActionCommand_CanExecuteRequested;
            ToolsBatchActionCommand.ExecuteRequested +=
                ToolsBatchActionCommand_ExecuteRequested;
        }
        #endregion
    }
}