using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using JLR.Utility.WinUI;
using JLR.Utility.WinUI.Controls;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;

namespace MediaBase.Controls
{
    public sealed partial class MediaEditor : UserControl
    {
        #region Fields
        private readonly MediaPlayer _player;
        private SoftwareBitmap _frameBitmap;
        private CanvasBitmap _currentFrame;
        private DispatcherQueueTimer _redrawTimer;
        private InputCursor _primaryCursor, _pointerOverCursor, _dragCursor;
        private Point _prevLeftMousePosition;
        private double _scaleFactor, _frameScale, _frameOffsetX, _frameOffsetY;
        private bool _isPointerOverFrame, _isPointerCapturedForFrame;
        private Rect _sourceRect, _destRect;
        private FollowMode _previousFollowMode;
        private bool _isScrubbing;
        #endregion

        #region Properties
        public Project ViewModel => (Project)DataContext;

        public EditorMode Mode
        {
            get => (EditorMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode",
                                        typeof(EditorMode),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(EditorMode.None,
                                            OnModeChanged));

        public MBMediaSource Source
        {
            get => (MBMediaSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source",
                                        typeof(MBMediaSource),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(null,
                                            OnSourceChanged));

        public int RefreshRate
        {
            get => (int)GetValue(RefreshRateProperty);
            set => SetValue(RefreshRateProperty, value);
        }

        public static readonly DependencyProperty RefreshRateProperty =
            DependencyProperty.Register("RefreshRate",
                                        typeof(int),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(App.RefreshRate,
                                            OnRefreshRateChanged));

        public InputSystemCursorShape PrimaryCursorShape
        {
            get => (InputSystemCursorShape)GetValue(PrimaryCursorShapeProperty);
            set => SetValue(PrimaryCursorShapeProperty, value);
        }

        public static readonly DependencyProperty PrimaryCursorShapeProperty =
            DependencyProperty.Register("PrimaryCursorShape",
                                        typeof(InputSystemCursorShape),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(InputSystemCursorShape.Hand,
                                            OnCursorShapeChanged));

        public InputSystemCursorShape PointerOverCursorShape
        {
            get => (InputSystemCursorShape)GetValue(PointerOverCursorShapeProperty);
            set => SetValue(PointerOverCursorShapeProperty, value);
        }

        public static readonly DependencyProperty PointerOverCursorShapeProperty =
            DependencyProperty.Register("PointerOverCursorShape",
                                        typeof(InputSystemCursorShape),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(InputSystemCursorShape.Cross,
                                            OnCursorShapeChanged));

        public InputSystemCursorShape PointerDragCursorShape
        {
            get => (InputSystemCursorShape)GetValue(PointerDragCursorShapeProperty);
            set => SetValue(PointerDragCursorShapeProperty, value);
        }

        public static readonly DependencyProperty PointerDragCursorShapeProperty =
            DependencyProperty.Register("PointerDragCursorShape",
                                        typeof(InputSystemCursorShape),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(InputSystemCursorShape.SizeAll,
                                            OnCursorShapeChanged));

        public Color TextOverlayColor
        {
            get => (Color)GetValue(TextOverlayColorProperty);
            set => SetValue(TextOverlayColorProperty, value);
        }

        public static readonly DependencyProperty TextOverlayColorProperty =
            DependencyProperty.Register("TextOverlayColor",
                                        typeof(Color),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(Colors.White));

        public string TextOverlayFontFamily
        {
            get => (string)GetValue(TextOverlayFontFamilyProperty);
            set => SetValue(TextOverlayFontFamilyProperty, value);
        }

        public static readonly DependencyProperty TextOverlayFontFamilyProperty =
            DependencyProperty.Register("TextOverlayFontFamily",
                                        typeof(string),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(null));

        public float TextOverlayFontSize
        {
            get => (float)GetValue(TextOverlayFontSizeProperty);
            set => SetValue(TextOverlayFontSizeProperty, value);
        }

        public static readonly DependencyProperty TextOverlayFontSizeProperty =
            DependencyProperty.Register("TextOverlayFontSize",
                                        typeof(float),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(14.0f));

        public FontStretch TextOverlayFontStretch
        {
            get => (FontStretch)GetValue(TextOverlayFontStretchProperty);
            set => SetValue(TextOverlayFontStretchProperty, value);
        }

        public static readonly DependencyProperty TextOverlayFontStretchProperty =
            DependencyProperty.Register("TextOverlayFontStretch",
                                        typeof(FontStretch),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(FontStretch.Normal));

        public FontStyle TextOverlayFontStyle
        {
            get => (FontStyle)GetValue(TextOverlayFontStyleProperty);
            set => SetValue(TextOverlayFontStyleProperty, value);
        }

        public static readonly DependencyProperty TextOverlayFontStyleProperty =
            DependencyProperty.Register("TextOverlayFontStyle",
                                        typeof(FontStyle),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(FontStyle.Normal));

        public FontWeight TextOverlayFontWeight
        {
            get => (FontWeight)GetValue(TextOverlayFontWeightProperty);
            set => SetValue(TextOverlayFontWeightProperty, value);
        }

        public static readonly DependencyProperty TextOverlayFontWeightProperty =
            DependencyProperty.Register("TextOverlayFontWeight",
                                        typeof(FontWeight),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(FontWeights.Bold));
        #endregion

        #region Constructor
        public MediaEditor()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<Project>();
            
            // Initialize media player
            _player = new MediaPlayer();
            _player.AutoPlay = false;
            _player.IsVideoFrameServerEnabled = true;
            _player.CommandManager.IsEnabled = false;

            _player.SourceChanged += Player_SourceChanged;
            _player.MediaOpened += Player_MediaOpened;
            _player.MediaEnded += Player_MediaEnded;
            _player.MediaFailed += Player_MediaFailed;
            _player.VideoFrameAvailable += Player_VideoFrameAvailable;
            _player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            _player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            _player.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;

            // Initialize redraw timer
            _redrawTimer = DispatcherQueue.CreateTimer();
            _redrawTimer.Interval = TimeSpan.FromTicks((int)(1.0 / RefreshRate * 10000000));
            _redrawTimer.IsRepeating = true;
            _redrawTimer.Tick += RedrawTimer_Tick;

            // Initialize Cursors
            _primaryCursor = InputSystemCursor.Create(PrimaryCursorShape);
            _pointerOverCursor = InputSystemCursor.Create(PointerOverCursorShape);
            _dragCursor = InputSystemCursor.Create(PointerDragCursorShape);

            RegisterMessages();
            InitializeCommands();
        }
        #endregion

        #region Dependency Property Callbacks
        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;

            if (editor.Source == null || editor.Mode == EditorMode.None)
            {
                editor.EditorCommandBar.Visibility = Visibility.Collapsed;
                editor.Timeline.Visibility = Visibility.Collapsed;
                return;
            }

            if (editor._player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing &&
                editor.Mode != EditorMode.View)
                editor._player.Pause();

            editor.EditorCommandBar.Visibility = Visibility.Visible;

            if (editor.Source.Duration == 0)
            {
                editor.Timeline.Visibility = Visibility.Collapsed;

                editor.PlayButton.Visibility = Visibility.Collapsed;
                editor.PauseButton.Visibility = Visibility.Collapsed;
                editor.PreviousFrameButton.Visibility = Visibility.Collapsed;
                editor.NextFrameButton.Visibility = Visibility.Collapsed;
                editor.PreviousMarkerButton.Visibility = Visibility.Collapsed;
                editor.NextMarkerButton.Visibility = Visibility.Collapsed;

                editor.TrimAndEditButtonSeparator.Visibility = Visibility.Collapsed;
                editor.ToggleActiveSelectionButton.Visibility = Visibility.Collapsed;
                editor.NewMarkerButton.Visibility = Visibility.Collapsed;
                editor.NewClipButton.Visibility = Visibility.Collapsed;
                editor.NewKeyframeButton.Visibility = Visibility.Collapsed;
                editor.CutSelectedButton.Visibility = Visibility.Collapsed;

                editor.PlaybackRateButtonSeparator.Visibility = Visibility.Collapsed;
                editor.PlaybackRateDecreaseButton.Visibility = Visibility.Collapsed;
                editor.PlaybackRateNormalButton.Visibility = Visibility.Collapsed;
                editor.PlaybackRateIncreaseButton.Visibility = Visibility.Collapsed;

                editor.ZoomAndPanButtonSeparator.Visibility = Visibility.Collapsed;
                editor.TimelineZoomOutButton.Visibility = Visibility.Collapsed;
                editor.TimelineZoomInButton.Visibility = Visibility.Collapsed;
                return;
            }

            editor.Timeline.Visibility = Visibility.Visible;

            editor.PlayButton.Visibility = Visibility.Visible;
            editor.PauseButton.Visibility = Visibility.Visible;
            editor.PreviousFrameButton.Visibility = Visibility.Visible;
            editor.NextFrameButton.Visibility = Visibility.Visible;
            editor.PreviousMarkerButton.Visibility = Visibility.Visible;
            editor.NextMarkerButton.Visibility = Visibility.Visible;

            editor.PlaybackRateButtonSeparator.Visibility = Visibility.Visible;
            editor.PlaybackRateDecreaseButton.Visibility = Visibility.Visible;
            editor.PlaybackRateNormalButton.Visibility = Visibility.Visible;
            editor.PlaybackRateIncreaseButton.Visibility = Visibility.Visible;

            editor.ZoomAndPanButtonSeparator.Visibility = Visibility.Visible;
            editor.TimelineZoomOutButton.Visibility = Visibility.Visible;
            editor.TimelineZoomInButton.Visibility = Visibility.Visible;

            switch(editor.Mode)
            {
                case EditorMode.Animate:
                    editor.TrimAndEditButtonSeparator.Visibility = Visibility.Visible;
                    editor.ToggleActiveSelectionButton.Visibility = Visibility.Collapsed;
                    editor.NewMarkerButton.Visibility = Visibility.Collapsed;
                    editor.NewClipButton.Visibility = Visibility.Collapsed;
                    editor.NewKeyframeButton.Visibility = Visibility.Visible;
                    editor.CutSelectedButton.Visibility = Visibility.Collapsed;
                    editor.Timeline.IsSelectionAdjustmentEnabled = false;
                    editor.Timeline.IsSelectionEnabled = false;
                    break;

                case EditorMode.Edit:
                    editor.TrimAndEditButtonSeparator.Visibility = Visibility.Visible;
                    editor.ToggleActiveSelectionButton.Visibility = Visibility.Visible;
                    editor.NewMarkerButton.Visibility = Visibility.Visible;
                    editor.NewClipButton.Visibility = Visibility.Visible;
                    editor.NewKeyframeButton.Visibility = Visibility.Collapsed;
                    editor.CutSelectedButton.Visibility = Visibility.Collapsed;
                    editor.Timeline.IsSelectionAdjustmentEnabled = true;
                    editor.Timeline.IsSelectionEnabled = true;
                    break;

                case EditorMode.Trim:
                    editor.TrimAndEditButtonSeparator.Visibility = Visibility.Visible;
                    editor.ToggleActiveSelectionButton.Visibility = Visibility.Visible;
                    editor.NewMarkerButton.Visibility = Visibility.Collapsed;
                    editor.NewClipButton.Visibility = Visibility.Collapsed;
                    editor.NewKeyframeButton.Visibility = Visibility.Collapsed;
                    editor.CutSelectedButton.Visibility = Visibility.Visible;
                    editor.Timeline.IsSelectionAdjustmentEnabled = true;
                    editor.Timeline.IsSelectionEnabled = true;
                    break;

                case EditorMode.View:
                    editor.TrimAndEditButtonSeparator.Visibility = Visibility.Collapsed;
                    editor.ToggleActiveSelectionButton.Visibility = Visibility.Collapsed;
                    editor.NewMarkerButton.Visibility = Visibility.Collapsed;
                    editor.NewClipButton.Visibility = Visibility.Collapsed;
                    editor.NewKeyframeButton.Visibility = Visibility.Collapsed;
                    editor.CutSelectedButton.Visibility = Visibility.Collapsed;
                    editor.Timeline.IsSelectionAdjustmentEnabled = false;
                    editor.Timeline.IsSelectionEnabled = false;
                    break;
            }
        }

