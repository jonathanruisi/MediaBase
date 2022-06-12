//#define SHOW_DEBUG_MESSAGES

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using JLR.Utility.WinUI.Controls;
using JLR.Utility.WinUI.Dialogs;
using JLR.Utility.WinUI.Messaging;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
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
        private bool _isPointerCapturedForFrame;
        private Point _prevLeftMousePosition;
        private double _scaleFactor, _prevPlaybackRate;
        private Rect _sourceRect, _destRect;
        private FollowMode _prevFollowMode;
        private ValueDragType _scrubType;
        private int _trackCount;
        #endregion

        #region Properties
        public Project ViewModel => (Project)DataContext;

        public int FramesPerSecond
        {
            get => (int)GetValue(FramesPerSecondProperty);
            private set => SetValue(FramesPerSecondProperty, value);
        }

        public static readonly DependencyProperty FramesPerSecondProperty =
            DependencyProperty.Register("FramesPerSecond",
                                        typeof(int),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(App.RefreshRate,
                                            OnFramesPerSecondChanged));

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

        public MediaPlaybackState PlaybackState
        {
            get => (MediaPlaybackState)GetValue(PlaybackStateProperty);
            private set => SetValue(PlaybackStateProperty, value);
        }

        public static readonly DependencyProperty PlaybackStateProperty =
            DependencyProperty.Register("PlaybackState",
                                        typeof(MediaPlaybackState),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(MediaPlaybackState.None,
                                            OnPlaybackStateChanged));

        public double PlaybackRate
        {
            get => (double)GetValue(PlaybackRateProperty);
            set => SetValue(PlaybackRateProperty, value);
        }

        public static readonly DependencyProperty PlaybackRateProperty =
            DependencyProperty.Register("PlaybackRate",
                                        typeof(double),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(0.0,
                                            OnPlaybackRateChanged));

        public bool IsLoopingEnabled
        {
            get => (bool)GetValue(IsLoopingEnabledProperty);
            set => SetValue(IsLoopingEnabledProperty, value);
        }

        public static readonly DependencyProperty IsLoopingEnabledProperty =
            DependencyProperty.Register("IsLoopingEnabled",
                                        typeof(bool),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(false,
                                            OnIsLoopingEnabledChanged));

        public TimeSpan CurrentPosition
        {
            get => TimeSpan.FromSeconds(decimal.ToDouble(CurrentFrame / FramesPerSecond));
            private set => CurrentFrame =
                decimal.ToInt32(decimal.Round((decimal)value.TotalSeconds * FramesPerSecond));
        }

        public decimal CurrentFrame
        {
            get => (decimal)GetValue(CurrentFrameProperty);
            private set => SetValue(CurrentFrameProperty, value);
        }

        public static readonly DependencyProperty CurrentFrameProperty =
            DependencyProperty.Register("CurrentFrame",
                                        typeof(decimal),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(0,
                                            OnCurrentFrameChanged));

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

        public bool IsPanAndZoomEnabled
        {
            get => (bool)GetValue(IsPanAndZoomEnabledProperty);
            set => SetValue(IsPanAndZoomEnabledProperty, value);
        }

        public static readonly DependencyProperty IsPanAndZoomEnabledProperty =
            DependencyProperty.Register("IsPanAndZoomEnabled",
                                        typeof(bool),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(false));

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

        public TimeDisplayFormat TimeDisplayMode
        {
            get => (TimeDisplayFormat)GetValue(TimeDisplayModeProperty);
            set => SetValue(TimeDisplayModeProperty, value);
        }

        public static readonly DependencyProperty TimeDisplayModeProperty =
            DependencyProperty.Register("TimeDisplayMode",
                                        typeof(TimeDisplayFormat),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(TimeDisplayFormat.None));

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
        private static void OnFramesPerSecondChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor || editor._redrawTimer is null)
                return;

            editor._redrawTimer.Interval =
                TimeSpan.FromTicks((int)(1.0 / editor.FramesPerSecond * 10000000));

            // Timeline's FPS value is set through data binding
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"FPS changed: {editor.FramesPerSecond}");
#endif
        }

        private static async void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;

            // Free resources used by previous source,
            // reset the editor, then prepare the editor
            // for the new source.
            editor.ResetEditor();
            await editor.PrepareEditorForSource();

            // Refresh commands and UI state
            editor.RefreshCommandStates();
            editor.RefreshUI();
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Source changed: {editor.Source}");
#endif
        }

        private static void OnPlaybackStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Playback state changed: {Enum.GetName(editor.PlaybackState)}");
#endif
            if (editor.PlaybackState == MediaPlaybackState.None)
                editor._redrawTimer.Stop();
            else
                editor._redrawTimer.Start();

            if (editor.PlaybackState == MediaPlaybackState.Playing)
            {
                editor.PlayButton.Visibility = Visibility.Collapsed;
                editor.PauseButton.Visibility = Visibility.Visible;
            }
            else
            {
                editor.PlayButton.Visibility = Visibility.Visible;
                editor.PauseButton.Visibility = Visibility.Collapsed;
            }
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"    Redraw timer running: {editor._redrawTimer.IsRunning}");
#endif
            editor.RefreshCommandStates();
        }

        private static void OnPlaybackRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Playback rate changed: {editor.PlaybackRate}");
#endif
            editor.ViewModel.EditorPlaybackRateDecreaseCommand.NotifyCanExecuteChanged();
            editor.ViewModel.EditorPlaybackRateNormalCommand.NotifyCanExecuteChanged();
            editor.ViewModel.EditorPlaybackRateIncreaseCommand.NotifyCanExecuteChanged();
        }

        private static void OnIsLoopingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;

            editor._player.IsLoopingEnabled = editor.IsLoopingEnabled;
        }

        private static void OnCurrentFrameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor || editor.Source == null)
                return;
#if DEBUG && SHOW_DEBUG_MESSAGES
            if (editor.PlaybackState != MediaPlaybackState.Playing)
                Debug.WriteLine($"CURRENT FRAME: {editor.CurrentFrame}");
