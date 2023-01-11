using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using JLR.Utility.WinUI.ViewModel;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace MediaBase.Controls
{
    public sealed partial class PlaylistEditor : UserControl
    {
        #region Properties
        public ProjectManager ViewModel => (ProjectManager)DataContext;
        #endregion

        #region Constructor
        public PlaylistEditor()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ProjectManager>();
        }
        #endregion

        #region Event Handlers
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            messenger.Register<RequestMessage<bool>, string>(this, "ArePlaylistEditorItemsSelected", (r, m) =>
            {
                m.Reply(((PlaylistEditor)r).PlaylistEditorListView.SelectedItems.Any());
            });

            messenger.Register<CollectionRequestMessage<ViewModelElement>, string>(this, "GetSelectedPlaylistEditorItems", (r, m) =>
            {
                foreach (var element in ((PlaylistEditor)r).PlaylistEditorListView.SelectedItems.OfType<ViewModelElement>())
                {
                    m.Reply(element);
                }
            });
        }

        private void PlaylistEditorListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (PlaylistEditorListView.SelectedItem is not MultimediaSource multimediaSource)
                return;

            ViewModel.ActiveMediaSource = multimediaSource;

            e.Handled = true;
        }

        private void PlaylistEditorListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        #endregion
    }
}