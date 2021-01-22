using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using JLR.Utility.UWP.Controls;

using Microsoft.Graphics.Canvas;

namespace MediaBase
{
	public sealed partial class MediaPlayerPage : Page
	{
		#region Fields
		private MediaPlayer     _player;
		private SoftwareBitmap  _frameBitmap;
		private DispatcherTimer _uiUpdateTimer;
		private FollowMode      _previousFollowMode;
		private double          _previousPlaybackRate;
		private bool            _isMouseScrubbing;
		#endregion

		#region Properties
		public IPlayable PlayableSource
		{
			get => (IPlayable) GetValue(PlayableSourceProperty);
			set => SetValue(PlayableSourceProperty, value);
		}

		public static readonly DependencyProperty PlayableSourceProperty =
			DependencyProperty.Register("PlayableSource",
										typeof(IPlayable),
										typeof(MediaPlayerPage),
										new PropertyMetadata(null, OnPlayableSourceChanged));

		public MediaSlider.IMediaMarker SelectedMarker
		{
			get => (MediaSlider.IMediaMarker) GetValue(SelectedMarkerProperty);
			set => SetValue(SelectedMarkerProperty, value);
		}

		public static readonly DependencyProperty SelectedMarkerProperty =
			DependencyProperty.Register("SelectedMarker",
										typeof(MediaSlider.IMediaMarker),
										typeof(MediaPlayerPage),
										new PropertyMetadata(null, OnSelectedMarkerChanged));
		#endregion

		#region Events
		public event EventHandler<MediaSlider.IMediaMarker> SelectedMarkerChanged;

		private void RaiseSelectedMarkerChanged()
		{
			var handler = SelectedMarkerChanged;
			handler?.Invoke(this, SelectedMarker);
		}
		#endregion

		#region Constructor
		public MediaPlayerPage()
		{
			InitializeComponent();
			InitializeCommands();
		}
		#endregion