#endif
            // Frame change was not due to the user interacting with the timeline,
            // therefore the timeline needs to be synchronized to the frame count.
            if (editor._scrubType == ValueDragType.None)
            {
                editor.Timeline.Position = (decimal)editor.CurrentPosition.TotalSeconds;
            }

            // If an animated image has reached its last frame, pause playback.
            if (editor.Source.ContentType == MediaContentType.Image &&
                editor.PlaybackState == MediaPlaybackState.Playing &&
                editor.CurrentFrame == editor.Source.TotalFrames)
            {
                if (editor.IsLoopingEnabled)
                    editor.CurrentFrame = 0;
                else
                    editor.PlaybackState = MediaPlaybackState.Paused;

                editor.ViewModel.EditorPlayCommand.NotifyCanExecuteChanged();
                editor.ViewModel.EditorPauseCommand.NotifyCanExecuteChanged();
                editor.ViewModel.EditorNewKeyframeCommand.NotifyCanExecuteChanged();
                editor.ViewModel.EditorPlaybackRateDecreaseCommand.NotifyCanExecuteChanged();
                editor.ViewModel.EditorPlaybackRateNormalCommand.NotifyCanExecuteChanged();
                editor.ViewModel.EditorPlaybackRateIncreaseCommand.NotifyCanExecuteChanged();
            }

            editor.ViewModel.EditorPreviousFrameCommand.NotifyCanExecuteChanged();
            editor.ViewModel.EditorNextFrameCommand.NotifyCanExecuteChanged();
            editor.ViewModel.EditorPreviousMarkerCommand.NotifyCanExecuteChanged();
            editor.ViewModel.EditorNextMarkerCommand.NotifyCanExecuteChanged();
        }

        private static void OnFrameOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Frame offset changed: ({editor.FrameOffsetX}, {editor.FrameOffsetY})");
#endif
            editor.ViewModel.EditorCenterFrameCommand.NotifyCanExecuteChanged();
        }

        private static void OnCursorShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Cursor changed: {e.Property}");
#endif
            if (e.Property == PrimaryCursorShapeProperty)
            {
                editor._primaryCursor = InputSystemCursor.Create((InputSystemCursorShape)e.NewValue);
                if (!editor._isPointerCapturedForFrame && !editor.TestPointOverFrame(editor._prevLeftMousePosition))
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
                if (editor.TestPointOverFrame(editor._prevLeftMousePosition))
                    editor.ProtectedCursor = editor._hoverCursor;
            }
        }
        #endregion

        #region Event Handlers (UserControl)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine("LOADED");
#endif
            // Initialize timeline
            Timeline.PositionChanged += Timeline_PositionChanged;
            Timeline.SelectionChanged += Timeline_SelectionChanged;
            Timeline.ZoomChanged += Timeline_ZoomChanged;
            Timeline.TimelineValueDragStarted += Timeline_DragStarted;
            Timeline.TimelineValueDragCompleted += Timeline_DragCompleted;
            Timeline.TrackCountChanged += Timeline_TrackCountChanged;

            // Initialize swap chain
            SwapChainCanvas.SwapChain = new CanvasSwapChain(CanvasDevice.GetSharedDevice(),
                                                            (float)SwapChainCanvas.ActualWidth,
                                                            (float)SwapChainCanvas.ActualHeight,
                                                            PInvoke.User32.GetDpiForWindow(App.WindowHandle));

            // Set the editor's initial refresh rate
            FramesPerSecond = App.RefreshRate;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"UNLOADED");
#endif
            ResetEditor();

            SwapChainCanvas.RemoveFromVisualTree();
            SwapChainCanvas.SwapChain = null;
        }
        #endregion

        #region Event Handlers (Media Player)
        private void Player_MediaOpened(MediaPlayer sender, object args)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"MEDIA OPENED");
#endif
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                Timeline.Duration = sender.PlaybackSession.NaturalDuration;
                Timeline.IsSelectionEnabled = false;
                Timeline.IsPositionAdjustmentEnabled = true;
                Timeline.IsSelectionAdjustmentEnabled = true;
                Timeline.IsZoomAdjustmentEnabled = true;
                Timeline.ZoomOutFull();
                RefreshCommandStates();
            });
        }

        private void Player_MediaEnded(MediaPlayer sender, object args)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"MEDIA ENDED");
#endif
            if (DispatcherQueue != null)
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    if (IsLoopingEnabled)
                    {
                        _player.PlaybackSession.Position = TimeSpan.Zero;
                        _player.Play();
                    }

                    RefreshCommandStates();
                });
        }

        private void Player_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"MEDIA FAILED");
#endif
            if (DispatcherQueue != null)
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, RefreshCommandStates);
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Playback state changed (MediaPlayer): {Enum.GetName(sender.PlaybackState)}");
#endif
            if (DispatcherQueue != null)
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    PlaybackState = sender.PlaybackState;
                });
        }

        private void PlaybackSession_PlaybackRateChanged(MediaPlaybackSession sender, object args)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Playback rate changed (MediaPlayer): {sender.PlaybackRate}");
#endif
            if (DispatcherQueue != null)
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    PlaybackRate = sender.PlaybackRate;
                });
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            if (DispatcherQueue != null)
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
#if DEBUG && SHOW_DEBUG_MESSAGES
                    if (PlaybackState != MediaPlaybackState.Playing)
                        Debug.WriteLine($"Position changed (MediaPlayer): {sender.Position}");
#endif
                    CurrentPosition = sender.Position;
                });
        }

        private void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Seek completed (MediaPlayer)");
