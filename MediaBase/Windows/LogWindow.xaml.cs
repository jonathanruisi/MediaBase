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
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI;
using Windows.UI;

namespace MediaBase
{
    public sealed partial class LogWindow : Window
    {
        private readonly AppWindow _appWindow;
        private readonly DispatcherQueueTimer _testTimer;

        public LogWindow()
        {
            InitializeComponent();

            _appWindow = this.GetAppWindowForCurrentWindow();
            _appWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            _appWindow.SetPresenter(AppWindowPresenterKind.Default);
            _appWindow.Resize(new SizeInt32(500, 1000));
            _appWindow.Title = "Debug Log";

            _testTimer = DispatcherQueue.CreateTimer();
            _testTimer.IsRepeating = true;
            _testTimer.Interval = TimeSpan.FromSeconds(0.05);
            _testTimer.Tick += TestTimer_Tick;
            _testTimer.Start();
        }

        private void TestTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            var textColor = Color.FromArgb(255, (byte)((Random.Shared.Next(1, 16) * 16) - 1),
                                                (byte)((Random.Shared.Next(1, 16) * 16) - 1),
                                                (byte)((Random.Shared.Next(1, 16) * 16) - 1));
            var textRun = new Run
            {
                Text = DateTime.Now.ToString(),
                Foreground = new SolidColorBrush(textColor),
                FontSize = Random.Shared.Next(8, 32)
            };

            var paragraph = new Paragraph();
            paragraph.Inlines.Add(textRun);
            LogTextBlock.Blocks.Add(paragraph);
        }
    }
}