		#region Dependency Property Callbacks
		private static async void OnPlayableSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is MediaPlayerPage page))
				return;

			Debug.WriteLine(e.NewValue == null ? "PlayableSource --> NULL" : "PlayableSource --> LOADING");

			// Free resources
			if (page._player.Source is MediaSource currentSource)
				currentSource.Dispose();
			page._player.Source = null;

			// Load new media source
			if (e.NewValue is IPlayable newSource)
			{
				page.TextBlockTimeTemp.Text = "Loading...";
				page._player.Source         = await newSource.GetMediaSourceAsync();
			}

			// (Un)subscribe to marker collection changed event
			if (e.OldValue is IMarkable oldMarkable)
			{
				oldMarkable.Markers.CollectionChanged -= page.Markers_CollectionChanged;
			}

			if (e.NewValue is IMarkable newMarkable)
			{
				newMarkable.Markers.CollectionChanged += page.Markers_CollectionChanged;
			}
		}

		private static void OnSelectedMarkerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is MediaPlayerPage page) || !(e.NewValue is MediaSlider.IMediaMarker newMarker))
				return;

			if (page._player.PlaybackSession.CanSeek &&
				page._player.PlaybackSession.CanPause)
			{
				var rate = 0.0;
				if (page._player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
				{
					rate                                      = page._player.PlaybackSession.PlaybackRate;
					page._player.PlaybackSession.PlaybackRate = 0;
				}

				page._player.PlaybackSession.Position =
					TimeSpan.FromSeconds(decimal.ToDouble(newMarker.Position));

				if (page._player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
					page._player.PlaybackSession.PlaybackRate = rate;
			}

			if (newMarker.Duration > 0)
			{
				page.Slider.SetSelectionFromMarker(newMarker);
			}
			else
			{
				page.Slider.SelectionStart = null;
				page.Slider.SelectionEnd   = null;
			}

			page.RaiseSelectedMarkerChanged();
		}
		#endregion

		#region Event Handlers (Page)
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			// Initialize media player
			_player                          = new MediaPlayer {IsVideoFrameServerEnabled = true};
			_player.CommandManager.IsEnabled = false;

			_player.SourceChanged       += MediaPlayer_SourceChanged;
			_player.MediaOpened         += MediaPlayer_MediaOpened;
			_player.MediaEnded          += MediaPlayer_MediaEnded;
			_player.MediaFailed         += MediaPlayer_MediaFailed;
			_player.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;

			_player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
			_player.PlaybackSession.PositionChanged      += PlaybackSession_PositionChanged;
			_player.PlaybackSession.SeekCompleted        += PlaybackSession_SeekCompleted;

			// Initialize UI update timer
			_uiUpdateTimer      =  new DispatcherTimer();
			_uiUpdateTimer.Tick += UIUpdateTimer_Tick;
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			// Initialize swap chain
			SwapChainPanel.SwapChain = new CanvasSwapChain(CanvasDevice.GetSharedDevice(),
														   (float) SwapChainPanel.ActualWidth,
														   (float) SwapChainPanel.ActualHeight,
														   DisplayInformation.GetForCurrentView().LogicalDpi);

			// Initialize slider
			Slider.PositionChanged  += Slider_PositionChanged;
			Slider.SelectionChanged += Slider_SelectionChanged;
			Slider.ZoomChanged      += Slider_ZoomChanged;

			Slider.PositionDragStarted    += Slider_PositionDragStarted;
			Slider.PositionDragCompleted  += Slider_PositionDragCompleted;
			Slider.SelectionDragStarted   += Slider_SelectionDragStarted;
			Slider.SelectionDragCompleted += Slider_SelectionDragCompleted;
			Slider.ZoomDragStarted        += Slider_ZoomDragStarted;
			Slider.ZoomDragCompleted      += Slider_ZoomDragCompleted;
		}

		private void Page_Unloaded(object sender, RoutedEventArgs e)
		{
			SwapChainPanel.RemoveFromVisualTree();
			SwapChainPanel.SwapChain = null;
		}
		#endregion

		#region Event Handlers (Swap Chain)
		private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			_frameBitmap = null;
			SwapChainPanel.SwapChain?.ResizeBuffers(e.NewSize);
		}
		#endregion

		#region Event Handlers (Media Player)
		private async void MediaPlayer_SourceChanged(MediaPlayer sender, object args)
		{
			Debug.WriteLine(sender.Source == null ? "MediaSource --> CLOSED" : "MediaSource --> OPENING");
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				_uiUpdateTimer.Stop();
				ResetSlider();
				ResetTimeDisplays();
				RefreshPlayerCommands();
			});
		}

		private async void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
		{
			Debug.WriteLine("Media Opened");
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				_uiUpdateTimer.Stop();

				// Adjust slider
				Slider.Duration = sender.PlaybackSession.NaturalDuration;

				if (sender.Source is MediaSource mediaSource)
				{
					var fps = (double) mediaSource.CustomProperties["FPS"];
					Slider.FramesPerSecond  = (int) Math.Ceiling(fps);
					_uiUpdateTimer.Interval = TimeSpan.FromSeconds(0.5 / fps);
				}

				Slider.IsPositionAdjustmentEnabled  = true;
				Slider.IsSelectionAdjustmentEnabled = true;
				Slider.IsZoomAdjustmentEnabled      = true;

				// Restore markers to slider
				if (PlayableSource is IMarkable markable)
				{
					foreach (var marker in markable.Markers)
					{
						Slider.Markers.Add(marker);
					}
				}

				// TODO: Fix this (we're supposed to use MediaSlider.VisibleDuration property)
				Slider.ZoomStart = Slider.Start;
				Slider.ZoomEnd   = Slider.End;

				// Update UI
				TextBlockTimeTemp.Text = string.Empty;
				SyncSliderToMedia();
				SyncTimeDisplaysToSlider();
				RefreshPlayerCommands();
			});
		}

		private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
		{
			Debug.WriteLine("Media Ended");
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				_uiUpdateTimer.Stop();
				RefreshPlayerCommands();
			});
		}

		private async void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
		{
			Debug.WriteLine("Media Failed");
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				_uiUpdateTimer.Stop();
				TextBlockTimeTemp.Text = "ERROR";
				ResetSlider();
				RefreshPlayerCommands();
			});
		}

		private async void MediaPlayer_VideoFrameAvailable(MediaPlayer sender, object args)
		{
			await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				if (_frameBitmap == null)
				{
					_frameBitmap = new SoftwareBitmap(BitmapPixelFormat.Rgba8,
													  (int) SwapChainPanel.ActualWidth,
													  (int) SwapChainPanel.ActualHeight,
													  BitmapAlphaMode.Ignore);
				}

				using var frame = CanvasBitmap.CreateFromSoftwareBitmap(SwapChainPanel.SwapChain.Device, _frameBitmap);
				using var ds    = SwapChainPanel.SwapChain.CreateDrawingSession(Colors.Black);

				_player.CopyFrameToVideoSurface(frame);
				ds.DrawImage(frame);

				SwapChainPanel.SwapChain.Present(1);
			});
		}
		#endregion

		#region Event Handlers (Playback Session)
		private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
		{
			Debug.WriteLine($"PlaybackState: {Enum.GetName(typeof(MediaPlaybackState), sender.PlaybackState)}");
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				switch (sender.PlaybackState)
				{
					case MediaPlaybackState.Playing:
						_uiUpdateTimer.Start();
						ButtonPlayPause.Command = Commands.PlayerPauseCommand.PlayerPause;
						break;

					case MediaPlaybackState.Buffering:
						_uiUpdateTimer.Stop();
						ButtonPlayPause.Command = Commands.PlayerPauseCommand.PlayerPause;
						break;

					default:
						_uiUpdateTimer.Stop();
						SyncSliderToMedia();
						SyncTimeDisplaysToSlider();
						ButtonPlayPause.Command = Commands.PlayerPlayCommand.PlayerPlay;
						break;
				}

				RefreshPlayerCommands();
			});
		}

		private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
		{
			if (sender.PlaybackState != MediaPlaybackState.Paused || _isMouseScrubbing)
				return;

			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, SyncSliderToMedia);
		}

		private async void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
		{
			if (!_isMouseScrubbing)
				return;

			decimal pos = 0;
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { pos = Slider.Position; });
			sender.Position = TimeSpan.FromSeconds(decimal.ToDouble(pos));
		}
		#endregion

		#region Event Handlers (Slider)
		private void Slider_PositionChanged(object sender, decimal e)
		{
			SyncTimeDisplaysToSlider();
			Commands.PlayerPreviousFrameCommand.PlayerPreviousFrame.NotifyCanExecuteChanged();
			Commands.PlayerNextFrameCommand.PlayerNextFrame.NotifyCanExecuteChanged();
		}

		private void Slider_SelectionChanged(object sender, (decimal start, decimal end)? e)
		{

		}

		private void Slider_ZoomChanged(object sender, (decimal start, decimal end) e)
		{

		}

		private async void Slider_PositionDragStarted(object sender, EventArgs e)
		{
			_previousPlaybackRate                = _player.PlaybackSession.PlaybackRate;
			_player.PlaybackSession.PlaybackRate = 0;

			decimal pos = 0;
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				_previousFollowMode       = Slider.PositionFollowMode;
				Slider.PositionFollowMode = FollowMode.NoFollow;
				pos                       = Slider.Position;
			});

			_isMouseScrubbing                = true;
			_player.PlaybackSession.Position = TimeSpan.FromSeconds(decimal.ToDouble(pos));
		}

		private async void Slider_PositionDragCompleted(object sender, EventArgs e)
		{
			decimal pos = 0;
			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				Slider.PositionFollowMode = _previousFollowMode;
				pos                       = Slider.Position;
			});

			_isMouseScrubbing                    = false;
			_player.PlaybackSession.Position     = TimeSpan.FromSeconds(decimal.ToDouble(pos));
			_player.PlaybackSession.PlaybackRate = _previousPlaybackRate;
		}

		private void Slider_SelectionDragStarted(object sender, EventArgs e)
		{
			_previousFollowMode       = Slider.PositionFollowMode;
			Slider.PositionFollowMode = FollowMode.NoFollow;
		}

		private void Slider_SelectionDragCompleted(object sender, EventArgs e)
		{
			Slider.PositionFollowMode = _previousFollowMode;
			Commands.PlayerNewClipCommand.PlayerNewClip.NotifyCanExecuteChanged();

			// Update clip
			if (SelectedMarker != null &&
				SelectedMarker.Duration > 0 &&
				Slider.SelectionStart != null && Slider.SelectionEnd != null &&
				PlayableSource is IMarkable markable)
			{
				var index = markable.Markers.IndexOf((Marker) SelectedMarker);

				if (index >= 0)
				{
					markable.Markers[index].Position = (decimal) Slider.SelectionStart;
					markable.Markers[index].Duration = (decimal) Slider.SelectionEnd - (decimal) Slider.SelectionStart;
				}
			}
		}

		private void Slider_ZoomDragStarted(object sender, EventArgs e)
		{
			_previousFollowMode       = Slider.PositionFollowMode;
			Slider.PositionFollowMode = FollowMode.NoFollow;
		}

		private void Slider_ZoomDragCompleted(object sender, EventArgs e)
		{
			Slider.PositionFollowMode = _previousFollowMode;
		}
		#endregion

		#region Event Handlers (Timer)
		private void UIUpdateTimer_Tick(object sender, object e)
		{
			SyncSliderToMedia();
			RefreshPlayerCommands();
		}
		#endregion

		#region Event Handlers (Markers Collection)
		private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (var marker in e.NewItems.OfType<Marker>())
					{
						Slider.Markers.Add(marker);
					}

					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (var marker in e.OldItems.OfType<Marker>())
					{
						Slider.Markers.Remove(marker);
					}

					break;

				case NotifyCollectionChangedAction.Replace:
					// TODO: Implement equality comparison
					break;

				case NotifyCollectionChangedAction.Reset:
					Slider.Markers.Clear();
					break;
			}
		}
		#endregion

		#region Private Methods
		private void ResetSlider()
		{
			Slider.Start    = 0;
			Slider.Position = 0;
			Slider.End      = 10;
			Slider.Markers.Clear();

			Slider.IsPositionAdjustmentEnabled  = false;
			Slider.IsSelectionAdjustmentEnabled = false;
			Slider.IsZoomAdjustmentEnabled      = false;
		}

		private void ResetTimeDisplays()
		{
			TextBlockTimeElapsed.Text   = string.Empty;
			TextBlockTimeRemaining.Text = string.Empty;
			TextBlockTimeTemp.Text      = string.Empty;
		}

		private void SyncSliderToMedia()
		{
			Slider.Position = Slider.GetNearestSnapValue(
				(decimal) _player.PlaybackSession.Position.TotalSeconds,
				false,
				MediaSlider.SnapIntervals.Frame);
		}

		private void SyncTimeDisplaysToSlider()
		{
			TextBlockTimeElapsed.Text   = Slider.ToString("F", CultureInfo.InvariantCulture);
			TextBlockTimeRemaining.Text = Slider.ToString("FR", CultureInfo.InvariantCulture);
		}

		private void RefreshPlayerCommands()
		{
			Commands.PlayerPlayCommand.PlayerPlay.NotifyCanExecuteChanged();
			Commands.PlayerPauseCommand.PlayerPause.NotifyCanExecuteChanged();
			Commands.PlayerPreviousFrameCommand.PlayerPreviousFrame.NotifyCanExecuteChanged();
			Commands.PlayerNextFrameCommand.PlayerNextFrame.NotifyCanExecuteChanged();
			Commands.PlayerPreviousMarkerCommand.PlayerPreviousMarker.NotifyCanExecuteChanged();
			Commands.PlayerNextMarkerCommand.PlayerNextMarker.NotifyCanExecuteChanged();
			Commands.PlayerNewMarkerCommand.PlayerNewMarker.NotifyCanExecuteChanged();
			Commands.PlayerNewClipCommand.PlayerNewClip.NotifyCanExecuteChanged();
			Commands.PlayerFullscreenCommand.PlayerFullscreen.NotifyCanExecuteChanged();
			Commands.PlayerRateIncreaseCommand.PlayerRateIncrease.NotifyCanExecuteChanged();
			Commands.PlayerRateDecreaseCommand.PlayerRateDecrease.NotifyCanExecuteChanged();
			Commands.PlayerRateNormalCommand.PlayerRateNormal.NotifyCanExecuteChanged();
		}

		private Marker PreviousMarkerFromCurrentPosition()
		{
			if (!(PlayableSource is IMarkable markable))
				return null;

			var i = markable.Markers.Count - 1;
			while (i >= 0 && markable.Markers[i].Position >= Slider.Position - 0.5M)  // The "0.5" provides a one second dead zone
			{
				i--;
			}

			return i >= 0 ? markable.Markers[i] : null;
		}

		private Marker NextMarkerFromCurrentPosition()
		{
			if (!(PlayableSource is IMarkable markable))
				return null;

			var i = 0;
			while (i < markable.Markers.Count && markable.Markers[i].Position <= Slider.Position)
			{
				i++;
			}

			return i < markable.Markers.Count ? markable.Markers[i] : null;
		}
		#endregion
	}
}