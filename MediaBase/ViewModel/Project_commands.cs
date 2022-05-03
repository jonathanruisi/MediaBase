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
        public XamlUICommand ProjectNew { get; private set; }
        public XamlUICommand ProjectOpen { get; private set; }
        public XamlUICommand ProjectSave { get; private set; }
        public XamlUICommand ProjectSaveAs { get; private set; }
        public XamlUICommand ProjectNewFolder { get; private set; }
        public XamlUICommand ProjectImportFiles { get; private set; }
        public XamlUICommand ProjectImportFolder { get; private set; }
        public XamlUICommand ProjectRemoveItem { get; private set; }
        public XamlUICommand ProjectRemoveSelected { get; private set; }
        public XamlUICommand ProjectRemoveAll { get; private set; }
        public XamlUICommand ProjectRenameItem { get; private set; }
        #endregion

        #region Tool Commands

        #endregion

        #region View Commands
        public XamlUICommand ViewNormal { get; private set; }
        public XamlUICommand ViewCompact { get; private set; }
        public XamlUICommand ViewFullscreen { get; private set; }
        #endregion

        #region Help Commands
        public XamlUICommand HelpAbout { get; private set; }
        #endregion

        #region Editor Commands
        public XamlUICommand EditorPlay { get; private set; }
        public XamlUICommand EditorPause { get; private set; }
        public XamlUICommand EditorPreviousFrame { get; private set; }
        public XamlUICommand EditorNextFrame { get; private set; }
        public XamlUICommand EditorPreviousMarker { get; private set; }
        public XamlUICommand EditorNextMarker { get; private set; }
        public XamlUICommand EditorToggleActiveSelection { get; private set; }
        public XamlUICommand EditorNewMarker { get; private set; }
        public XamlUICommand EditorNewClip { get; private set; }
        public XamlUICommand EditorNewKeyframe { get; private set; }
        public XamlUICommand EditorCutSelected { get; private set; }
        public XamlUICommand EditorPlaybackRateDecrease { get; private set; }
        public XamlUICommand EditorPlaybackRateIncrease { get; private set; }
        public XamlUICommand EditorPlaybackRateNormal { get; private set; }
        public XamlUICommand EditorCenterImage { get; private set; }
        public XamlUICommand EditorImageZoomFit { get; private set; }
        public XamlUICommand EditorImageZoomFull { get; private set; }
        public XamlUICommand EditorTimelineZoomOut { get; private set; }
        public XamlUICommand EditorTimelineZoomIn { get; private set; }
        public XamlUICommand EditorAnimateImage { get; private set; }
        public XamlUICommand EditorTrimMedia { get; private set; }
        public XamlUICommand EditorMarkMedia { get; private set; }
        #endregion

        private void InitializeCommands()
        {
            #region Project Commands
            ProjectNew = new XamlUICommand
            {
                Label = "New...",
                Description = "Begin a new project",
                IconSource = new SymbolIconSource { Symbol = Symbol.Document }
            };

            ProjectNew.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.N,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            ProjectOpen = new XamlUICommand
            {
                Label = "Open...",
                Description = "Open an existing project",
                IconSource = new SymbolIconSource { Symbol = Symbol.OpenLocal }
            };

            ProjectOpen.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.O,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            ProjectSave = new XamlUICommand
            {
                Label = "Save",
                Description = "Save the current project",
                IconSource = new SymbolIconSource { Symbol = Symbol.SaveLocal }
            };

            ProjectSave.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.S,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            ProjectSaveAs = new XamlUICommand
            {
                Label = "Save As...",
                Description = "Save the current project to a different file",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE792 }
            };

            ProjectSaveAs.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.S,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                IsEnabled = true
            });

            ProjectNewFolder = new XamlUICommand
            {
                Label = "New Folder...",
                Description = "Create a new folder at this location",
                IconSource = new SymbolIconSource { Symbol = Symbol.NewFolder }
            };

            ProjectImportFiles = new XamlUICommand
            {
                Label = "Files...",
                Description = "Browse for and import media files to this location",
                IconSource = new SymbolIconSource { Symbol = Symbol.Import }
            };

            ProjectImportFolder = new XamlUICommand
            {
                Label = "Folder...",
                Description = "Browse for and import a folder to this location",
                IconSource = new SymbolIconSource { Symbol = Symbol.ImportAll }
            };

            ProjectRemoveItem = new XamlUICommand
            {
                Label = "Item",
                Description = "Remove this item"
            };

            ProjectRemoveSelected = new XamlUICommand
            {
                Label = "Selected",
                Description = "Remove selected (checked) items"
            };

            ProjectRemoveSelected.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Delete,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            ProjectRemoveAll = new XamlUICommand
            {
                Label = "All",
                Description = "Remove all items"
            };

            ProjectRemoveAll.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Delete,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                IsEnabled = true
            });

            ProjectRenameItem = new XamlUICommand
            {
                Label = "Rename...",
                Description = "Rename this item",
                IconSource = new SymbolIconSource { Symbol = Symbol.Rename }
            };
            #endregion

            #region Tool Commands

            #endregion

            #region View Commands
            ViewNormal = new XamlUICommand
            {
                Label = "Normal",
                Description = "Normal \"overlapped\" view",
                IconSource = new SymbolIconSource { Symbol = Symbol.BackToWindow }
            };

            ViewNormal.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F10,
                IsEnabled = true
            });

            ViewCompact = new XamlUICommand
            {
                Label = "Compact",
                Description = "Compact view"
            };

            ViewCompact.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F11,
                IsEnabled = true
            });

            ViewFullscreen = new XamlUICommand
            {
                Label = "Fullscreen",
                Description = "Fullscreen view",
                IconSource = new SymbolIconSource { Symbol = Symbol.FullScreen }
            };

            ViewFullscreen.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F12,
                IsEnabled = true
            });
            #endregion

            #region Help Commands
            HelpAbout = new XamlUICommand
            {
                Label = "About...",
                Description = "Display information about this app",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE946 }
            };

            HelpAbout.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F1,
                IsEnabled = true
            });
            #endregion

            #region Editor Commands
            EditorPlay = new XamlUICommand
            {
                Label = "Play",
                Description = "Begin playback",
                IconSource = new SymbolIconSource { Symbol = Symbol.Play }
            };

            EditorPlay.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Space,
                IsEnabled = true
            });

            EditorPause = new XamlUICommand
            {
                Label = "Pause",
                Description = "Pause playback",
                IconSource = new SymbolIconSource { Symbol = Symbol.Pause }
            };

            EditorPause.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Space,
                IsEnabled = true
            });

            EditorPreviousFrame = new XamlUICommand
            {
                Label = "Previous Frame",
                Description = "Seek back one frame",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE76B }
            };

            EditorPreviousFrame.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Left,
                IsEnabled = true
            });

            EditorNextFrame = new XamlUICommand
            {
                Label = "Next Frame",
                Description = "Seek forward one frame",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE76C }
            };

            EditorNextFrame.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Right,
                IsEnabled = true
            });

            EditorPreviousMarker = new XamlUICommand
            {
                Label = "Previous Marker",
                Description = "Seek to the previous marker",
                IconSource = new SymbolIconSource { Symbol = Symbol.Previous }
            };

            EditorPreviousMarker.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Left,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            EditorNextMarker = new XamlUICommand
            {
                Label = "Next Marker",
                Description = "Seek to the next marker",
                IconSource = new SymbolIconSource { Symbol = Symbol.Next }
            };

            EditorNextMarker.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Right,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            EditorToggleActiveSelection = new XamlUICommand
            {
                Label = "Toggle Active Selection",
                Description = "Enable/disable timeline selection controls",
                IconSource = new SymbolIconSource { Symbol = Symbol.Highlight }
            };

            EditorToggleActiveSelection.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.S,
                IsEnabled = true
            });

            EditorNewMarker = new XamlUICommand
            {
                Label = "New Marker",
                Description = "Add new marker at current position",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xECAF }
            };

            EditorNewMarker.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.M,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            EditorNewClip = new XamlUICommand
            {
                Label = "New Clip",
                Description = "Create clip from current selection",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xF406 }
            };

            EditorNewClip.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.M,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
                IsEnabled = true
            });

            EditorNewKeyframe = new XamlUICommand
            {
                Label = "New Keyframe",
                Description = "Add new keyframe",
                IconSource = new SymbolIconSource { Symbol = Symbol.Permissions }
            };

            EditorNewKeyframe.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.K,
                IsEnabled = true
            });

            EditorCutSelected = new XamlUICommand
            {
                Label = "Cut Selected",
                Description = "Add selection to cut list",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xF406 }
            };

            EditorCutSelected.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Delete,
                IsEnabled = true
            });

            EditorPlaybackRateDecrease = new XamlUICommand
            {
                Label = "Playback Rate -",
                Description = "Decrease playback rate",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xEC48 }
            };

            EditorPlaybackRateDecrease.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Down,
                IsEnabled = true
            });

            EditorPlaybackRateIncrease = new XamlUICommand
            {
                Label = "Playback Rate +",
                Description = "Increase playback rate",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xEC4A }
            };

            EditorPlaybackRateIncrease.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Up,
                IsEnabled = true
            });

            EditorPlaybackRateNormal = new XamlUICommand
            {
                Label = "Normal Playback Rate",
                Description = "Normal playback rate",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xEC49 }
            };

            EditorPlaybackRateNormal.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Home,
                IsEnabled = true
            });

            EditorCenterImage = new XamlUICommand
            {
                Label = "Center Image",
                Description = "Center image in window",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE799 }
            };

            EditorCenterImage.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.C,
                IsEnabled = true
            });

            EditorImageZoomFit = new XamlUICommand
            {
                Label = "Zoom Fit",
                Description = "Zoom to fit current view",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE9A6 }
            };

            EditorImageZoomFit.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Number0,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            EditorImageZoomFull = new XamlUICommand
            {
                Label = "Zoom Full",
                Description = "Zoom to actual size",
                IconSource = new SymbolIconSource { Symbol = Symbol.FullScreen }
            };

            EditorImageZoomFull.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.Number1,
                Modifiers = VirtualKeyModifiers.Control,
                IsEnabled = true
            });

            EditorTimelineZoomOut = new XamlUICommand
            {
                Label = "Zoom Out Timeline",
                Description = "Increase the visible timeline range",
                IconSource = new SymbolIconSource { Symbol = Symbol.ZoomOut }
            };

            EditorTimelineZoomOut.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.PageDown,
                IsEnabled = true
            });

            EditorTimelineZoomIn = new XamlUICommand
            {
                Label = "Zoom In Timeline",
                Description = "Decrease the visible timeline range",
                IconSource = new SymbolIconSource { Symbol = Symbol.ZoomIn }
            };

            EditorTimelineZoomIn.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.PageUp,
                IsEnabled = true
            });

            EditorAnimateImage = new XamlUICommand
            {
                Label = "Animate Image",
                Description = "Animate current image",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE805 }
            };

            EditorTrimMedia = new XamlUICommand
            {
                Label = "Trim",
                Description = "Trim current media item",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xE78A }
            };

            EditorMarkMedia = new XamlUICommand
            {
                Label = "Mark",
                Description = "Define tags, chapters, and clips for the current media item",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xED63 }
            };
            #endregion
        }
    }
}