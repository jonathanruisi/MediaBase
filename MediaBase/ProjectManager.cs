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

using MediaBase.ViewModel;

using CommunityToolkit.Mvvm.Messaging;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.Storage;
using Windows.System;

namespace MediaBase
{
    [ViewModelType("Workspace")]
    public sealed class ProjectManager : ViewModelElement
    {
        #region Constants
        public static readonly string DefaultTitle = "MediaBASE";
        public static readonly string[] MediaFileExtensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".avi", ".mov", ".wmv", ".mp4", ".mkv"
        };

        public static readonly string[] MediaBaseFileExtensions = new[]
        {
            ".mbw", ".mbp"
        };
        #endregion

        #region Fields
        private StorageFile _file;
        private ViewModelNode _activeWorkspaceNode;
        private MultimediaSource _activeMediaSource;
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
            get => _activeWorkspaceNode;
            set
            {
                if (_activeWorkspaceNode != null)
                    _activeWorkspaceNode.IsSelected = false;

                SetProperty(ref _activeWorkspaceNode, value);

                if (_activeWorkspaceNode != null)
                    _activeWorkspaceNode.IsSelected = true;
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
        /// Gets a value indicating whether or not there are changes to
        /// any project in the workspace that have not been saved.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value, true);
        }

        public ObservableCollection<Project> Projects { get; }

        public Dictionary<Guid, IMultimediaItem> MediaDictionary { get; }

        public ObservableCollection<string> TagDatabase { get; }
        #endregion

        #region Commands
        public XamlUICommand ProjectNewCommand { get; private set; }
        public XamlUICommand ProjectOpenCommand { get; private set; }
        public XamlUICommand ProjectSaveCommand { get; private set; }
        public XamlUICommand ProjectSaveAsCommand { get; private set; }
        public XamlUICommand ProjectOpenWorkspaceCommand { get; private set; }
        public XamlUICommand ProjectSaveWorkspaceCommand { get; private set; }
        public XamlUICommand ProjectSaveWorkspaceAsCommand { get; private set; }
        public XamlUICommand ProjectCloseWorkspaceCommand { get; private set; }
        public XamlUICommand ProjectNewFolderCommand { get; private set; }
        public XamlUICommand ProjectImportFilesCommand { get; private set; }
        public XamlUICommand ProjectImportFolderCommand { get; private set; }
        public XamlUICommand ProjectRemoveItemCommand { get; private set; }
        public XamlUICommand ProjectRemoveSelectedCommand { get; private set; }
        public XamlUICommand ProjectRemoveAllCommand { get; private set; }
        public XamlUICommand ProjectRenameItemCommand { get; private set; }
        public XamlUICommand ProjectSelectMultipleCommand { get; private set; }
        public XamlUICommand ProjectDeleteMarkerCommand { get; private set; }
        #endregion

        #region Constructor
        public ProjectManager()
        {
            _activeWorkspaceNode = null;
            _activeMediaSource = null;
            _hasUnsavedChanges = false;

            Projects = new ObservableCollection<Project>();
            MediaDictionary = new Dictionary<Guid, IMultimediaItem>();
            TagDatabase = new ObservableCollection<string>();

            Projects.CollectionChanged += Projects_CollectionChanged;

            InitializeCommands();
            RegisterMessages();
        }
        #endregion

        #region Event Handlers
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
        }

        private void RegisterForViewModelSerializedPropertyChangeNotification()
        {
            Messenger.Register<SerializedPropertyChangedMessage>(this, (r, m) =>
            {
                HasUnsavedChanges = true;

                // Unregister from further messages.
                // We will re-register when project is saved.
                Messenger.Unregister<SerializedPropertyChangedMessage>(this);
            });
        }

        private void InitializeCommands()
        {
            ProjectNewCommand = new XamlUICommand
            {
                Label = "New...",
                Description = "Begin a new project",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE81E }
            };

            ProjectNewCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.N,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            ProjectOpenCommand = new XamlUICommand
            {
                Label = "Open...",
                Description = "Open an existing project",
                IconSource = new SymbolIconSource { Symbol = Symbol.OpenLocal }
            };

            ProjectOpenCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.O,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            ProjectSaveCommand = new XamlUICommand
            {
                Label = "Save",
                Description = "Save the current project",
                IconSource = new SymbolIconSource { Symbol = Symbol.SaveLocal }
            };

            ProjectSaveCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.S,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            ProjectSaveAsCommand = new XamlUICommand
            {
                Label = "Save As...",
                Description = "Save the current project to a different file",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE792 }
            };

            ProjectSaveAsCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.S,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                IsEnabled = true
            });

            ProjectOpenWorkspaceCommand = new XamlUICommand
            {
                Label = "Open Workspace...",
                Description = "Open an existing workspace",
                IconSource = new SymbolIconSource { Symbol = Symbol.Library }
            };

            ProjectSaveWorkspaceCommand = new XamlUICommand
            {
                Label = "Save Workspace",
                Description = "Save the current workspace",
                IconSource = new SymbolIconSource { Symbol = Symbol.SaveLocal }
            };

            ProjectSaveWorkspaceAsCommand = new XamlUICommand
            {
                Label = "Save Workspace As...",
                Description = "Save the current workspace to a different file",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE792 }
            };

            ProjectCloseWorkspaceCommand = new XamlUICommand
            {
                Label = "Close Workspace",
                Description = "Close the current workspace",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE8BB }
            };

            ProjectNewFolderCommand = new XamlUICommand
            {
                Label = "New Folder...",
                Description = "Create a new folder at this location",
                IconSource = new SymbolIconSource { Symbol = Symbol.NewFolder }
            };

            ProjectImportFilesCommand = new XamlUICommand
            {
                Label = "Files...",
                Description = "Browse for and import media files to this location",
                IconSource = new SymbolIconSource { Symbol = Symbol.Import }
            };

            ProjectImportFolderCommand = new XamlUICommand
            {
                Label = "Folder...",
                Description = "Browse for and import a folder to this location",
                IconSource = new SymbolIconSource { Symbol = Symbol.ImportAll }
            };

            ProjectRemoveItemCommand = new XamlUICommand
            {
                Label = "Item",
                Description = "Remove this item"
            };

            ProjectRemoveItemCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Delete,
                IsEnabled = true
            });

            ProjectRemoveSelectedCommand = new XamlUICommand
            {
                Label = "Selected",
                Description = "Remove selected (checked) items"
            };

            ProjectRemoveAllCommand = new XamlUICommand
            {
                Label = "All",
                Description = "Remove all items"
            };

            ProjectRenameItemCommand = new XamlUICommand
            {
                Label = "Rename...",
                Description = "Rename this item",
                IconSource = new SymbolIconSource { Symbol = Symbol.Rename }
            };

            ProjectSelectMultipleCommand = new XamlUICommand
            {
                Label = "Toggle Multi-Select",
                Description = "Toggle multiple selection mode",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE762 }
            };
        }
        #endregion
    }
}