#endif
            if (DispatcherQueue != null && _scrubType == ValueDragType.Position ||
                                           _scrubType == ValueDragType.SelectionStart ||
                                           _scrubType == ValueDragType.SelectionEnd ||
                                           _scrubType == ValueDragType.Selection)
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    decimal previewPosition = _scrubType switch
                    {
                        ValueDragType.Position => Timeline.Position,
                        ValueDragType.SelectionEnd => Timeline.SelectionEnd,
                        _ => Timeline.SelectionStart
                    };

                    sender.Position = TimeSpan.FromSeconds(decimal.ToDouble(previewPosition));
                });
            }
        }

        private void Player_VideoFrameAvailable(MediaPlayer sender, object args)
        {
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
            {
                if (_frameSizingBitmap == null)
                {
                    int width = (int)(IsPanAndZoomEnabled
                        ? Source.WidthInPixels
                        : SwapChainCanvas.ActualWidth);
                    int height = (int)(IsPanAndZoomEnabled
                        ? Source.HeightInPixels
                        : SwapChainCanvas.ActualHeight);
                    _frameSizingBitmap = new SoftwareBitmap(BitmapPixelFormat.Rgba8,
                                                            width, height,
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
        #endregion

        #region Event Handlers (Timers)
        private void RedrawTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            if (_frameBitmap == null)
                return;

            using var ds = SwapChainCanvas.SwapChain.CreateDrawingSession(Colors.Black);
            var isAnimatedImagePlaying = Source.ContentType == MediaContentType.Image &&
                                         PlaybackState == MediaPlaybackState.Playing &&
                                         CurrentFrame < Source.TotalFrames;

            // Increment the frame count if we are playing an animated image
            if (isAnimatedImagePlaying)
                CurrentFrame += (decimal)PlaybackRate;

            // Adjust frame position and scale using keyframes (if enabled)
            if (IsPanAndZoomEnabled &&
                (PlaybackState == MediaPlaybackState.Playing ||
                 (PlaybackState == MediaPlaybackState.Paused &&
                  (_scrubType == ValueDragType.Position ||
                   _scrubType == ValueDragType.SelectionStart ||
                   _scrubType == ValueDragType.SelectionEnd ||
                   _scrubType == ValueDragType.Selection))))
            {
                CalculateFrameScaleAndPosition();
            }

            ApplyFrameScaleAndPosition();

            // Draw the image
            ds.DrawImage(_frameBitmap, _destRect, _sourceRect);

            // Draw category adornment
            var totalBorderSize = 12.0f;
            var catIconSize = 60.0f;
            var iconSpacing = 5.0f;
            int catOffset = 0;
            var numCategories = 0;
            if (ViewModel.ActiveMediaSource.IsCategory1)
                numCategories++;
            if (ViewModel.ActiveMediaSource.IsCategory2)
                numCategories++;
            if (ViewModel.ActiveMediaSource.IsCategory3)
                numCategories++;
            if (ViewModel.ActiveMediaSource.IsCategory4)
                numCategories++;
            float borderThickness = totalBorderSize / numCategories;

            if (numCategories > 0)
            {
                if (ViewModel.ActiveMediaSource.IsCategory1)
                {
                    // Draw border
                    DrawBorder(Colors.Gold, borderThickness, catOffset * borderThickness);

                    // Draw icon
                    var xOffset = (float)_destRect.Left + totalBorderSize + iconSpacing;
                    var yOffset = (float)_destRect.Top + totalBorderSize + iconSpacing;
                    using var path = new CanvasPathBuilder(ds);
                    path.BeginFigure(0, catIconSize);
                    path.AddLine(catIconSize / 2, 0);
                    path.AddLine(catIconSize, catIconSize);
                    path.EndFigure(CanvasFigureLoop.Closed);
                    using var categoryIconGeometry = CanvasGeometry.CreatePath(path);
                    ds.FillGeometry(categoryIconGeometry, xOffset, yOffset, Colors.Gold);
                    catOffset++;
                }

                if (ViewModel.ActiveMediaSource.IsCategory2)
                {
                    DrawBorder(Colors.CornflowerBlue, borderThickness, catOffset * borderThickness);

                    var xOffset = (float)_destRect.Left + totalBorderSize + iconSpacing + (iconSpacing * catOffset) + (catOffset * catIconSize);
                    var yOffset = (float)_destRect.Top + totalBorderSize + iconSpacing;
                    ds.FillRectangle(xOffset, yOffset, catIconSize, catIconSize, Colors.CornflowerBlue);
                    catOffset++;
                }

                if (ViewModel.ActiveMediaSource.IsCategory3)
                {
                    DrawBorder(Colors.IndianRed, borderThickness, catOffset * borderThickness);

                    var xOffset = (float)_destRect.Left + totalBorderSize + iconSpacing + (iconSpacing * catOffset) + (catOffset * catIconSize);
                    var yOffset = (float)_destRect.Top + totalBorderSize + iconSpacing;
                    ds.FillEllipse(xOffset + (catIconSize / 2), yOffset + (catIconSize / 2), catIconSize / 2, catIconSize / 2, Colors.IndianRed);
                    catOffset++;
                }

                if (ViewModel.ActiveMediaSource.IsCategory4)
                {
                    DrawBorder(Colors.ForestGreen, borderThickness, catOffset * borderThickness);

                    var xOffset = (float)_destRect.Left + totalBorderSize + iconSpacing + (iconSpacing * catOffset) + (catOffset * catIconSize);
                    var yOffset = (float)_destRect.Top + totalBorderSize + iconSpacing;
                    using var path = new CanvasPathBuilder(ds);
                    path.BeginFigure(0, catIconSize / 2);
                    path.AddLine(catIconSize / 2, 0);
                    path.AddLine(catIconSize, catIconSize / 2);
                    path.AddLine(catIconSize / 2, catIconSize);
                    path.EndFigure(CanvasFigureLoop.Closed);
                    using var categoryIconGeometry = CanvasGeometry.CreatePath(path);
                    ds.FillGeometry(categoryIconGeometry, xOffset, yOffset, Colors.ForestGreen);
                }
            }

            // Draw timecode/frame count
            if (IsPlaybackPossible && TimeDisplayMode != TimeDisplayFormat.None)
            {
                var timeStr = TimeDisplayMode switch
                {
                    TimeDisplayFormat.FrameNumber
                        => CurrentFrame.ToString("#"),
                    TimeDisplayFormat.TimecodeWithFrame
                        => CurrentPosition.TotalSeconds.ToTimecodeString(FramesPerSecond),
                    TimeDisplayFormat.TimecodeWithMillis
                        => CurrentPosition.TotalSeconds.ToTimecodeString(FramesPerSecond, true),
                      _ => string.Empty
                };

                using var positionTextFormat = new CanvasTextFormat
                {
                    FontFamily = TextOverlayFontFamily,
                    FontSize = TextOverlayFontSize,
                    FontStretch = TextOverlayFontStretch,
                    FontStyle = TextOverlayFontStyle,
                    FontWeight = TextOverlayFontWeight,
                    HorizontalAlignment = CanvasHorizontalAlignment.Left
                };

                ds.DrawText(timeStr, 5, 5, TextOverlayColor, positionTextFormat);
            }

            SwapChainCanvas.SwapChain.Present();

            // Local function to draw category adornment border
            void DrawBorder(Color color, float thickness, float margin)
            {
                ds.DrawRectangle(new Rect(_destRect.Left + margin + (thickness / 2),
                                          _destRect.Top + margin + (thickness / 2),
                                          _destRect.Width - thickness - margin * 2,
                                          _destRect.Height - thickness - margin * 2),
                                 color, thickness);
            }
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
            // This won't affect MediaPlayer position.
            // It is needed to scrub animated images.
            if (_scrubType == ValueDragType.Position ||
                _scrubType == ValueDragType.SelectionStart ||
                _scrubType == ValueDragType.SelectionEnd ||
                _scrubType == ValueDragType.Selection)
            {
                CurrentFrame = decimal.ToInt32(decimal.Round(e * FramesPerSecond));
            }
        }

        private void Timeline_SelectionChanged(object sender, (decimal start, decimal end, bool isEnabled) e)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Selection changed (Timeline): ({Timeline.SelectionStart:0.000}, {Timeline.SelectionEnd:0.000})");
#endif
            ViewModel.EditorNewClipCommand.NotifyCanExecuteChanged();
            ViewModel.EditorCutSelectedCommand.NotifyCanExecuteChanged();
        }

        private void Timeline_ZoomChanged(object sender, (decimal start, decimal end) e)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Zoom changed (Timeline): ({Timeline.ZoomStart:0.000}, {Timeline.ZoomEnd:0.000})");
#endif
            ViewModel.EditorTimelineZoomInCommand.NotifyCanExecuteChanged();
            ViewModel.EditorTimelineZoomOutCommand.NotifyCanExecuteChanged();
        }

        private void Timeline_DragStarted(object sender, ValueDragType e)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Drag started (Timeline): {Enum.GetName(typeof(ValueDragType), e)}");
#endif
            _scrubType = e;
            _prevFollowMode = Timeline.PositionFollowMode;
            Timeline.PositionFollowMode = FollowMode.NoFollow;

            // Set playback position to the control being scrubbed
            if (e == ValueDragType.Position ||
                e == ValueDragType.SelectionStart ||
                e == ValueDragType.SelectionEnd ||
                e == ValueDragType.Selection)
            {
                _prevPlaybackRate = PlaybackRate;

                decimal previewPosition = e switch
                {
                    ValueDragType.Position => Timeline.Position,
                    ValueDragType.SelectionEnd => Timeline.SelectionEnd,
                    _ => Timeline.SelectionStart
                };

                if (Source.ContentType == MediaContentType.Video)
                {
                    _player.PlaybackSession.PlaybackRate = 0;
                    _player.PlaybackSession.Position =
                        TimeSpan.FromSeconds(decimal.ToDouble(previewPosition));
                }
                else
                {
                    PlaybackRate = 0;
                    CurrentFrame = decimal.ToInt32(decimal.Round(previewPosition * FramesPerSecond));
                }
            }
        }

        private void Timeline_DragCompleted(object sender, ValueDragType e)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Drag started (Timeline): {Enum.GetName(typeof(ValueDragType), e)}");
