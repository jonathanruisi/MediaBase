using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace MediaBase
{
	internal static class Commands
	{
		#region Project Commands
		public static class ProjectNewCommand
		{
			public static XamlUICommand ProjectNew { get; }

			static ProjectNewCommand()
			{
				ProjectNew = new XamlUICommand
				{
					Label       = "New...",
					Description = "Begin a new project",
					IconSource  = new SymbolIconSource {Symbol = Symbol.NewWindow}
				};

				ProjectNew.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.N,
					Modifiers = VirtualKeyModifiers.Control,
					IsEnabled = true
				});
			}
		}

		public static class ProjectOpenCommand
		{
			public static XamlUICommand ProjectOpen { get; }

			static ProjectOpenCommand()
			{
				ProjectOpen = new XamlUICommand
				{
					Label       = "Open...",
					Description = "Open an existing project",
					IconSource  = new SymbolIconSource {Symbol = Symbol.OpenLocal}
				};

				ProjectOpen.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.O,
					Modifiers = VirtualKeyModifiers.Control,
					IsEnabled = true
				});
			}
		}

		public static class ProjectSaveCommand
		{
			public static XamlUICommand ProjectSave { get; }

			static ProjectSaveCommand()
			{
				ProjectSave = new XamlUICommand
				{
					Label       = "Save",
					Description = "Save the current project",
					IconSource  = new SymbolIconSource {Symbol = Symbol.SaveLocal}
				};

				ProjectSave.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.S,
					Modifiers = VirtualKeyModifiers.Control,
					IsEnabled = true
				});
			}
		}

		public static class ProjectSaveAsCommand
		{
			public static XamlUICommand ProjectSaveAs { get; }

			static ProjectSaveAsCommand()
			{
				ProjectSaveAs = new XamlUICommand
				{
					Label       = "Save As...",
					Description = "Save the current project to a different file",
					IconSource  = new SymbolIconSource {Symbol = (Symbol) 0xE28F}
				};

				ProjectSaveAs.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.S,
					Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
					IsEnabled = true
				});
			}
		}
		#endregion

		#region Media Library Commands
		public static class MediaLibraryNewFolderCommand
		{
			public static XamlUICommand MediaLibraryNewFolder { get; }

			static MediaLibraryNewFolderCommand()
			{
				MediaLibraryNewFolder = new XamlUICommand
				{
					Label       = "New Folder...",
					Description = "Create new folder",
					IconSource  = new SymbolIconSource {Symbol = Symbol.NewFolder}
				};
			}
		}

		public static class MediaLibraryImportFilesCommand
		{
			public static XamlUICommand MediaLibraryImportFiles { get; }

			static MediaLibraryImportFilesCommand()
			{
				MediaLibraryImportFiles = new XamlUICommand
				{
					Label       = "Files...",
					Description = "Browse for media files to import",
					IconSource  = new SymbolIconSource {Symbol = Symbol.Import}
				};
			}
		}

		public static class MediaLibraryImportFolderCommand
		{
			public static XamlUICommand MediaLibraryImportFolder { get; }

			static MediaLibraryImportFolderCommand()
			{
				MediaLibraryImportFolder = new XamlUICommand
				{
					Label       = "Folder...",
					Description = "Browse for media folder to import",
					IconSource  = new SymbolIconSource {Symbol = Symbol.ImportAll}
				};
			}
		}

		public static class MediaLibraryRemoveItemCommand
		{
			public static XamlUICommand MediaLibraryRemoveItem { get; }

			static MediaLibraryRemoveItemCommand()
			{
				MediaLibraryRemoveItem = new XamlUICommand
				{
					Label       = "Item",
					Description = "Remove item",
					IconSource  = new SymbolIconSource {Symbol = Symbol.TouchPointer}
				};
			}
		}

		public static class MediaLibraryRemoveSelectedCommand
		{
			public static XamlUICommand MediaLibraryRemoveSelected { get; }

			static MediaLibraryRemoveSelectedCommand()
			{
				MediaLibraryRemoveSelected = new XamlUICommand
				{
					Label       = "Selected",
					Description = "Remove all selected (checked) items",
					IconSource  = new SymbolIconSource {Symbol = Symbol.Bullets}
				};

				MediaLibraryRemoveSelected.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.Delete,
					Modifiers = VirtualKeyModifiers.Control,
					IsEnabled = true
				});
			}
		}

		public static class MediaLibraryRemoveAllCommand
		{
			public static XamlUICommand MediaLibraryRemoveAll { get; }

			static MediaLibraryRemoveAllCommand()
			{
				MediaLibraryRemoveAll = new XamlUICommand
				{
					Label       = "All",
					Description = "Remove all items",
					IconSource  = new SymbolIconSource {Symbol = Symbol.List}
				};

				MediaLibraryRemoveAll.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.Delete,
					Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
					IsEnabled = true
				});
			}
		}

		public static class MediaLibraryRenameItemCommand
		{
			public static XamlUICommand MediaLibraryRenameItem { get; }

			static MediaLibraryRenameItemCommand()
			{
				MediaLibraryRenameItem = new XamlUICommand
				{
					Label       = "Rename...",
					Description = "Rename item",
					IconSource  = new SymbolIconSource {Symbol = Symbol.Rename}
				};
			}
		}
		#endregion

		#region Player Commands
		public static class PlayerPlayCommand
		{
			public static XamlUICommand PlayerPlay { get; }

			static PlayerPlayCommand()
			{
				PlayerPlay = new XamlUICommand
				{
					Label       = "Play",
					Description = "Begin playback",
					IconSource  = new SymbolIconSource {Symbol = Symbol.Play}
				};

				PlayerPlay.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.Space,
					IsEnabled = true
				});
			}
		}

		public static class PlayerPauseCommand
		{
			public static XamlUICommand PlayerPause { get; }

			static PlayerPauseCommand()
			{
				PlayerPause = new XamlUICommand
				{
					Label       = "Pause",
					Description = "Pause playback",
					IconSource  = new SymbolIconSource {Symbol = Symbol.Pause}
				};

				PlayerPause.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.Space,
					IsEnabled = true
				});
			}
		}

		public static class PlayerPreviousFrameCommand
		{
			public static XamlUICommand PlayerPreviousFrame { get; }

			static PlayerPreviousFrameCommand()
			{
				PlayerPreviousFrame = new XamlUICommand
				{
					Label       = "Previous Frame",
					Description = "Move back one frame",
					IconSource  = new SymbolIconSource {Symbol = Symbol.Previous}
				};

				PlayerPreviousFrame.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.Left,
					IsEnabled = true
				});
			}
		}

		public static class PlayerNextFrameCommand
		{
			public static XamlUICommand PlayerNextFrame { get; }

			static PlayerNextFrameCommand()
			{
				PlayerNextFrame = new XamlUICommand
				{
					Label       = "Next Frame",
					Description = "Move forward one frame",
					IconSource  = new SymbolIconSource {Symbol = Symbol.Next}
				};

				PlayerNextFrame.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.Right,
					IsEnabled = true
				});
			}
		}

		public static class PlayerNewMarkerCommand
		{
			public static XamlUICommand PlayerNewMarker { get; }

			static PlayerNewMarkerCommand()
			{
				PlayerNewMarker = new XamlUICommand
				{
					Label       = "New Marker",
					Description = "Add new marker",
					IconSource  = new SymbolIconSource {Symbol = Symbol.MapPin}
				};

				PlayerNewMarker.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.M,
					Modifiers = VirtualKeyModifiers.Control,
					IsEnabled = true
				});
			}
		}

		public static class PlayerNewClipCommand
		{
			public static XamlUICommand PlayerNewClip { get; }

			static PlayerNewClipCommand()
			{
				PlayerNewClip = new XamlUICommand
				{
					Label       = "New Clip",
					Description = "Add new clip",
					IconSource  = new SymbolIconSource {Symbol = Symbol.Attach}
				};

				PlayerNewClip.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.L,
					Modifiers = VirtualKeyModifiers.Control,
					IsEnabled = true
				});
			}
		}

		public static class PlayerFullscreenCommand
		{
			public static XamlUICommand PlayerFullscreen { get; }

			static PlayerFullscreenCommand()
			{
				PlayerFullscreen = new XamlUICommand
				{
					Label       = "Fullscreen",
					Description = "Toggle fullscreen playback",
					IconSource  = new SymbolIconSource {Symbol = Symbol.FullScreen}
				};

				PlayerFullscreen.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.F,
					IsEnabled = true
				});
			}
		}

		public static class PlayerRateIncreaseCommand
		{
			public static XamlUICommand PlayerRateIncrease { get; }

			static PlayerRateIncreaseCommand()
			{
				PlayerRateIncrease = new XamlUICommand
				{
					Label       = "Rate +",
					Description = "Increase playback rate",
					IconSource  = new SymbolIconSource {Symbol = Symbol.Add}
				};

				PlayerRateIncrease.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.Up,
					IsEnabled = true
				});
			}
		}

		public static class PlayerRateDecreaseCommand
		{
			public static XamlUICommand PlayerRateDecrease { get; }

			static PlayerRateDecreaseCommand()
			{
				PlayerRateDecrease = new XamlUICommand
				{
					Label       = "Rate -",
					Description = "Decrease playback rate",
					IconSource  = new SymbolIconSource {Symbol = Symbol.Remove}
				};

				PlayerRateDecrease.KeyboardAccelerators.Add(new KeyboardAccelerator
				{
					Key       = VirtualKey.Down,
					IsEnabled = true
				});
			}
		}
		#endregion
	}
}