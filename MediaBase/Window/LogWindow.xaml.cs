using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using JLR.Utility.WinUI;
using Windows.Graphics;

namespace MediaBase.Window
{
    public sealed partial class LogWindow : Microsoft.UI.Xaml.Window
    {
        private readonly AppWindow _appWindow;

        public LogWindow()
        {
            InitializeComponent();

            _appWindow = this.GetAppWindowForCurrentWindow();
            _appWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            _appWindow.SetPresenter(AppWindowPresenterKind.Default);
            _appWindow.Resize(new SizeInt32(500, 1000));
            _appWindow.Title = "Debug Log";
        }
    }
}