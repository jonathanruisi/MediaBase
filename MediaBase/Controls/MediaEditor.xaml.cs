using System;
using System.Linq;

using JLR.Utility.WinUI.Controls;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.Foundation;
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
        private readonly DispatcherQueueTimer _redrawTimer;
        private SoftwareBitmap _frameSizingBitmap;
        private CanvasBitmap _frameBitmap;
        private InputCursor _primaryCursor, _hoverCursor, _dragCursor;
        private bool _isPointerOverFrame, _isPointerCapturedForFrame, _isScrubbing, _isImageAnimationPlaying;
        private Point _prevLeftMousePosition;
        private double _scaleFactor, _playbackRate;
        private Rect _sourceRect, _destRect;
        private FollowMode _prevFollowMode;
        private int _framesPerSecond, _imageAnimationFrame;
        #endregion

        #region Properties
        public Project ViewModel => (Project)DataContext;

        public int FramesPerSecond
        {
            get => _framesPerSecond;
            private set
            {
                _framesPerSecond = value;
                Timeline.FramesPerSecond = _framesPerSecond;
                _redrawTimer.Interval = TimeSpan.FromTicks((int)(1.0 / _framesPerSecond * 10000000));
            }
        }

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

        public Marker SelectedMarker
        {
            get => (Marker)GetValue(SelectedMarkerProperty);
            set => SetValue(SelectedMarkerProperty, value);
        }

        public static readonly DependencyProperty SelectedMarkerProperty =
            DependencyProperty.Register("SelectedMarker",
                                        typeof(Marker),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(null,
                                            OnSelectedMarkerChanged));

        public double FrameScale
        {
            get => (double)GetValue(FrameScaleProperty);
            set => SetValue(FrameScaleProperty, value);
        }

        public static readonly DependencyProperty FrameScaleProperty =
            DependencyProperty.Register("FrameScale",
                                        typeof(double),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(0.0));

        public double FrameOffsetX
        {
            get => (double)GetValue(FrameOffsetXProperty);
            set => SetValue(FrameOffsetXProperty, value);
        }

        public static readonly DependencyProperty FrameOffsetXProperty =
            DependencyProperty.Register("FrameOffsetX",
                                        typeof(double),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(0.0,
                                            OnFrameOffsetChanged));

        public double FrameOffsetY
        {
            get => (double)GetValue(FrameOffsetYProperty);
            set => SetValue(FrameOffsetYProperty, value);
        }

        public static readonly DependencyProperty FrameOffsetYProperty =
            DependencyProperty.Register("FrameOffsetY",
                                        typeof(double),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(0.0,
                                            OnFrameOffsetChanged));

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

        public InputSystemCursorShape HoverCursorShape
        {
            get => (InputSystemCursorShape)GetValue(HoverCursorShapeProperty);
            set => SetValue(HoverCursorShapeProperty, value);
        }

        public static readonly DependencyProperty HoverCursorShapeProperty =
            DependencyProperty.Register("HoverCursorShape",
                                        typeof(InputSystemCursorShape),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(InputSystemCursorShape.Cross,
                                            OnCursorShapeChanged));

        public InputSystemCursorShape DragCursorShape
        {
            get => (InputSystemCursorShape)GetValue(DragCursorShapeProperty);
            set => SetValue(DragCursorShapeProperty, value);
        }

        public static readonly DependencyProperty DragCursorShapeProperty =
            DependencyProperty.Register("DragCursorShape",
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
                                        new PropertyMetadata(24.0f));

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

            _player.MediaOpened += Player_MediaOpened;
            _player.MediaEnded += Player_MediaEnded;
            _player.MediaFailed += Player_MediaFailed;
            _player.VideoFrameAvailable += Player_VideoFrameAvailable;
            _player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            _player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            _player.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;
            _player.PlaybackSession.PlaybackRateChanged += PlaybackSession_PlaybackRateChanged;

            // Initialize frame timer
            _redrawTimer = DispatcherQueue.CreateTimer();
            _redrawTimer.IsRepeating = true;
            _redrawTimer.Tick += RedrawTimer_Tick;

            // Initialize cursors
            _primaryCursor = InputSystemCursor.Create(PrimaryCursorShape);
            _hoverCursor = InputSystemCursor.Create(HoverCursorShape);
            _dragCursor = InputSystemCursor.Create(DragCursorShape);
            
            InitializeCommands();
            RegisterMessages();
        }
        #endregion

        #region Dependency Property Callbacks
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
            editor._isImageAnimationPlaying = false;
            editor._imageAnimationFrame = 0;

            // De-select markers/keyframes
            editor.ViewModel.SelectedMarker = null;

            // Cleanup resources used by the previous media source (if needed)
            if (editor._player.Source is MediaSource oldSource && oldSource != null)
            {
                oldSource.Dispose();
                editor._player.Source = null;
            }

            if (editor._frameBitmap != null)
            {
                editor._frameBitmap.Dispose();
                editor._frameBitmap = null;
            }

            // Reset frame position and scale
            editor.FrameScale = 0;
            editor.FrameOffsetX = 0;
            editor.FrameOffsetY = 0;

            // Reset timeline
            editor.Timeline.Reset();

            if (editor.Source == null)
            {
                editor.FramesPerSecond = App.RefreshRate;
                editor.RefreshCommandStates();
                return;
            }

            if (editor.Source.ContentType == MediaContentType.Image)
            {
                editor.FramesPerSecond = App.RefreshRate;
                editor._frameBitmap = await CanvasBitmap.LoadAsync(editor.SwapChainCanvas.SwapChain.Device,
                    await ((MediaFile)editor.Source).File.OpenReadAsync());
                editor.ScaleFrameToFit(editor._frameBitmap.SizeInPixels.Width,
                                       editor._frameBitmap.SizeInPixels.Height);
                editor.ApplyFramePositionAndScale();
                editor._redrawTimer.Start();
            }
            else if (editor.Source.ContentType == MediaContentType.Video)
            {
                editor.FramesPerSecond = (int)Math.Ceiling(editor.Source.FramesPerSecond);
                editor._player.Source = await editor.Source.GetMediaSourceAsync();
            }

            // Add markers to timeline
            foreach (var marker in editor.Source.Markers)
            {
                editor.Timeline.Markers.Add(marker);
            }

            // Add keyframes to timeline
            foreach (var keyframe in editor.Source.Keyframes)
            {
                editor.Timeline.Markers.Add(new Marker
                {
                    Position = keyframe.Time,
                    Duration = 0,
                    Track = 0,
                    Name = keyframe.GetType().Name
                });
            }

            // Add cuts to timeline as selections
            foreach (var (start, end) in editor.Source.Cuts)
            {
                editor.Timeline.AddSelection(start, end);
            }

            // Refresh commands and UI state
            editor.RefreshCommandStates();
            editor.RefreshUIState();
        }

        private static void OnSelectedMarkerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;
        }

        private static void OnFrameOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;

            editor.ViewModel.EditorCenterFrameCommand.NotifyCanExecuteChanged();
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
            else if (e.Property == DragCursorShapeProperty)
            {
                editor._dragCursor = InputSystemCursor.Create((InputSystemCursorShape)e.NewValue);
                if (editor._isPointerCapturedForFrame)
                    editor.ProtectedCursor = editor._dragCursor;
            }
            else if (e.Property == HoverCursorShapeProperty)
            {
                editor._hoverCursor = InputSystemCursor.Create((InputSystemCursorShape)e.NewValue);
                if (editor._isPointerOverFrame)
                    editor.ProtectedCursor = editor._hoverCursor;
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

            // Set initial (default) refresh rate
            FramesPerSecond = App.RefreshRate;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            SwapChainCanvas.RemoveFromVisualTree();
            SwapChainCanvas.SwapChain = null;
        }
        #endregion

        #region Event Handlers (Media Player)
        private void Player_MediaOpened(MediaPlayer sender, object args)
        {
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                Timeline.Duration = sender.PlaybackSession.NaturalDuration;
                Timeline.IsPositionAdjustmentEnabled = true;
                Timeline.IsZoomAdjustmentEnabled = true;
                Timeline.ZoomOutFull();

                Timeline.Position = Timeline.GetNearestSnapValue(
                    (decimal)sender.PlaybackSession.Position.TotalSeconds,
                    false, SnapInterval.Frame);
                RefreshCommandStates();
            });
        }

        private void Player_MediaEnded(MediaPlayer sender, object args)
        {
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                _redrawTimer.Stop();
                RefreshCommandStates();
            });
        }

        private void Player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                _redrawTimer.Stop();
                Timeline.Reset();
                RefreshCommandStates();
            });
        }

        private void Player_VideoFrameAvailable(MediaPlayer sender, object args)
        {
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
            {
                if (_frameSizingBitmap == null)
                {
                    _frameSizingBitmap = new SoftwareBitmap(BitmapPixelFormat.Rgba8,
                                                            (int)SwapChainCanvas.ActualWidth,
                                                            (int)SwapChainCanvas.ActualHeight,
                                                            BitmapAlphaMode.Ignore);
                }

                if (_frameBitmap != null)
                {
                    _frameBitmap.Dispose();
                    _frameBitmap = null;
                }

                _frameBitmap = CanvasBitmap.CreateFromSoftwareBitmap(SwapChainCanvas.SwapChain.Device,
                                                                     _frameSizingBitmap);
                _player.CopyFrameToVideoSurface(_frameBitmap);
            });
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            if (DispatcherQueue == null)
                return;
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            if (DispatcherQueue == null)
                return;
        }

        private void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
        {
            if (DispatcherQueue == null)
                return;
        }

        private void PlaybackSession_PlaybackRateChanged(MediaPlaybackSession sender, object args)
        {
            if (DispatcherQueue == null)
                return;

            _playbackRate = sender.PlaybackRate;
        }
        #endregion

        #region Event Handlers (Timers)
        private void RedrawTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            if (_frameBitmap == null)
                return;

            using var ds = SwapChainCanvas.SwapChain.CreateDrawingSession(Colors.Black);

            if (_isImageAnimationPlaying)
            {
                if (_imageAnimationFrame < Source.TotalFrames)
                    _imageAnimationFrame++;
                else
                    _isImageAnimationPlaying = false;
            }

            if (_isImageAnimationPlaying || _player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                UpdateFramePositionAndScale();
            ApplyFramePositionAndScale();
            ds.DrawImage(_frameBitmap, _destRect, _sourceRect);

            if (Source.Duration > 0)
            {
                var positionString = Source.ContentType == MediaContentType.Video
                    ? _player.PlaybackSession.Position.TotalSeconds.ToTimecodeString(FramesPerSecond)
                    : _imageAnimationFrame.ToString();

                using var positionTextFormat = new CanvasTextFormat
                {
                    FontFamily = TextOverlayFontFamily,
                    FontSize = TextOverlayFontSize,
                    FontStretch = TextOverlayFontStretch,
                    FontStyle = TextOverlayFontStyle,
                    FontWeight = TextOverlayFontWeight,
                    HorizontalAlignment = CanvasHorizontalAlignment.Left
                };

                ds.DrawText(positionString, 5, 5, TextOverlayColor, positionTextFormat);
            }

            SwapChainCanvas.SwapChain.Present();
        }
        #endregion

        #region Event Handlers (SwapChain)
        private void SwapChainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_frameSizingBitmap != null)
            {
                _frameSizingBitmap.Dispose();
                _frameSizingBitmap = null;
            }

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
                    FrameOffsetX += centerX - point.Position.X;
                    FrameOffsetY -= centerY - point.Position.Y;
                }
            }
        }

        private void RenderAreaBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(RenderAreaBorder);

            if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                RenderAreaBorder.ReleasePointerCapture(e.Pointer);
                ProtectedCursor = _isPointerOverFrame ? _hoverCursor : _primaryCursor;
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
            if (_isPointerOverFrame && ProtectedCursor != _hoverCursor)
                ProtectedCursor = _hoverCursor;
            else if (!_isPointerOverFrame && ProtectedCursor != _primaryCursor)
                ProtectedCursor = _primaryCursor;

            if (!point.Properties.IsLeftButtonPressed || !_isPointerCapturedForFrame)
                return;

            if (ProtectedCursor != _dragCursor)
                ProtectedCursor = _dragCursor;

            FrameOffsetX += point.Position.X - _prevLeftMousePosition.X;
            FrameOffsetY -= point.Position.Y - _prevLeftMousePosition.Y;

            _prevLeftMousePosition = point.Position;
        }

        private void RenderAreaBorder_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (_isPointerCapturedForFrame)
                return;

            FrameScale += e.GetCurrentPoint(RenderAreaBorder).Properties.MouseWheelDelta / 120;
        }
        #endregion

        #region Event Handlers (Commands - CanExecuteRequested)
        private void EditorPlayCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Image
                                ? !_isImageAnimationPlaying
                                : _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused;
        }

        private void EditorPauseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Image
                                ? _isImageAnimationPlaying
                                : _player.PlaybackSession.CanPause &&
                                  _player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
        }

        private void EditorPreviousFrameCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Image
                                ? !_isImageAnimationPlaying
                                : _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused &&
                                  _player.PlaybackSession.Position >= Timeline.FrameDuration;
        }

        private void EditorNextFrameCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Image
                                ? !_isImageAnimationPlaying
                                : _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused &&
                                  _player.PlaybackSession.NaturalDuration - _player.PlaybackSession.Position
                                    >= Timeline.FrameDuration;
        }

        private void EditorPreviousMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Image
                                ? GetPreviousKeyframeFromCurrentPosition(0.5M) != null
                                : (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
                                   _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused) &&
                                  GetPreviousMarkerFromCurrentPosition(0.5M) != null;
        }

        private void EditorNextMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Image
                                ? GetNextKeyframeFromCurrentPosition() != null
                                : (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
                                   _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused) &&
                                  GetNextMarkerFromCurrentPosition() != null;
        }

        private void EditorToggleActiveSelectionCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Video &&
                              (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
                               _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused);
        }

        private void EditorCutSelectedCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Video &&
                              (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
                               _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused) &&
                              Timeline.IsSelectionEnabled &&
                              Timeline.SelectionStart != Timeline.SelectionEnd;
        }

        private void EditorNewMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Video &&
                              (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
                               _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused);
        }

        private void EditorNewClipCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Video &&
                              (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
                               _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused) &&
                              Timeline.IsSelectionEnabled &&
                              Timeline.SelectionStart != Timeline.SelectionEnd;
        }

        private void EditorNewKeyframeCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Image ||
                              _player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ||
                              _player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused;
        }

        private void EditorPlaybackRateDecreaseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Image
                                ? _isImageAnimationPlaying &&
                                  _playbackRate > 0.25
                                : _player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing &&
                                  _playbackRate > 0.5;
        }

        private void EditorPlaybackRateIncreaseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              Source.ContentType == MediaContentType.Image
                                ? _isImageAnimationPlaying &&
                                  _playbackRate < 4.0
                                : _player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing &&
                                  _playbackRate < 3.0;
        }

        private void EditorPlaybackRateNormalCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable &&
                              _playbackRate != 1.0 &&
                              Source.ContentType == MediaContentType.Image
                                ? _isImageAnimationPlaying
                                : _player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
        }

        private void EditorCenterFrameCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsLoaded && Source != null && (FrameOffsetX != 0 || FrameOffsetY != 0);
        }

        private void EditorFrameZoomFitCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsLoaded && Source != null;
        }

        private void EditorFrameZoomFullCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsLoaded && Source != null;
        }

        private void EditorTimelineZoomOutCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable;
        }

        private void EditorTimelineZoomInCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsMediaPlayable;
        }

        private void ToolsAnimateMediaCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsLoaded && Source != null;
        }
        #endregion

        #region Event Handlers (Commands - ExecuteRequested)
        private void EditorPlayCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                _isImageAnimationPlaying = true;
            else
                _player.Play();

            PlayButton.Visibility = Visibility.Collapsed;
            PauseButton.Visibility = Visibility.Visible;
        }

        private void EditorPauseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                _isImageAnimationPlaying = false;
            else
                _player.Pause();

            PlayButton.Visibility = Visibility.Visible;
            PauseButton.Visibility = Visibility.Collapsed;
        }

        private void EditorPreviousFrameCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            _player.PlaybackSession.Position -= TimeSpan.FromSeconds(1.0 / FramesPerSecond);
        }

        private void EditorNextFrameCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            _player.PlaybackSession.Position += TimeSpan.FromSeconds(1.0 / FramesPerSecond);
        }

        private void EditorPreviousMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
            {
                var keyframe = GetPreviousKeyframeFromCurrentPosition(0.5M);
                /*if (keyframe != null)
                    ViewModel.SelectedKeyframe = keyframe;*/
            }
            else
            {
                var marker = GetPreviousMarkerFromCurrentPosition(0.5M);
                if (marker != null)
                    ViewModel.SelectedMarker = marker;
            }
        }

        private void EditorNextMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
            {
                var keyframe = GetNextKeyframeFromCurrentPosition();
                /*if (keyframe != null)
                    ViewModel.SelectedKeyframe = keyframe;*/
            }
            else
            {
                var marker = GetNextMarkerFromCurrentPosition();
                if (marker != null)
                    ViewModel.SelectedMarker = marker;
            }
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

        private void ToolsAnimateMediaCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }
        #endregion

        #region Private Properties
        private bool IsMediaPlayable
        {
            get
            {
                if (IsLoaded && Source != null)
                {
                    if (Source is MediaFile file && file == null)
                        return false;

                    if (Source.ContentType == MediaContentType.Image)
                        return Source.Duration > 0;

                    return _player.Source != null &&
                           _player.Source is MediaSource source &&
                           source.IsOpen &&
                           source.State == MediaSourceState.Opened;
                }

                return false;
            }
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

            ViewModel.ToolsAnimateMediaCommand.CanExecuteRequested +=
                ToolsAnimateMediaCommand_CanExecuteRequested;
            ViewModel.ToolsAnimateMediaCommand.ExecuteRequested +=
                ToolsAnimateMediaCommand_ExecuteRequested;
        }

        private void RefreshCommandStates()
        {
            ViewModel.EditorPlayCommand.NotifyCanExecuteChanged();
            ViewModel.EditorPauseCommand.NotifyCanExecuteChanged();
            ViewModel.EditorPreviousFrameCommand.NotifyCanExecuteChanged();
            ViewModel.EditorNextFrameCommand.NotifyCanExecuteChanged();
            ViewModel.EditorPreviousMarkerCommand.NotifyCanExecuteChanged();
            ViewModel.EditorNextMarkerCommand.NotifyCanExecuteChanged();
            ViewModel.EditorToggleActiveSelectionCommand.NotifyCanExecuteChanged();
            ViewModel.EditorNewMarkerCommand.NotifyCanExecuteChanged();
            ViewModel.EditorNewClipCommand.NotifyCanExecuteChanged();
            ViewModel.EditorNewKeyframeCommand.NotifyCanExecuteChanged();
            ViewModel.EditorCutSelectedCommand.NotifyCanExecuteChanged();
            ViewModel.EditorPlaybackRateDecreaseCommand.NotifyCanExecuteChanged();
            ViewModel.EditorPlaybackRateNormalCommand.NotifyCanExecuteChanged();
            ViewModel.EditorPlaybackRateIncreaseCommand.NotifyCanExecuteChanged();
            ViewModel.EditorCenterFrameCommand.NotifyCanExecuteChanged();
            ViewModel.EditorFrameZoomFitCommand.NotifyCanExecuteChanged();
            ViewModel.EditorFrameZoomFullCommand.NotifyCanExecuteChanged();
            ViewModel.EditorTimelineZoomInCommand.NotifyCanExecuteChanged();
            ViewModel.EditorTimelineZoomOutCommand.NotifyCanExecuteChanged();
            ViewModel.ToolsAnimateMediaCommand.NotifyCanExecuteChanged();
        }

        private void RefreshUIState()
        {
            if (Source == null)
            {
                EditorCommandBar.Visibility = Visibility.Collapsed;
                Timeline.Visibility = Visibility.Collapsed;
                return;
            }

            EditorCommandBar.Visibility = Visibility.Visible;
            Timeline.IsSelectionEnabled = false;

            var visibility = Source.Duration > 0 ? Visibility.Visible : Visibility.Collapsed;

            Timeline.Visibility = visibility;

            PlayButton.Visibility = visibility;
            PauseButton.Visibility = Visibility.Collapsed;
            PreviousFrameButton.Visibility = visibility;
            NextFrameButton.Visibility = visibility;
            PreviousMarkerButton.Visibility = visibility;
            NextMarkerButton.Visibility = visibility;

            TrimAndEditButtonSeparator.Visibility = visibility;
            ToggleActiveSelectionButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : visibility;
            NewMarkerButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : visibility;
            NewClipButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : visibility;
            NewKeyframeButton.Visibility = visibility;
            CutSelectedButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : visibility;

            PlaybackRateButtonSeparator.Visibility = visibility;
            PlaybackRateDecreaseButton.Visibility = visibility;
            PlaybackRateNormalButton.Visibility = visibility;
            PlaybackRateIncreaseButton.Visibility = visibility;

            ZoomAndPanButtonSeparator.Visibility = visibility;
            TimelineZoomOutButton.Visibility = visibility;
            TimelineZoomInButton.Visibility = visibility;
        }

        private void RegisterMessages()
        {

        }

        private void ScaleFrameToFit(uint widthInPixels, uint heightInPixels)
        {
            var destWidth = SwapChainCanvas.SwapChain.SizeInPixels.Width;
            var destHeight = SwapChainCanvas.SwapChain.SizeInPixels.Height;
            var scaleW = 10 * Math.Log((double)destWidth / widthInPixels) / Math.Log(2.0);
            var scaleH = 10 * Math.Log((double)destHeight / heightInPixels) / Math.Log(2.0);
            FrameScale = Math.Min(scaleW, scaleH);
        }

        private void UpdateFramePositionAndScale()
        {
            var keyframes = Source.Keyframes.OfType<PanAndZoomKeyframe>().OrderBy(x => x.Time);

            // Source has no keyframes for position/scale
            if (!keyframes.Any())
                return;

            // Get current playback position
            var currentPos = Source.ContentType == MediaContentType.Image
                ? _imageAnimationFrame / (decimal)FramesPerSecond
                : (decimal)_player.PlaybackSession.Position.TotalSeconds;

            // Source has only one keyframe for position/scale,
            // or current position is at or before the 1st keyframe
            if (keyframes.Count() == 1 || currentPos <= keyframes.First().Time)
            {
                var currentKeyframe = keyframes.First();
                FrameOffsetX = currentKeyframe.OffsetX;
                FrameOffsetY = currentKeyframe.OffsetY;
                FrameScale = currentKeyframe.Scale;
                return;
            }

            // Current position is at or after the last keyframe
            if (currentPos >= keyframes.Last().Time)
            {
                var currentKeyframe = keyframes.First();
                currentKeyframe = keyframes.Last();
                FrameOffsetX = currentKeyframe.OffsetX;
                FrameOffsetY = currentKeyframe.OffsetY;
                FrameScale = currentKeyframe.Scale;
                return;
            }

            // Current playback position is between two keyframes
            var prevKeyframe = keyframes.Where(x => x.Time <= currentPos).OrderBy(x => currentPos - x.Time).First();
            var nextKeyframe = keyframes.Where(x => x.Time > currentPos).OrderBy(x => x.Time - currentPos).First();

            FrameOffsetX = CalculateValue(prevKeyframe.Time,
                                          nextKeyframe.Time,
                                          prevKeyframe.OffsetX,
                                          nextKeyframe.OffsetX);

            FrameOffsetY = CalculateValue(prevKeyframe.Time,
                                          nextKeyframe.Time,
                                          prevKeyframe.OffsetY,
                                          nextKeyframe.OffsetY);

            FrameScale = CalculateValue(prevKeyframe.Time,
                                        nextKeyframe.Time,
                                        prevKeyframe.Scale,
                                        nextKeyframe.Scale);

            // Local function to calculate the value of a parameter
            // at a specific time between two keyframes
            double CalculateValue(decimal t0, decimal t1, double v0, double v1)
            {
                return decimal.ToDouble(((decimal)(v1 - v0) / (t1 - t0) * (currentPos - t0)) + (decimal)v0);
            }
        }

        private void ApplyFramePositionAndScale()
        {
            var sourceWidth = _frameBitmap.SizeInPixels.Width;
            var sourceHeight = _frameBitmap.SizeInPixels.Height;
            var destWidth = SwapChainCanvas.SwapChain.SizeInPixels.Width;
            var destHeight = SwapChainCanvas.SwapChain.SizeInPixels.Height;

            _scaleFactor = Math.Pow(2, 0.1 * FrameScale);
            var scaledSourceWidth = Math.Round(sourceWidth * _scaleFactor, 6);
            if (scaledSourceWidth <= destWidth)
            {
                _sourceRect.Width = sourceWidth;
                _sourceRect.X = 0;
                _destRect.Width = scaledSourceWidth;
                _destRect.X = ((destWidth - scaledSourceWidth) / 2) + FrameOffsetX;
            }
            else
            {
                _sourceRect.Width = destWidth / _scaleFactor;
                _sourceRect.X = ((sourceWidth - _sourceRect.Width) / 2) - (FrameOffsetX / _scaleFactor);
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
                _destRect.Y = ((destHeight - scaledSourceHeight) / 2) - FrameOffsetY;
            }
            else
            {
                _sourceRect.Height = destHeight / _scaleFactor;
                _sourceRect.Y = ((sourceHeight - _sourceRect.Height) / 2) + (FrameOffsetY / _scaleFactor);
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

        private Marker GetPreviousMarkerFromCurrentPosition(decimal minDistanceFromMarker = 0.0M)
        {
            if (Source.Markers.Count == 0)
                return null;

            var markers = from marker in Source.Markers
                          where Timeline.Position >= marker.Position + minDistanceFromMarker
                          orderby Timeline.Position - marker.Position
                          select marker;

            if (markers.Any())
                return markers.First();
            return null;
        }

        private Marker GetNextMarkerFromCurrentPosition()
        {
            if (Source.Markers.Count == 0)
                return null;

            var markers = from marker in Source.Markers
                          where Timeline.Position < marker.Position
                          orderby marker.Position - Timeline.Position
                          select marker;

            if (markers.Any())
                return markers.First();
            return null;
        }

        private Keyframe GetPreviousKeyframeFromCurrentPosition(decimal minDistanceFromKeyframe = 0.0M)
        {
            if (Source.Keyframes.Count == 0)
                return null;

            var keyframes = from keyframe in Source.Keyframes
                            where Timeline.Position >= keyframe.Time + minDistanceFromKeyframe
                            orderby Timeline.Position - keyframe.Time
                            select keyframe;

            if (keyframes.Any())
                return keyframes.First();
            return null;
        }

        private Keyframe GetNextKeyframeFromCurrentPosition()
        {
            if (Source.Keyframes.Count == 0)
                return null;

            var keyframes = from keyframe in Source.Keyframes
                            where Timeline.Position < keyframe.Time
                            orderby keyframe.Time - Timeline.Position
                            select keyframe;

            if (keyframes.Any())
                return keyframes.First();
            return null;
        }
        #endregion
    }
}