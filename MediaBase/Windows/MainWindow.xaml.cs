using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

using JLR.Utility.WinUI;

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

            InitializeCommands();
        }
        #endregion

        #region Event Handlers (Window & Title Bar)
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {

        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            
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
        #endregion

        #region Private Methods
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
                                    AppTitleTextBlock.ActualWidth +
                                    RightDragColumn.ActualWidth) * scaleAdjustment);
            dragRect.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
            dragRects.Add(dragRect);

            appWindow.TitleBar.SetDragRectangles(dragRects.ToArray());
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

        #region Commands
        private void InitializeCommands()
        {
            ViewModel.ProjectNew.CanExecuteRequested += ProjectNew_CanExecuteRequested;
            ViewModel.ProjectNew.ExecuteRequested += ProjectNew_ExecuteRequested;

            ViewModel.ProjectOpen.CanExecuteRequested += ProjectOpen_CanExecuteRequested;
            ViewModel.ProjectOpen.ExecuteRequested += ProjectOpen_ExecuteRequested;

            ViewModel.ProjectSave.CanExecuteRequested += ProjectSave_CanExecuteRequested;
            ViewModel.ProjectSave.ExecuteRequested += ProjectSave_ExecuteRequested;

            ViewModel.ProjectSaveAs.CanExecuteRequested += ProjectSaveAs_CanExecuteRequested;
            ViewModel.ProjectSaveAs.ExecuteRequested += ProjectSaveAs_ExecuteRequested;

            ViewModel.ViewNormal.CanExecuteRequested += ViewNormal_CanExecuteRequested;
            ViewModel.ViewNormal.ExecuteRequested += ViewNormal_ExecuteRequested;

            ViewModel.ViewCompact.CanExecuteRequested += ViewCompact_CanExecuteRequested;
            ViewModel.ViewCompact.ExecuteRequested += ViewCompact_ExecuteRequested;

            ViewModel.ViewFullscreen.CanExecuteRequested += ViewFullscreen_CanExecuteRequested;
            ViewModel.ViewFullscreen.ExecuteRequested += ViewFullscreen_ExecuteRequested;

            ViewModel.HelpAbout.CanExecuteRequested += HelpAbout_CanExecuteRequested;
            ViewModel.HelpAbout.ExecuteRequested += HelpAbout_ExecuteRequested;
        }

        private void ProjectNew_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectOpen_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectSave_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectSaveAs_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ViewNormal_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ViewCompact_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ViewFullscreen_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void HelpAbout_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectNew_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectOpen_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectSave_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ProjectSaveAs_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ViewNormal_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ViewCompact_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void ViewFullscreen_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void HelpAbout_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }
        #endregion
    }
}