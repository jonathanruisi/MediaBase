using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using JLR.Utility.WinUI.ViewModel;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
            
        }

        private void RelatedMediaList_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {

        }

        private void RelatedMediaList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (RelatedMediaList.SelectedItem is MultimediaSource multimediaSource)
                ViewModel.ActiveMediaSource = multimediaSource;

            App.Current.Services.GetService<IMessenger>()
                .Send<GeneralMessage, string>("ScrollActiveMediaSourceIntoView");

            e.Handled = true;
        }
        #endregion
    }
}