#endif
            // Set playback to playhead position
            if (e == ValueDragType.Position ||
                e == ValueDragType.SelectionStart ||
                e == ValueDragType.SelectionEnd ||
                e == ValueDragType.Selection)
            {
                if (Source.ContentType == MediaContentType.Video)
                {
                    _player.PlaybackSession.Position =
                        TimeSpan.FromSeconds(decimal.ToDouble(Timeline.Position));
                    _player.PlaybackSession.PlaybackRate = _prevPlaybackRate;
                }
                else
                {
                    CurrentFrame = decimal.ToInt32(decimal.Round(Timeline.Position * FramesPerSecond));
                    PlaybackRate = _prevPlaybackRate;
                }
            }

            // If a marker is selected and the current selection changed, update the marker
            if ((e == ValueDragType.SelectionStart ||
                 e == ValueDragType.SelectionEnd ||
                 e == ValueDragType.Selection) &&
                ViewModel.SelectedMarker != null &&
                ViewModel.SelectedMarker.Duration > 0 &&
                Source is VideoSource videoSource)
            {
                var index = videoSource.Markers.IndexOf(ViewModel.SelectedMarker);

                if (index >= 0)
                {
                    ViewModel.SelectedMarker = null;

                    var newMarker = new Marker
                    {
                        Name = videoSource.Markers[index].Name,
                        Position = Timeline.SelectionStart,
                        Duration = Timeline.SelectionEnd - Timeline.SelectionStart,
                        Group = videoSource.Markers[index].Group
                    };

                    videoSource.Markers.RemoveAt(index);
                    videoSource.Markers.Insert(index, newMarker);
                    ViewModel.SelectedMarker = videoSource.Markers[index];
                }
            }

            Timeline.PositionFollowMode = _prevFollowMode;
            _scrubType = ValueDragType.None;
        }

        private void Timeline_TrackCountChanged(object sender, int e)
        {
#if DEBUG && SHOW_DEBUG_MESSAGES
            Debug.WriteLine($"Track count changed: {e}");
#endif
            var delta = e - _trackCount;
            Timeline.Height += delta * (Timeline.TrackHeight + Timeline.TrackSpacing);
            _trackCount = e;
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
            _prevLeftMousePosition = new Point(double.NaN, double.NaN);

            if (_isPointerCapturedForFrame)
                return;

            ProtectedCursor = _primaryCursor;
        }

        private void RenderAreaBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(RenderAreaBorder);

            if (_destRect.Contains(point.Position) && IsPanAndZoomEnabled)
            {
                var isCtrlPressed = TestKeyStates(Windows.System.VirtualKey.Control, CoreVirtualKeyStates.Down);

                if (point.Properties.IsLeftButtonPressed && !isCtrlPressed)
                {
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

            _prevLeftMousePosition = point.Position;
        }

        private void RenderAreaBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(RenderAreaBorder);

            if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                RenderAreaBorder.ReleasePointerCapture(e.Pointer);
                ProtectedCursor = TestPointOverFrame(point.Position) && IsPanAndZoomEnabled
                    ? _hoverCursor
                    : _primaryCursor;
            }

            _prevLeftMousePosition = point.Position;
        }

        private void RenderAreaBorder_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _isPointerCapturedForFrame = false;
        }

        private void RenderAreaBorder_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            _isPointerCapturedForFrame = false;
            _prevLeftMousePosition = new Point(double.NaN, double.NaN);
        }

        private void RenderAreaBorder_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(RenderAreaBorder);
            var isPointerOverFrame = TestPointOverFrame(point.Position);

            if (isPointerOverFrame && IsPanAndZoomEnabled && ProtectedCursor != _hoverCursor)
                ProtectedCursor = _hoverCursor;
            else if ((!isPointerOverFrame || !IsPanAndZoomEnabled) && ProtectedCursor != _primaryCursor)
                ProtectedCursor = _primaryCursor;

            if (IsPanAndZoomEnabled && point.Properties.IsLeftButtonPressed && _isPointerCapturedForFrame)
            {
                if (ProtectedCursor != _dragCursor)
                    ProtectedCursor = _dragCursor;

                FrameOffsetX += point.Position.X - _prevLeftMousePosition.X;
                FrameOffsetY -= point.Position.Y - _prevLeftMousePosition.Y;
            }

            _prevLeftMousePosition = point.Position;
        }

        private void RenderAreaBorder_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (!IsPanAndZoomEnabled || _isPointerCapturedForFrame)
                return;

            FrameScale += e.GetCurrentPoint(RenderAreaBorder).Properties.MouseWheelDelta / 120;
        }
        #endregion

        #region Event Handlers (Commands - CanExecuteRequested)
        private void EditorPlayCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible && PlaybackState == MediaPlaybackState.Paused;
        }

        private void EditorPauseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible && PlaybackState == MediaPlaybackState.Playing;
        }

        private void EditorToggleLoopingCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              (PlaybackState == MediaPlaybackState.Playing ||
                               PlaybackState == MediaPlaybackState.Paused);
        }

        private void EditorPreviousFrameCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible && CurrentFrame > 0;
        }

        private void EditorNextFrameCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible && CurrentFrame < Source.TotalFrames;
        }

        private void EditorPreviousMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              Source.ContentType == MediaContentType.Image
                                ? Timeline.GetClosestMarkerBeforeCurrentPosition<PanAndZoomKeyframe>(0.5M) != null
                                : Timeline.GetClosestMarkerBeforeCurrentPosition<Marker>(0.5M) != null;
        }

        private void EditorNextMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              Source.ContentType == MediaContentType.Image
                                ? Timeline.GetClosestMarkerAfterCurrentPosition<PanAndZoomKeyframe>(0.1M) != null
                                : Timeline.GetClosestMarkerAfterCurrentPosition<Marker>(0.1M) != null;
        }

        private void EditorToggleActiveSelectionCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible && Source.ContentType == MediaContentType.Video;
        }

        private void EditorCutSelectedCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              Source.ContentType == MediaContentType.Video &&
                              Timeline.IsSelectionEnabled &&
                              Timeline.SelectionStart != Timeline.SelectionEnd;
        }

        private void EditorNewMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible && Source.ContentType == MediaContentType.Video;
        }

        private void EditorNewClipCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              Source.ContentType == MediaContentType.Video &&
                              Timeline.IsSelectionEnabled &&
                              Timeline.SelectionStart != Timeline.SelectionEnd;
        }

        private void EditorNewKeyframeCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              Source.ContentType == MediaContentType.Image && // TODO: Remove this once video-compatible keyframe types are added
                              (PlaybackState == MediaPlaybackState.Playing ||
                               PlaybackState == MediaPlaybackState.Paused);
        }

        private void EditorPlaybackRateDecreaseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              PlaybackState == MediaPlaybackState.Playing &&
                              Source.ContentType == MediaContentType.Image
                                ? PlaybackRate > 0.25
                                : PlaybackRate > 0.5;
        }

        private void EditorPlaybackRateIncreaseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              PlaybackState == MediaPlaybackState.Playing &&
                              Source.ContentType == MediaContentType.Image
                                ? PlaybackRate < 4.0
                                : PlaybackRate < 3.0;
        }

        private void EditorPlaybackRateNormalCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              PlaybackState == MediaPlaybackState.Playing &&
                              PlaybackRate != 1.0;
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
            args.CanExecute = IsPlaybackPossible;
        }

        private void EditorTimelineZoomInCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible;
        }

        private void ToolsAnimateMediaCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsLoaded && Source != null && Source.Duration == 0;
        }
        #endregion

        #region Event Handlers (Commands - ExecuteRequested)
        private void EditorPlayCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                PlaybackState = MediaPlaybackState.Playing;
            else
                _player.Play();
        }

        private void EditorPauseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                PlaybackState = MediaPlaybackState.Paused;
            else
                _player.Pause();
        }

        private void EditorToggleLoopingCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            
        }

        private void EditorPreviousFrameCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                CurrentFrame -= 1M;
            else
                _player.PlaybackSession.Position -= Timeline.FrameDuration;
        }

        private void EditorNextFrameCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                CurrentFrame += 1M;
            else
                _player.PlaybackSession.Position += Timeline.FrameDuration;
        }

        private void EditorPreviousMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                SeekToMarker(Timeline.GetClosestMarkerBeforeCurrentPosition<PanAndZoomKeyframe>(0.5M));
            else
                SeekToMarker(Timeline.GetClosestMarkerBeforeCurrentPosition<Marker>(0.5M));
        }

        private void EditorNextMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                SeekToMarker(Timeline.GetClosestMarkerAfterCurrentPosition<PanAndZoomKeyframe>(0.5M));
            else
                SeekToMarker(Timeline.GetClosestMarkerAfterCurrentPosition<Marker>(0.5M));
        }

        private void EditorToggleActiveSelectionCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if ((Timeline.SelectionStart <= Timeline.ZoomEnd &&
                 Timeline.SelectionEnd >= Timeline.ZoomStart) ||
                !Timeline.IsSelectionEnabled)
                return;

            Timeline.SelectionStart = Timeline.ZoomStart + ((Timeline.ZoomEnd - Timeline.ZoomStart) / 3);
            Timeline.SelectionEnd = Timeline.ZoomEnd - ((Timeline.ZoomEnd - Timeline.ZoomStart) / 3);
        }

        private void EditorCutSelectedCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source is not VideoSource video)
                return;

            video.Cuts.Add((Timeline.SelectionStart, Timeline.SelectionEnd));
        }

        private async void EditorNewMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source is not VideoSource video)
                return;

            // Record current position here so that the new marker reflects
            // the time at which the command was invoked, as opposed to the time
            // at which the user completes the process of adding the marker.
            var pos = CurrentPosition;

            var dlg = new TextPromptDialog
            {
                Title = "New Marker",
                PromptText = "Enter a name for the marker",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            var marker = new Marker
            {
                Name = dlg.Text,
                Position = (decimal)pos.TotalSeconds,
                Duration = 0,
                Group = 0
            };

            var index = 0;
            while (index < video.Markers.Count && video.Markers[index].Position <= marker.Position)
                index++;

            video.Markers.Insert(index, marker);
        }

        private async void EditorNewClipCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source is not VideoSource video)
                return;

            var dlg = new TextPromptDialog
            {
                Title = "New Clip",
                PromptText = "Enter a name for the clip",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            var marker = new Marker
            {
                Name = dlg.Text,
                Position = Timeline.SelectionStart,
                Duration = Timeline.SelectionEnd - Timeline.SelectionStart,
                Group = 0  // TODO: This needs to be assigned from somewhere else
            };

            var index = 0;
            while (index < video.Markers.Count && video.Markers[index].Position <= marker.Position)
                index++;

            video.Markers.Insert(index, marker);
        }

        private void EditorNewKeyframeCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var keyframe = new PanAndZoomKeyframe
            {
                Name = "Keyframe",
                Position = Timeline.Position,
                OffsetX = FrameOffsetX,
                OffsetY = FrameOffsetY,
                Scale = FrameScale
            };

            if (Source.Keyframes.Any(x => x.Position == keyframe.Position))
                return;

            var index = 0;
            while (index < Source.Keyframes.Count && Source.Keyframes[index].Position <= keyframe.Position)
                index++;

            Source.Keyframes.Insert(index, keyframe);
        }

        private void EditorPlaybackRateDecreaseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                PlaybackRate -= 0.25;
            else
                _player.PlaybackSession.PlaybackRate -= 0.5;
        }

        private void EditorPlaybackRateIncreaseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                PlaybackRate += 0.25;
            else
                _player.PlaybackSession.PlaybackRate += 0.5;
        }

        private void EditorPlaybackRateNormalCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                PlaybackRate = 1.0;
            else
                _player.PlaybackSession.PlaybackRate = 1.0;
        }

        private void EditorCenterFrameCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            FrameOffsetX = 0;
            FrameOffsetY = 0;
        }

        private void EditorFrameZoomFitCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            FrameOffsetX = 0;
            FrameOffsetY = 0;
            ScaleFrameToFit(Source.WidthInPixels, Source.HeightInPixels);
        }

        private void EditorFrameZoomFullCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            FrameOffsetX = 0;
            FrameOffsetY = 0;
            FrameScale = 0;
        }

        private void EditorTimelineZoomOutCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            Timeline.VisibleDuration *= 2;
            Timeline.CenterVisibleWindow(Timeline.Position);
        }

        private void EditorTimelineZoomInCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            Timeline.VisibleDuration *= 0.5;
            Timeline.CenterVisibleWindow(Timeline.Position);
        }

        private async void ToolsAnimateMediaCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            ResetEditor();
            Source.Duration = 5;
            await PrepareEditorForSource();
            RefreshCommandStates();
            RefreshUI();
        }
        #endregion

        #region Private Properties
        private bool IsPlaybackPossible
        {
            get
            {
                if (!IsLoaded || Source == null || (Source is IMediaFile file && file.File == null))
                    return false;

                return Source.ContentType == MediaContentType.Image
                    ? Source.Duration > 0
                    : _player.Source != null &&
                       (PlaybackState == MediaPlaybackState.Playing ||
                        PlaybackState == MediaPlaybackState.Paused);
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

            ViewModel.EditorToggleLoopingCommand.CanExecuteRequested +=
                EditorToggleLoopingCommand_CanExecuteRequested;
            ViewModel.EditorToggleLoopingCommand.ExecuteRequested +=
                EditorToggleLoopingCommand_ExecuteRequested;

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
            ViewModel.EditorToggleLoopingCommand.NotifyCanExecuteChanged();
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

        private void RefreshUI()
        {
            if (Source == null)
            {
                EditorCommandBar.Visibility = Visibility.Collapsed;
                Timeline.Visibility = Visibility.Collapsed;
                return;
            }

            // Command bar is always visible when a source is loaded
            EditorCommandBar.Visibility = Visibility.Visible;

            // Set visibility based on whether or not the source is playable (duration > 0)
            var vis = Source.Duration > 0 ? Visibility.Visible : Visibility.Collapsed;

            Timeline.Visibility = vis;

            PlayButton.Visibility = vis;
            PauseButton.Visibility = Visibility.Collapsed;
            PreviousFrameButton.Visibility = vis;
            NextFrameButton.Visibility = vis;
            PreviousMarkerButton.Visibility = vis;
            NextMarkerButton.Visibility = vis;

            TrimAndEditButtonSeparator.Visibility = vis;
            ToggleActiveSelectionButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : vis;
            NewMarkerButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : vis;
            NewClipButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : vis;
            NewKeyframeButton.Visibility = vis;
            CutSelectedButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : vis;
            DeleteMarkerButton.Visibility = vis;

            PlaybackRateButtonSeparator.Visibility = vis;
            PlaybackRateDecreaseButton.Visibility = vis;
            PlaybackRateIncreaseButton.Visibility = vis;
            PlaybackRateNormalButton.Visibility = vis;

            ZoomAndPanButtonSeparator.Visibility = vis;
            TimelineZoomOutButton.Visibility = vis;
            TimelineZoomInButton.Visibility = vis;
            FrameZoomFitButton.Visibility = IsPanAndZoomEnabled
                ? Visibility.Visible : Visibility.Collapsed;
            FrameZoomFullButton.Visibility = IsPanAndZoomEnabled
                ? Visibility.Visible : Visibility.Collapsed;
            CenterFrameButton.Visibility = IsPanAndZoomEnabled
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RegisterMessages()
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            // MBMediaSource.Keyframes collection changed
            messenger.Register<CollectionChangedMessage<ITimelineMarker>>(this, (r, m) =>
            {
                if (m.Sender != Source ||
                    m.PropertyName != nameof(MBMediaSource.Keyframes))
                    return;

                foreach (var keyframe in m.OldValue)
                    Timeline.Markers.Remove(keyframe);

                foreach (var keyframe in m.NewValue)
                    Timeline.Markers.Add(keyframe);
            });

            // VideoSource.Markers collection changed
            messenger.Register<CollectionChangedMessage<Marker>>(this, (r, m) =>
            {
                if (m.Sender != Source ||
                    m.PropertyName != nameof(VideoSource.Markers))
                    return;

                foreach (var marker in m.OldValue)
                    Timeline.Markers.Remove(marker);

                foreach (var marker in m.NewValue)
                    Timeline.Markers.Add(marker);
            });

            // VideoSource.Cuts collection changed
            messenger.Register<CollectionChangedMessage<(decimal start, decimal end)>>(this, (r, m) =>
            {
                if (m.Sender != Source || m.PropertyName != nameof(VideoSource.Cuts))
                    return;

                foreach (var (start, end) in m.OldValue)
                    Timeline.RemoveSelection(start, end);

                foreach (var (start, end) in m.NewValue)
                    Timeline.AddSelection(start, end);
            });

            // Selected marker changed
            messenger.Register<PropertyChangedMessage<Marker>>(this, (r, m) =>
            {
                if (m.Sender != ViewModel ||
                    m.PropertyName != nameof(Project.SelectedMarker) ||
                    m.NewValue == null)
                    return;

                if (Source.ContentType == MediaContentType.Image)
                {
                    CurrentPosition = TimeSpan.FromSeconds(decimal.ToDouble(m.NewValue.Position));
                }
                else if (Source.ContentType == MediaContentType.Video)
                {
                    _player.PlaybackSession.Position =
                        TimeSpan.FromSeconds(decimal.ToDouble(m.NewValue.Position));

                    if (m.NewValue.Duration > 0)
                    {
                        Timeline.SetSelectionFromMarker(m.NewValue);
                        Timeline.IsSelectionEnabled = true;
                        Timeline.IsSelectionAdjustmentEnabled = true;
                    }
                    else
                    {
                        Timeline.IsSelectionEnabled = false;
                    }
                }
            });
        }

        private void ResetEditor()
        {
            // Stop and current playback and halt redraw timer
            if (_player.PlaybackSession.PlaybackState is
                MediaPlaybackState.Opening or
                MediaPlaybackState.Buffering or
                MediaPlaybackState.Playing)
            {
                _player.Pause();
                while (PlaybackState != MediaPlaybackState.Paused) ; // TODO: Better way perhaps?
            }

            _redrawTimer.Stop();
            if (PlaybackState != MediaPlaybackState.None)
                PlaybackState = MediaPlaybackState.None;
            CurrentFrame = 0;
            PlaybackRate = 1;
            TimeDisplayMode = TimeDisplayFormat.None;

            // De-select markers/keyframes
            ViewModel.SelectedMarker = null;

            // Cleanup resources used by the previous media source (if needed)
            if (_player.Source is IDisposable oldSource && oldSource != null)
            {
                oldSource.Dispose();
                _player.Source = null;
            }

            if (_frameBitmap != null)
            {
                _frameBitmap.Dispose();
                _frameBitmap = null;
            }

            if (_frameSizingBitmap != null)
            {
                _frameSizingBitmap.Dispose();
                _frameSizingBitmap = null;
            }

            // Reset frame position and scale
            FrameScale = 0;
            FrameOffsetX = 0;
            FrameOffsetY = 0;

            // Reset timeline
            Timeline.Reset();
        }

        private async Task PrepareEditorForSource()
        {
            if (Source == null)
            {
                FramesPerSecond = App.RefreshRate;
                IsPanAndZoomEnabled = false;
                TimeDisplayMode = TimeDisplayFormat.None;
                return;
            }

            FramesPerSecond = (int)Math.Ceiling(Source.FramesPerSecond);
            if (Source is ImageFile image)
            {
                IsPanAndZoomEnabled = true;
                TimeDisplayMode = TimeDisplayFormat.FrameNumber;
                _frameBitmap = await CanvasBitmap.LoadAsync(SwapChainCanvas.SwapChain.Device,
                                                            await image.File.OpenReadAsync());

                ScaleFrameToFit(image.WidthInPixels, image.HeightInPixels);
                ApplyFrameScaleAndPosition();

                // Configure timeline (if image is animated) and pause image on first frame
                if (image.Duration > 0)
                {
                    Timeline.Duration = TimeSpan.FromSeconds(decimal.ToDouble(image.Duration));
                    Timeline.Position = 0;
                    Timeline.IsSelectionEnabled = false;
                    Timeline.IsPositionAdjustmentEnabled = true;
                    Timeline.IsSelectionAdjustmentEnabled = false;
                    Timeline.IsZoomAdjustmentEnabled = true;
                    Timeline.ZoomOutFull();
                    PlaybackState = MediaPlaybackState.Paused;
                }
                else // Still image - manually start frame timer
                {
                    _redrawTimer.Start();
                }
            }
            else if (Source is VideoSource video)
            {
                IsPanAndZoomEnabled = false;
                TimeDisplayMode = TimeDisplayFormat.TimecodeWithFrame;

                // Initially set timeline to the expected duration
                // This is set again when the MediaPlayer reports it has opened a source
                Timeline.Duration = TimeSpan.FromSeconds(decimal.ToDouble(video.Duration));
                _player.Source = await video.GetPlaybackSourceAsync();

                // Add markers to timeline
                foreach (var marker in video.Markers)
                {
                    Timeline.Markers.Add(marker);
                }

                // Add cuts to timeline as selections
                if (!video.AreCutsApplied)
                {
                    foreach (var (start, end) in video.Cuts)
                    {
                        Timeline.AddSelection(start, end);
                    }
                }
            }

            // Add keyframes to timeline
            foreach (var keyframe in Source.Keyframes)
            {
                Timeline.Markers.Add(keyframe);
            }
        }

        private void SeekToMarker(ITimelineMarker marker)
        {
            if (marker == null)
                return;

            if (Source.ContentType == MediaContentType.Image)
                CurrentFrame = decimal.ToInt32(decimal.Round(marker.Position * FramesPerSecond));
            else
                _player.PlaybackSession.Position =
                    TimeSpan.FromSeconds(decimal.ToDouble(marker.Position));
        }

        private void ScaleFrameToFit(uint widthInPixels, uint heightInPixels)
        {
            var destWidth = SwapChainCanvas.SwapChain.SizeInPixels.Width;
            var destHeight = SwapChainCanvas.SwapChain.SizeInPixels.Height;
            var scaleW = 10 * Math.Log((double)destWidth / widthInPixels) / Math.Log(2.0);
            var scaleH = 10 * Math.Log((double)destHeight / heightInPixels) / Math.Log(2.0);
            FrameScale = Math.Min(scaleW, scaleH);
        }

        private void CalculateFrameScaleAndPosition()
        {
            var keyframes = Source.Keyframes.OfType<PanAndZoomKeyframe>().OrderBy(x => x.Position);

            // Source has no keyframes for position/scale
            if (!keyframes.Any())
                return;

            // Get current playback position
            var currentPos = (decimal)CurrentPosition.TotalSeconds;

            // Source has only one keyframe for position/scale,
            // or current position is at or before the 1st keyframe
            if (keyframes.Count() == 1 || currentPos <= keyframes.First().Position)
            {
                var currentKeyframe = keyframes.First();
                FrameOffsetX = currentKeyframe.OffsetX;
                FrameOffsetY = currentKeyframe.OffsetY;
                FrameScale = currentKeyframe.Scale;
                return;
            }

            // Current position is at or after the last keyframe
            if (currentPos >= keyframes.Last().Position)
            {
                var currentKeyframe = keyframes.First();
                currentKeyframe = keyframes.Last();
                FrameOffsetX = currentKeyframe.OffsetX;
                FrameOffsetY = currentKeyframe.OffsetY;
                FrameScale = currentKeyframe.Scale;
                return;
            }

            // Current playback position is between two keyframes
            var prevKeyframe = keyframes.Where(x => x.Position <= currentPos).OrderBy(x => currentPos - x.Position).First();
            var nextKeyframe = keyframes.Where(x => x.Position > currentPos).OrderBy(x => x.Position - currentPos).First();

            FrameOffsetX = CalculateValue(prevKeyframe.Position,
                                          nextKeyframe.Position,
                                          prevKeyframe.OffsetX,
                                          nextKeyframe.OffsetX);

            FrameOffsetY = CalculateValue(prevKeyframe.Position,
                                          nextKeyframe.Position,
                                          prevKeyframe.OffsetY,
                                          nextKeyframe.OffsetY);

            FrameScale = CalculateValue(prevKeyframe.Position,
                                        nextKeyframe.Position,
                                        prevKeyframe.Scale,
                                        nextKeyframe.Scale);

            // Local function to calculate the value of a parameter
            // at a specific time between two keyframes
            double CalculateValue(decimal t0, decimal t1, double v0, double v1)
            {
                return decimal.ToDouble(((decimal)(v1 - v0) / (t1 - t0) * (currentPos - t0)) + (decimal)v0);
            }
        }

        private void ApplyFrameScaleAndPosition()
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
        private bool TestPointOverFrame(Point point)
        {
            return _destRect.Contains(point);
        }

        private static bool TestKeyStates(Windows.System.VirtualKey key, CoreVirtualKeyStates states)
        {
            return InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(states);
        }
        #endregion
    }
}