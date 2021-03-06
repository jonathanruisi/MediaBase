using System;
using System.Diagnostics;
using System.Linq;

using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using ColorCode.Common;

using JLR.Utility.UWP.Controls;
using JLR.Utility.UWP.Dialogs;

namespace MediaBase
{
	public sealed partial class MediaPlayerPage
	{
		#region Private Methods
		private void InitializeCommands()
		{
			// PlayerPlayCommand
			Commands.PlayerPlayCommand.PlayerPlay.CanExecuteRequested +=
				PlayerPlay_CanExecuteRequested;
			Commands.PlayerPlayCommand.PlayerPlay.ExecuteRequested +=
				PlayerPlay_ExecuteRequested;
			ButtonPlayPause.Command = Commands.PlayerPlayCommand.PlayerPlay;

			// PlayerPauseCommand
			Commands.PlayerPauseCommand.PlayerPause.CanExecuteRequested +=
				PlayerPause_CanExecuteRequested;
			Commands.PlayerPauseCommand.PlayerPause.ExecuteRequested +=
				PlayerPause_ExecuteRequested;

			// PlayerPreviousFrameCommand
			Commands.PlayerPreviousFrameCommand.PlayerPreviousFrame.CanExecuteRequested +=
				PlayerPreviousFrame_CanExecuteRequested;
			Commands.PlayerPreviousFrameCommand.PlayerPreviousFrame.ExecuteRequested +=
				PlayerPreviousFrame_ExecuteRequested;
			ButtonPreviousFrame.Command = Commands.PlayerPreviousFrameCommand.PlayerPreviousFrame;

			// PlayerNextFrameCommand
			Commands.PlayerNextFrameCommand.PlayerNextFrame.CanExecuteRequested +=
				PlayerNextFrame_CanExecuteRequested;
			Commands.PlayerNextFrameCommand.PlayerNextFrame.ExecuteRequested +=
				PlayerNextFrame_ExecuteRequested;
			ButtonNextFrame.Command = Commands.PlayerNextFrameCommand.PlayerNextFrame;

			// PlayerPreviousMarkerCommand
			Commands.PlayerPreviousMarkerCommand.PlayerPreviousMarker.CanExecuteRequested +=
				PlayerPreviousMarker_CanExecuteRequested;
			Commands.PlayerPreviousMarkerCommand.PlayerPreviousMarker.ExecuteRequested +=
				PlayerPreviousMarker_ExecuteRequested;
			ButtonPreviousMarker.Command = Commands.PlayerPreviousMarkerCommand.PlayerPreviousMarker;

			// PlayerNextMarkerCommand
			Commands.PlayerNextMarkerCommand.PlayerNextMarker.CanExecuteRequested +=
				PlayerNextMarker_CanExecuteRequested;
			Commands.PlayerNextMarkerCommand.PlayerNextMarker.ExecuteRequested +=
				PlayerNextMarker_ExecuteRequested;
			ButtonNextMarker.Command = Commands.PlayerNextMarkerCommand.PlayerNextMarker;

			// PlayerNewMarkerCommand
			Commands.PlayerNewMarkerCommand.PlayerNewMarker.CanExecuteRequested +=
				PlayerNewMarker_CanExecuteRequested;
			Commands.PlayerNewMarkerCommand.PlayerNewMarker.ExecuteRequested +=
				PlayerNewMarker_ExecuteRequested;
			ButtonNewMarker.Command = Commands.PlayerNewMarkerCommand.PlayerNewMarker;

			// PlayerNewClipCommand
			Commands.PlayerNewClipCommand.PlayerNewClip.CanExecuteRequested +=
				PlayerNewClip_CanExecuteRequested;
			Commands.PlayerNewClipCommand.PlayerNewClip.ExecuteRequested +=
				PlayerNewClip_ExecuteRequested;
			ButtonNewClip.Command = Commands.PlayerNewClipCommand.PlayerNewClip;

			// PlayerFullscreenCommand
			Commands.PlayerFullscreenCommand.PlayerFullscreen.CanExecuteRequested +=
				PlayerFullscreen_CanExecuteRequested;
			Commands.PlayerFullscreenCommand.PlayerFullscreen.ExecuteRequested +=
				PlayerFullscreen_ExecuteRequested;
			ButtonFullscreen.Command = Commands.PlayerFullscreenCommand.PlayerFullscreen;

			// PlayerRateIncreaseCommand
			Commands.PlayerRateIncreaseCommand.PlayerRateIncrease.CanExecuteRequested +=
				PlayerRateIncrease_CanExecuteRequested;
			Commands.PlayerRateIncreaseCommand.PlayerRateIncrease.ExecuteRequested +=
				PlayerRateIncrease_ExecuteRequested;
			ButtonIncreaseRate.Command = Commands.PlayerRateIncreaseCommand.PlayerRateIncrease;

			// PlayerRateDecreaseCommand
			Commands.PlayerRateDecreaseCommand.PlayerRateDecrease.CanExecuteRequested +=
				PlayerRateDecrease_CanExecuteRequested;
			Commands.PlayerRateDecreaseCommand.PlayerRateDecrease.ExecuteRequested +=
				PlayerRateDecrease_ExecuteRequested;
			ButtonDecreaseRate.Command = Commands.PlayerRateDecreaseCommand.PlayerRateDecrease;

			// PlayerRateNormalCommand
			Commands.PlayerRateNormalCommand.PlayerRateNormal.CanExecuteRequested +=
				PlayerRateNormal_CanExecuteRequested;
			Commands.PlayerRateNormalCommand.PlayerRateNormal.ExecuteRequested +=
				PlayerRateNormal_ExecuteRequested;
			ButtonNormalRate.Command = Commands.PlayerRateNormalCommand.PlayerRateNormal;

			// PlayerZoomInTimelimeCommand
			Commands.PlayerZoomInTimelimeCommand.PlayerZoomInTimelime.CanExecuteRequested +=
				PlayerZoomInTimelime_CanExecuteRequested;
			Commands.PlayerZoomInTimelimeCommand.PlayerZoomInTimelime.ExecuteRequested +=
				PlayerZoomInTimelime_ExecuteRequested;
			ButtonTimelineZoomIn.Command = Commands.PlayerZoomInTimelimeCommand.PlayerZoomInTimelime;

			// PlayerZoomOutTimelineCommand
			Commands.PlayerZoomOutTimelimeCommand.PlayerZoomOutTimelime.CanExecuteRequested +=
				PlayerZoomOutTimelime_CanExecuteRequested;
			Commands.PlayerZoomOutTimelimeCommand.PlayerZoomOutTimelime.ExecuteRequested +=
				PlayerZoomOutTimelime_ExecuteRequested;
			ButtonTimelineZoomOut.Command = Commands.PlayerZoomOutTimelimeCommand.PlayerZoomOutTimelime;
		}
		#endregion

