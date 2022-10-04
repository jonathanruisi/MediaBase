using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI.UI.Controls;

using JLR.Utility.WinUI;
using JLR.Utility.WinUI.Dialogs;
using JLR.Utility.WinUI.ViewModel;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
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
using Windows.System;

using WinRT;
using WinRT.Interop;

namespace MediaBase
{
    public sealed partial class MainWindow : Window
    {
        #region Fields
        private readonly AppWindow _appWindow;
        private GridLength _systemBrowserWidth, _workspaceBrowserWidth;
        #endregion

        #region Properties
        public ProjectManager ViewModel { get; private set; }
        #endregion

        #region Commands
        public XamlUICommand ViewNormalCommand { get; private set; }
        public XamlUICommand ViewCompactCommand { get; private set; }
        public XamlUICommand ViewFullscreenCommand { get; private set; }
        public XamlUICommand HelpDebugLogWindowCommand { get; private set; }
        public XamlUICommand HelpAboutCommand { get; private set; }
        public XamlUICommand ExitCommand { get; private set; }
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            ViewModel = App.Current.Services.GetService<ProjectManager>();
            if (!ViewModel.IsActive)
                ViewModel.IsActive = true;

            Activated += MainWindow_Activated;
            Closed += MainWindow_Closed;
            SizeChanged += MainWindow_SizeChanged;

            _appWindow = this.GetAppWindowForCurrentWindow();
            _appWindow.Changed += AppWindow_Changed;
            _appWindow.Title = ProjectManager.DefaultTitle;

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
        private void ViewChangePresenter_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = _appWindow != null;
        }

        private void HelpDebugLogWindowCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = true;
        }

        private void HelpAboutCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = true;
        }

        private void ExitCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = true;
        }
        #endregion

        #region Event Handlers (Commands - ExecuteRequested)
        private void ViewChangePresenter_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            SwitchPresenter((AppWindowPresenterKind)args.Parameter);
        }

        private void HelpDebugLogWindowCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void HelpAboutCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {

        }

        private void ExitCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            ViewModel.IsActive = false;
            App.Current.Exit();
        }
        #endregion

        #region Event Handlers (Window & Title Bar)
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {

        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {

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

        private void SystemBrowserToggle_Click(object sender, RoutedEventArgs e)
        {
            if (SystemBrowserPanel.Visibility == Visibility.Visible)
            {
                _systemBrowserWidth = SystemBrowserColumn.Width;
                SystemBrowserColumn.Width = new GridLength(0);
                SystemBrowserPanel.Visibility = Visibility.Collapsed;
                SystemBrowserGridSplitter.Visibility = Visibility.Collapsed;
            }
            else
            {
                SystemBrowserColumn.Width = _systemBrowserWidth;
                SystemBrowserPanel.Visibility = Visibility.Visible;
                SystemBrowserGridSplitter.Visibility = Visibility.Visible;
            }
        }

        private void WorkspaceBrowserToggle_Click(object sender, RoutedEventArgs e)
        {
            if (WorkspaceBrowserPanel.Visibility == Visibility.Visible)
            {
                _workspaceBrowserWidth = WorkspaceBrowserColumn.Width;
                WorkspaceBrowserColumn.Width = new GridLength(0);
                WorkspaceBrowserPanel.Visibility = Visibility.Collapsed;
                WorkspaceBrowserGridSplitter.Visibility = Visibility.Collapsed;
            }
            else
            {
                WorkspaceBrowserColumn.Width = _workspaceBrowserWidth;
                WorkspaceBrowserPanel.Visibility = Visibility.Visible;
                WorkspaceBrowserGridSplitter.Visibility = Visibility.Visible;
            }
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            ViewNormalCommand = new XamlUICommand
            {
                Label = "Normal",
                Description = "Normal \"overlapped\" view",
                IconSource = new SymbolIconSource { Symbol = Symbol.BackToWindow }
            };

            ViewNormalCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F9,
                IsEnabled = true
            });

            ViewCompactCommand = new XamlUICommand
            {
                Label = "Compact",
                Description = "Compact view"
            };

            ViewCompactCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F10,
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
                Key = VirtualKey.F11,
                IsEnabled = true
            });

            HelpDebugLogWindowCommand = new XamlUICommand
            {
                Label = "Debug Log...",
                Description = "Open the live debug log window",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xEBE8 }
            };

            HelpDebugLogWindowCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.F12,
                IsEnabled = true
            });

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

            ExitCommand = new XamlUICommand
            {
                Label = "Exit",
                Description = "Exit the application",
                IconSource = new SymbolIconSource { Symbol = (Symbol)0xF3B1 }
            };

            ViewNormalCommand.CanExecuteRequested +=
                ViewChangePresenter_CanExecuteRequested;
            ViewNormalCommand.ExecuteRequested +=
                ViewChangePresenter_ExecuteRequested;

            ViewCompactCommand.CanExecuteRequested +=
                ViewChangePresenter_CanExecuteRequested;
            ViewCompactCommand.ExecuteRequested +=
                ViewChangePresenter_ExecuteRequested;

            ViewFullscreenCommand.CanExecuteRequested +=
                ViewChangePresenter_CanExecuteRequested;
            ViewFullscreenCommand.ExecuteRequested +=
                ViewChangePresenter_ExecuteRequested;

            HelpDebugLogWindowCommand.CanExecuteRequested +=
                HelpDebugLogWindowCommand_CanExecuteRequested;
            HelpDebugLogWindowCommand.ExecuteRequested +=
                HelpDebugLogWindowCommand_ExecuteRequested;

            HelpAboutCommand.CanExecuteRequested +=
                HelpAboutCommand_CanExecuteRequested;
            HelpAboutCommand.ExecuteRequested +=
                HelpAboutCommand_ExecuteRequested;

            ExitCommand.CanExecuteRequested +=
                ExitCommand_CanExecuteRequested;
            ExitCommand.ExecuteRequested +=
                ExitCommand_ExecuteRequested;
        }

        private void RegisterMessages()
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            // ProjectManager.HasUnsavedChanges
            messenger.Register<PropertyChangedMessage<bool>>(this, (r, m) =>
            {
                if (m.Sender != ViewModel || m.PropertyName != nameof(ProjectManager.HasUnsavedChanges))
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
                                MenuColumn.ActualWidth +
                                ToggleColumn.ActualWidth) * scaleAdjustment);
            dragRect.Y = 0;
            dragRect.Width = (int)((LeftDragColumn.ActualWidth +
                                    AppTitleUnsavedIndicatorTextBlock.ActualWidth +
                                    AppTitleProjectNameTextBlock.ActualWidth +
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