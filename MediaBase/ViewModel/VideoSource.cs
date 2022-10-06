using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.Messaging;
using JLR.Utility.WinUI.ViewModel;

using CommunityToolkit.Mvvm.Messaging;

using Windows.Media.Playback;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents a multimedia object containing video.
    /// </summary>
    public abstract class VideoSource : MBMediaSource
    {
        #region Fields
        private bool _areCutsApplied;
        private decimal _trimmedDuration;
        #endregion

        #region Properties
        public override MediaContentType ContentType => MediaContentType.Video;

        /// <summary>
        /// Gets or sets a value indicating whether or not
        /// the time ranges specified in <see cref="Cuts"/>
        /// will be removed from the source the next time it is loaded.
        /// </summary>
        /// <remarks>
        /// This is not a permanent operation and therefore does not
        /// affect the original source of this <see cref="VideoSource"/>.
        /// </remarks>
        [ViewModelProperty(nameof(AreCutsApplied), XmlNodeType.Element, false, true)]
        public bool AreCutsApplied
        {
            get => _areCutsApplied;
            set => SetProperty(ref _areCutsApplied, value);
        }

        /// <summary>
        /// Gets a value indicating what the duration of the
        /// video will be after all cuts are applied.
        /// </summary>
        public decimal TrimmedDuration
        {
            get => _trimmedDuration;
            private set => SetProperty(ref _trimmedDuration, value);
        }

        /// <summary>
        /// Gets a collection of <see cref="Marker"/> objects
        /// used to define chapters and clips.
        /// </summary>
        [ViewModelCollection(nameof(Markers), "Marker")]
        public ObservableCollection<Marker> Markers { get; }

        /// <summary>
        /// Gets a collection used to specify spans of time
        /// intended to be cut from the media.
        /// </summary>
        [ViewModelCollection(nameof(Cuts), "Cut", true, true)]
        public ObservableCollection<(decimal start, decimal end)> Cuts { get; }

        protected List<(decimal start, decimal end)> PlayableRanges { get; }
        #endregion

        #region Constructor
        protected VideoSource()
        {
            _areCutsApplied = false;
            _trimmedDuration = 0;
            PlayableRanges = new List<(decimal start, decimal end)>();

            Markers = new ObservableCollection<Marker>();
            Markers.CollectionChanged += Markers_CollectionChanged;

            Cuts = new ObservableCollection<(decimal start, decimal end)>();
            Cuts.CollectionChanged += Cuts_CollectionChanged;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// When overridden in a derived class, this method
        /// constructs an <see cref="IMediaPlaybackSource"/> from this
        /// <see cref="VideoSource"/> so that it can be played
        /// using <see cref="MediaPlayer"/>.
        /// </summary>
        /// <returns>A <see cref="MediaPlayer"/>-compatible object.</returns>
        public abstract Task<IMediaPlaybackSource> GetPlaybackSourceAsync();
        #endregion

        #region Event Handlers
        private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var markerMessage = new CollectionChangedMessage<Marker>(this, nameof(Markers), e.Action);

            if (e.OldItems != null)
            {
                foreach (Marker oldMarker in e.OldItems)
                    markerMessage.OldValue.Add(oldMarker);
            }

            if (e.NewItems != null)
            {
                foreach (Marker newMarker in e.NewItems)
                    markerMessage.NewValue.Add(newMarker);
            }

            Messenger.Send(markerMessage);
            NotifySerializedCollectionChanged(nameof(Markers));
        }

        private void Cuts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EvaluateCuts();

            var cutMessage = new CollectionChangedMessage<(decimal start, decimal end)>(this, nameof(Cuts), e.Action);

            if (e.OldItems != null)
            {
                foreach ((decimal start, decimal end) oldCut in e.OldItems)
                    cutMessage.OldValue.Add(oldCut);
            }

            if (e.NewItems != null)
            {
                foreach ((decimal start, decimal end) newCut in e.NewItems)
                    cutMessage.NewValue.Add(newCut);
            }

            Messenger.Send(cutMessage);
            NotifySerializedCollectionChanged(nameof(Cuts));
        }
        #endregion

        #region Method Overrides (ViewModelElement)
        protected override object CustomPropertyParser(string propertyName, string content)
        {
            var baseProperty = base.CustomPropertyParser(propertyName, content);
            if (baseProperty != null)
                return baseProperty;

            if (propertyName == "Cut")
            {
                var cutStrings = content.Split(':');
                return (decimal.Parse(cutStrings[0]), decimal.Parse(cutStrings[1]));
            }

            return null;
        }

        protected override string CustomPropertyWriter(string propertyName, object value)
        {
            var baseProperty = base.CustomPropertyWriter(propertyName, value);
            if (baseProperty != null)
                return baseProperty;

            if (propertyName == "Cut")
            {
                var (start, end) = ((decimal start, decimal end))value;
                return $"{start}:{end}";
            }

            if (propertyName == nameof(AreCutsApplied))
            {
                return AreCutsApplied ? 1.ToString() : 0.ToString();
            }

            return null;
        }
        #endregion

        #region Private Methods
        private void EvaluateCuts()
        {
            decimal lastStart = 0;
            PlayableRanges.Clear();

            foreach (var (start, end) in Cuts.OrderBy(x => x.start))
            {
                if (lastStart >= Duration)
                    break;

                if (start <= 0)
                {
                    lastStart = end;
                    continue;
                }

                PlayableRanges.Add((lastStart, start));
                lastStart = end;
            }

            if (lastStart > 0 && lastStart < Duration)
                PlayableRanges.Add((lastStart, Duration));

            if (PlayableRanges.Count == 0)
                TrimmedDuration = Duration;
            else
            {
                decimal trimmedDuration = 0;
                foreach (var (start, end) in PlayableRanges)
                {
                    trimmedDuration += end - start;
                }

                TrimmedDuration = trimmedDuration;
            }
        }
        #endregion
    }
}