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

namespace MediaBase.ViewModel
{
    [ViewModelType("Workspace")]
    public sealed class ProjectManager : ViewModelElement
    {
        #region Constants
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
        private Func<IList<TreeViewNode>> _systemBrowserSelectedNodesFunction;
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

        public Func<IList<TreeViewNode>> SystemBrowserSelectedNodesFunction
        {
            get => _systemBrowserSelectedNodesFunction;
            set => SetProperty(ref _systemBrowserSelectedNodesFunction, value);
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

        public Dictionary<Guid, IMultimediaItem> MediaDictionary { get; }
        public Dictionary<string, Guid> MediaFileDictionary { get; }

        public ObservableCollection<string> TagDatabase { get; }
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
            _activeNode = null;
            _activeMediaSource = null;
            _systemBrowserSelectedNodesFunction = null;
            _hasUnsavedChanges = false;

            Projects = new ObservableCollection<Project>();
            MediaDictionary = new Dictionary<Guid, IMultimediaItem>();
            MediaFileDictionary = new Dictionary<string, Guid>();
            TagDatabase = new ObservableCollection<string>();

            Projects.CollectionChanged += Projects_CollectionChanged;

            InitializeCommands();
            RegisterMessages();

            IsActive = true;
        }
        #endregion

        #region Public Methods
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
                Name = DefaultTitle;
            else if (Projects.Count == 1)
                Name = $"Workspace: {Projects[0].Name}";
            else
                Name = $"Workspace: {Projects.Count} Projects";
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
            args.CanExecute = ActiveNode is Project project || Projects.Count == 1;
        }

        private void ProjectCloseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveNode is Project project || Projects.Count == 1;
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
            args.CanExecute = ActiveNode is MediaFolder &&
                              SystemBrowserSelectedNodesFunction != null &&
                              SystemBrowserSelectedNodesFunction().Any();
        }

        private void WorkspaceRemoveItemCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveNode is ViewModelNode;
        }

        private void WorkspaceRemoveSelectedCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = Projects.Any(x => x.DepthFirstEnumerable().Any(y => y.IsSelected));
        }

        private void WorkspaceRenameItemCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveNode is ViewModelNode;
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

        private void ProjectSaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectSaveAsCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectCloseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void WorkspaceOpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void WorkspaceSaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void WorkspaceSaveAsCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
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
            int fileCount = 0, folderCount = 0;
            var messenger = App.Current.Services.GetService<IMessenger>();
            var selectedNodes = SystemBrowserSelectedNodesFunction();

            var folderList = new List<StorageFolder>();
            var fileList = new List<StorageFile>();

            foreach (var node in selectedNodes)
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
            messenger.Send(new SetInfoBarMessage
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
                    if (selectedNodes.Contains(node.Parent))
                        return true;

                    node = node.Parent;
                }

                return false;
            }

            void AddFile(StorageFile sourceFile, MediaFolder destinationFolder)
            {
                if (MediaFileDictionary.ContainsKey(sourceFile.Path))
                {
                    var file = MediaDictionary[MediaFileDictionary[sourceFile.Path]];
                    if (file is not MediaFile) throw new Exception(
                        $"Database lookup error: Unable to find MediaFile associated with the file at {sourceFile.Path}");

                    if (((MediaFile)file).ContentType == MediaContentType.Image)
                    {
                        fileCount++;
                        var imageSource = new ImageSource(file);
                        MediaDictionary.Add(imageSource.Id, imageSource);
                        destinationFolder.Children.Add(imageSource);
                    }
                    else if (((MediaFile)file).ContentType == MediaContentType.Video)
                    {
                        fileCount++;
                        var videoSource = new VideoSource(file);
                        MediaDictionary.Add(videoSource.Id, videoSource);
                        destinationFolder.Children.Add(videoSource);
                    }
                }
                else if (sourceFile.ContentType.ToLower().Contains("image"))
                {
                    fileCount++;
                    var imageFile = new ImageFile(sourceFile);
                    MediaDictionary.Add(imageFile.Id, imageFile);
                    MediaFileDictionary.Add(sourceFile.Path, imageFile.Id);

                    var imageSource = new ImageSource(imageFile);
                    MediaDictionary.Add(imageSource.Id, imageSource);
                    destinationFolder.Children.Add(imageSource);
                }
                else if (sourceFile.ContentType.ToLower().Contains("video"))
                {
                    fileCount++;
                    var videoFile = new VideoFile(sourceFile);
                    MediaDictionary.Add(videoFile.Id, videoFile);
                    MediaFileDictionary.Add(sourceFile.Path, videoFile.Id);

                    var videoSource = new VideoSource(videoFile);
                    MediaDictionary.Add(videoSource.Id, videoSource);
                    destinationFolder.Children.Add(videoSource);
                }
            }

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
            return null;
        }

        protected override void HijackSerialization(string propertyName, object value, ref XmlWriter writer)
        {
            
        }
        #endregion

        #region Private Methods
        private void RegisterMessages()
        {
            Messenger.Register<MediaLookupRequestMessage>(this, (r, m) =>
            {
                if (MediaDictionary.ContainsKey(m.Id))
                    m.Reply(MediaDictionary[m.Id]);
            });

            Messenger.Register<CollectionChangedMessage<string>>(this, (r, m) =>
            {
                if (m.Sender is not IMediaMetadata &&
                    m.PropertyName != nameof(IMediaMetadata.Tags))
                    return;

                foreach (var tag in m.NewValue)
                {
                    if (!TagDatabase.Contains(tag))
                        TagDatabase.Add(tag);
                }
            });

            Messenger.Register<PropertyChangedMessage<bool>>(this, (r, m) =>
            {
                if (m.Sender is Project && m.PropertyName == nameof(Project.HasUnsavedChanges))
                {
                    HasUnsavedChanges = Projects.Count(x => x.HasUnsavedChanges) > 0;
                }
            });
        }

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