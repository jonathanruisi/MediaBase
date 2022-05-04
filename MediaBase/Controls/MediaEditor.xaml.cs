using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;

namespace MediaBase.Controls
{
    public sealed partial class MediaEditor : UserControl
    {
        #region Properties
        public Project ViewModel => (Project)DataContext;
        #endregion

        #region Constructor
        public MediaEditor()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<Project>();
        }
        #endregion

        #region Event Handlers (UserControl)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Player == null)
            {
                // Initialize
                ViewModel.Player = new MediaPlayer();
                ViewModel.Player.IsVideoFrameServerEnabled = true;
                ViewModel.Player.CommandManager.IsEnabled = false;
                ViewModel.Player.AutoPlay = false;

                // Subscribe to events
                ViewModel.Player.SourceChanged += Player_SourceChanged;
                ViewModel.Player.MediaOpened += Player_MediaOpened;
                ViewModel.Player.MediaEnded += Player_MediaEnded;
                ViewModel.Player.MediaFailed += Player_MediaFailed;
                ViewModel.Player.VideoFrameAvailable += Player_VideoFrameAvailable;
                ViewModel.Player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
                ViewModel.Player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
                ViewModel.Player.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Free swap-chain resources
            SwapChainCanvas.RemoveFromVisualTree();
            SwapChainCanvas.SwapChain = null;

            // Free MediaPlayer resources
            if (ViewModel.Player != null)
            {
                // Unsubscribe from events
                ViewModel.Player.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                ViewModel.Player.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
                ViewModel.Player.PlaybackSession.SeekCompleted -= PlaybackSession_SeekCompleted;
                ViewModel.Player.SourceChanged -= Player_SourceChanged;
                ViewModel.Player.MediaOpened -= Player_MediaOpened;
                ViewModel.Player.MediaEnded -= Player_MediaEnded;
                ViewModel.Player.MediaFailed -= Player_MediaFailed;
                ViewModel.Player.VideoFrameAvailable -= Player_VideoFrameAvailable;

                // Free resources
                ViewModel.Player.Source = null;
                ViewModel.Player.Dispose();
                ViewModel.Player = null;
            }
        }
        #endregion

        #region Event Handlers (MediaPlayer)
        private void Player_SourceChanged(MediaPlayer sender, object args)
        {
            
        }

        private void Player_MediaOpened(MediaPlayer sender, object args)
        {
            
        }

        private void Player_MediaEnded(MediaPlayer sender, object args)
        {
            
        }

        private void Player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            
        }

        private void Player_VideoFrameAvailable(MediaPlayer sender, object args)
        {
            
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            
        }

        private void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
        {
            
        }
        #endregion

        #region Event Handlers (SwapChain)
        private void SwapChainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SwapChainCanvas.SwapChain?.ResizeBuffers(e.NewSize);
        }
        #endregion

        #region Event Handlers (Pointer)
        private void RenderAreaBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerExited(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerMoved(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {

        }
        #endregion
    }
}