using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.System;

namespace MediaBase.ViewModel
{
    public partial class Project
    {
        #region Project Commands
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
        #endregion

        #region Tool Commands

        #endregion

        #region View Commands
        public XamlUICommand ViewNormalCommand { get; private set; }
        public XamlUICommand ViewCompactCommand { get; private set; }
        public XamlUICommand ViewFullscreenCommand { get; private set; }
        #endregion

        #region Help Commands
        public XamlUICommand HelpAboutCommand { get; private set; }
        #endregion

        #region Editor Commands
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
        public XamlUICommand EditorCenterImageCommand { get; private set; }
        public XamlUICommand EditorImageZoomFitCommand { get; private set; }
        public XamlUICommand EditorImageZoomFullCommand { get; private set; }
        public XamlUICommand EditorTimelineZoomOutCommand { get; private set; }
        public XamlUICommand EditorTimelineZoomInCommand { get; private set; }
        public XamlUICommand EditorAnimateImageCommand { get; private set; }
        public XamlUICommand EditorTrimMediaCommand { get; private set; }
        public XamlUICommand EditorMarkMediaCommand { get; private set; }
        #endregion

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
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE76B }
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
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE76C }
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
                IconSource = new SymbolIconSource { Symbol = Symbol.Previous }
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
                IconSource = new SymbolIconSource { Symbol = Symbol.Next }
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
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xF406 }
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

            EditorCenterImageCommand = new XamlUICommand
            {
                Label = "Center Image",
                Description = "Center image in window",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE799 }
            };

            EditorCenterImageCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.C,
                IsEnabled = true
            });

            EditorImageZoomFitCommand = new XamlUICommand
            {
                Label = "Zoom Fit",
                Description = "Zoom to fit current view",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE9A6 }
            };

            EditorImageZoomFitCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Number0,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            EditorImageZoomFullCommand = new XamlUICommand
            {
                Label = "Zoom Full",
                Description = "Zoom to actual size",
                IconSource = new SymbolIconSource { Symbol = Symbol.FullScreen }
            };

            EditorImageZoomFullCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
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

            EditorAnimateImageCommand = new XamlUICommand
            {
                Label = "Animate Image",
                Description = "Animate current image",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE805 }
            };

            EditorTrimMediaCommand = new XamlUICommand
            {
                Label = "Trim",
                Description = "Trim current media item",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE78A }
            };

            EditorMarkMediaCommand = new XamlUICommand
            {
                Label = "Mark",
                Description = "Define tags, chapters, and clips for the current media item",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xED63 }
            };
            #endregion

            #region Assign Event Handlers
            ProjectNewCommand.CanExecuteRequested += ProjectNewCommand_CanExecuteRequested;
            ProjectNewCommand.ExecuteRequested += ProjectNewCommand_ExecuteRequested;

            ProjectOpenCommand.CanExecuteRequested += ProjectOpenCommand_CanExecuteRequested;
            ProjectOpenCommand.ExecuteRequested += ProjectOpenCommand_ExecuteRequested;

            ProjectSaveCommand.CanExecuteRequested += ProjectSaveCommand_CanExecuteRequested;
            ProjectSaveCommand.ExecuteRequested += ProjectSaveCommand_ExecuteRequested;

            ProjectSaveAsCommand.CanExecuteRequested += ProjectSaveAsCommand_CanExecuteRequested;
            ProjectSaveAsCommand.ExecuteRequested += ProjectSaveAsCommand_ExecuteRequested;

            ProjectNewFolderCommand.CanExecuteRequested += ProjectNewFolderCommand_CanExecuteRequested;
            ProjectNewFolderCommand.ExecuteRequested += ProjectNewFolderCommand_ExecuteRequested;

            ProjectImportFilesCommand.CanExecuteRequested += ProjectImportFilesCommand_CanExecuteRequested;
            ProjectImportFilesCommand.ExecuteRequested += ProjectImportFilesCommand_ExecuteRequested;

            ProjectImportFolderCommand.CanExecuteRequested += ProjectImportFolderCommand_CanExecuteRequested;
            ProjectImportFolderCommand.ExecuteRequested += ProjectImportFolderCommand_ExecuteRequested;

            ProjectRemoveItemCommand.CanExecuteRequested += ProjectRemoveItemCommand_CanExecuteRequested;
            ProjectRemoveItemCommand.ExecuteRequested += ProjectRemoveItemCommand_ExecuteRequested;

            ProjectRemoveSelectedCommand.CanExecuteRequested += ProjectRemoveSelectedCommand_CanExecuteRequested;
            ProjectRemoveSelectedCommand.ExecuteRequested += ProjectRemoveSelectedCommand_ExecuteRequested;

            ProjectRemoveAllCommand.CanExecuteRequested += ProjectRemoveAllCommand_CanExecuteRequested;
            ProjectRemoveAllCommand.ExecuteRequested += ProjectRemoveAllCommand_ExecuteRequested;

            ProjectRenameItemCommand.CanExecuteRequested += ProjectRenameItemCommand_CanExecuteRequested;
            ProjectRenameItemCommand.ExecuteRequested += ProjectRenameItemCommand_ExecuteRequested;
            #endregion
        }

        #region Event Handlers (CanExecuteRequested)
        private void ProjectNewCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectOpenCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectSaveCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectSaveAsCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectNewFolderCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive && ActiveNode is MediaFolder;
        }

        private void ProjectImportFilesCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive && ActiveNode is MediaFolder;
        }

        private void ProjectImportFolderCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive && ActiveNode is MediaFolder;
        }

        private void ProjectRemoveItemCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsActive && ActiveNode?.Depth > 0;
        }

        private void ProjectRemoveSelectedCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectRemoveAllCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectRenameItemCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }
        #endregion

        #region Event Handlers (ExecuteRequested)
        private void ProjectNewCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
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

        private void ProjectNewFolderCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectImportFilesCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
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
    }
}