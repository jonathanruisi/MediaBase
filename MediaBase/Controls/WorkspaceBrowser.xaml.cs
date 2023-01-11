using System;
using System.Linq;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using JLR.Utility.WinUI;
using JLR.Utility.WinUI.ViewModel;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace MediaBase.Controls
{
    public sealed partial class WorkspaceBrowser : UserControl
    {
        #region Properties
        public ProjectManager ViewModel => (ProjectManager)DataContext;
        #endregion

        #region Constructor
        public WorkspaceBrowser()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ProjectManager>();
        }
        #endregion

        #region Event Handlers (UserControl)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            messenger.Register<RequestMessage<bool>, string>(this, "AreWorkspaceBrowserItemsSelected", (r, m) =>
            {
                m.Reply(((WorkspaceBrowser)r).WorkspaceBrowserListView.SelectedItems.Any());
            });

            messenger.Register<CollectionRequestMessage<ViewModelElement>, string>(this, "GetSelectedWorkspaceBrowserItems", (r, m) =>
            {
                foreach (var element in ((WorkspaceBrowser)r).WorkspaceBrowserListView.SelectedItems.OfType<ViewModelElement>())
                {
                    m.Reply(element);
                }
            });

            messenger.Register<GeneralMessage, string>(this, "ClearWorkspaceBrowserSelection", (r, m) =>
            {
                ((WorkspaceBrowser)r).WorkspaceBrowserListView.ClearSelectedItems();
            });
            
            messenger.Register<GeneralMessage, string>(this, "ScrollActiveMediaSourceIntoView", (r, m) =>
            {
                if (ViewModel.ActiveProject != ViewModel.ActiveMediaSource.Root)
                    ViewModel.ActiveProject = (Project)ViewModel.ActiveMediaSource.Root;

                if (ViewModel.ActiveWorkspaceBrowserFolder != ViewModel.ActiveMediaSource.Parent)
                    ViewModel.ActiveWorkspaceBrowserFolder = ViewModel.ActiveMediaSource.Parent;

                ((WorkspaceBrowser)r).WorkspaceBrowserListView.ScrollIntoView(ViewModel.ActiveMediaSource, ScrollIntoViewAlignment.Leading);
            });

            // ViewModel.ActiveProject changed
            messenger.Register<PropertyChangedMessage<Project>>(this, (r, m) =>
            {
                if (m.Sender != ViewModel ||
                    m.PropertyName != nameof(ViewModel.ActiveProject))
                    return;

                ((WorkspaceBrowser)r).ProjectSelectionComboBox.SelectedItem = m.NewValue;
            });
        }
        #endregion

        #region Event Handlers
        private async void ProjectSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.ActiveWorkspaceBrowserNode = null;
            if (ProjectSelectionComboBox.SelectedItem is Project project)
            {
                ViewModel.ActiveWorkspaceBrowserFolder = project;
                await ProjectManager.MakeItemsReadyAsync(project);
            }
            else
                ViewModel.ActiveWorkspaceBrowserFolder = null;
        }

        private void WorkspaceBrowserListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is ListViewItem item)
                ViewModel.ActiveWorkspaceBrowserNode = item.DataContext as ViewModelElement;
            else
                ViewModel.ActiveWorkspaceBrowserNode = null;

            e.Handled = true;
        }

        private async void WorkspaceBrowserListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (WorkspaceBrowserListView.SelectedItem is not ViewModelElement element)
            {
                ViewModel.ActiveWorkspaceBrowserNode = null;
                return;
            }

            ViewModel.ActiveWorkspaceBrowserNode = element;
            if (ViewModel.ActiveWorkspaceBrowserNode is MultimediaSource multimediaSource)
                ViewModel.ActiveMediaSource = multimediaSource;
            else if (ViewModel.ActiveWorkspaceBrowserNode is MediaFolder folder)
            {
                ViewModel.ActiveWorkspaceBrowserFolder = folder;

                // TODO: Do this somewhere else?
                await ProjectManager.MakeItemsReadyAsync(ViewModel.ActiveWorkspaceBrowserFolder);
            }
            else if (ViewModel.ActiveWorkspaceBrowserNode is Playlist playlist)
            {
                ViewModel.ActivePlaylist = playlist;
                ViewModel.ActiveMediaSource = null; // TODO: Probably don't need this. Just being cautious.
                ViewModel.PlaylistMoveFirst();
            }

            e.Handled = true;
        }

        private void WorkspaceBrowserListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
        #endregion
    }
}