        private static async void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;

            // Stop any current playback and halt all timers
            if (editor._player.PlaybackSession.PlaybackState is
                MediaPlaybackState.Opening or
                MediaPlaybackState.Buffering or
                MediaPlaybackState.Playing)
                editor._player.Pause();
            editor._redrawTimer.Stop();

            // Cleanup resources used by the previous media source (if needed)
            if (editor._player.Source is MediaSource oldSource && oldSource != null)
            {
                oldSource.Dispose();
                editor._player.Source = null;
            }

            if (editor._currentFrame != null)
            {
                editor._currentFrame.Dispose();
                editor._currentFrame = null;
            }

            // Reset image scaling
            editor._frameScale = 0;
            editor._frameOffsetX = 0;
            editor._frameOffsetY = 0;

            // Reset timeline
            editor.Timeline.Reset();

            // Update commands

            // Set editor mode and load new media source
            if (editor.Source == null)
            {
                editor.Mode = EditorMode.None;
            }
            else if (editor.Source.ContentType == MediaContentType.Image)
            {
                editor.Mode = EditorMode.View;
                editor.RefreshRate = App.RefreshRate;

                if (editor.Source.Duration > 0)
                    editor._player.Source = await editor.Source.GetMediaSourceAsync();
                else
                {
                    editor._currentFrame = await CanvasBitmap.LoadAsync(editor.SwapChainCanvas.SwapChain.Device,
                                                                        await ((MediaFile)editor.Source).File.OpenReadAsync());
                    editor.SetScaleToFit(editor._currentFrame.SizeInPixels.Width,
                                     editor._currentFrame.SizeInPixels.Height);
                    editor.ScaleCurrentFrame();
                    editor._redrawTimer.Start();
                }
            }
            else if (editor.Source.ContentType == MediaContentType.Video)
            {
                editor.Mode = EditorMode.View;
                editor.RefreshRate = (int)Math.Ceiling(editor.Source.FramesPerSecond);
                editor._player.Source = await editor.Source.GetMediaSourceAsync();
            }
        }

