using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using CommunityToolkit.Mvvm.Messaging;

using JLR.Utility.WinUI;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;

using WinRT.Interop;

namespace MediaBase
{
    public partial class App : Application
    {
        #region Properties
        public new static App Current => (App)Application.Current;
        public static MainWindow Window { get; private set; }
        public static IntPtr WindowHandle { get; private set; }
        public static int RefreshRate => 120;   // TODO: Query this value from the system
        public IServiceProvider Services { get; }
        #endregion

        #region Constructor
        public App()
        {
            UnhandledException += App_UnhandledException;
            Services = ConfigureServices();
            InitializeComponent();
        }
        #endregion

        #region Public Methods
        public static async void ShowMessageBoxAsync(string content, string title)
        {
            var messageDialog = new MessageDialog(content, title);
            InitializeWithWindow.Initialize(messageDialog, WindowHandle);
            await messageDialog.ShowAsync();
        }
        #endregion

        #region Event Handlers
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Window = new MainWindow();
            WindowHandle = WindowNative.GetWindowHandle(Window);
            Window.Activate();

            // Handle file-activated launch
            var activationArgs = AppInstance.GetActivatedEventArgs();
            if (activationArgs is FileActivatedEventArgs fileArgs && fileArgs.Kind == ActivationKind.File)
            {
                if (fileArgs.Files.Count > 1)
                {
                    ShowMessageBoxAsync("MediaBase was launched from multiple files.\n" +
                                        "This is not supported.",
                                        "File Launch Error");
                }
                else
                {
                    var ext = fileArgs.Files[0].GetFileExtension();
                    if (ext == ProjectManager.WorkspaceFileExtension)
                        Services.GetService<ProjectManager>().WorkspaceOpenCommand.Execute(fileArgs.Files[0]);
                    if (ext == ProjectManager.ProjectFileExtension)
                        Services.GetService<ProjectManager>().ProjectOpenCommand.Execute(fileArgs.Files[0]);
                }
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            ShowMessageBoxAsync(e.Message, "Unhandled Exception");
        }
        #endregion

        #region Private Methods
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMessenger>(StrongReferenceMessenger.Default);
            services.AddSingleton(new ProjectManager());
            return services.BuildServiceProvider();
        }
        #endregion
    }
}