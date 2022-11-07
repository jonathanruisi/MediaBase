using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

using JLR.Utility.WinUI;
using JLR.Utility.WinUI.Controls;
using JLR.Utility.WinUI.Dialogs;
using JLR.Utility.WinUI.Messaging;

using MediaBase.Dialogs;
using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Core;

namespace MediaBase.Controls
{
    public sealed partial class MediaEditor : UserControl
    {
        #region Constants
        // Playback Rate (Animated Image)
        private const double AnimatedImage_MinimumPlaybackRate = 0.25;
        private const double AnimatedImage_MaximumPlaybackRate = 4.0;
        private const double AnimatedImage_PlaybackRateIncrement = 0.25;

        // Playback Rate (Video)
        private const double Video_MinimumPlaybackRate = 0.5;
        private const double Video_MaximumPlaybackRate = 3.0;
        private const double Video_PlaybackRateIncrement = 0.5;

        // Group Adornments
        private const float GroupAdornment_TotalBorderSize = 12.0f;
        private const float GroupAdornment_IconSize = 60.0f;
        private const float GroupAdornment_IconSpacing = 5.0f;
        #endregion

        #region Fields
        private readonly MediaPlayer _player;
        private readonly DispatcherQueueTimer _redrawTimer;
        private SoftwareBitmap _frameSizingBitmap;
        private CanvasBitmap _frameBitmap;
        private InputCursor _primaryCursor, _hoverCursor, _dragCursor;
        private bool _isPointerCapturedForFrame;
        private Point _prevLeftMousePosition;
        private Rect _sourceRect, _destRect, _fullDestRect;
        private double _scaleFactor, _prevPlaybackRate, _mouseOffsetX, _mouseOffsetY;
        private FollowMode _prevFollowMode;
        private ValueDragType _scrubType;
        private DateTime _sourceChangedTimestamp;
        private double _textFadeOpacity, _textFadeOpacityIncrement;
        private int _currentSourceIndex, _currentSourceParentTotal;
        #endregion

        #region Properties
        public ProjectManager ViewModel => (ProjectManager)DataContext;

        public TimeSpan CurrentPosition
        {
            get => TimeSpan.FromSeconds(decimal.ToDouble(CurrentFrame / RefreshRate));
            private set => CurrentFrame =
                decimal.ToInt32(decimal.Round((decimal)value.TotalSeconds * RefreshRate));
        }

        public int RefreshRate
        {
            get => (int)GetValue(RefreshRateProperty);
            private set => SetValue(RefreshRateProperty, value);
        }

        public static readonly DependencyProperty RefreshRateProperty =
            DependencyProperty.Register("RefreshRate",
                                        typeof(int),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(App.RefreshRate,
                                            OnRefreshRateChanged));

        public MultimediaSource Source
        {
            get => (MultimediaSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source",
                                        typeof(MultimediaSource),
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

        public decimal CurrentFrame
        {
            get => (decimal)GetValue(CurrentFrameProperty);
            private set => SetValue(CurrentFrameProperty, value);
        }

        public static readonly DependencyProperty CurrentFrameProperty =
            DependencyProperty.Register("CurrentFrame",
                                        typeof(decimal),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(0M,
                                            OnCurrentFrameChanged));

        public bool IsFastPositionUpdate
        {
            get => (bool)GetValue(IsFastPositionUpdateProperty);
            set => SetValue(IsFastPositionUpdateProperty, value);
        }

        public static readonly DependencyProperty IsFastPositionUpdateProperty =
            DependencyProperty.Register("IsFastPositionUpdate",
                                        typeof(bool),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(false));

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

        public double FrameOpacity
        {
            get => (double)GetValue(FrameOpacityProperty);
            set => SetValue(FrameOpacityProperty, value);
        }

        public static readonly DependencyProperty FrameOpacityProperty =
            DependencyProperty.Register("FrameOpacity",
                                        typeof(double),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(1.0));

        public int PanSpeedMultiplier
        {
            get => (int)GetValue(PanSpeedMultiplierProperty);
            set => SetValue(PanSpeedMultiplierProperty, value);
        }

        public static readonly DependencyProperty PanSpeedMultiplierProperty =
            DependencyProperty.Register("PanSpeedMultiplier",
                                        typeof(int),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(10));

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

        public bool IsHoldCurrentPanAndZoom
        {
            get => (bool)GetValue(IsHoldCurrentPanAndZoomProperty);
            set => SetValue(IsHoldCurrentPanAndZoomProperty, value);
        }

        public static readonly DependencyProperty IsHoldCurrentPanAndZoomProperty =
            DependencyProperty.Register("IsHoldCurrentPanAndZoom",
                                        typeof(bool),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(false));

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

        public double TitleTextDisplayDuration
        {
            get => (double)GetValue(TitleTextDisplayDurationProperty);
            set => SetValue(TitleTextDisplayDurationProperty, value);
        }

        public static readonly DependencyProperty TitleTextDisplayDurationProperty =
            DependencyProperty.Register("TitleTextDisplayDuration",
                                        typeof(double),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(2.0));

        public double TitleTextFadeDuration
        {
            get => (double)GetValue(TitleTextFadeDurationProperty);
            set => SetValue(TitleTextFadeDurationProperty, value);
        }

        public static readonly DependencyProperty TitleTextFadeDurationProperty =
            DependencyProperty.Register("TitleTextFadeDuration",
                                        typeof(double),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(0.5));

        public Color TextOverlayColor
        {
            get => (Color)GetValue(TextOverlayColorProperty);
            set => SetValue(TextOverlayColorProperty, value);
        }

        public static readonly DependencyProperty TextOverlayColorProperty =
            DependencyProperty.Register("TextOverlayColor",
                                        typeof(Color),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(Colors.Black));

        public Color TextOverlayOutlineColor
        {
            get => (Color)GetValue(TextOverlayOutlineColorProperty);
            set => SetValue(TextOverlayOutlineColorProperty, value);
        }

        public static readonly DependencyProperty TextOverlayOutlineColorProperty =
            DependencyProperty.Register("TextOverlayOutlineColor",
                                        typeof(Color),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(Colors.White));

        public double TextOverlayOutlineThickness
        {
            get => (double)GetValue(TextOverlayOutlineThicknessProperty);
            set => SetValue(TextOverlayOutlineThicknessProperty, value);
        }

        public static readonly DependencyProperty TextOverlayOutlineThicknessProperty =
            DependencyProperty.Register("TextOverlayOutlineThickness",
                                        typeof(double),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(0.5));

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
        #endregion

        #region Constructor
        public MediaEditor()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ProjectManager>();

            // Initialize media player
            _player = new MediaPlayer { AutoPlay = false, IsVideoFrameServerEnabled = true };
            _player.CommandManager.IsEnabled = false;

            _player.MediaOpened += Player_MediaOpened;
            _player.MediaEnded += Player_MediaEnded;
            _player.MediaFailed += Player_MediaFailed;
            _player.VideoFrameAvailable += Player_VideoFrameAvailable;
            _player.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            _player.PlaybackSession.PlaybackRateChanged += PlaybackSession_PlaybackRateChanged;
            _player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            _player.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;

            // Initialize frame timer
            _redrawTimer = DispatcherQueue.CreateTimer();
            _redrawTimer.IsRepeating = true;
            _redrawTimer.Tick += RedrawTimer_Tick;

            // Initialize cursors
            _primaryCursor = InputSystemCursor.Create(PrimaryCursorShape);
            _hoverCursor = InputSystemCursor.Create(HoverCursorShape);
            _dragCursor = InputSystemCursor.Create(DragCursorShape);

            // Initialize commands
            InitializeCommands();
        }
        #endregion

        #region Dependency Property Callbacks
        private static void OnRefreshRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor || editor._redrawTimer is null)
                return;

            editor.UpdateRedrawInterval();

            // Timeline's FPS value is set through data binding
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
        }

