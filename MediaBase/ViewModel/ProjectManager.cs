using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.Messaging;
using JLR.Utility.WinUI.ViewModel;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.Storage;
using Windows.System;
using JLR.Utility.WinUI.Dialogs;
using CommunityToolkit.Mvvm.Messaging.Messages;
using JLR.Utility.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Diagnostics;
using System.IO;

namespace MediaBase.ViewModel
{
    [ViewModelType("Workspace")]
    public sealed class ProjectManager : ViewModelElement
    {
        #region Constants
        public static readonly string DefaultName = "Workspace";
        public static readonly string DefaultTitle = "MediaBASE";
        public static readonly string[] MediaBaseFileExtensions = new[]
        {
            ".mbw", ".mbp"
        };
        #endregion

        #region Fields
        private StorageFile _file;
        private ViewModelNode _activeNode;
        private MultimediaSource _activeMediaSource;
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
        /// Gets or sets a reference to the currently active workspace item.
        /// </summary>
        public ViewModelNode ActiveNode
        {
            get => _activeNode;
            set
            {
                if (_activeNode != null)
                    _activeNode.IsSelected = false;

                SetProperty(ref _activeNode, value);

                if (_activeNode != null)
                    _activeNode.IsSelected = true;
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
                    _activeMediaSource.IsSelected = false;

                SetProperty(ref _activeMediaSource, value);

                if (_activeMediaSource != null)
                    _activeMediaSource.IsSelected = true;
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
        public XamlUICommand ProjectNewCommand { get; private set; }
        public XamlUICommand ProjectOpenCommand { get; private set; }
        public XamlUICommand ProjectSaveCommand { get; private set; }
        public XamlUICommand ProjectSaveAsCommand { get; private set; }
        public XamlUICommand ProjectCloseCommand { get; private set; }
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
        #endregion

        #region Constructor
        public ProjectManager()
        {
            Name = DefaultName;
            _description = DefaultTitle;
            _activeNode = null;
            _activeMediaSource = null;
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
        }
        #endregion

        #region Event Handlers (Commands - CanExecuteRequested)
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
            args.CanExecute = (ActiveNode is Project project && project.HasUnsavedChanges) ||
                              (Projects.Count == 1 && Projects[0].HasUnsavedChanges);
        }

        private void ProjectSaveAsCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveNode is Project || Projects.Count == 1;
        }

        private void ProjectCloseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveNode is Project || Projects.Count == 1;
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
            args.CanExecute = ActiveNode is MediaFolder;
        }

