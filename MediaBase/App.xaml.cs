using System;

using CommunityToolkit.Mvvm.Messaging;

using JLR.Utility.WinUI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;

using WinRT.Interop;

namespace MediaBase
{
    public partial class App : Application
    {
        #region Properties
        public new static App Current => (App)Application.Current;
        public static MainWindow Window { get; private set; }
        public static AppWindow AppWindow { get; private set; }
        public static IntPtr WindowHandle { get; private set; }
        #endregion

        #region Constructor
        public App()
        {
            UnhandledException += App_UnhandledException;
            InitializeComponent();
        }
        #endregion

        #region Public Methods

        #endregion

        #region Event Handlers
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Window = new MainWindow();
            WindowHandle = WindowNative.GetWindowHandle(Window);
            AppWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(WindowHandle));

            Window.Activate();
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            
        }
        #endregion

        #region Private Methods

        #endregion
    }
}