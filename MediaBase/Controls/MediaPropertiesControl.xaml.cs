using System.Linq;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

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
                if (m.Sender != ViewModel ||
                    m.PropertyName != nameof(ViewModel.ActiveMediaSource))
                    return;

                if (m.NewValue == null)
                    ((MediaPropertiesControl)r).MarkerCollectionViewSource.Source = null;
                else
                {
                    // Create query to group tracks, markers, and keyframes
                    var query = ViewModel.ActiveMediaSource.Markers.OrderBy(x => x.Position)
                                                                   .GroupBy(x =>
                                                                   {
                                                                       if (x.GetType() == typeof(Marker))
                                                                           return nameof(Marker);
                                                                       else if (x.GetType() == typeof(Keyframe))
                                                                           return nameof(Keyframe);
                                                                       return null;
                                                                   }).OrderByDescending(x => x.Key);

                    ((MediaPropertiesControl)r).MarkerCollectionViewSource.Source = query;
                }
            });
        }
        #endregion
    }
}