        private void WorkspaceImportCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            if (ActiveNode is MediaFolder)
            {
                var request = Messenger.Send<RequestMessage<bool>>();
                args.CanExecute = request.HasReceivedResponse && request.Response;
            }
            else
            {
                args.CanExecute = false;
            }
        }

        private void WorkspaceRemoveItemCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveNode is not null;
        }

        private void WorkspaceRemoveSelectedCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = Projects.Any(x => x.DepthFirstEnumerable().Any(y => y.IsSelected));
        }

        private void WorkspaceRenameItemCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveNode is not null;
        }
        #endregion

        #region Event Handlers (Commands - ExecuteRequested)
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
                var newProject = new Project
                {
                    IsActive = false,
                    Name = dlg.Text
                };
                newProject.IsActive = true;

                Projects.Add(newProject);
            }
        }

        private void ProjectOpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private async void ProjectSaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            Project project = null;
            if (ActiveNode is Project)
                project = ActiveNode as Project;
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
            if (ActiveNode is Project)
                project = ActiveNode as Project;
            else if (Projects.Count == 1)
                project = Projects[0];

            if (await project.PromptSaveLocation())
                await project.SaveAsync();
        }

        private async void ProjectCloseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            Project project = null;
            if (ActiveNode is Project)
                project = ActiveNode as Project;
            else if (Projects.Count == 1)
                project = Projects[0];

            if (await project.PromptSaveChanges() == false)
                return;

            project.IsActive = false;
            Projects.Remove(project);
        }

        private async void WorkspaceOpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
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

            var workspaceFile = await picker.PickSingleFileAsync();
            if (workspaceFile == null || !workspaceFile.IsAvailable)
                return;

            // Reset singleton workspace and load from workspace file
            IsActive = false;
            File = workspaceFile;

            XmlReader reader = null;
            try
            {
                reader = await GetXmlReaderForFileAsync(workspaceFile);
                ReadXml(reader);
            }
            finally
            {
                reader.Close();
            }
            IsActive = true;

            // Open each project in the workspace
            foreach (var project in Projects)
            {
                var projectFile = await StorageFile.GetFileFromPathAsync(project.Path);
                if (projectFile == null || !projectFile.IsAvailable)
                    continue;

                try
                {
                    reader = await GetXmlReaderForFileAsync(projectFile);
                    project.ReadXml(reader);
                }
                finally
                {
                    reader.Close();
                }
                project.File = projectFile;
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
            }
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

        private void WorkspaceCloseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
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
                ActiveNode.Children.Add(new MediaFolder(dlg.Text));
            }
        }

        private async void WorkspaceImportCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (((MediaFolder)ActiveNode).Root is not Project parentProject)
                throw new Exception("Unable to find target project");

            int fileCount = 0, folderCount = 0;
            var folderList = new List<StorageFolder>();
            var fileList = new List<StorageFile>();

            // Get list of selected nodes in the System Browser
            var request = Messenger.Send(new CollectionRequestMessage<TreeViewNode>());
            foreach (var node in request.Responses)
            {
                if (HasSelectedAncestor(node))
                    continue;

                if (node.Content is StorageFolder folder)
                    folderList.Add(folder);
                else if (node.Content is StorageFile file)
                    fileList.Add(file);
            }

            // Recursively import top-level folders
            foreach (var folder in folderList)
            {
                await AddFolder(folder, (MediaFolder)ActiveNode);
            }

            // Import all top-level files
            foreach (var file in fileList)
            {
                AddFile(file, (MediaFolder)ActiveNode);
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
            
        }

        private void WorkspaceRemoveSelectedCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void WorkspaceRenameItemCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
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
            Messenger.Register<CollectionChangedMessage<ViewModelNode>, string>(this, nameof(ViewModelNode.Children), (r, m) =>
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
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            // New Project
            ProjectNewCommand = new XamlUICommand
            {
                Label = "New Project...",
                Description = "Begin a new project",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE81E }
            };

            // Open Project
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

            // Save Project
            ProjectSaveCommand = new XamlUICommand
            {
                Label = "Save Project",
                Description = "Save the current project",
                IconSource = new SymbolIconSource { Symbol = Symbol.SaveLocal }
            };

            // Save Project As
            ProjectSaveAsCommand = new XamlUICommand
            {
                Label = "Save Project As...",
                Description = "Save the current project to a different file",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE792 }
            };

            // Close Project
            ProjectCloseCommand = new XamlUICommand
            {
                Label = "Close Project",
                Description = "Close the current project",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE8BB }
            };

            // Open Workspace
            WorkspaceOpenCommand = new XamlUICommand
            {
                Label = "Open Workspace...",
                Description = "Open an existing workspace",
                IconSource = new SymbolIconSource { Symbol = Symbol.Library }
            };

            // Save Workspace
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

            // Save Workspace As
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

            // Close Workspace
            WorkspaceCloseCommand = new XamlUICommand
            {
                Label = "Close Workspace",
                Description = "Close the current workspace",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE8BB }
            };

            // New Folder
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

            // Import Item(s)
            WorkspaceImportCommand = new XamlUICommand
            {
                Label = "Import Selected",
                Description = "Import items selected in the System Browser",
                IconSource = new SymbolIconSource { Symbol = Symbol.Import }
            };

            // Remove Item
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

            // Remove Selected
            WorkspaceRemoveSelectedCommand = new XamlUICommand
            {
                Label = "Remove Selected",
                Description = "Remove selected (checked) items"
            };

            // Rename Item
            WorkspaceRenameItemCommand = new XamlUICommand
            {
                Label = "Rename...",
                Description = "Rename this item",
                IconSource = new SymbolIconSource { Symbol = Symbol.Rename }
            };

            // Toggle Multiple Selection
            WorkspaceSelectMultipleCommand = new XamlUICommand
            {
                Label = "Toggle Multi-Select",
                Description = "Toggle multiple selection mode",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE762 }
            };

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
        }
        #endregion
    }
}