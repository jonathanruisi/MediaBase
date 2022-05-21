using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.Messaging;
using JLR.Utility.WinUI.ViewModel;

using Microsoft.Toolkit.Mvvm.Messaging;

using Windows.Media.Playback;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents a multimedia object.
    /// </summary>
    /// <remarks>
    /// The "MB" prefix in the class name is to prevent
    /// confusion with <see cref="Windows.Media.Core.MediaSource"/>.
    /// </remarks>
    public abstract class MBMediaSource : ViewModelNode
    {
        #region Fields
        private Guid _id;
        private int _rating;
        private decimal _duration;
        private uint _widthInPixels, _heightInPixels;
        private double _framesPerSecond;
        private decimal _trimmedDuration;
        #endregion

        #region Properties
        /// <summary>
        /// Uniquely identifies this object.
        /// </summary>
        [ViewModelObject(nameof(Id), XmlNodeType.Attribute, true)]
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets or sets the media's rating on a scale of 1 to 10.
        /// </summary>
        /// <remarks>A value of <b>zero</b> indicates the media is not rated.</remarks>
        [ViewModelObject(nameof(Rating), XmlNodeType.Element)]
        public int Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        /// <summary>
        /// Defines the type of media that is expected
        /// to be provided by this <see cref="MBMediaSource"/>.
        /// </summary>
        public abstract MediaContentType ContentType { get; }

        /// <summary>
        /// Gets the duration of the media, in seconds.
        /// </summary>
        public virtual decimal Duration
        {
            get => _duration;
            set
            {
                SetProperty(ref _duration, value);
                if (_duration == 0)
                    Keyframes.Clear();
            }
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
        /// Gets the width of the media, in pixels.
        /// </summary>
        public virtual uint WidthInPixels
        {
            get => _widthInPixels;
            protected set => SetProperty(ref _widthInPixels, value);
        }

        /// <summary>
        /// Gets the height of the media, in pixels.
        /// </summary>
        public virtual uint HeightInPixels
        {
            get => _heightInPixels;
            protected set => SetProperty(ref _heightInPixels, value);
        }

        /// <summary>
        /// Gets the refresh rate of the media.
        /// </summary>
        public virtual double FramesPerSecond
        {
            get => _framesPerSecond;
            protected set => SetProperty(ref _framesPerSecond, value);
        }

        /// <summary>
        /// Gets the total number of frames in the media.
        /// </summary>
        public int TotalFrames => (int)(Duration * (decimal)FramesPerSecond);

        /// <summary>
        /// Gets a collection of optional tags used to describe the media.
        /// </summary>
        /// <remarks>
        /// This collection contains numeric keys for tag values
        /// stored in a separate tag database.
        /// </remarks>
        [ViewModelCollection(nameof(Tags), "Tag")]
        public ObservableCollection<int> Tags { get; }

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

        /// <summary>
        /// Gets a collection of keyframes used to animate
        /// a property of the media.
        /// </summary>
        [ViewModelCollection(nameof(Keyframes), "Keyframe")]
        public ObservableCollection<Keyframe> Keyframes { get; }

        /// <summary>
        /// Gets a list of time ranges that have <b>NOT</b> been cut from the media.
        /// </summary>
        protected List<(decimal start, decimal end)> PlayableRanges { get; }
        #endregion

        #region Constructor
        protected MBMediaSource()
        {
            _id = Guid.NewGuid();
            _rating = 0;
            _duration = 0;
            _widthInPixels = 0;
            _heightInPixels = 0;
            _framesPerSecond = 0;
            _trimmedDuration = 0;
            PlayableRanges = new List<(decimal start, decimal end)>();

            Tags = new ObservableCollection<int>();
            Tags.CollectionChanged += Tags_CollectionChanged;

            Markers = new ObservableCollection<Marker>();
            Markers.CollectionChanged += Markers_CollectionChanged;

            Cuts = new ObservableCollection<(decimal start, decimal end)>();
            Cuts.CollectionChanged += Cuts_CollectionChanged;

            Keyframes = new ObservableCollection<Keyframe>();
            Keyframes.CollectionChanged += Keyframes_CollectionChanged;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Constructs an <see cref="IMediaPlaybackSource"/> from this
        /// <see cref="MBMediaSource"/> so that it can be played
        /// using <see cref="MediaPlayer"/>.
        /// </summary>
        /// <returns>A <see cref="MediaPlayer"/>-compatible object.</returns>
        public abstract Task<IMediaPlaybackSource> GetMediaSourceAsync();

        // TODO: Implement a static method to create a new IVideoSource (like a VideoClip or something) from the cuts applied to this VideoFile
        #endregion

        #region Event Handlers
        private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var tagMessage = new CollectionChangedMessage<int>(this, nameof(Tags));

            if (e.OldItems != null)
            {
                foreach (int oldTag in e.OldItems)
                    tagMessage.OldValue.Add(oldTag);
            }

            if (e.NewItems != null)
            {
                foreach (int newTag in e.NewItems)
                    tagMessage.NewValue.Add(newTag);
            }

            Messenger.Send(tagMessage);
            NotifySerializedCollectionChanged(nameof(Tags));
        }

        private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var markerMessage = new CollectionChangedMessage<Marker>(this, nameof(Markers));

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

            var cutMessage = new CollectionChangedMessage<(decimal start, decimal end)>(this, nameof(Cuts));

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

        private void Keyframes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Keyframes.Count > 0)
            {
                var maxTime = Keyframes.Max(x => x.Time);
                if (maxTime > Duration)
                    Duration = maxTime;
            }

            var keyframeMessage = new CollectionChangedMessage<Keyframe>(this, nameof(Keyframes));

            if (e.OldItems != null)
            {
                foreach (Keyframe oldKeyframe in e.OldItems)
                    keyframeMessage.OldValue.Add(oldKeyframe);
            }

            if (e.NewItems != null)
            {
                foreach (Keyframe newKeyframe in e.NewItems)
                    keyframeMessage.NewValue.Add(newKeyframe);
            }

            Messenger.Send(keyframeMessage);
            NotifySerializedCollectionChanged(nameof(Keyframes));
        }
        #endregion

        #region Method Overrides (ViewModelElement)
        protected override object CustomPropertyParser(string propertyName, string content)
        {
            if (propertyName == nameof(Id))
                return Guid.Parse(content);

            if (propertyName == nameof(Cuts))
            {
                var cutStrings = content.Split(':');
                return (decimal.Parse(cutStrings[0]), decimal.Parse(cutStrings[1]));
            }

            return null;
        }

        protected override string CustomPropertyWriter(string propertyName, object value)
        {
            if (propertyName != nameof(Cuts))
                return null;

            var (start, end) = ((decimal start, decimal end))value;
            return $"{start}:{end}";
        }
        #endregion

        #region Private Methods
        private void EvaluateCuts()
        {
            decimal lastStart = 0;
            PlayableRanges.Clear();

            foreach (var cut in Cuts.OrderBy(x => x.start))
            {
                if (lastStart >= Duration)
                    break;

                if (cut.start <= 0)
                {
                    lastStart = cut.end;
                    continue;
                }

                PlayableRanges.Add((lastStart, cut.start));
                lastStart = cut.end;
            }

            if (lastStart > 0 && lastStart < Duration)
                PlayableRanges.Add((lastStart, Duration));

            if (PlayableRanges.Count == 0)
                TrimmedDuration = Duration;
            else
            {
                decimal trimmedDuration = 0;
                foreach (var range in PlayableRanges)
                {
                    trimmedDuration += range.end - range.start;
                }

                TrimmedDuration = trimmedDuration;
            }

            // TODO: Keyframes need to be adjusted here as well
        }
        #endregion
    }
}