        private static void OnPlaybackStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;

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

            editor.RefreshCommandStates();
        }

        private static void OnPlaybackRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;

            editor.UpdateRedrawInterval();

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
            if (d is not MediaEditor editor ||
                editor.Source is null ||
                ((decimal)e.OldValue).CompareTo((decimal)e.NewValue) == 0)
                return;

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

            editor.ViewModel.EditorCenterFrameCommand.NotifyCanExecuteChanged();
        }

        private static void OnCursorShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MediaEditor editor)
                return;

            if (e.Property == PrimaryCursorShapeProperty)
            {
                editor._primaryCursor = InputSystemCursor.Create((InputSystemCursorShape)e.NewValue);
                if (!editor._isPointerCapturedForFrame && !editor._destRect.Contains(editor._prevLeftMousePosition))
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
                if (editor._destRect.Contains(editor._prevLeftMousePosition))
                    editor.ProtectedCursor = editor._hoverCursor;
            }
        }
        #endregion

        #region Event Handlers (UserControl)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize swap chain
            SwapChainCanvas.SwapChain = new CanvasSwapChain(CanvasDevice.GetSharedDevice(),
                                                            (float)SwapChainCanvas.ActualWidth,
                                                            (float)SwapChainCanvas.ActualHeight,
                                                            PInvoke.User32.GetDpiForWindow(App.WindowHandle));

            // Set default frame rate
            RefreshRate = App.RefreshRate;

            // Register messages
            RegisterMessages();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ResetEditor();

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
            if (DispatcherQueue == null)
                return;

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
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, RefreshCommandStates);
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                PlaybackState = sender.PlaybackState;
            });
        }

        private void PlaybackSession_PlaybackRateChanged(MediaPlaybackSession sender, object args)
        {
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                PlaybackRate = sender.PlaybackRate;
            });
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            if (DispatcherQueue == null)
                return;

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                if (!IsFastPositionUpdate)
                    CurrentPosition = sender.Position;
            });
        }

        private void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
        {
            if (DispatcherQueue == null || (_scrubType != ValueDragType.Position &&
                                            _scrubType != ValueDragType.SelectionStart &&
                                            _scrubType != ValueDragType.SelectionEnd &&
                                            _scrubType != ValueDragType.Selection))
            {
                return;
            }

            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                decimal previewPosition = _scrubType switch
                {
                    ValueDragType.Position     => Timeline.Position,
                    ValueDragType.SelectionEnd => Timeline.SelectionEnd,
                    _                          => Timeline.SelectionStart
                };

                sender.Position = TimeSpan.FromSeconds(decimal.ToDouble(previewPosition));
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
                                                            (int)Source.WidthInPixels,
                                                            (int)Source.HeightInPixels,
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
            if (_frameBitmap == null || Source == null)
                return;

            using var ds = SwapChainCanvas.SwapChain.CreateDrawingSession(Colors.Black);
            var isAnimatedImagePlaying = Source.ContentType == MediaContentType.Image &&
                                         PlaybackState == MediaPlaybackState.Playing &&
                                         CurrentFrame < Source.TotalFrames;

            // Increment the frame count if we are playing an animated image
            if (isAnimatedImagePlaying)
                CurrentFrame += (decimal)PlaybackRate;
            else if (Source.ContentType == MediaContentType.Video && IsFastPositionUpdate)
                CurrentPosition = _player.PlaybackSession.Position;

            // Make adjustments based on any applicable keyframes
            // during playback and scrubbing only
            if (PlaybackState == MediaPlaybackState.Playing ||
                (PlaybackState == MediaPlaybackState.Paused &&
                 (_scrubType == ValueDragType.Position ||
                  _scrubType == ValueDragType.SelectionStart ||
                  _scrubType == ValueDragType.SelectionEnd ||
                  _scrubType == ValueDragType.Selection)))
            {
                ApplyKeyframeAdjustments();
            }
            ApplyFrameScaleAndPosition();

            // Draw the image
            ds.DrawImage(_frameBitmap, _destRect, _sourceRect, (float)FrameOpacity);

            // Determine the number of groups
            var groupOffset = 0;
            var numGroups = 0;
            if (Source.CheckGroupFlag(4))
                numGroups++;
            if (Source.CheckGroupFlag(3))
                numGroups++;
            if (Source.CheckGroupFlag(2))
                numGroups++;
            if (Source.CheckGroupFlag(1))
                numGroups++;

            // Create custom dash pattern based on the number of groups
            var dashStyle = new CanvasStrokeStyle
            {
                DashOffset = 0,
                DashStyle = CanvasDashStyle.Solid,
                DashCap = CanvasCapStyle.Flat,
                CustomDashStyle = new float[]
                {
                    GroupAdornment_TotalBorderSize / numGroups,
                    GroupAdornment_TotalBorderSize - (GroupAdornment_TotalBorderSize / numGroups)
                }
            };

            // Draw grouping adornment
            for (var i = 1; i <= 4; i++)
            {
                if (Source.CheckGroupFlag(i) == false)
                    continue;

                var color = i switch
                {
                    1 => Colors.Gold,
                    2 => Colors.CornflowerBlue,
                    3 => Colors.IndianRed,
                    4 => Colors.ForestGreen,
                    _ => throw new NotImplementedException()
                };

                dashStyle.DashOffset = groupOffset * (GroupAdornment_TotalBorderSize / numGroups);
                ds.DrawRectangle(new Rect(_destRect.Left + (GroupAdornment_TotalBorderSize / 2),
                                          _destRect.Top + (GroupAdornment_TotalBorderSize / 2),
                                          _destRect.Width - GroupAdornment_TotalBorderSize,
                                          _destRect.Height - GroupAdornment_TotalBorderSize),
                                 color, GroupAdornment_TotalBorderSize, dashStyle);

                var xOffset = (float)_destRect.Left +
                              GroupAdornment_TotalBorderSize +
                              GroupAdornment_IconSpacing +
                              (GroupAdornment_IconSpacing * groupOffset) +
                              (groupOffset * GroupAdornment_IconSize);
                var yOffset = (float)_destRect.Top +
                              GroupAdornment_TotalBorderSize +
                              GroupAdornment_IconSpacing;

                ds.FillEllipse(xOffset + (GroupAdornment_IconSize / 2),
                               yOffset + (GroupAdornment_IconSize / 2),
                               GroupAdornment_IconSize / 2,
                               GroupAdornment_IconSize / 2,
                               color);

                groupOffset++;
            }

            // Draw timecode / frame count
            if (IsPlaybackPossible && TimeDisplayMode != TimeDisplayFormat.None)
            {
                var timeStr = TimeDisplayMode == TimeDisplayFormat.FrameNumber
                    ? $"Frame: {CurrentFrame:#}"
                    : CurrentPosition.TotalSeconds.ToTimecodeString(RefreshRate, TimeDisplayMode);

                var timeRemainStr = TimeDisplayMode == TimeDisplayFormat.FrameNumber
                    ? $"Remaining Frames: {Source.TotalFrames - CurrentFrame:#}"
                    : (decimal.ToDouble(Source.Duration) - CurrentPosition.TotalSeconds).ToTimecodeString(RefreshRate, TimeDisplayMode);

                using var positionTextFormat = new CanvasTextFormat
                {
                    FontFamily = FontFamily.Source,
                    FontSize = (float)FontSize,
                    FontStretch = FontStretch,
                    FontStyle = FontStyle,
                    FontWeight = FontWeight,
                    HorizontalAlignment = CanvasHorizontalAlignment.Left
                };

                using var timeRemainTextFormat = new CanvasTextFormat
                {
                    FontFamily = FontFamily.Source,
                    FontSize = (float)FontSize,
                    FontStretch = FontStretch,
                    FontStyle = FontStyle,
                    FontWeight = FontWeight,
                    HorizontalAlignment = CanvasHorizontalAlignment.Right
                };

                using var positionTextLayout = new CanvasTextLayout(ds, timeStr, positionTextFormat,
                    (float)SwapChainCanvas.ActualWidth, (float)SwapChainCanvas.ActualHeight);
                using var positionTextGeometry = CanvasGeometry.CreateText(positionTextLayout);

                using var timeRemainTextLayout = new CanvasTextLayout(ds, timeRemainStr, timeRemainTextFormat,
                    (float)SwapChainCanvas.ActualWidth, (float)SwapChainCanvas.ActualHeight);
                using var timeRemainTextGeometry = CanvasGeometry.CreateText(timeRemainTextLayout);

                ds.FillGeometry(positionTextGeometry, 15, 15, TextOverlayColor);
                ds.DrawGeometry(positionTextGeometry, 15, 15, TextOverlayOutlineColor, (float)TextOverlayOutlineThickness);

                ds.FillGeometry(timeRemainTextGeometry, -15, 15, TextOverlayColor);
                ds.DrawGeometry(timeRemainTextGeometry, -15, 15, TextOverlayOutlineColor, (float)TextOverlayOutlineThickness);
            }

            // Determine title text opacity (during title text fade)
            if (DateTime.Now - _sourceChangedTimestamp <= TimeSpan.FromSeconds(TitleTextDisplayDuration))
            {
                _textFadeOpacity = 1.0;
                _textFadeOpacityIncrement = 0;
                (_currentSourceIndex, _currentSourceParentTotal) = ViewModel.GetActiveMediaSourceIndexAndParentTotal();
            }
            else if (_textFadeOpacity == 1.0 && _textFadeOpacityIncrement == 0)
            {
                _textFadeOpacityIncrement = 1.0 / (TitleTextFadeDuration * RefreshRate);
            }
            else if (_textFadeOpacity > 0 && _textFadeOpacityIncrement > 0)
            {
                _textFadeOpacity -= _textFadeOpacityIncrement;
            }

            // Display title text (during title text display and fade-out)
            if (_textFadeOpacity > 0)
            {
                using var titleTextFormat = new CanvasTextFormat
                {
                    FontFamily = FontFamily.Source,
                    FontSize = (float)FontSize,
                    FontStretch = FontStretch,
                    FontStyle = FontStyle,
                    FontWeight = FontWeight,
                    HorizontalAlignment = CanvasHorizontalAlignment.Left
                };

                // Current source number out of total in folder
                using var sourceNumberTextLayout = new CanvasTextLayout(ds,
                    $"{_currentSourceIndex}/{_currentSourceParentTotal}", titleTextFormat,
                    (float)SwapChainCanvas.ActualWidth, (float)SwapChainCanvas.ActualHeight);
                using var sourceNumberTextGeometry = CanvasGeometry.CreateText(sourceNumberTextLayout);

                var x = (float)((SwapChainCanvas.ActualWidth / 2.0) - (sourceNumberTextLayout.DrawBounds.Width / 2.0));
                var y = 5.0f;

                ds.FillGeometry(sourceNumberTextGeometry, x, y, Color.FromArgb((byte)(_textFadeOpacity * 255),
                    TextOverlayColor.R, TextOverlayColor.G, TextOverlayColor.B));
                ds.DrawGeometry(sourceNumberTextGeometry, x, y, Color.FromArgb((byte)(_textFadeOpacity * 255),
                    TextOverlayOutlineColor.R, TextOverlayOutlineColor.G, TextOverlayOutlineColor.B),
                    (float)TextOverlayOutlineThickness);

                // Source title
                using var titleTextLayout = new CanvasTextLayout(ds, Source.Name, titleTextFormat,
                    (float)SwapChainCanvas.ActualWidth, (float)SwapChainCanvas.ActualHeight);
                using var titleTextGeometry = CanvasGeometry.CreateText(titleTextLayout);

                x = (float)((SwapChainCanvas.ActualWidth / 2.0) - (titleTextLayout.DrawBounds.Width / 2.0));
                y = (float)(SwapChainCanvas.ActualHeight - titleTextLayout.DrawBounds.Height - 15);

                ds.FillGeometry(titleTextGeometry, x, y, Color.FromArgb((byte)(_textFadeOpacity * 255),
                    TextOverlayColor.R, TextOverlayColor.G, TextOverlayColor.B));
                ds.DrawGeometry(titleTextGeometry, x, y, Color.FromArgb((byte)(_textFadeOpacity * 255),
                    TextOverlayOutlineColor.R, TextOverlayOutlineColor.G, TextOverlayOutlineColor.B),
                    (float)TextOverlayOutlineThickness);
            }

            SwapChainCanvas.SwapChain.Present();
        }
        #endregion

        #region Event Handlers (SwapChain)
        private void SwapChainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SwapChainCanvas.SwapChain?.ResizeBuffers(e.NewSize);

            if (!IsHoldCurrentPanAndZoom)
            {
                FrameOffsetX = 0;
                FrameOffsetY = 0;
                if (Source != null)
                    FrameScale = decimal.ToDouble(CalculateFrameScaleToFit(Source.WidthInPixels,
                                                                       Source.HeightInPixels));
            }
        }
        #endregion

        #region Event Handlers (Timeline)
        private void Timeline_PositionChanged(object sender, decimal e)
        {
            if (_scrubType is ValueDragType.Position or
                              ValueDragType.SelectionStart or
                              ValueDragType.SelectionEnd or
                              ValueDragType.Selection)
            {
                // This won't affect MediaPlayer position.
                // It is needed to scrub animated images.
                CurrentFrame = decimal.ToInt32(decimal.Round(e * RefreshRate));
            }
        }

        private void Timeline_SelectionChanged(object sender, (decimal start, decimal end, bool isEnabled) e)
        {
            ViewModel.EditorNewMarkerCommand.NotifyCanExecuteChanged();
            ViewModel.EditorCutSelectedCommand.NotifyCanExecuteChanged();
        }

        private void Timeline_ZoomChanged(object sender, (decimal start, decimal end) e)
        {
            ViewModel.EditorTimelineZoomInCommand.NotifyCanExecuteChanged();
            ViewModel.EditorTimelineZoomOutCommand.NotifyCanExecuteChanged();
        }

        private void Timeline_DragStarted(object sender, ValueDragType e)
        {
            _scrubType = e;
            _prevFollowMode = Timeline.PositionFollowMode;
            Timeline.PositionFollowMode = FollowMode.NoFollow;

            // Set playback position to the control being scrubbed
            if (e is ValueDragType.Position or
                     ValueDragType.SelectionStart or
                     ValueDragType.SelectionEnd or
                     ValueDragType.Selection)
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
                    CurrentFrame = decimal.ToInt32(decimal.Round(previewPosition * RefreshRate));
                }
            }
        }

        private void Timeline_DragCompleted(object sender, ValueDragType e)
        {
            // Set playback to playhead position
            if (e is ValueDragType.Position or
                     ValueDragType.SelectionStart or
                     ValueDragType.SelectionEnd or
                     ValueDragType.Selection)
            {
                if (Source.ContentType == MediaContentType.Video)
                {
                    _player.PlaybackSession.Position =
                        TimeSpan.FromSeconds(decimal.ToDouble(Timeline.Position));
                    _player.PlaybackSession.PlaybackRate = _prevPlaybackRate;
                }
                else
                {
                    CurrentFrame = decimal.ToInt32(decimal.Round(Timeline.Position * RefreshRate));
                    PlaybackRate = _prevPlaybackRate;
                }
            }

            // If a marker is selected and the current selection changed, update the marker
            if ((e == ValueDragType.SelectionStart ||
                 e == ValueDragType.SelectionEnd ||
                 e == ValueDragType.Selection) &&
                ViewModel.SelectedMarker != null &&
                ViewModel.SelectedMarker.Duration > 0)
            {
                var index = Source.Markers.IndexOf(ViewModel.SelectedMarker);

                if (index >= 0)
                {
                    ViewModel.SelectedMarker = null;

                    var adjustedMarker = new Marker
                    {
                        Name = Source.Markers[index].Name,
                        Position = Timeline.SelectionStart,
                        Duration = Timeline.SelectionEnd - Timeline.SelectionStart,
                        Group = Source.Markers[index].Group
                    };

                    // Track order will change if the last marker on a track is removed
                    // and another modified track put back in its place if
                    // AutoRemoveEmptyTracks = true.
                    var prevAutoRemoveSetting = Timeline.AutoRemoveEmptyTracks;
                    Timeline.AutoRemoveEmptyTracks = false;

                    Source.Markers.RemoveAt(index);
                    Source.Markers.Insert(index, adjustedMarker);

                    Timeline.AutoRemoveEmptyTracks = prevAutoRemoveSetting;
                    ViewModel.SelectedMarker = Source.Markers[index];
                }
            }

            Timeline.PositionFollowMode = _prevFollowMode;
            _scrubType = ValueDragType.None;
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
                var isCtrlPressed = App.TestKeyStates(Windows.System.VirtualKey.Control, CoreVirtualKeyStates.Down);

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
                ProtectedCursor = _destRect.Contains(point.Position) && IsPanAndZoomEnabled
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
            var isPointerOverFrame = _destRect.Contains(point.Position);

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

            var point = e.GetCurrentPoint(RenderAreaBorder);
            var delta = point.Properties.MouseWheelDelta / 120;

            if (App.TestKeyStates(Windows.System.VirtualKey.Control, CoreVirtualKeyStates.Down))
            {// Scale
                FrameScale += delta;

                if (_fullDestRect.Contains(point.Position))
                {
                    var newScaleFactor = Math.Pow(2, 0.1 * FrameScale);

                    var newScaledWidth = Math.Round(_frameBitmap.SizeInPixels.Width * newScaleFactor, 6);
                    var newScaledHeight = Math.Round(_frameBitmap.SizeInPixels.Height * newScaleFactor, 6);

                    var widthDelta = newScaledWidth - _fullDestRect.Width;
                    var heightDelta = newScaledHeight - _fullDestRect.Height;

                    _mouseOffsetX = point.Position.X - _fullDestRect.GetCenterPoint().X;
                    FrameOffsetX -= (widthDelta * (_mouseOffsetX / (_fullDestRect.Width / 2))) / 2;

                    _mouseOffsetY = point.Position.Y - _fullDestRect.GetCenterPoint().Y;
                    FrameOffsetY += (heightDelta * (_mouseOffsetY / (_fullDestRect.Height / 2))) / 2;
                }
            }
            else if (App.TestKeyStates(Windows.System.VirtualKey.Shift, CoreVirtualKeyStates.Down))
            {// Horizontal scroll
                FrameOffsetX += delta * PanSpeedMultiplier;
            }
            else
            {// Vertical scroll
                FrameOffsetY -= delta * PanSpeedMultiplier;
            }
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
                              Timeline.GetClosestMarkerBeforeCurrentPosition<Marker>(0.5M) != null;
        }

        private void EditorNextMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              Timeline.GetClosestMarkerAfterCurrentPosition<Marker>(0.5M) != null;
        }

        private void EditorToggleActiveSelectionCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible && Source.ContentType == MediaContentType.Video;
        }

        private void EditorAddTrackCommmand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible && Source.ContentType == MediaContentType.Video;
        }

        private void EditorNewMarkerCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible && Source.ContentType == MediaContentType.Video;
        }

        private void EditorNewKeyframeCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible;
        }

        private void EditorCutSelectedCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              Source.ContentType == MediaContentType.Video &&
                              Timeline.IsSelectionEnabled &&
                              Timeline.SelectionStart != Timeline.SelectionEnd;
        }

        private void EditorPlaybackRateDecreaseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              PlaybackState == MediaPlaybackState.Playing &&
                              Source.ContentType == MediaContentType.Image
                                ? PlaybackRate > AnimatedImage_MinimumPlaybackRate
                                : PlaybackRate > Video_MinimumPlaybackRate;
        }

        private void EditorPlaybackRateIncreaseCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              PlaybackState == MediaPlaybackState.Playing &&
                              Source.ContentType == MediaContentType.Image
                                ? PlaybackRate < AnimatedImage_MaximumPlaybackRate
                                : PlaybackRate < Video_MaximumPlaybackRate;
        }

        private void EditorPlaybackRateNormalCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsPlaybackPossible &&
                              PlaybackState == MediaPlaybackState.Playing &&
                              PlaybackRate != 1.0;
        }

        private void EditorTogglePanAndZoomLockCommand_CanExecuteRequested(XamlUICommand sender, CanExecuteRequestedEventArgs args)
        {
            args.CanExecute = IsLoaded && Source != null;
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
            SeekToMarker(Timeline.GetClosestMarkerBeforeCurrentPosition<Marker>(0.5M));
        }

        private void EditorNextMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
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

        private async void EditorAddTrackCommmand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var dlg = new TextPromptDialog
            {
                Title = "New Track",
                PromptText = "Enter a name for the track",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = Content.XamlRoot
            };

            var result = await dlg.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            if (Source.Tracks.Contains(dlg.Text))
                App.ShowMessageBoxAsync("Track already exists in this media", "Duplicate Track");
            else
                Source.Tracks.Add(dlg.Text);
        }

        private async void EditorNewMarkerCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            // Record current position here so that the new marker reflects
            // the time at which the command was invoked, as opposed to the time
            // at which the user completes the process of adding the marker.
            var pos = CurrentPosition;
            var isClip = Timeline.IsSelectionEnabled &&
                         Timeline.SelectionStart != Timeline.SelectionEnd;

            var dlg = new MarkerDialog
            {
                Title = "New Marker",
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                IsSecondaryButtonEnabled = false,
                TimeDisplayMode = TimeDisplayFormat.TimecodeWithFrame,
                Position = isClip ? Timeline.SelectionStart : (decimal)pos.TotalSeconds,
                Duration = isClip ? Timeline.SelectionEnd - Timeline.SelectionStart : 0,
                FramesPerSecond = RefreshRate,
                XamlRoot = XamlRoot
            };

            foreach (var style in Timeline.MarkerStyleGroups)
            {
                dlg.MarkerStyles.Add(style.Key);
            }

            var result = await dlg.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            var marker = new Marker
            {
                Name = dlg.MarkerName,
                Position = dlg.Position,
                Duration = dlg.Duration,
                Group = dlg.Track,
                Style = dlg.MarkerStyle
            };

            if (marker.Duration > 0 && string.IsNullOrEmpty(marker.Group))
            {
                var newTrack = $"Track {Source.Tracks.Count + 1}";
                Source.Tracks.Add(newTrack);
                marker.Group = newTrack;
            }

            var index = 0;
            while (index < Source.Markers.Count && Source.Markers[index].Position <= marker.Position)
                index++;

            Source.Markers.Insert(index, marker);
        }

        private void EditorNewKeyframeCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            var keyframe = new Keyframe((decimal)CurrentPosition.TotalSeconds, "Keyframes");

            string scaleValue;
            if (Math.Round((decimal)FrameScale, 6).ToString() ==
                Math.Round(CalculateFrameScaleToFit(Source.WidthInPixels, Source.HeightInPixels), 6).ToString())
                scaleValue = "Fit";
            else
                scaleValue = Math.Round((decimal)FrameScale, 6).ToString();

            var offsetXValue = Math.Round((decimal)FrameOffsetX, 4).ToString();
            var offsetYValue = Math.Round((decimal)FrameOffsetY, 4).ToString();
            var opacityValue = Math.Round((decimal)Opacity, 6).ToString();
            var playbackRateValue = Math.Round((decimal)PlaybackRate, 2).ToString();

            if (!Source.Keyframes.Any())
            {
                keyframe.Adjustments.Add(KeyframeAdjustment.Scale, scaleValue);
                keyframe.Adjustments.Add(KeyframeAdjustment.OffsetX, offsetXValue);
                keyframe.Adjustments.Add(KeyframeAdjustment.OffsetY, offsetYValue);
                keyframe.Adjustments.Add(KeyframeAdjustment.Opacity, opacityValue);
                keyframe.Adjustments.Add(KeyframeAdjustment.PlaybackRate, playbackRateValue);
            }
            else
            {
                if (Source.Keyframes.Any(x => x.Adjustments.ContainsKey(KeyframeAdjustment.Scale) &&
                                              x.Adjustments[KeyframeAdjustment.Scale].Split(',')[0] != scaleValue))
                    keyframe.Adjustments.Add(KeyframeAdjustment.Scale, scaleValue);

                if (Source.Keyframes.Any(x => x.Adjustments.ContainsKey(KeyframeAdjustment.OffsetX) &&
                                              x.Adjustments[KeyframeAdjustment.OffsetX].Split(',')[0] != offsetXValue))
                    keyframe.Adjustments.Add(KeyframeAdjustment.OffsetX, offsetXValue);

                if (Source.Keyframes.Any(x => x.Adjustments.ContainsKey(KeyframeAdjustment.OffsetY) &&
                                              x.Adjustments[KeyframeAdjustment.OffsetY].Split(',')[0] != offsetYValue))
                    keyframe.Adjustments.Add(KeyframeAdjustment.OffsetY, offsetYValue);

                if (Source.Keyframes.Any(x => x.Adjustments.ContainsKey(KeyframeAdjustment.Opacity) &&
                                              x.Adjustments[KeyframeAdjustment.Opacity].Split(',')[0] != opacityValue))
                    keyframe.Adjustments.Add(KeyframeAdjustment.Opacity, opacityValue);

                if (Source.Keyframes.Any(x => x.Adjustments.ContainsKey(KeyframeAdjustment.PlaybackRate) &&
                                              x.Adjustments[KeyframeAdjustment.PlaybackRate].Split(',')[0] != playbackRateValue))
                    keyframe.Adjustments.Add(KeyframeAdjustment.PlaybackRate, playbackRateValue);
            }

            // Add the keyframe to the source if adjustments were made
            if (keyframe.Adjustments.Count > 0)
            {
                Source.Markers.Add(keyframe);
            }
        }

        private void EditorCutSelectedCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source is not VideoSource video)
                return;

            video.Cuts.Add((Timeline.SelectionStart, Timeline.SelectionEnd));
        }

        private void EditorPlaybackRateDecreaseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                PlaybackRate -= AnimatedImage_PlaybackRateIncrement;
            else
                _player.PlaybackSession.PlaybackRate -= Video_PlaybackRateIncrement;
        }

        private void EditorPlaybackRateIncreaseCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (Source.ContentType == MediaContentType.Image)
                PlaybackRate += AnimatedImage_PlaybackRateIncrement;
            else
                _player.PlaybackSession.PlaybackRate += Video_PlaybackRateIncrement;
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
            FrameScale = decimal.ToDouble(CalculateFrameScaleToFit(Source.WidthInPixels, Source.HeightInPixels));
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
        #endregion

        #region Private Properties
        private bool IsPlaybackPossible
        {
            get
            {
                if (!IsLoaded || Source == null || (Source is IMultimediaItem mmItem && !mmItem.IsReady))
                    return false;

                return Source is ViewModel.ImageSource image
                    ? image.IsAnimated
                    : _player.Source != null &&
                      (PlaybackState == MediaPlaybackState.Playing ||
                       PlaybackState == MediaPlaybackState.Paused);
            }
        }
        #endregion

        #region Private Methods
        private void ResetEditor()
        {
            // Stop current playback
            if (PlaybackState is MediaPlaybackState.Opening or
                                 MediaPlaybackState.Buffering or
                                 MediaPlaybackState.Playing)
            {
                _player.Pause();
            }

            // Halt redraw timer
            _redrawTimer.Stop();

            // Reset various properties
            if (PlaybackState != MediaPlaybackState.None)
                PlaybackState = MediaPlaybackState.None;
            CurrentFrame = 0;
            PlaybackRate = 1;
            TimeDisplayMode = TimeDisplayFormat.None;
            _prevFollowMode = FollowMode.NoFollow;

            // De-select all markers
            ViewModel.SelectedMarker = null;

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
            if (!IsHoldCurrentPanAndZoom)
            {
                FrameScale = 0;
                FrameOffsetX = 0;
                FrameOffsetY = 0;
            }

            // Reset timeline
            Timeline.Reset();
        }

        private async Task PrepareEditorForSource()
        {
            if (Source == null)
            {
                RefreshRate = App.RefreshRate;
                TimeDisplayMode = TimeDisplayFormat.None;
                return;
            }

            IsPanAndZoomEnabled = true;

            if (!Source.IsReady)
                await Source.MakeReady();

            RefreshRate = Source.FramesPerSecond > 0
                ? (int)Math.Ceiling(Source.FramesPerSecond)
                : App.RefreshRate;

            if (Source is ViewModel.ImageSource image)
            {
                TimeDisplayMode = TimeDisplayFormat.FrameNumber;
                _frameBitmap = await CanvasBitmap.LoadAsync(SwapChainCanvas.SwapChain.Device,
                    await (image.Source as ImageFile).File.OpenReadAsync());
                if (!IsHoldCurrentPanAndZoom)
                    FrameScale = decimal.ToDouble(CalculateFrameScaleToFit(Source.WidthInPixels, Source.HeightInPixels));
                ApplyFrameScaleAndPosition();

                // Configure timeline (if image is animated) and pause on first frame
                if (image.IsAnimated)
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
                else
                {
                    _redrawTimer.Start();
                }
            }
            else if (Source is VideoSource video)
            {
                TimeDisplayMode = TimeDisplayFormat.TimecodeWithFrame;
                Timeline.Duration = TimeSpan.FromSeconds(decimal.ToDouble(video.Duration));
                Timeline.Position = 0;

                _player.Source = await video.BuildMediaSourceAsync();
                
                if (!IsHoldCurrentPanAndZoom)
                    FrameScale = decimal.ToDouble(CalculateFrameScaleToFit(Source.WidthInPixels, Source.HeightInPixels));

                // Add cuts to timeline as selections
                if (!video.AreCutsApplied)
                {
                    foreach (var (start, end) in video.Cuts)
                    {
                        Timeline.AddSelection(start, end);
                    }
                }
            }

            foreach (var marker in Source.Markers)
            {
                Timeline.Markers.Add(marker);
            }

            _sourceChangedTimestamp = DateTime.Now;
        }

        private void SeekToMarker(ITimelineMarker marker)
        {
            if (marker == null)
                return;

            if (Source.ContentType == MediaContentType.Image)
                CurrentFrame = decimal.ToInt32(decimal.Round(marker.Position * RefreshRate));
            else if (Source.ContentType == MediaContentType.Video)
                _player.PlaybackSession.Position =
                    TimeSpan.FromSeconds(decimal.ToDouble(marker.Position));
        }

        private void ApplyKeyframeAdjustments()
        {
            var currentPos = (decimal)CurrentPosition.TotalSeconds;

            var scaleKeyframes = new LinkedList<Keyframe>(Source.Keyframes.Where(x => x.Adjustments.ContainsKey(KeyframeAdjustment.Scale)));
            var offsetXKeyframes = new LinkedList<Keyframe>(Source.Keyframes.Where(x => x.Adjustments.ContainsKey(KeyframeAdjustment.OffsetX)));
            var offsetYKeyframes = new LinkedList<Keyframe>(Source.Keyframes.Where(x => x.Adjustments.ContainsKey(KeyframeAdjustment.OffsetY)));
            var opacityKeyframes = new LinkedList<Keyframe>(Source.Keyframes.Where(x => x.Adjustments.ContainsKey(KeyframeAdjustment.Opacity)));
            var playbackRateKeyframes = new LinkedList<Keyframe>(Source.Keyframes.Where(x => x.Adjustments.ContainsKey(KeyframeAdjustment.PlaybackRate)));

            if (scaleKeyframes.Count > 0)
            {
                IsPanAndZoomEnabled = true;

                if (scaleKeyframes.Count == 1 ||
                    currentPos < scaleKeyframes.First.Value.Position ||
                    currentPos >= scaleKeyframes.Last.Value.Position)
                {
                    string valueString = null;

                    if (scaleKeyframes.Count == 1 || currentPos < scaleKeyframes.First.Value.Position)
                        valueString = scaleKeyframes.First.Value.Adjustments[KeyframeAdjustment.Scale];
                    else if (currentPos >= scaleKeyframes.Last.Value.Position)
                        valueString = scaleKeyframes.Last.Value.Adjustments[KeyframeAdjustment.Scale];

                    var valueStrings = valueString.Split(',');
                    FrameScale = decimal.ToDouble(ParseScale(valueStrings[0]));
                }
                else
                {
                    var prevScaleKeyframe = scaleKeyframes.First;
                    while (prevScaleKeyframe.Next.Value.Position <= currentPos)
                        prevScaleKeyframe = prevScaleKeyframe.Next;

                    var nextScaleKeyframe = scaleKeyframes.Last;
                    while (nextScaleKeyframe.Previous.Value.Position > currentPos)
                        nextScaleKeyframe = nextScaleKeyframe.Previous;

                    var prevScaleValueStrings = prevScaleKeyframe.Value.Adjustments[KeyframeAdjustment.Scale].Split(',');
                    var isPrevHold = prevScaleValueStrings.Length > 1 && prevScaleValueStrings[1][0] == 'H';

                    var prevScaleValue = ParseScale(prevScaleValueStrings[0]);
                    var nextScaleValue = ParseScale(nextScaleKeyframe.Value.Adjustments[KeyframeAdjustment.Scale].Split(',')[0]);

                    if (isPrevHold)
                        FrameScale = decimal.ToDouble(prevScaleValue);
                    else
                        FrameScale = CalculateValue(prevScaleKeyframe.Value.Position, nextScaleKeyframe.Value.Position,
                                                    prevScaleValue, nextScaleValue);
                }

                decimal ParseScale(string valueString)
                {
                    if (valueString == "Fit")
                        return CalculateFrameScaleToFit(Source.WidthInPixels, Source.HeightInPixels);
                    else
                    {
                        if (!decimal.TryParse(valueString, out var value))
                            throw new Exception("Unable to parse keyframe adjustment value");
                        return value;
                    }
                }
            }

            if (offsetXKeyframes.Count > 0)
            {
                IsPanAndZoomEnabled = true;

                if (offsetXKeyframes.Count == 1 ||
                    currentPos < offsetXKeyframes.First.Value.Position ||
                    currentPos >= offsetXKeyframes.Last.Value.Position)
                {
                    string valueString = null;

                    if (offsetXKeyframes.Count == 1 || currentPos < offsetXKeyframes.First.Value.Position)
                        valueString = offsetXKeyframes.First.Value.Adjustments[KeyframeAdjustment.OffsetX];
                    else if (currentPos >= offsetXKeyframes.Last.Value.Position)
                        valueString = offsetXKeyframes.Last.Value.Adjustments[KeyframeAdjustment.OffsetX];

                    var valueStrings = valueString.Split(',');
                    FrameOffsetX = decimal.ToDouble(ParseValue(valueStrings[0]));
                }
                else
                {
                    var prevOffsetXKeyframe = offsetXKeyframes.First;
                    while (prevOffsetXKeyframe.Next.Value.Position <= currentPos)
                        prevOffsetXKeyframe = prevOffsetXKeyframe.Next;

                    var nextOffsetXKeyframe = offsetXKeyframes.Last;
                    while (nextOffsetXKeyframe.Previous.Value.Position > currentPos)
                        nextOffsetXKeyframe = nextOffsetXKeyframe.Previous;

                    var prevOffsetXValueStrings = prevOffsetXKeyframe.Value.Adjustments[KeyframeAdjustment.OffsetX].Split(',');
                    var isPrevHold = prevOffsetXValueStrings.Length > 1 && prevOffsetXValueStrings[1][0] == 'H';

                    var prevOffsetXValue = ParseValue(prevOffsetXValueStrings[0]);
                    var nextOffsetXValue = ParseValue(nextOffsetXKeyframe.Value.Adjustments[KeyframeAdjustment.OffsetX].Split(',')[0]);

                    if (isPrevHold)
                        FrameOffsetX = decimal.ToDouble(prevOffsetXValue);
                    else
                        FrameOffsetX = CalculateValue(prevOffsetXKeyframe.Value.Position, nextOffsetXKeyframe.Value.Position,
                                                      prevOffsetXValue, nextOffsetXValue);
                }
            }

            if (offsetYKeyframes.Count > 0)
            {
                IsPanAndZoomEnabled = true;

                if (offsetYKeyframes.Count == 1 ||
                    currentPos < offsetYKeyframes.First.Value.Position ||
                    currentPos >= offsetYKeyframes.Last.Value.Position)
                {
                    string valueString = null;

                    if (offsetYKeyframes.Count == 1 || currentPos < offsetYKeyframes.First.Value.Position)
                        valueString = offsetYKeyframes.First.Value.Adjustments[KeyframeAdjustment.OffsetY];
                    else if (currentPos >= offsetYKeyframes.Last.Value.Position)
                        valueString = offsetYKeyframes.Last.Value.Adjustments[KeyframeAdjustment.OffsetY];

                    var valueStrings = valueString.Split(',');
                    FrameOffsetY = decimal.ToDouble(ParseValue(valueStrings[0]));
                }
                else
                {
                    var prevOffsetYKeyframe = offsetYKeyframes.First;
                    while (prevOffsetYKeyframe.Next.Value.Position <= currentPos)
                        prevOffsetYKeyframe = prevOffsetYKeyframe.Next;

                    var nextOffsetYKeyframe = offsetYKeyframes.Last;
                    while (nextOffsetYKeyframe.Previous.Value.Position > currentPos)
                        nextOffsetYKeyframe = nextOffsetYKeyframe.Previous;

                    var prevOffsetYValueStrings = prevOffsetYKeyframe.Value.Adjustments[KeyframeAdjustment.OffsetY].Split(',');
                    var isPrevHold = prevOffsetYValueStrings.Length > 1 && prevOffsetYValueStrings[1][0] == 'H';

                    var prevOffsetYValue = ParseValue(prevOffsetYValueStrings[0]);
                    var nextOffsetYValue = ParseValue(nextOffsetYKeyframe.Value.Adjustments[KeyframeAdjustment.OffsetY].Split(',')[0]);

                    if (isPrevHold)
                        FrameOffsetY = decimal.ToDouble(prevOffsetYValue);
                    else
                        FrameOffsetY = CalculateValue(prevOffsetYKeyframe.Value.Position, nextOffsetYKeyframe.Value.Position,
                                                      prevOffsetYValue, nextOffsetYValue);
                }
            }

            if (opacityKeyframes.Count > 0)
            {
                if (opacityKeyframes.Count == 1 ||
                    currentPos < opacityKeyframes.First.Value.Position ||
                    currentPos >= opacityKeyframes.Last.Value.Position)
                {
                    string valueString = null;

                    if (opacityKeyframes.Count == 1 || currentPos < opacityKeyframes.First.Value.Position)
                        valueString = opacityKeyframes.First.Value.Adjustments[KeyframeAdjustment.Opacity];
                    else if (currentPos >= opacityKeyframes.Last.Value.Position)
                        valueString = opacityKeyframes.Last.Value.Adjustments[KeyframeAdjustment.Opacity];

                    var valueStrings = valueString.Split(',');
                    FrameOpacity = decimal.ToDouble(ParseValue(valueStrings[0]));
                }
                else
                {
                    var prevOpacityKeyframe = opacityKeyframes.First;
                    while (prevOpacityKeyframe.Next.Value.Position <= currentPos)
                        prevOpacityKeyframe = prevOpacityKeyframe.Next;

                    var nextOpacityKeyframe = opacityKeyframes.Last;
                    while (nextOpacityKeyframe.Previous.Value.Position > currentPos)
                        nextOpacityKeyframe = nextOpacityKeyframe.Previous;

                    var prevOpacityValueStrings = prevOpacityKeyframe.Value.Adjustments[KeyframeAdjustment.Opacity].Split(',');
                    var isPrevHold = prevOpacityValueStrings.Length > 1 && prevOpacityValueStrings[1][0] == 'H';

                    var prevOpacityValue = ParseValue(prevOpacityValueStrings[0]);
                    var nextOpacityValue = ParseValue(nextOpacityKeyframe.Value.Adjustments[KeyframeAdjustment.Opacity].Split(',')[0]);

                    if (isPrevHold)
                        FrameOpacity = decimal.ToDouble(prevOpacityValue);
                    else
                        FrameOpacity = CalculateValue(prevOpacityKeyframe.Value.Position, nextOpacityKeyframe.Value.Position,
                                                      prevOpacityValue, nextOpacityValue);
                }
            }

            if (playbackRateKeyframes.Count > 0)
            {
                if (playbackRateKeyframes.Count == 1 ||
                    currentPos < playbackRateKeyframes.First.Value.Position ||
                    currentPos >= playbackRateKeyframes.Last.Value.Position)
                {
                    string valueString = null;

                    if (playbackRateKeyframes.Count == 1 || currentPos < playbackRateKeyframes.First.Value.Position)
                        valueString = playbackRateKeyframes.First.Value.Adjustments[KeyframeAdjustment.PlaybackRate];
                    else if (currentPos >= playbackRateKeyframes.Last.Value.Position)
                        valueString = playbackRateKeyframes.Last.Value.Adjustments[KeyframeAdjustment.PlaybackRate];

                    var valueStrings = valueString.Split(',');
                    PlaybackRate = decimal.ToDouble(ParseValue(valueStrings[0]));
                }
                else
                {
                    var prevPlaybackRateKeyframe = playbackRateKeyframes.First;
                    while (prevPlaybackRateKeyframe.Next.Value.Position <= currentPos)
                        prevPlaybackRateKeyframe = prevPlaybackRateKeyframe.Next;

                    var nextPlaybackRateKeyframe = playbackRateKeyframes.Last;
                    while (nextPlaybackRateKeyframe.Previous.Value.Position > currentPos)
                        nextPlaybackRateKeyframe = nextPlaybackRateKeyframe.Previous;

                    var prevPlaybackRateValueStrings = prevPlaybackRateKeyframe.Value.Adjustments[KeyframeAdjustment.PlaybackRate].Split(',');
                    var isPrevHold = prevPlaybackRateValueStrings.Length > 1 && prevPlaybackRateValueStrings[1][0] == 'H';

                    var prevPlaybackRateValue = ParseValue(prevPlaybackRateValueStrings[0]);
                    var nextPlaybackRateValue = ParseValue(nextPlaybackRateKeyframe.Value.Adjustments[KeyframeAdjustment.PlaybackRate].Split(',')[0]);

                    if (isPrevHold)
                        PlaybackRate = decimal.ToDouble(prevPlaybackRateValue);
                    else
                        PlaybackRate = CalculateValue(prevPlaybackRateKeyframe.Value.Position, nextPlaybackRateKeyframe.Value.Position,
                                                      prevPlaybackRateValue, nextPlaybackRateValue);
                }
            }

            // Local function to parse a keyframe adjustment string
            decimal ParseValue(string valueString)
            {
                if (!decimal.TryParse(valueString, out var value))
                    throw new Exception("Unable to parse keyframe adjustment value");
                return value;
            }

            // Local function to calculate the value of a parameter
            // at a specific time between two keyframes
            double CalculateValue(decimal t0, decimal t1, decimal v0, decimal v1)
            {
                return decimal.ToDouble(((v1 - v0) / (t1 - t0) * (currentPos - t0)) + v0);
            }
        }

        private decimal CalculateFrameScaleToFit(uint widthInPixels, uint heightInPixels)
        {
            var destWidth = SwapChainCanvas.SwapChain.SizeInPixels.Width;
            var destHeight = SwapChainCanvas.SwapChain.SizeInPixels.Height;
            var scaleW = 10 * (decimal)Math.Log((double)destWidth / widthInPixels) / (decimal)Math.Log(2.0);
            var scaleH = 10 * (decimal)Math.Log((double)destHeight / heightInPixels) / (decimal)Math.Log(2.0);
            return Math.Min(scaleW, scaleH);
        }

        // This is better than Photoshop
        private void ApplyFrameScaleAndPosition()
        {
            var sourceWidth = _frameBitmap.SizeInPixels.Width;
            var sourceHeight = _frameBitmap.SizeInPixels.Height;
            var destWidth = SwapChainCanvas.SwapChain.SizeInPixels.Width;
            var destHeight = SwapChainCanvas.SwapChain.SizeInPixels.Height;

            // Scale frame and define its rectangle related to the visible screen area
            _scaleFactor = Math.Pow(2, 0.1 * FrameScale);
            _fullDestRect.Width = sourceWidth * _scaleFactor;
            _fullDestRect.X = ((destWidth - _fullDestRect.Width) / 2) + FrameOffsetX;
            _fullDestRect.Height = sourceHeight * _scaleFactor;
            _fullDestRect.Y = ((destHeight - _fullDestRect.Height) / 2) - FrameOffsetY;

            // Adjust source and destination rectangles' X coordinate and width
            if (_fullDestRect.Width <= destWidth)
            {
                _sourceRect.Width = sourceWidth;
                _sourceRect.X = 0;
                _destRect.Width = _fullDestRect.Width;
                _destRect.X = _fullDestRect.X;

                if (_destRect.X < 0)
                {
                    var cropScaledX = -_destRect.X / _scaleFactor;
                    _sourceRect.X = cropScaledX;
                    _sourceRect.Width -= cropScaledX;
                    _destRect.Width += _destRect.X;
                    _destRect.X = 0;
                }
                else if (_destRect.Right > destWidth)
                {
                    _sourceRect.Width -= (_destRect.Right - destWidth) / _scaleFactor;
                    _destRect.Width -= _destRect.Right - destWidth;
                    _destRect.X = destWidth - _destRect.Width;
                }
            }
            else
            {
                _sourceRect.Width = destWidth / _scaleFactor;
                _sourceRect.X = ((sourceWidth - _sourceRect.Width) / 2) - (FrameOffsetX / _scaleFactor);
                _destRect.Width = destWidth;
                _destRect.X = 0;

                if (_sourceRect.X < 0)
                {
                    var cropScaledX = -_sourceRect.X * _scaleFactor;
                    _destRect.X = cropScaledX;
                    _destRect.Width -= cropScaledX;
                    _sourceRect.Width += _sourceRect.X;
                    _sourceRect.X = 0;
                }
                else if (_sourceRect.Right > sourceWidth)
                {
                    _destRect.Width -= (_sourceRect.Right - sourceWidth) * _scaleFactor;
                    _sourceRect.Width -= _sourceRect.Right - sourceWidth;
                    _sourceRect.X = sourceWidth - _sourceRect.Width;
                }
            }

            // Adjust source and destination rectangles' Y coordinate and height
            if (_fullDestRect.Height <= destHeight)
            {
                _sourceRect.Height = sourceHeight;
                _sourceRect.Y = 0;
                _destRect.Height = _fullDestRect.Height;
                _destRect.Y = _fullDestRect.Y;

                if (_destRect.Y < 0)
                {
                    var cropScaledY = -_destRect.Y / _scaleFactor;
                    _sourceRect.Y = cropScaledY;
                    _sourceRect.Height -= cropScaledY;
                    _destRect.Height += _destRect.Y;
                    _destRect.Y = 0;
                }
                else if (_destRect.Bottom > destHeight)
                {
                    _sourceRect.Height -= (_destRect.Bottom - destHeight) / _scaleFactor;
                    _destRect.Height -= _destRect.Bottom - destHeight;
                    _destRect.Y = destHeight - _destRect.Height;
                }
            }
            else
            {
                _sourceRect.Height = destHeight / _scaleFactor;
                _sourceRect.Y = ((sourceHeight - _sourceRect.Height) / 2) + (FrameOffsetY / _scaleFactor);
                _destRect.Height = destHeight;
                _destRect.Y = 0;

                if (_sourceRect.Y < 0)
                {
                    var cropScaledY = -_sourceRect.Y * _scaleFactor;
                    _destRect.Y = cropScaledY;
                    _destRect.Height -= cropScaledY;
                    _sourceRect.Height += _sourceRect.Y;
                    _sourceRect.Y = 0;
                }
                else if (_sourceRect.Bottom > sourceHeight)
                {
                    _destRect.Height -= (_sourceRect.Bottom - sourceHeight) * _scaleFactor;
                    _sourceRect.Height -= _sourceRect.Bottom - sourceHeight;
                    _sourceRect.Y = sourceHeight - _sourceRect.Height;
                }
            }
        }

        private void UpdateRedrawInterval()
        {
            _redrawTimer.Interval = TimeSpan.FromTicks((int)(1.0 / RefreshRate * 10000000 / Math.Max(1.0, PlaybackRate)));
        }

        private void RegisterMessages()
        {
            var messenger = App.Current.Services.GetService<IMessenger>();

            // MultimediaSource.Markers collection changed
            messenger.Register<CollectionChangedMessage<Marker>, string>(this, nameof(MultimediaSource.Markers), (r, m) =>
            {
                if (m.Sender != Source || m.PropertyName != nameof(MultimediaSource.Markers))
                    return;

                foreach (var marker in m.OldValue)
                    ((MediaEditor)r).Timeline.Markers.Remove(marker);

                foreach (var marker in m.NewValue)
                    ((MediaEditor)r).Timeline.Markers.Add(marker);
            });

            // MultimediaSource.Tracks collection changed
            messenger.Register<CollectionChangedMessage<string>, string>(this, nameof(MultimediaSource.Tracks), (r, m) =>
            {
                if (m.Sender != Source || m.PropertyName != nameof(MultimediaSource.Tracks))
                    return;

                foreach (var track in m.OldValue)
                    ((MediaEditor)r).Timeline.Tracks.Remove(track);

                foreach (var track in m.NewValue)
                    ((MediaEditor)r).Timeline.Tracks.Add(track);
            });

            // VideoSource.Cuts collection changed
            messenger.Register<CollectionChangedMessage<(decimal start, decimal end)>, string>(this, nameof(VideoSource.Cuts), (r, m) =>
            {
                if (m.Sender != Source || m.PropertyName != nameof(VideoSource.Cuts))
                    return;

                foreach (var (start, end) in m.OldValue)
                    ((MediaEditor)r).Timeline.RemoveSelection(start, end);

                foreach (var (start, end) in m.NewValue)
                    ((MediaEditor)r).Timeline.AddSelection(start, end);
            });

            // ViewModel.SelectedMarker changed
            messenger.Register<PropertyChangedMessage<Marker>>(this, (r, m) =>
            {
                if (m.Sender != ViewModel ||
                    m.PropertyName != nameof(ViewModel.SelectedMarker) ||
                    m.NewValue == null)
                    return;

                SeekToMarker(m.NewValue);

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
            });

            // MediaTimeline.MarkerStyleGroups request
            messenger.Register<CollectionRequestMessage<string>, string>(this, nameof(MediaTimeline.MarkerStyleGroups), (r, m) =>
            {
                foreach (var group in ((MediaEditor)r).Timeline.MarkerStyleGroups)
                {
                    m.Reply(group.Key);
                }
            });
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
            ActiveSelectionToggleButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : vis;
            NewTrackButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : vis;
            NewMarkerButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : vis;
            NewKeyframeButton.Visibility = vis;
            CutSelectedButton.Visibility = Source.ContentType == MediaContentType.Image
                ? Visibility.Collapsed : vis;
            DeleteMarkerButton.Visibility = vis;

            PlaybackRateButtonSeparator.Visibility = vis;
            PlaybackRateDecreaseButton.Visibility = vis;
            PlaybackRateIncreaseButton.Visibility = vis;
            PlaybackRateNormalButton.Visibility = vis;
            PlaybackRateText.Visibility = vis;

            ZoomAndPanButtonSeparator.Visibility = vis;
            TimelineZoomOutButton.Visibility = vis;
            TimelineZoomInButton.Visibility = vis;

            OpacityAdjustmentSeparator.Visibility = vis;
            OpacitySlider.Visibility = vis;
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
            ViewModel.EditorAddTrackCommmand.NotifyCanExecuteChanged();
            ViewModel.EditorNewMarkerCommand.NotifyCanExecuteChanged();
            ViewModel.EditorNewKeyframeCommand.NotifyCanExecuteChanged();
            ViewModel.EditorCutSelectedCommand.NotifyCanExecuteChanged();
            ViewModel.EditorPlaybackRateDecreaseCommand.NotifyCanExecuteChanged();
            ViewModel.EditorPlaybackRateIncreaseCommand.NotifyCanExecuteChanged();
            ViewModel.EditorPlaybackRateNormalCommand.NotifyCanExecuteChanged();
            ViewModel.EditorTogglePanAndZoomLockCommand.NotifyCanExecuteChanged();
            ViewModel.EditorCenterFrameCommand.NotifyCanExecuteChanged();
            ViewModel.EditorFrameZoomFitCommand.NotifyCanExecuteChanged();
            ViewModel.EditorFrameZoomFullCommand.NotifyCanExecuteChanged();
            ViewModel.EditorTimelineZoomOutCommand.NotifyCanExecuteChanged();
            ViewModel.EditorTimelineZoomInCommand.NotifyCanExecuteChanged();
        }

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

            ViewModel.EditorAddTrackCommmand.CanExecuteRequested +=
                EditorAddTrackCommmand_CanExecuteRequested;
            ViewModel.EditorAddTrackCommmand.ExecuteRequested +=
                EditorAddTrackCommmand_ExecuteRequested;

            ViewModel.EditorNewMarkerCommand.CanExecuteRequested +=
                EditorNewMarkerCommand_CanExecuteRequested;
            ViewModel.EditorNewMarkerCommand.ExecuteRequested +=
                EditorNewMarkerCommand_ExecuteRequested;

            ViewModel.EditorNewKeyframeCommand.CanExecuteRequested +=
                EditorNewKeyframeCommand_CanExecuteRequested;
            ViewModel.EditorNewKeyframeCommand.ExecuteRequested +=
                EditorNewKeyframeCommand_ExecuteRequested;

            ViewModel.EditorCutSelectedCommand.CanExecuteRequested +=
                EditorCutSelectedCommand_CanExecuteRequested;
            ViewModel.EditorCutSelectedCommand.ExecuteRequested +=
                EditorCutSelectedCommand_ExecuteRequested;

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

            ViewModel.EditorTogglePanAndZoomLockCommand.CanExecuteRequested +=
                EditorTogglePanAndZoomLockCommand_CanExecuteRequested;

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
        #endregion
    }
}