﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

using JLR.Utility.WinUI;

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

        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

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
    }
}