        private static void OnRefreshRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;

            var isRunning = editor._redrawTimer.IsRunning;
            if (isRunning)
                editor._redrawTimer.Stop();

            editor._redrawTimer.Interval = TimeSpan.FromTicks((int)(1.0 / editor.RefreshRate * 10000000));

            if (isRunning)
                editor._redrawTimer.Start();
        }

        private static void OnCursorShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;

            if (e.Property == PrimaryCursorShapeProperty)
            {
                editor._primaryCursor = InputSystemCursor.Create((InputSystemCursorShape)e.NewValue);
                if (!editor._isPointerCapturedForFrame && !editor._isPointerOverFrame)
                    editor.ProtectedCursor = editor._primaryCursor;
            }
            else if (e.Property == PointerDragCursorShapeProperty)
            {
                editor._dragCursor = InputSystemCursor.Create((InputSystemCursorShape)e.NewValue);
                if (editor._isPointerCapturedForFrame)
                    editor.ProtectedCursor = editor._dragCursor;
            }
            else if (e.Property == PointerOverCursorShapeProperty)
            {
                editor._pointerOverCursor = InputSystemCursor.Create((InputSystemCursorShape)e.NewValue);
                if (editor._isPointerOverFrame)
                    editor.ProtectedCursor = editor._pointerOverCursor;
            }
        }
        #endregion

        #region Event Handlers (UserControl)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize timeline
            Timeline.PositionChanged += Timeline_PositionChanged;
            Timeline.SelectionChanged += Timeline_SelectionChanged;
            Timeline.ZoomChanged += Timeline_ZoomChanged;
            Timeline.SelectedMarkerChanged += Timeline_SelectedMarkerChanged;
            Timeline.SelectionListChanged += Timeline_SelectionListChanged;
            Timeline.TrackCountChanged += Timeline_TrackCountChanged;

            Timeline.PositionDragStarted += Timeline_PositionDragStarted;
            Timeline.PositionDragCompleted += Timeline_PositionDragCompleted;
            Timeline.SelectionDragStarted += Timeline_SelectionDragStarted;
            Timeline.SelectionDragCompleted += Timeline_SelectionDragCompleted;
            Timeline.ZoomDragStarted += Timeline_ZoomDragStarted;
            Timeline.ZoomDragCompleted += Timeline_ZoomDragCompleted;

            // Initialize swap chain
            SwapChainCanvas.SwapChain = new CanvasSwapChain(CanvasDevice.GetSharedDevice(),
                                                            (float)SwapChainCanvas.ActualWidth,
                                                            (float)SwapChainCanvas.ActualHeight,
                                                            PInvoke.User32.GetDpiForWindow(App.WindowHandle));
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            SwapChainCanvas.RemoveFromVisualTree();
            SwapChainCanvas.SwapChain = null;
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
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
            {
                if (_frameBitmap == null)
                {
                    _frameBitmap = new SoftwareBitmap(BitmapPixelFormat.Rgba8,
                                                      (int)SwapChainCanvas.ActualWidth,
                                                      (int)SwapChainCanvas.ActualHeight,
                                                      BitmapAlphaMode.Ignore);
                }

                if (_currentFrame != null)
                {
                    _currentFrame.Dispose();
                    _currentFrame = null;
                }

                _currentFrame = CanvasBitmap.CreateFromSoftwareBitmap(SwapChainCanvas.SwapChain.Device, _frameBitmap);
                _player.CopyFrameToVideoSurface(_currentFrame);
            });
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

        #region Event Handlers (Timers)
        private void RedrawTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            if (_currentFrame == null)
                return;

            using var ds = SwapChainCanvas.SwapChain.CreateDrawingSession(Colors.Black);
            using var positionTextFormat = new CanvasTextFormat
            {
                FontFamily = TextOverlayFontFamily,
                FontSize = TextOverlayFontSize,
                FontStretch = TextOverlayFontStretch,
                FontStyle = TextOverlayFontStyle,
                FontWeight = TextOverlayFontWeight,
                HorizontalAlignment = CanvasHorizontalAlignment.Left
            };

            var drawAreaWidth = (float)RenderAreaBorder.ActualWidth;
            var drawAreaHeight = (float)RenderAreaBorder.ActualHeight;
            ScaleCurrentFrame();

            ds.DrawImage(_currentFrame, _destRect, _sourceRect);
            if (Source.Duration > 0)
                ds.DrawText(_player.PlaybackSession.Position.TotalSeconds.ToTimecodeString(RefreshRate),
                            5, 5, TextOverlayColor, positionTextFormat);

            SwapChainCanvas.SwapChain.Present();
        }
        #endregion

        #region Event Handlers (SwapChain)
        private void SwapChainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _frameBitmap = null;
            SwapChainCanvas.SwapChain?.ResizeBuffers(e.NewSize);
        }
        #endregion

        #region Event Handlers (Timeline)
        private void Timeline_PositionChanged(object sender, decimal e)
        {
            
        }

        private void Timeline_SelectionChanged(object sender, (decimal start, decimal end, bool isEnabled) e)
        {
            
        }

        private void Timeline_ZoomChanged(object sender, (decimal start, decimal end) e)
        {
            
        }

        private void Timeline_SelectedMarkerChanged(object sender, ITimelineMarker e)
        {
            
        }

        private void Timeline_SelectionListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            
        }

        private void Timeline_TrackCountChanged(object sender, int e)
        {
            
        }

        private void Timeline_PositionDragStarted(object sender, EventArgs e)
        {
            
        }

        private void Timeline_PositionDragCompleted(object sender, EventArgs e)
        {
            
        }

        private void Timeline_SelectionDragStarted(object sender, EventArgs e)
        {
            
        }

        private void Timeline_SelectionDragCompleted(object sender, EventArgs e)
        {
            
        }

        private void Timeline_ZoomDragStarted(object sender, EventArgs e)
        {
            
        }

        private void Timeline_ZoomDragCompleted(object sender, EventArgs e)
        {
            
        }
        #endregion

        #region Event Handlers (Pointer)
        private void RenderAreaBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_isPointerCapturedForFrame)
                return;

            ProtectedCursor = _primaryCursor;
        }

        private void RenderAreaBorder_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverFrame = false;
        }

        private void RenderAreaBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(RenderAreaBorder);

            if (_destRect.Contains(point.Position))
            {
                var isCtrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                                                       .HasFlag(CoreVirtualKeyStates.Down);

                if (point.Properties.IsLeftButtonPressed && !isCtrlPressed)
                {
                    _prevLeftMousePosition = point.Position;
                    _isPointerCapturedForFrame = RenderAreaBorder.CapturePointer(e.Pointer);
                    ProtectedCursor = _dragCursor;
                }
                else if (point.Properties.IsLeftButtonPressed && isCtrlPressed)
                {
                    var centerX = RenderAreaBorder.ActualWidth / 2;
                    var centerY = RenderAreaBorder.ActualHeight / 2;
                    _frameOffsetX += centerX - point.Position.X;
                    _frameOffsetY -= centerY - point.Position.Y;
                }
            }
        }

        private void RenderAreaBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(RenderAreaBorder);

            if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                RenderAreaBorder.ReleasePointerCapture(e.Pointer);
                ProtectedCursor = _isPointerOverFrame ? _pointerOverCursor : _primaryCursor;
            }
        }

        private void RenderAreaBorder_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _isPointerCapturedForFrame = false;
        }

        private void RenderAreaBorder_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            _isPointerCapturedForFrame = false;
            _isPointerOverFrame = false;
        }

        private void RenderAreaBorder_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(RenderAreaBorder);

            _isPointerOverFrame = _destRect.Contains(point.Position);
            if (_isPointerOverFrame && ProtectedCursor != _pointerOverCursor)
                ProtectedCursor = _pointerOverCursor;
            else if (!_isPointerOverFrame && ProtectedCursor != _primaryCursor)
                ProtectedCursor = _primaryCursor;

            if (!point.Properties.IsLeftButtonPressed || !_isPointerCapturedForFrame)
                return;

            if (ProtectedCursor != _dragCursor)
                ProtectedCursor = _dragCursor;

            _frameOffsetX += point.Position.X - _prevLeftMousePosition.X;
            _frameOffsetY -= point.Position.Y - _prevLeftMousePosition.Y;

            _prevLeftMousePosition = point.Position;
        }

        private void RenderAreaBorder_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (_isPointerCapturedForFrame)
                return;

            _frameScale += e.GetCurrentPoint(RenderAreaBorder).Properties.MouseWheelDelta / 120;
        }
        #endregion

        #region Event Handlers (Commands - CanExecuteRequested)
        private void EditorPlayCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPauseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPreviousFrameCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorNextFrameCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPreviousMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorNextMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorToggleActiveSelectionCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorCutSelectedCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorNewMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorNewClipCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorNewKeyframeCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPlaybackRateDecreaseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPlaybackRateIncreaseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPlaybackRateNormalCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorCenterFrameCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorFrameZoomFitCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorFrameZoomFullCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorTimelineZoomOutCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorTimelineZoomInCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            
        }
        #endregion

        #region Event Handlers (Commands - ExecuteRequested)
        private void EditorPlayCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPauseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPreviousFrameCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorNextFrameCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPreviousMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorNextMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorToggleActiveSelectionCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorCutSelectedCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorNewMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorNewClipCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorNewKeyframeCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPlaybackRateDecreaseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPlaybackRateIncreaseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPlaybackRateNormalCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorCenterFrameCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorFrameZoomFitCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorFrameZoomFullCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorTimelineZoomOutCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorTimelineZoomInCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            ViewModel.EditorPlayCommand.CanExecuteRequested +=
                EditorPlayCommand_CanExecuteRequested;
            ViewModel.EditorPlayCommand.ExecuteRequested +=
                EditorPlayCommand_ExecuteRequested;

            ViewModel.EditorPauseCommand.CanExecuteRequested +=
                EditorPauseCommand_CanExecuteRequested;
            ViewModel.EditorPauseCommand.ExecuteRequested +=
                EditorPauseCommand_ExecuteRequested;

            ViewModel.EditorPreviousFrameCommand.CanExecuteRequested +=
                EditorPreviousFrameCommand_CanExecuteRequested;
            ViewModel.EditorPreviousFrameCommand.ExecuteRequested +=
                EditorPreviousFrameCommand_ExecuteRequested;

            ViewModel.EditorNextFrameCommand.CanExecuteRequested +=
                EditorNextFrameCommand_CanExecuteRequested;
            ViewModel.EditorNextFrameCommand.ExecuteRequested +=
                EditorNextFrameCommand_ExecuteRequested;

            ViewModel.EditorPreviousMarkerCommand.CanExecuteRequested +=
                EditorPreviousMarkerCommand_CanExecuteRequested;
            ViewModel.EditorPreviousMarkerCommand.ExecuteRequested +=
                EditorPreviousMarkerCommand_ExecuteRequested;

            ViewModel.EditorNextMarkerCommand.CanExecuteRequested +=
                EditorNextMarkerCommand_CanExecuteRequested;
            ViewModel.EditorNextMarkerCommand.ExecuteRequested +=
                EditorNextMarkerCommand_ExecuteRequested;

            ViewModel.EditorToggleActiveSelectionCommand.CanExecuteRequested +=
                EditorToggleActiveSelectionCommand_CanExecuteRequested;
            ViewModel.EditorToggleActiveSelectionCommand.ExecuteRequested +=
                EditorToggleActiveSelectionCommand_ExecuteRequested;

            ViewModel.EditorCutSelectedCommand.CanExecuteRequested +=
                EditorCutSelectedCommand_CanExecuteRequested;
            ViewModel.EditorCutSelectedCommand.ExecuteRequested +=
                EditorCutSelectedCommand_ExecuteRequested;

            ViewModel.EditorNewMarkerCommand.CanExecuteRequested +=
                EditorNewMarkerCommand_CanExecuteRequested;
            ViewModel.EditorNewMarkerCommand.ExecuteRequested +=
                EditorNewMarkerCommand_ExecuteRequested;

            ViewModel.EditorNewClipCommand.CanExecuteRequested +=
                EditorNewClipCommand_CanExecuteRequested;
            ViewModel.EditorNewClipCommand.ExecuteRequested +=
                EditorNewClipCommand_ExecuteRequested;

            ViewModel.EditorNewKeyframeCommand.CanExecuteRequested +=
                EditorNewKeyframeCommand_CanExecuteRequested;
            ViewModel.EditorNewKeyframeCommand.ExecuteRequested +=
                EditorNewKeyframeCommand_ExecuteRequested;

            ViewModel.EditorPlaybackRateDecreaseCommand.CanExecuteRequested +=
                EditorPlaybackRateDecreaseCommand_CanExecuteRequested;
            ViewModel.EditorPlaybackRateDecreaseCommand.ExecuteRequested +=
                EditorPlaybackRateDecreaseCommand_ExecuteRequested;

            ViewModel.EditorPlaybackRateIncreaseCommand.CanExecuteRequested +=
                EditorPlaybackRateIncreaseCommand_CanExecuteRequested;
            ViewModel.EditorPlaybackRateIncreaseCommand.ExecuteRequested +=
                EditorPlaybackRateIncreaseCommand_ExecuteRequested;

            ViewModel.EditorPlaybackRateNormalCommand.CanExecuteRequested +=
                EditorPlaybackRateNormalCommand_CanExecuteRequested;
            ViewModel.EditorPlaybackRateNormalCommand.ExecuteRequested +=
                EditorPlaybackRateNormalCommand_ExecuteRequested;

            ViewModel.EditorCenterFrameCommand.CanExecuteRequested +=
                EditorCenterFrameCommand_CanExecuteRequested;
            ViewModel.EditorCenterFrameCommand.ExecuteRequested +=
                EditorCenterFrameCommand_ExecuteRequested;

            ViewModel.EditorFrameZoomFitCommand.CanExecuteRequested +=
                EditorFrameZoomFitCommand_CanExecuteRequested;
            ViewModel.EditorFrameZoomFitCommand.ExecuteRequested +=
                EditorFrameZoomFitCommand_ExecuteRequested;

            ViewModel.EditorFrameZoomFullCommand.CanExecuteRequested +=
                EditorFrameZoomFullCommand_CanExecuteRequested;
            ViewModel.EditorFrameZoomFullCommand.ExecuteRequested +=
                EditorFrameZoomFullCommand_ExecuteRequested;

            ViewModel.EditorTimelineZoomOutCommand.CanExecuteRequested +=
                EditorTimelineZoomOutCommand_CanExecuteRequested;
            ViewModel.EditorTimelineZoomOutCommand.ExecuteRequested +=
                EditorTimelineZoomOutCommand_ExecuteRequested;

            ViewModel.EditorTimelineZoomInCommand.CanExecuteRequested +=
                EditorTimelineZoomInCommand_CanExecuteRequested;
            ViewModel.EditorTimelineZoomInCommand.ExecuteRequested +=
                EditorTimelineZoomInCommand_ExecuteRequested;
        }

        private void RegisterMessages()
        {
            
        }

        private void SetScaleToFit(uint widthInPixels, uint heightInPixels)
        {
            var destWidth = SwapChainCanvas.SwapChain.SizeInPixels.Width;
            var destHeight = SwapChainCanvas.SwapChain.SizeInPixels.Height;
            var scaleW = 10 * Math.Log((double)destWidth / widthInPixels) / Math.Log(2.0);
            var scaleH = 10 * Math.Log((double)destHeight / heightInPixels) / Math.Log(2.0);
            _frameScale = Math.Min(scaleW, scaleH);
        }

        private void ScaleCurrentFrame()
        {
            var sourceWidth = _currentFrame.SizeInPixels.Width;
            var sourceHeight = _currentFrame.SizeInPixels.Height;
            var destWidth = SwapChainCanvas.SwapChain.SizeInPixels.Width;
            var destHeight = SwapChainCanvas.SwapChain.SizeInPixels.Height;

            _scaleFactor = Math.Pow(2, 0.1 * _frameScale);
            var scaledSourceWidth = Math.Round(sourceWidth * _scaleFactor, 6);
            if (scaledSourceWidth <= destWidth)
            {
                _sourceRect.Width = sourceWidth;
                _sourceRect.X = 0;
                _destRect.Width = scaledSourceWidth;
                _destRect.X = ((destWidth - scaledSourceWidth) / 2) + _frameOffsetX;
            }
            else
            {
                _sourceRect.Width = destWidth / _scaleFactor;
                _sourceRect.X = ((sourceWidth - _sourceRect.Width) / 2) - (_frameOffsetX / _scaleFactor);
                _destRect.Width = destWidth;
                _destRect.X = 0;

                if (_sourceRect.X < 0)
                {
                    _destRect.X = -(_sourceRect.X * _scaleFactor);
                    _sourceRect.X = 0;
                }
                else if (_sourceRect.Right > sourceWidth)
                {
                    _destRect.X = -((_sourceRect.Right - sourceWidth) * _scaleFactor);
                    _sourceRect.X = sourceWidth - _sourceRect.Width;
                }
            }

            var scaledSourceHeight = Math.Round(sourceHeight * _scaleFactor, 6);
            if (scaledSourceHeight <= destHeight)
            {
                _sourceRect.Height = sourceHeight;
                _sourceRect.Y = 0;
                _destRect.Height = scaledSourceHeight;
                _destRect.Y = ((destHeight - scaledSourceHeight) / 2) - _frameOffsetY;
            }
            else
            {
                _sourceRect.Height = destHeight / _scaleFactor;
                _sourceRect.Y = ((sourceHeight - _sourceRect.Height) / 2) + (_frameOffsetY / _scaleFactor);
                _destRect.Height = destHeight;
                _destRect.Y = 0;

                if (_sourceRect.Y < 0)
                {
                    _destRect.Y = -(_sourceRect.Y * _scaleFactor);
                    _sourceRect.Y = 0;
                }
                else if (_sourceRect.Bottom > sourceHeight)
                {
                    _destRect.Y = -((_sourceRect.Bottom - sourceHeight) * _scaleFactor);
                    _sourceRect.Y = sourceHeight - _sourceRect.Height;
                }
            }
        }
        #endregion
    }
}