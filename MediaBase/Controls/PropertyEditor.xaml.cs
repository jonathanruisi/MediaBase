using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MediaBase.Controls
{
    public sealed partial class PropertyEditor : UserControl
    {
        #region Properties
        public Project ViewModel => (Project)DataContext;
        #endregion

        #region Constructor
        public PropertyEditor()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<Project>();

            RegisterMessages();
        }
        #endregion

        #region Event Handlers (UserControl)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void RelatedMediaListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

        }
        #endregion

        #region Private Methods
        private void RegisterMessages()
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            messenger.Register<PropertyChangedMessage<MBMediaSource>>(this, (r, m) =>
            {
                if (m.Sender != ViewModel || m.PropertyName != nameof(ViewModel.ActiveMediaSource))
                    return;

                if (ViewModel.ActiveMediaSource is VideoSource videoSource)
                    MarkerListView.ItemsSource = videoSource.Markers;
                else
                    MarkerListView.ItemsSource = null;
            });
        }
        #endregion
    }
}