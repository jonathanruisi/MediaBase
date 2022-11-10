using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

using CommunityToolkit.Mvvm.Messaging;

using JLR.Utility.WinUI.Messaging;
using JLR.Utility.WinUI.ViewModel;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents a multimedia object.
    /// </summary>
    public abstract class MultimediaSource : ViewModelElement, IMultimediaItem, IMediaMetadata, IVideoProperties
    {
        #region Fields
        private Guid _id, _sourceId;
        private IMultimediaItem _source;
        private int _rating, _groupFlags;
        private uint _widthInPixels, _heightInPixels;
        private double _framesPerSecond;
        private decimal _duration;
        #endregion

        #region Properties
        [ViewModelProperty(nameof(Id), XmlNodeType.Element, true)]
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="Guid"/> which identifies
        /// this <see cref="MultimediaSource"/>'s data source.
        /// </summary>
        [ViewModelProperty(nameof(SourceId), XmlNodeType.Element, true)]
        public Guid SourceId
        {
            get => _sourceId;
            set => SetProperty(ref _sourceId, value);
        }

        public IMultimediaItem Source
        {
            get => _source;
            set
            {
                IsReady = false;

                if (value.ContentType != ContentType)
                    throw new ArgumentException("Invalid media content type", nameof(value));

                SetProperty(ref _source, value);
            }
        }

        [ViewModelProperty(nameof(Rating), XmlNodeType.Element)]
        public int Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        public virtual uint WidthInPixels
        {
            get => _widthInPixels;
            protected set => SetProperty(ref _widthInPixels, value);
        }

        public virtual uint HeightInPixels
        {
            get => _heightInPixels;
            protected set => SetProperty(ref _heightInPixels, value);
        }

        public virtual double FramesPerSecond
        {
            get => _framesPerSecond;
            protected set => SetProperty(ref _framesPerSecond, value);
        }

        public virtual decimal Duration
        {
            get => _duration;
            protected set => SetProperty(ref _duration, value);
        }

        [ViewModelProperty(nameof(GroupFlags), XmlNodeType.Element)]
        public int GroupFlags
        {
            get => _groupFlags;
            set => SetProperty(ref _groupFlags, value);
        }

        public abstract MediaContentType ContentType { get; }

        public abstract bool IsReady { get; protected set; }

        /// <summary>
        /// Gets the total number of frames in the media.
        /// </summary>
        public int TotalFrames => (int)(Duration * (decimal)FramesPerSecond);

        [ViewModelCollection(nameof(Tags), "Tag")]
        public ObservableCollection<string> Tags { get; }

        /// <summary>
        /// Gets a collection of markers used to provide
        /// information or trigger an action (keyframe markers)
        /// at a specified time (or span of time) in the media.
        /// </summary>
        [ViewModelCollection(nameof(Markers), "Marker")]
        public ObservableCollection<Marker> Markers { get; }

        public IEnumerable<Keyframe> Keyframes =>
            Markers.Where(x => x.GetType() == typeof(Keyframe)).Cast<Keyframe>();

        public IEnumerable<Marker> NonKeyframeMarkers =>
            Markers.Where(x => x.GetType() == typeof(Marker));

        [ViewModelCollection(nameof(Tracks), "Track")]
        public ObservableCollection<string> Tracks { get; }

        [ViewModelCollection(nameof(RelatedMedia), hijackSerdes: true)]
        public ObservableCollection<IMultimediaItem> RelatedMedia { get; }
        #endregion

        #region Constructors
        protected MultimediaSource() : this(null) { }

        protected MultimediaSource(Guid id) : this(id, null) { }

        protected MultimediaSource(IMultimediaItem source) : this(Guid.NewGuid(), source) { }

        protected MultimediaSource(Guid id, IMultimediaItem source)
        {
            if (source != null && source.ContentType != ContentType)
                throw new ArgumentException("Invalid media content type", nameof(source));

            _id = id;
            _source = source;
            _sourceId = _source?.Id ?? Guid.Empty;
            _rating = 0;
            _widthInPixels = 0;
            _heightInPixels = 0;
            _framesPerSecond = 0;
            _duration = 0;
            _groupFlags = 0;
            Name = _source?.Name;

            Tags = new ObservableCollection<string>();
            Tags.CollectionChanged += Tags_CollectionChanged;

            Markers = new ObservableCollection<Marker>();
            Markers.CollectionChanged += Markers_CollectionChanged;

            Tracks = new ObservableCollection<string>();
            Tracks.CollectionChanged += Tracks_CollectionChanged;

            RelatedMedia = new ObservableCollection<IMultimediaItem>();
            RelatedMedia.CollectionChanged += RelatedMedia_CollectionChanged;
        }
        #endregion

        #region Interface Implementation (IGroupable)
        public bool CheckGroupFlag(int group)
        {
            group--;
            if (group is < 0 or > 7)
                throw new ArgumentOutOfRangeException(nameof(group));

            return (GroupFlags & (1 << group)) != 0;
        }

        public void ToggleGroupFlag(int group)
        {
            group--;
            if (group is < 0 or > 7)
                throw new ArgumentOutOfRangeException(nameof(group));

            GroupFlags ^= (1 << group);
        }
        #endregion

        #region Event Handlers
        private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var tagMessage = new CollectionChangedMessage<string>(this, nameof(Tags), e.Action)
            {
                OldStartingIndex = e.OldStartingIndex,
                NewStartingIndex = e.NewStartingIndex
            };

            if (e.OldItems != null)
            {
                foreach (string oldTag in e.OldItems)
                    tagMessage.OldValue.Add(oldTag);
            }

            if (e.NewItems != null)
            {
                foreach (string newTag in e.NewItems)
                    tagMessage.NewValue.Add(newTag);
            }

            Messenger.Send(tagMessage, nameof(Tags));
            NotifySerializedCollectionChanged(nameof(Tags));
        }

        private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var isKeyframeNotify = false;
            var isNonKeyframeMarkerNotify = false;
            var markerMessage = new CollectionChangedMessage<Marker>(this, nameof(Markers), e.Action)
            {
                OldStartingIndex = e.OldStartingIndex,
                NewStartingIndex = e.NewStartingIndex
            };

            if (e.OldItems != null)
            {
                foreach (Marker oldMarker in e.OldItems)
                {
                    markerMessage.OldValue.Add(oldMarker);
                    if (!isKeyframeNotify && oldMarker is Keyframe)
                        isKeyframeNotify = true;
                    else if (!isNonKeyframeMarkerNotify && oldMarker.GetType() == typeof(Marker))
                        isNonKeyframeMarkerNotify = true;

                }
            }

            if (e.NewItems != null)
            {
                foreach (Marker newMarker in e.NewItems)
                {
                    markerMessage.NewValue.Add(newMarker);
                    if (!isKeyframeNotify && newMarker is Keyframe)
                        isKeyframeNotify = true;
                    else if (!isNonKeyframeMarkerNotify && newMarker.GetType() == typeof(Marker))
                        isNonKeyframeMarkerNotify = true;
                }
            }

            // TODO: Use grouping instead
            if (isKeyframeNotify)
                OnPropertyChanged(nameof(Keyframes));
            if (isNonKeyframeMarkerNotify)
                OnPropertyChanged(nameof(NonKeyframeMarkers));
            Messenger.Send(markerMessage, nameof(Markers));
            NotifySerializedCollectionChanged(nameof(Markers));
        }

        private void Tracks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var trackMessage = new CollectionChangedMessage<string>(this, nameof(Tracks), e.Action)
            {
                OldStartingIndex = e.OldStartingIndex,
                NewStartingIndex = e.NewStartingIndex
            };

            if (e.OldItems != null)
            {
                foreach (string oldTrack in e.OldItems)
                    trackMessage.OldValue.Add(oldTrack);
            }

            if (e.NewItems != null)
            {
                foreach (string newTrack in e.NewItems)
                    trackMessage.NewValue.Add(newTrack);
            }

            Messenger.Send(trackMessage, nameof(Tracks));
            NotifySerializedCollectionChanged(nameof(Tracks));
        }

        private void RelatedMedia_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var reatedMediaMessage = new CollectionChangedMessage<IMultimediaItem>(this, nameof(RelatedMedia), e.Action)
            {
                OldStartingIndex = e.OldStartingIndex,
                NewStartingIndex = e.NewStartingIndex
            };

            if (e.OldItems != null)
            {
                foreach (IMultimediaItem oldRelation in e.OldItems)
                    reatedMediaMessage.OldValue.Add(oldRelation);
            }

            if (e.NewItems != null)
            {
                foreach (IMultimediaItem newRelation in e.NewItems)
                    reatedMediaMessage.NewValue.Add(newRelation);
            }

            Messenger.Send(reatedMediaMessage, nameof(RelatedMedia));
            NotifySerializedCollectionChanged(nameof(RelatedMedia));
        }
        #endregion

        #region Interface Implementation (IMultimediaItem)
        public virtual async Task<bool> MakeReady()
        {
            // Retrieve source object
            if (Source == null && SourceId != Guid.Empty)
            {
                var response = Messenger.Send(new MediaLookupRequestMessage(SourceId));
                if (response == null || response.Response == null)
                {
                    IsReady = false;
                    return false;
                }

                Source = response.Response;
            }

            // Make sure SourceId is set
            if (SourceId == Guid.Empty)
                SourceId = Source.Id;

            // Make source ready if necessary
            if (Source.IsReady || await Source.MakeReady())
            {
                // Set name from source if name is not already set
                if (string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Source.Name))
                    Name = Source.Name;

                // Set dimensions
                if (Source is IMediaDimensions dimensions)
                {
                    WidthInPixels = dimensions.WidthInPixels;
                    HeightInPixels = dimensions.HeightInPixels;
                }

                // Set video properties
                if (Source is IVideoProperties properties)
                {
                    FramesPerSecond = properties.FramesPerSecond;
                    Duration = properties.Duration;
                }

                return true;
            }

            IsReady = false;
            return false;
        }
        #endregion

        #region Method Overrides (ViewModelElement)
        protected override object CustomPropertyParser(string propertyName, string content, params string[] args)
        {
            if (propertyName == nameof(Id) ||
                propertyName == nameof(SourceId) ||
                (args.Length > 0 && args[0] == "RelatedMediaId"))
                return Guid.Parse(content);

            return null;
        }

        protected override object HijackDeserialization(string propertyName,
                                                        ref XmlReader reader,
                                                        params string[] args)
        {
            if (propertyName == nameof(RelatedMedia) && args.Length > 0)
            {
                reader.MoveToFirstAttribute();
                var id = Guid.Parse(reader.ReadContentAsString());
                reader.ReadEndElement();

                var result = (IMultimediaItem)InstantiateObjectFromXmlTagName(args[0]);
                result.Id = id;
                return result;
            }

            return null;
        }

        protected override void HijackSerialization(string propertyName,
                                                    object value,
                                                    ref XmlWriter writer,
                                                    params string[] args)
        {
            if (propertyName == nameof(RelatedMedia) && args.Length > 0)
            {
                if (value is not IMultimediaItem media)
                    throw new Exception(
                        "Argument passed to custom serializer could not be cast to IMultimediaItem");

                var xmlTag = GetXmlTagForType(value.GetType());
                if (string.IsNullOrEmpty(xmlTag))
                    throw new Exception(
                        "Argument passed to custom serializer is not recognized as a ViewModelElement serializable type");

                writer.WriteStartElement(xmlTag);
                writer.WriteAttributeString(nameof(media.Id), media.Id.ToString());
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}