		#region Event Handlers (CanExecute)
		private void PlayerPlay_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				_player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused;
		}

		private void PlayerPause_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
		}

		private void PlayerPreviousFrame_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				_player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused &&
				_player.PlaybackSession.Position >= Slider.FrameDuration;
		}

		private void PlayerNextFrame_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				_player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused &&
				_player.PlaybackSession.NaturalDuration - _player.PlaybackSession.Position >
				Slider.FrameDuration;
		}

		private void PlayerPreviousMarker_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				(_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
				 _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused) &&
				PreviousMarkerFromCurrentPosition() != null;
		}

		private void PlayerNextMarker_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				(_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
				 _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused) &&
				NextMarkerFromCurrentPosition() != null;
		}

		private void PlayerNewMarker_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				(_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
				 _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused);
		}

		private void PlayerNewClip_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				Slider.SelectionStart != null &&
				Slider.SelectionStart != Slider.SelectionEnd &&
				(_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
				 _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused);
		}

		private void PlayerFullscreen_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				(_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
				 _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused);
		}

		private void PlayerRateIncrease_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing &&
				_player.PlaybackSession.PlaybackRate < 4.0;
		}

		private void PlayerRateDecrease_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing &&
				_player.PlaybackSession.PlaybackRate > 0.5;
		}

		private void PlayerRateNormal_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened &&
				_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
		}

		private void PlayerZoomInTimelime_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened;
		}

		private void PlayerZoomOutTimelime_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
		{
			args.CanExecute =
				IsLoaded &&
				_player.Source != null &&
				_player.Source is MediaSource source &&
				source.IsOpen &&
				source.State == MediaSourceState.Opened;
		}
		#endregion

		#region Event Handlers (Execute)
		private void PlayerPlay_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			_player.Play();
		}

		private void PlayerPause_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			_player.Pause();
		}

		private void PlayerPreviousFrame_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			var rate = _player.PlaybackSession.PlaybackRate;

			_player.PlaybackSession.PlaybackRate =  0;
			_player.PlaybackSession.Position     -= Slider.FrameDuration;
			_player.PlaybackSession.PlaybackRate =  rate;
		}

		private void PlayerNextFrame_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			var rate = _player.PlaybackSession.PlaybackRate;

			_player.PlaybackSession.PlaybackRate =  0;
			_player.PlaybackSession.Position     += Slider.FrameDuration;
			_player.PlaybackSession.PlaybackRate =  rate;
		}

		private void PlayerPreviousMarker_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			var marker = PreviousMarkerFromCurrentPosition();
			if (marker != null)
			{
				if (SelectedMarker == marker)
					SelectedMarker = null;
				SelectedMarker = marker;
			}
		}

		private void PlayerNextMarker_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			var marker = NextMarkerFromCurrentPosition();
			if (marker != null)
			{
				if (SelectedMarker == marker)
					SelectedMarker = null;
				SelectedMarker = marker;
			}
		}

		private async void PlayerNewMarker_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			if (!(PlayableSource is IMarkable markable))
				return;

			SelectedMarker = null;

			var dlg = new TextPromptDialog
			{
				Title             = "New Marker",
				PromptText        = "Enter a name for the marker",
				PrimaryButtonText = "OK",
				CloseButtonText   = "Cancel"
			};

			var dlgResult = await dlg.ShowAsync();
			if (dlgResult == ContentDialogResult.Primary)
			{
				var marker = new Marker
				{
					Name     = dlg.Text,
					Position = Slider.Position,
					Duration = 0
				};

				var insertionIndex = 0;
				while (insertionIndex < markable.Markers.Count &&
					   markable.Markers[insertionIndex].Position <= marker.Position)
				{
					insertionIndex++;
				}

				markable.Markers.Insert(insertionIndex, marker);
			}
		}

		private async void PlayerNewClip_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			if (!(PlayableSource is IMarkable markable) ||
				Slider.SelectionStart == null ||
				Slider.SelectionEnd == null)
				return;

			SelectedMarker = null;

			var dlg = new TextPromptDialog
			{
				Title             = "New Clip",
				PromptText        = "Enter a name for the clip",
				PrimaryButtonText = "OK",
				CloseButtonText   = "Cancel"
			};

			var dlgResult = await dlg.ShowAsync();
			if (dlgResult == ContentDialogResult.Primary)
			{
				var marker = new Marker
				{
					Name     = dlg.Text,
					Position = (decimal) Slider.SelectionStart,
					Duration = (decimal) Slider.SelectionEnd - (decimal) Slider.SelectionStart
				};

				var insertionIndex = 0;
				while (insertionIndex < markable.Markers.Count &&
					   markable.Markers[insertionIndex].Position <= marker.Position)
				{
					insertionIndex++;
				}

				markable.Markers.Insert(insertionIndex, marker);
			}
		}

		private void PlayerFullscreen_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			var appView = ApplicationView.GetForCurrentView();
			if (appView.IsFullScreenMode)
				appView.ExitFullScreenMode();
			else
				appView.TryEnterFullScreenMode();
		}

		private void PlayerRateIncrease_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			if (_player.PlaybackSession.PlaybackRate < 4.0)
				_player.PlaybackSession.PlaybackRate += 0.5;
		}

		private void PlayerRateDecrease_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			if (_player.PlaybackSession.PlaybackRate > 0.5)
				_player.PlaybackSession.PlaybackRate -= 0.5;
		}

		private void PlayerRateNormal_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			_player.PlaybackSession.PlaybackRate = 1.0;
		}

		private void PlayerZoomInTimelime_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			Slider.VisibleDuration *= 0.5;
		}

		private void PlayerZoomOutTimelime_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
		{
			Slider.VisibleDuration *= 2;
		}
		#endregion
	}
}