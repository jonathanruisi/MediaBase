using System.Collections.ObjectModel;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaBase.Dialogs
{
    public sealed partial class MarkerDialog : ContentDialog
    {
        #region Properties
        public ProjectManager ViewModel => (ProjectManager)DataContext;
        public ObservableCollection<string> MarkerStyles { get; }

        public string MarkerName
        {
            get => (string)GetValue(MarkerNameProperty);
            set => SetValue(MarkerNameProperty, value);
        }

        public static readonly DependencyProperty MarkerNameProperty =
            DependencyProperty.Register("MarkerName",
                                        typeof(string),
                                        typeof(MarkerDialog),
                                        new PropertyMetadata(string.Empty));

        public decimal Position
        {
            get => (decimal)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position",
                                        typeof(decimal),
                                        typeof(MarkerDialog),
                                        new PropertyMetadata(0));

        public decimal Duration
        {
            get => (decimal)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration",
                                        typeof(decimal),
                                        typeof(MarkerDialog),
                                        new PropertyMetadata(0));

        public string Track
        {
            get => (string)GetValue(TrackProperty);
            set => SetValue(TrackProperty, value);
        }

        public static readonly DependencyProperty TrackProperty =
            DependencyProperty.Register("Group",
                                        typeof(string),
                                        typeof(MarkerDialog),
                                        new PropertyMetadata(string.Empty));

        public string MarkerStyle
        {
            get => (string)GetValue(MarkerStyleProperty);
            set => SetValue(MarkerStyleProperty, value);
        }

        public static readonly DependencyProperty MarkerStyleProperty =
            DependencyProperty.Register("MarkerStyle",
                                        typeof(string),
                                        typeof(MarkerDialog),
                                        new PropertyMetadata(string.Empty));

        public TimeDisplayFormat TimeDisplayMode
        {
            get => (TimeDisplayFormat)GetValue(TimeDisplayModeProperty);
            set => SetValue(TimeDisplayModeProperty, value);
        }

        public static readonly DependencyProperty TimeDisplayModeProperty =
            DependencyProperty.Register("TimeDisplayMode",
                                        typeof(TimeDisplayFormat),
                                        typeof(MarkerDialog),
                                        new PropertyMetadata(TimeDisplayFormat.None));

        public int FramesPerSecond
        {
            get => (int)GetValue(FramesPerSecondProperty);
            set => SetValue(FramesPerSecondProperty, value);
        }

        public static readonly DependencyProperty FramesPerSecondProperty =
            DependencyProperty.Register("FramesPerSecond",
                                        typeof(int),
                                        typeof(MarkerDialog),
                                        new PropertyMetadata(App.RefreshRate));
        #endregion

        #region Constructor
        public MarkerDialog()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ProjectManager>();

            MarkerStyles = new ObservableCollection<string>();
        }
        #endregion

        #region Event Handlers
        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            TrackList.SelectedIndex = Duration == 0 ? -1 : 0;

            if (StyleList.Items.Count > 0)
                StyleList.SelectedIndex = 0;
        }
        #endregion

        #region Private Methods
        private string GetPositionString()
        {
            return Position.ToTimecodeString(FramesPerSecond, TimeDisplayMode);
        }

        private string GetDurationString()
        {
            return Duration.ToTimecodeString(FramesPerSecond, TimeDisplayMode);
        }
        #endregion
    }
}