using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.WinUI.UI.Controls;

using JLR.Utility.WinUI;
using JLR.Utility.WinUI.Dialogs;
using JLR.Utility.WinUI.ViewModel;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;

using WinRT;
using WinRT.Interop;

namespace MediaBase
{
    public sealed partial class MainWindow : Window
    {
        #region Fields
        private static readonly string DefaultAppTitle = "MediaBASE";
        private readonly AppWindow _appWindow;
        #endregion

        #region Properties
        public Project ViewModel { get; private set; }
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            ViewModel = App.Current.Services.GetService<Project>();

            Activated += MainWindow_Activated;
            Closed += MainWindow_Closed;
            SizeChanged += MainWindow_SizeChanged;

            _appWindow = this.GetAppWindowForCurrentWindow();
            _appWindow.Changed += AppWindow_Changed;
            _appWindow.Title = DefaultAppTitle;

            // Use custom title bar, if supported
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = _appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                AppTitleBar.Loaded += AppTitleBar_Loaded;
                AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
            }
            else
            {
                AppTitleBar.Visibility = Visibility.Collapsed;
            }

            RegisterMessages();
            InitializeCommands();
        }
        #endregion

        #region Event Handlers (Commands - CanExecuteRequested)
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
            args.CanExecute = ViewModel != null && ViewModel.IsActive && ViewModel.HasUnsavedChanges;
        }

        private void ProjectSaveAsCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = ViewModel != null && ViewModel.IsActive;
        }

        private void ViewChangePresenter_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = _appWindow != null;
        }

        private void HelpAboutCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = true;
        }
        #endregion

        #region Event Handlers (Commands - ExecuteRequested)
        private async void ProjectNewCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            // Prompt user to save unsaved changes (if applicable)
            if (await PromptToSaveChanges() == false)
                return;

            var dlg = new TextPromptDialog
            {
                Title             = "New Project",
                PromptText        = "Enter a name for the new project",
                PrimaryButtonText = "OK",
                CloseButtonText   = "Cancel",
                XamlRoot          = Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.IsActive = false;
                ViewModel.Name = dlg.Text;
                ViewModel.IsActive = true;
            }
        }

        private async void ProjectOpenCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            // Prompt user to save unsaved changes (if applicable)
            if (await PromptToSaveChanges() == false)
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
            var newProject = (Project)await ViewModelElement.FromXmlFileAsync(file);
            if (newProject == null)
                return;

            ViewModel.IsActive = false;
            ViewModel.Name = newProject.Name;
            ViewModel.File = file;

            foreach (var child in newProject.MediaLibrary.Children)
            {
                ViewModel.MediaLibrary.Children.Add(child);
            }

            await LoadMediaFiles();
            ViewModel.IsActive = true;
        }

        private async void ProjectSaveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (ViewModel.File == null || !ViewModel.File.IsAvailable)
            {
                var saveFile = await PromptSaveLocation();
                if (saveFile == null || !saveFile.IsAvailable)
                    return;

                ViewModel.File = saveFile;
            }

            await ViewModel.SaveAsync();
        }

        private async void ProjectSaveAsCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var saveFile = await PromptSaveLocation();
            if (saveFile == null || !saveFile.IsAvailable)
                return;

            ViewModel.File = saveFile;
            await ViewModel.SaveAsync();
        }

        private void ViewChangePresenter_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            SwitchPresenter((AppWindowPresenterKind)args.Parameter);
        }

        private void HelpAboutCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {

        }
        #endregion

        #region Event Handlers (Window & Title Bar)
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            // TODO: This doesn't work
            _ = await PromptToSaveChanges();
        }

        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            // TODO: Do something here so the window respects MinWidth and MinHeight of the XAML layout
        }

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
                SetDragRegionForCustomTitleBar(_appWindow);
        }

        private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported() &&
                _appWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                SetDragRegionForCustomTitleBar(_appWindow);
            }
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (!args.DidPresenterChange || !AppWindowTitleBar.IsCustomizationSupported())
                return;

            switch (sender.Presenter.Kind)
            {
                case AppWindowPresenterKind.CompactOverlay:
                    AppTitleBar.Visibility = Visibility.Collapsed;
                    break;

                case AppWindowPresenterKind.FullScreen:
                    AppTitleBar.Visibility = Visibility.Collapsed;
                    sender.TitleBar.ExtendsContentIntoTitleBar = true;
                    break;

                case AppWindowPresenterKind.Overlapped:
                    AppTitleBar.Visibility = Visibility.Visible;
                    sender.TitleBar.ExtendsContentIntoTitleBar = true;
                    SetDragRegionForCustomTitleBar(sender);
                    break;

                default:
                    sender.TitleBar.ResetToDefault();
                    break;
            }
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            ViewModel.ProjectNewCommand.CanExecuteRequested +=
                ProjectNewCommand_CanExecuteRequested;
            ViewModel.ProjectNewCommand.ExecuteRequested +=
                ProjectNewCommand_ExecuteRequested;

            ViewModel.ProjectOpenCommand.CanExecuteRequested +=
                ProjectOpenCommand_CanExecuteRequested;
            ViewModel.ProjectOpenCommand.ExecuteRequested +=
                ProjectOpenCommand_ExecuteRequested;

            ViewModel.ProjectSaveCommand.CanExecuteRequested +=
                ProjectSaveCommand_CanExecuteRequested;
            ViewModel.ProjectSaveCommand.ExecuteRequested +=
                ProjectSaveCommand_ExecuteRequested;

            ViewModel.ProjectSaveAsCommand.CanExecuteRequested +=
                ProjectSaveAsCommand_CanExecuteRequested;
            ViewModel.ProjectSaveAsCommand.ExecuteRequested +=
                ProjectSaveAsCommand_ExecuteRequested;

            ViewModel.ViewNormalCommand.CanExecuteRequested +=
                ViewChangePresenter_CanExecuteRequested;
            ViewModel.ViewNormalCommand.ExecuteRequested +=
                ViewChangePresenter_ExecuteRequested;

            ViewModel.ViewCompactCommand.CanExecuteRequested +=
                ViewChangePresenter_CanExecuteRequested;
            ViewModel.ViewCompactCommand.ExecuteRequested +=
                ViewChangePresenter_ExecuteRequested;

            ViewModel.ViewFullscreenCommand.CanExecuteRequested +=
                ViewChangePresenter_CanExecuteRequested;
            ViewModel.ViewFullscreenCommand.ExecuteRequested +=
                ViewChangePresenter_ExecuteRequested;

            ViewModel.HelpAboutCommand.CanExecuteRequested +=
                HelpAboutCommand_CanExecuteRequested;
            ViewModel.HelpAboutCommand.ExecuteRequested +=
                HelpAboutCommand_ExecuteRequested;
        }

        private void RegisterMessages()
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            // Project.HasUnsavedChanges
            messenger.Register<PropertyChangedMessage<bool>>(this, (r, m) =>
            {
                if (m.Sender != ViewModel || m.PropertyName != nameof(Project.HasUnsavedChanges))
                    return;

                AppTitleUnsavedIndicatorTextBlock.Visibility = m.NewValue
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            });

            // Set info bar message
            messenger.Register<SetInfoBarMessage>(this, (r, m) =>
            {
                AppInfoBar.Title = m.Title;
                AppInfoBar.Message = m.Message;
                AppInfoBar.Severity = m.Severity;
                AppInfoBar.IsClosable = m.IsCloseable;
                AppInfoBar.IsOpen = true;
            });
        }

        private async Task LoadMediaFiles()
        {
            var messenger = App.Current.Services.GetService<IMessenger>();
            var mediaFileEnumerator = ViewModel.MediaLibrary.DepthFirstEnumerable().OfType<MediaFile>();
            var mediaFileCount = mediaFileEnumerator.Count();

            int index = 1, errors = 0;
            foreach (var mediaFile in mediaFileEnumerator)
            {
                messenger.Send(new SetInfoBarMessage
                {
                    Title = "Opening Project",
                    Message = $"Loading file {index} of {mediaFileCount}: {mediaFile.Name}",
                    Severity = InfoBarSeverity.Informational,
                    IsCloseable = false
                });

                if (await mediaFile.LoadFileFromPathAsync() == false)
                    errors++;
                index++;
            }

            // Generate and display summary message
            string message;
            InfoBarSeverity severity;

            if (errors == 0)
            {
                message = $"{mediaFileCount} file{(mediaFileCount != 1 ? "s" : "")} loaded successfully.";
                severity = InfoBarSeverity.Success;
            }
            else if (errors == mediaFileCount)
            {
                message = $"Unable to load {mediaFileCount} file{(mediaFileCount != 1 ? "s" : "")}.";
                severity = InfoBarSeverity.Error;
            }
            else
            {
                var str = new StringBuilder();
                str.Append($"{mediaFileCount - errors} file{(mediaFileCount - errors != 1 ? "s" : "")} loaded successfully. ");
                str.Append($"Unable to load {errors} file{(errors != 1 ? "s" : "")}.");
                message = str.ToString();
                severity = InfoBarSeverity.Warning;
            }

            messenger.Send(new SetInfoBarMessage
            {
                Title = "Project Loaded",
                Message = message,
                Severity = severity,
                IsCloseable = true
            });
        }

        private async Task<bool> PromptToSaveChanges()
        {
            if (!ViewModel.IsActive || !ViewModel.HasUnsavedChanges)
                return true;

            var dlg = new ContentDialog
            {
                Title = "Unsaved Changes",
                Content = $"Save changes to {ViewModel.Name}?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                CloseButtonText = "Cancel",
                XamlRoot = Content.XamlRoot
            };

            var choice = await dlg.ShowAsync();

            // Cancel
            if (choice == ContentDialogResult.None)
                return false;

            // Yes
            if (choice == ContentDialogResult.Primary)
            {
                if (ViewModel.File == null || !ViewModel.File.IsAvailable)
                {
                    var saveFile = await PromptSaveLocation();
                    if (saveFile == null || !saveFile.IsAvailable)
                        return false;
                    ViewModel.File = saveFile;
                }

                await ViewModel.SaveAsync();
            }

            return true;
        }

        private async Task<StorageFile> PromptSaveLocation()
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                CommitButtonText = "Save",
                SuggestedFileName = ViewModel.Name
            };

            picker.FileTypeChoices.Add("MediaBase Project Files", new List<string> { ".mbp" });
            InitializeWithWindow.Initialize(picker, App.WindowHandle);
            return await picker.PickSaveFileAsync();
        }

        private double GetScaleAdjustment()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            var hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI
            var result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor");
            }

            var scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }

        private void SetDragRegionForCustomTitleBar(AppWindow appWindow)
        {
            if (!AppWindowTitleBar.IsCustomizationSupported() ||
                !appWindow.TitleBar.ExtendsContentIntoTitleBar)
                return;

            var scaleAdjustment = GetScaleAdjustment();

            LeftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);
            RightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);

            var dragRects = new List<RectInt32>();

            RectInt32 dragRect;
            dragRect.X = (int)((LeftPaddingColumn.ActualWidth +
                                IconColumn.ActualWidth +
                                MenuColumn.ActualWidth) * scaleAdjustment);
            dragRect.Y = 0;
            dragRect.Width = (int)((LeftDragColumn.ActualWidth +
                                    AppTitleProjectNameTextBlock.ActualWidth +
                                    AppTitleUnsavedIndicatorTextBlock.ActualWidth +
                                    RightDragColumn.ActualWidth) * scaleAdjustment);
            dragRect.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
            dragRects.Add(dragRect);

            appWindow.TitleBar.SetDragRectangles(dragRects.ToArray());
        }

        private void SwitchPresenter(AppWindowPresenterKind presenterKind)
        {
            if (_appWindow == null)
                return;

            if (presenterKind != _appWindow.Presenter.Kind)
                _appWindow.SetPresenter(presenterKind);
        }
        #endregion

        #region Interop
        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hMonitor,
                                                    Monitor_DPI_Type dpiType,
                                                    out uint dpiX, out uint dpiY);

        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }
        #endregion
    }
}