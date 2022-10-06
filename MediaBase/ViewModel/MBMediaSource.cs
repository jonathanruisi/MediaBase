using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.Controls;
using JLR.Utility.WinUI.Messaging;
using JLR.Utility.WinUI.ViewModel;

using CommunityToolkit.Mvvm.Messaging;

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
    public abstract class MBMediaSource : ViewModelNode, IMediaMetadata
    {
        #region Fields
        private Guid _id;
        private int _rating;
        private decimal _duration;
        private uint _widthInPixels, _heightInPixels;
        private double _framesPerSecond;
        private bool _isCategory1, _isCategory2, _isCategory3, _isCategory4;
        #endregion

        #region Properties
        /// <summary>
        /// Defines the type of media that is expected
        /// to be provided by this <see cref="MBMediaSource"/>.
        /// </summary>
        public abstract MediaContentType ContentType { get; }

        /// <summary>
        /// Gets a value indicating whether or not
        /// the <see cref="MBMediaSource"/> is ready to be consumed.
        /// </summary>
        public abstract bool IsReady { get; protected set; }

        /// <summary>
        /// Gets a <see cref="Guid"/> which uniquely identifies this object.
        /// </summary>
        [ViewModelProperty(nameof(Id), XmlNodeType.Attribute, true)]
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [ViewModelProperty(nameof(Rating), XmlNodeType.Element)]
        public int Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

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
        /// Gets the refresh rate of the media, in frames/second.
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

        [ViewModelCollection(nameof(Tags), "Tag")]
        public ObservableCollection<string> Tags { get; }

        /// <summary>
        /// Gets a collection of keyframes used to trigger
        /// various actions at specified times in the media.
        /// </summary>
        [ViewModelCollection(nameof(Keyframes), "Keyframe")]
        public ObservableCollection<Marker> Keyframes { get; }

        /// <summary>
        /// Indicates a multi-purpose marker of category #1
        /// </summary>
        [ViewModelProperty("C1", XmlNodeType.Element, false, true)]
        public bool IsCategory1
        {
            get => _isCategory1;
            set => SetProperty(ref _isCategory1, value);
        }

        /// <summary>
        /// Indicates a multi-purpose marker of category #2
        /// </summary>
        [ViewModelProperty("C2", XmlNodeType.Element, false, true)]
        public bool IsCategory2
        {
            get => _isCategory2;
            set => SetProperty(ref _isCategory2, value);
        }

        /// <summary>
        /// Indicates a multi-purpose marker of category #3
        /// </summary>
        [ViewModelProperty("C3", XmlNodeType.Element, false, true)]
        public bool IsCategory3
        {
            get => _isCategory3;
            set => SetProperty(ref _isCategory3, value);
        }

        /// <summary>
        /// Indicates a multi-purpose marker of category #4
        /// </summary>
        [ViewModelProperty("C4", XmlNodeType.Element, false, true)]
        public bool IsCategory4
        {
            get => _isCategory4;
            set => SetProperty(ref _isCategory4, value);
        }
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
            _isCategory1 = false;
            _isCategory2 = false;
            _isCategory3 = false;
            _isCategory4 = false;

            Tags = new ObservableCollection<string>();
            Tags.CollectionChanged += Tags_CollectionChanged;

            Keyframes = new ObservableCollection<Marker>();
            Keyframes.CollectionChanged += Keyframes_CollectionChanged;
        }
        #endregion

        #region Event Handlers
        private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var tagMessage = new CollectionChangedMessage<string>(this, nameof(Tags), e.Action);

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

            Messenger.Send(tagMessage);
            NotifySerializedCollectionChanged(nameof(Tags));
        }

        private void Keyframes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Keyframes.Count > 0)
            {
                var maxTime = Keyframes.Max(x => x.Position);
                if (maxTime > Duration)
                    Duration = maxTime;
            }

            var keyframeMessage = new CollectionChangedMessage<ITimelineMarker>(this, nameof(Keyframes), e.Action);

            if (e.OldItems != null)
            {
                foreach (ITimelineMarker oldKeyframe in e.OldItems)
                    keyframeMessage.OldValue.Add(oldKeyframe);
            }

            if (e.NewItems != null)
            {
                foreach (ITimelineMarker newKeyframe in e.NewItems)
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

            return null;
        }

        protected override string CustomPropertyWriter(string propertyName, object value)
        {
            if ((propertyName == "C1" && IsCategory1) ||
                (propertyName == "C2" && IsCategory2) ||
                (propertyName == "C3" && IsCategory3) ||
                (propertyName == "C4" && IsCategory4))
                return "1";

            return null;
        }
        #endregion
    }
}