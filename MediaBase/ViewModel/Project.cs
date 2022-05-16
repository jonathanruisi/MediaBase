using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.ViewModel;

using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// MediaBASE project ViewModel
    /// </summary>
    [ViewModelObject("Project", XmlNodeType.Element)]
    public sealed partial class Project : ViewModelElement
    {
        #region Fields
        public readonly string[] MediaFileExtensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".avi", ".mov", ".wmv", ".mp4", ".mkv"
        };

        private const string MediaLibraryName = "Media Library";
        private bool _hasUnsavedChanges;
        private StorageFile _file;
        private ViewModelNode _activeProjectNode;
        private MBMediaSource _activeMediaSource;
        private Marker _selectedMarker;
        private ImageAnimationKeyframe _selectedKeyframe;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the root of this <see cref="Project"/>'s media library.
        /// </summary>
        [ViewModelObject(MediaLibraryName, XmlNodeType.Element)]
        public MediaFolder MediaLibrary { get; set; }

        /// <summary>
        /// Gets a value indicating whether or not there are changes to
        /// this <see cref="Project"/> that have yet to be saved.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value, true);
        }

        /// <summary>
        /// Gets or sets a reference to the file in which
        /// this <see cref="Project"/> is stored.
        /// </summary>
        public StorageFile File
        {
            get => _file;
            set => SetProperty(ref _file, value, true);
        }

        /// <summary>
        /// Gets or sets a reference to the currently active project item.
        /// </summary>
        public ViewModelNode ActiveNode
        {
            get => _activeProjectNode;
            set => SetProperty(ref _activeProjectNode, value, true);
        }

        /// <summary>
        /// Gets or sets a reference to the currently active media source.
        /// </summary>
        public MBMediaSource ActiveMediaSource
        {
            get => _activeMediaSource;
            set => SetProperty(ref _activeMediaSource, value, true);
        }

        /// <summary>
        /// Gets or sets a reference to the currently selected marker.
        /// </summary>
        public Marker SelectedMarker
        {
            get => _selectedMarker;
            set => SetProperty(ref _selectedMarker, value, true);
        }

        /// <summary>
        /// Gets or sets a reference to the currently selected keyframe.
        /// </summary>
        public ImageAnimationKeyframe SelectedKeyframe
        {
            get => _selectedKeyframe;
            set => SetProperty(ref _selectedKeyframe, value, true);
        }
        #endregion

        #region Commands
        // Project
        public XamlUICommand ProjectNewCommand { get; private set; }
        public XamlUICommand ProjectOpenCommand { get; private set; }
        public XamlUICommand ProjectSaveCommand { get; private set; }
        public XamlUICommand ProjectSaveAsCommand { get; private set; }
        public XamlUICommand ProjectNewFolderCommand { get; private set; }
        public XamlUICommand ProjectImportFilesCommand { get; private set; }
        public XamlUICommand ProjectImportFolderCommand { get; private set; }
        public XamlUICommand ProjectRemoveItemCommand { get; private set; }
        public XamlUICommand ProjectRemoveSelectedCommand { get; private set; }
        public XamlUICommand ProjectRemoveAllCommand { get; private set; }
        public XamlUICommand ProjectRenameItemCommand { get; private set; }

        // View
        public XamlUICommand ViewNormalCommand { get; private set; }
        public XamlUICommand ViewCompactCommand { get; private set; }
        public XamlUICommand ViewFullscreenCommand { get; private set; }

        // Tools
        public XamlUICommand ToolsAnimateMediaCommand { get; private set; }

        // Help
        public XamlUICommand HelpAboutCommand { get; private set; }

        // Editor
        public XamlUICommand EditorPlayCommand { get; private set; }
        public XamlUICommand EditorPauseCommand { get; private set; }
        public XamlUICommand EditorPreviousFrameCommand { get; private set; }
        public XamlUICommand EditorNextFrameCommand { get; private set; }
        public XamlUICommand EditorPreviousMarkerCommand { get; private set; }
        public XamlUICommand EditorNextMarkerCommand { get; private set; }
        public XamlUICommand EditorToggleActiveSelectionCommand { get; private set; }
        public XamlUICommand EditorNewMarkerCommand { get; private set; }
        public XamlUICommand EditorNewClipCommand { get; private set; }
        public XamlUICommand EditorNewKeyframeCommand { get; private set; }
        public XamlUICommand EditorCutSelectedCommand { get; private set; }
        public XamlUICommand EditorPlaybackRateDecreaseCommand { get; private set; }
        public XamlUICommand EditorPlaybackRateIncreaseCommand { get; private set; }
        public XamlUICommand EditorPlaybackRateNormalCommand { get; private set; }
        public XamlUICommand EditorCenterFrameCommand { get; private set; }
        public XamlUICommand EditorFrameZoomFitCommand { get; private set; }
        public XamlUICommand EditorFrameZoomFullCommand { get; private set; }
        public XamlUICommand EditorTimelineZoomOutCommand { get; private set; }
        public XamlUICommand EditorTimelineZoomInCommand { get; private set; }
        public XamlUICommand EditorAnimateMediaCommand { get; private set; }
        public XamlUICommand EditorTrimMediaCommand { get; private set; }
        public XamlUICommand EditorMarkMediaCommand { get; private set; }
        #endregion

        #region Constructor
        public Project()
        {
            _hasUnsavedChanges = false;
            _file = null;
            _activeProjectNode = null;
            _activeMediaSource = null;
            _selectedMarker = null;
            _selectedKeyframe = null;
            MediaLibrary = new MediaFolder { Name = MediaLibraryName };

            InitializeCommands();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Saves the XML representation of this <see cref="Project"/>
        /// to its associated <see cref="StorageFile"/>.
        /// </summary>
        public async Task SaveAsync()
        {
            if (!HasUnsavedChanges)
                return;

            if (File == null)
            {
                throw new FileNotFoundException(
                    "This project is not associated with a StorageFile");
            }

            if (!File.IsAvailable)
            {
                throw new InvalidOperationException(
                    "The StorageFile associated with this project is not available");
            }

            // Create a temporary backup of the current save file,
            // then erase the current save file.
            StorageFile tempBackup = null;
            System.IO.File.Delete(File.Path + ".bak");
            if (System.IO.File.Exists(File.Path))
            {
                tempBackup = await File.CopyAsync(await File.GetParentAsync(), File.Name + ".bak");
                await FileIO.WriteTextAsync(File, string.Empty);
            }

            var success = true;
            XmlWriter writer = null;
            try
            {
                var settings = new XmlWriterSettings
                {
                    Async = true,
                    Indent = true,
                    IndentChars = "\t",
                    OmitXmlDeclaration = true,
                    ConformanceLevel = ConformanceLevel.Document,
                    CloseOutput = true
                };

                writer = XmlWriter.Create(await File.OpenStreamForWriteAsync(), settings);
                WriteXml(writer);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error writing XML: {ex.Message}");
            }
            finally
            {
                writer?.Close();
            }

            if (success)
            {
                HasUnsavedChanges = false;

                // Delete the temporary backup file
                if (tempBackup != null)
                    await tempBackup.DeleteAsync(StorageDeleteOption.PermanentDelete);

                // Re-register for view model general change notification
                RegisterForViewModelSerializedPropertyChangeNotification();
            }
            else
            {
                if (tempBackup != null)
                    await tempBackup.MoveAndReplaceAsync(File);
            }
        }
        #endregion

        #region Method Overrides (ObservableRecipient)
        protected override void OnActivated()
        {
            base.OnActivated();
            RegisterForViewModelSerializedPropertyChangeNotification();
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            Messenger.Unregister<SerializedPropertyChangedMessage>(this);

            MediaLibrary.Children.Clear();
            _activeProjectNode = MediaLibrary;
            _activeMediaSource = null;
            _hasUnsavedChanges = false;
            _file = null;
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            #region Project Commands
            ProjectNewCommand = new XamlUICommand
            {
                Label = "New...",
                Description = "Begin a new project",
                IconSource = new SymbolIconSource { Symbol = Symbol.Document }
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

            ProjectRemoveSelectedCommand = new XamlUICommand
            {
                Label = "Selected",
                Description = "Remove selected (checked) items"
            };

            ProjectRemoveSelectedCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Delete,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            ProjectRemoveAllCommand = new XamlUICommand
            {
                Label = "All",
                Description = "Remove all items"
            };

            ProjectRemoveAllCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Delete,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                IsEnabled = true
            });

            ProjectRenameItemCommand = new XamlUICommand
            {
                Label = "Rename...",
                Description = "Rename this item",
                IconSource = new SymbolIconSource { Symbol = Symbol.Rename }
            };
            #endregion

            #region Tool Commands
            ToolsAnimateMediaCommand = new XamlUICommand
            {
                Label = "Animate...",
                Description = "Animate media position and scale"
            };
            #endregion

            #region View Commands
            ViewNormalCommand = new XamlUICommand
            {
                Label = "Normal",
                Description = "Normal \"overlapped\" view",
                IconSource = new SymbolIconSource { Symbol = Symbol.BackToWindow }
            };

            ViewNormalCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F10,
                IsEnabled = true
            });

            ViewCompactCommand = new XamlUICommand
            {
                Label = "Compact",
                Description = "Compact view"
            };

            ViewCompactCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F11,
                IsEnabled = true
            });

            ViewFullscreenCommand = new XamlUICommand
            {
                Label = "Fullscreen",
                Description = "Fullscreen view",
                IconSource = new SymbolIconSource { Symbol = Symbol.FullScreen }
            };

            ViewFullscreenCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F12,
                IsEnabled = true
            });
            #endregion

            #region Help Commands
            HelpAboutCommand = new XamlUICommand
            {
                Label = "About...",
                Description = "Display information about this app",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE946 }
            };

            HelpAboutCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F1,
                IsEnabled = true
            });
            #endregion

            #region Editor Commands
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

            EditorNewMarkerCommand = new XamlUICommand
            {
                Label = "New Marker",
                Description = "Add new marker at current position",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xECAF }
            };

            EditorNewMarkerCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.M,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            EditorNewClipCommand = new XamlUICommand
            {
                Label = "New Clip",
                Description = "Create clip from current selection",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xF406 }
            };

            EditorNewClipCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.M,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                IsEnabled = true
            });

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

            EditorCutSelectedCommand = new XamlUICommand
            {
                Label = "Cut Selected",
                Description = "Add selection to cut list",
                IconSource = new SymbolIconSource { Symbol = Symbol.Trim }
            };

            EditorCutSelectedCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Delete,
                IsEnabled = true
            });

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

            EditorAnimateMediaCommand = new XamlUICommand
            {
                Label = "Animate",
                Description = "Animate current media",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE805 }
            };

            EditorTrimMediaCommand = new XamlUICommand
            {
                Label = "Trim",
                Description = "Trim current media",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE78A }
            };

            EditorMarkMediaCommand = new XamlUICommand
            {
                Label = "Mark",
                Description = "Define tags, chapters, and clips for the current media",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xED63 }
            };
            #endregion
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
        #endregion
    }
}