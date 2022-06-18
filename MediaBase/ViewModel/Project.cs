using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.Dialogs;
using JLR.Utility.WinUI.Messaging;
using JLR.Utility.WinUI.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

using WinRT.Interop;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// MediaBASE project ViewModel
    /// </summary>
    [ViewModelObject("Project", XmlNodeType.Element)]
    public sealed partial class Project : MediaFolder
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
        #endregion

        #region Properties
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
            set
            {
                if (_activeProjectNode != null)
                    _activeProjectNode.IsSelected = false;

                SetProperty(ref _activeProjectNode, value, true);

                if (_activeProjectNode != null)
                    _activeProjectNode.IsSelected = true;
            }
        }

        /// <summary>
        /// Gets or sets a reference to the currently active media source.
        /// </summary>
        public MBMediaSource ActiveMediaSource
        {
            get => _activeMediaSource;
            set
            {
                if (_activeMediaSource != null)
                    _activeMediaSource.IsSelected = false;

                SetProperty(ref _activeMediaSource, value, true);

                if (_activeMediaSource != null)
                    _activeMediaSource.IsSelected = true;

                GeneralPreviousCommand.NotifyCanExecuteChanged();
                GeneralNextCommand.NotifyCanExecuteChanged();
                ToolsMark1Command.NotifyCanExecuteChanged();
                ToolsMark2Command.NotifyCanExecuteChanged();
                ToolsMark3Command.NotifyCanExecuteChanged();
                ToolsMark4Command.NotifyCanExecuteChanged();
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

                ProjectDeleteMarkerCommand.NotifyCanExecuteChanged();
            }
        }

        public HashSet<string> TagDatabase { get; }

        public int FileCount => DepthFirstEnumerable().OfType<IMediaFile>().Count();

        public int FolderCount => DepthFirstEnumerable().OfType<MediaFolder>().Count();
        #endregion

        #region Commands
        // General
        public XamlUICommand GeneralPreviousCommand { get; private set; }
        public XamlUICommand GeneralNextCommand { get; private set; }

        // Project
        public XamlUICommand ProjectNewCommand { get; private set; }
        public XamlUICommand ProjectOpenCommand { get; private set; }
        public XamlUICommand ProjectSaveCommand { get; private set; }
        public XamlUICommand ProjectSaveAsCommand { get; private set; }
        public XamlUICommand ProjectCloseCommand { get; private set; }
        public XamlUICommand ProjectNewFolderCommand { get; private set; }
        public XamlUICommand ProjectImportFilesCommand { get; private set; }
        public XamlUICommand ProjectImportFolderCommand { get; private set; }
        public XamlUICommand ProjectRemoveItemCommand { get; private set; }
        public XamlUICommand ProjectRemoveSelectedCommand { get; private set; }
        public XamlUICommand ProjectRemoveAllCommand { get; private set; }
        public XamlUICommand ProjectRenameItemCommand { get; private set; }
        public XamlUICommand ProjectSelectMultipleCommand { get; private set; }
        public XamlUICommand ProjectDeleteMarkerCommand { get; private set; }

        // Tools
        public XamlUICommand ToolsAnimateMediaCommand { get; private set; }
        public XamlUICommand ToolsCategoryActionCommand { get; private set; }
        public XamlUICommand ToolsMark1Command { get; private set; }
        public XamlUICommand ToolsMark2Command { get; private set; }
        public XamlUICommand ToolsMark3Command { get; private set; }
        public XamlUICommand ToolsMark4Command { get; private set; }

        // Editor
        public XamlUICommand EditorPlayCommand { get; private set; }
        public XamlUICommand EditorPauseCommand { get; private set; }
        public XamlUICommand EditorToggleLoopingCommand { get; private set; }
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
        #endregion

        #region Constructor
        public Project()
        {
            _hasUnsavedChanges = false;
            _file = null;
            _activeProjectNode = null;
            _activeMediaSource = null;
            _selectedMarker = null;
            TagDatabase = new HashSet<string>();

            InitializeCommands();
            RegisterMessages();
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

            HijackMemberViewModelElementSerialization = (p) =>
            {
                return new SerializedViewModelElementReference { Path = ((Project)p).File.Path };
            };

            // Find and save all nested projects
            foreach (var project in DepthFirstEnumerable().OfType<Project>())
            {
                if (project != this)
                    await project.SaveAsync();
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

        public async Task<bool> PromptToSaveChanges(XamlRoot xamlRoot)
        {
            if (!IsActive || !HasUnsavedChanges)
                return true;

            var dlg = new ContentDialog
            {
                Title = "Unsaved Changes",
                Content = $"Save changes to {Name}?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                CloseButtonText = "Cancel",
                XamlRoot = xamlRoot
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
                    var saveFile = await PromptSaveLocation();
                    if (saveFile == null || !saveFile.IsAvailable)
                        return false;
                    File = saveFile;
                }

                await SaveAsync();
            }

            return true;
        }

        public async Task<StorageFile> PromptSaveLocation()
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                CommitButtonText = "Save",
                SuggestedFileName = Name
            };

            picker.FileTypeChoices.Add("MediaBase Project Files", new List<string> { ".mbp" });
            InitializeWithWindow.Initialize(picker, App.WindowHandle);
            return await picker.PickSaveFileAsync();
        }

        public static async Task LoadMediaFilesAsync(ViewModelNode node)
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            int count = 0, errors = 0;

            // Project files
            var modificationList = new List<(SerializedViewModelElementReference fileRef, int index, Project project)>();
            foreach (var fileRef in node.Children.OfType<SerializedViewModelElementReference>())
            {
                modificationList.Add((fileRef, node.Children.IndexOf(fileRef), (Project)await fileRef.DeserializeFromPathAsync()));
            }

            foreach (var (fileRef, index, project) in modificationList)
            {
                if (index == -1 || project == null)
                    errors++;
                else
                {
                    project.File = fileRef.File;
                    fileRef.Parent.Children[index] = project;
                }

                count++;
            }

            // Media files
            foreach (var file in node.Children.OfType<IMediaFile>())
            {
                if ((file as MBMediaSource).IsReady)
                    continue;

                if (file.File != null)
                {
                    if (await file.ReadPropertiesFromFileAsync() == false)
                        errors++;
                }
                else if (!string.IsNullOrEmpty(file.Path))
                {
                    if (await file.LoadFileFromPathAsync() == false)
                        errors++;
                }
                else
                {
                    errors++;
                }

                count++;
            }

            // Warn user if errors occurred
            if (errors > 0)
            {
                messenger.Send(new SetInfoBarMessage
                {
                    Title = "File Load Error",
                    Message = $"Unable to load {errors} out of {count} file{(errors != 1 ? "s" : "")} in the current node.",
                    Severity = InfoBarSeverity.Warning,
                    IsCloseable = true
                });
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

            Children.Clear();
            ActiveNode = this;
            ActiveMediaSource = null;
            SelectedMarker = null;
            HasUnsavedChanges = false;
            TagDatabase.Clear();
            File = null;
            Name = null;
        }
        #endregion

        #region Event Handlers (Commands - CanExecuteRequested)
        private void GeneralPreviousCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveMediaSource != null && ActiveMediaSource != ActiveMediaSource.Parent.Children.First();
        }

        private void GeneralNextCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveMediaSource != null && ActiveMediaSource != ActiveMediaSource.Parent.Children.Last();
        }

        private void ProjectNewCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = true;
        }

        private void ProjectOpenCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = true;
        }

        private void ProjectSaveCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive && HasUnsavedChanges;
        }

        private void ProjectSaveAsCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive;
        }

        private void ProjectCloseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive;
        }

        private void ProjectDeleteMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = SelectedMarker != null;
        }

        private void ToolsMarkCategoryCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ActiveMediaSource != null;
        }
        #endregion

        #region Event Handlers (Commands - ExecuteRequested)
        private void GeneralPreviousCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var index = ActiveMediaSource.Parent.Children.IndexOf(ActiveMediaSource);
            ActiveMediaSource = (MBMediaSource)ActiveMediaSource.Parent.Children[index - 1];
        }

        private void GeneralNextCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var index = ActiveMediaSource.Parent.Children.IndexOf(ActiveMediaSource);
            ActiveMediaSource = (MBMediaSource)ActiveMediaSource.Parent.Children[index + 1];
        }

        private async void ProjectNewCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            // Prompt user to save unsaved changes (if applicable)
            if (await PromptToSaveChanges(App.Window.Content.XamlRoot) == false)
                return;

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
                IsActive = false;
                Name = dlg.Text;
                IsActive = true;
            }
        }

        private async void ProjectOpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            // Prompt user to save unsaved changes (if applicable)
            if (await PromptToSaveChanges(App.Window.Content.XamlRoot) == false)
                return;

            // Prompt user for project file to open
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                CommitButtonText = "Open Project",
                FileTypeFilter = { ".mbp" }
            };

            InitializeWithWindow.Initialize(picker, App.WindowHandle);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
                return;

            // Read project file and populate project browser
            var newProject = (Project)await FromXmlFileAsync(file);
            if (newProject == null)
                return;

            IsActive = false;
            Name = newProject.Name;
            File = file;

            foreach (var child in newProject.Children)
            {
                Children.Add(child);
            }

            // Load any files in the root project folder (because they are visible in the project browser)
            await LoadMediaFilesAsync(this);
            IsActive = true;
        }

        private async void ProjectSaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (File == null || !File.IsAvailable)
            {
                var saveFile = await PromptSaveLocation();
                if (saveFile == null || !saveFile.IsAvailable)
                    return;

                File = saveFile;
            }

            await SaveAsync();
        }

        private async void ProjectSaveAsCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var saveFile = await PromptSaveLocation();
            if (saveFile == null || !saveFile.IsAvailable)
                return;

            File = saveFile;
            await SaveAsync();
        }

        private async void ProjectCloseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            // Prompt user to save unsaved changes (user can cancel the close)
            if (await PromptToSaveChanges(App.Window.Content.XamlRoot) == false)
                return;

            IsActive = false;
        }

        private void ProjectDeleteMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var markerToDelete = SelectedMarker;
            SelectedMarker = null;
            if (ActiveMediaSource.Keyframes.Contains(markerToDelete)) // TODO: Make Marker IEquatable<> here and elsewhere to fix this problem
                ActiveMediaSource.Keyframes.Remove(markerToDelete);
            else if (ActiveMediaSource is VideoSource videoSource && videoSource.Markers.Contains(markerToDelete))
                videoSource.Markers.Remove(markerToDelete);
        }

        private void ToolsMarkCategoryCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (!int.TryParse((string)args.Parameter, out int category))
                return;

            switch (category)
            {
                case 1:
                    ActiveMediaSource.IsCategory1 = !ActiveMediaSource.IsCategory1;
                    break;
                case 2:
                    ActiveMediaSource.IsCategory2 = !ActiveMediaSource.IsCategory2;
                    break;
                case 3:
                    ActiveMediaSource.IsCategory3 = !ActiveMediaSource.IsCategory3;
                    break;
                case 4:
                    ActiveMediaSource.IsCategory4 = !ActiveMediaSource.IsCategory4;
                    break;
            }
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            #region General Commands
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
            #endregion

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

            ProjectCloseCommand = new XamlUICommand
            {
                Label = "Close",
                Description = "Close the current project",
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

            ProjectSelectMultipleCommand = new XamlUICommand
            {
                Label = "Toggle Multi-Select",
                Description = "Toggle multiple selection mode",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE762 }
            };

            ProjectDeleteMarkerCommand = new XamlUICommand
            {
                Label = "Delete Marker",
                Description = "Delete currently selected marker",
                IconSource = new SymbolIconSource { Symbol = Symbol.Delete }
            };

            ProjectDeleteMarkerCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Delete,
                IsEnabled = true
            });
            #endregion

            #region Tool Commands
            ToolsAnimateMediaCommand = new XamlUICommand
            {
                Label = "Animate...",
                Description = "Animate media position and scale"
            };

            ToolsCategoryActionCommand = new XamlUICommand
            {
                Label = "Category Action...",
                Description = "Perform an action on a categorized item",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE781 }
            };

            ToolsCategoryActionCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.A,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                IsEnabled = true
            });

            ToolsMark1Command = new XamlUICommand
            {
                Label = "Category 1",
                Description = "Mark Item as Category 1",
                IconSource = (PathIconSource)App.Current.Resources["GoldTriangleIconSource"]
            };

            ToolsMark1Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Number1,
                IsEnabled = true
            });

            ToolsMark2Command = new XamlUICommand
            {
                Label = "Category 2",
                Description = "Mark Item as Category 2",
                IconSource = (PathIconSource)App.Current.Resources["BlueSquareIconSource"]
            };

            ToolsMark2Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Number2,
                IsEnabled = true
            });

            ToolsMark3Command = new XamlUICommand
            {
                Label = "Category 3",
                Description = "Mark Item as Category 3",
                IconSource = (PathIconSource)App.Current.Resources["RedCircleIconSource"]
            };

            ToolsMark3Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Number3,
                IsEnabled = true
            });

            ToolsMark4Command = new XamlUICommand
            {
                Label = "Category 4",
                Description = "Mark Item as Category 4",
                IconSource = (PathIconSource)App.Current.Resources["GreenDiamondIconSource"]
            };

            ToolsMark4Command.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Number4,
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

            EditorToggleLoopingCommand = new XamlUICommand
            {
                Label = "Loop Playback",
                Description = "Toggle looping of current media",
                IconSource = new SymbolIconSource { Symbol = Symbol.RepeatAll }
            };

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
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE81B }
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
            #endregion

            GeneralPreviousCommand.CanExecuteRequested += GeneralPreviousCommand_CanExecuteRequested;
            GeneralPreviousCommand.ExecuteRequested += GeneralPreviousCommand_ExecuteRequested;
            GeneralNextCommand.CanExecuteRequested += GeneralNextCommand_CanExecuteRequested;
            GeneralNextCommand.ExecuteRequested += GeneralNextCommand_ExecuteRequested;

            ProjectNewCommand.CanExecuteRequested += ProjectNewCommand_CanExecuteRequested;
            ProjectNewCommand.ExecuteRequested += ProjectNewCommand_ExecuteRequested;
            ProjectOpenCommand.CanExecuteRequested += ProjectOpenCommand_CanExecuteRequested;
            ProjectOpenCommand.ExecuteRequested += ProjectOpenCommand_ExecuteRequested;
            ProjectSaveCommand.CanExecuteRequested += ProjectSaveCommand_CanExecuteRequested;
            ProjectSaveCommand.ExecuteRequested += ProjectSaveCommand_ExecuteRequested;
            ProjectSaveAsCommand.CanExecuteRequested += ProjectSaveAsCommand_CanExecuteRequested;
            ProjectSaveAsCommand.ExecuteRequested += ProjectSaveAsCommand_ExecuteRequested;
            ProjectCloseCommand.CanExecuteRequested += ProjectCloseCommand_CanExecuteRequested;
            ProjectCloseCommand.ExecuteRequested += ProjectCloseCommand_ExecuteRequested;
            ProjectDeleteMarkerCommand.CanExecuteRequested += ProjectDeleteMarkerCommand_CanExecuteRequested;
            ProjectDeleteMarkerCommand.ExecuteRequested += ProjectDeleteMarkerCommand_ExecuteRequested;
            
            ToolsMark1Command.CanExecuteRequested += ToolsMarkCategoryCommand_CanExecuteRequested;
            ToolsMark1Command.ExecuteRequested += ToolsMarkCategoryCommand_ExecuteRequested;
            ToolsMark2Command.CanExecuteRequested += ToolsMarkCategoryCommand_CanExecuteRequested;
            ToolsMark2Command.ExecuteRequested += ToolsMarkCategoryCommand_ExecuteRequested;
            ToolsMark3Command.CanExecuteRequested += ToolsMarkCategoryCommand_CanExecuteRequested;
            ToolsMark3Command.ExecuteRequested += ToolsMarkCategoryCommand_ExecuteRequested;
            ToolsMark4Command.CanExecuteRequested += ToolsMarkCategoryCommand_CanExecuteRequested;
            ToolsMark4Command.ExecuteRequested += ToolsMarkCategoryCommand_ExecuteRequested;
        }

        private void RegisterMessages()
        {
            Messenger.Register<CollectionChangedMessage<string>>(this, (r, m) =>
            {
                if (m.Sender is not IMediaMetadata metadata &&
                    m.PropertyName != nameof(IMediaMetadata.Tags))
                    return;

                foreach (var tag in m.NewValue)
                    TagDatabase.Add(tag);
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
        #endregion
    }
}