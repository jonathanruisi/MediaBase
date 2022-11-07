using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaBase.Controls
{
    public sealed partial class MediaPropertiesControl : UserControl
    {
        #region Properties
        public ProjectManager ViewModel => (ProjectManager)DataContext;
        #endregion

        #region Constructor
        public MediaPropertiesControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ProjectManager>();
        }
        #endregion

        #region Event Handlers
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            messenger.Register<PropertyChangedMessage<MultimediaSource>>(this, (r, m) =>
            {
                if (m.Sender != ViewModel || m.PropertyName != nameof(ViewModel.ActiveMediaSource))
                    return;

                // TODO: Set marker listview's itemssource to the markers collection
                // TODO: Set keyframe listview's itemssource to the markers collection (need data template to isolated keyframe markers?)
            });
        }
        #endregion
    }
}