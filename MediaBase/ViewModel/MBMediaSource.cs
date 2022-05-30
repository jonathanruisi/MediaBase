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
        #endregion

        #region Properties
        /// <summary>
        /// Defines the type of media that is expected
        /// to be provided by this <see cref="MBMediaSource"/>.
        /// </summary>
        public abstract MediaContentType ContentType { get; }

        /// <summary>
        /// Gets a <see cref="Guid"/> which uniquely identifies this object.
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
        /// Gets a collection of keyframes used to trigger
        /// various actions at specified times in the media.
        /// </summary>
        [ViewModelCollection(nameof(Keyframes), "Keyframe")]
        public ObservableCollection<ITimelineMarker> Keyframes { get; }
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

            Tags = new ObservableCollection<int>();
            Tags.CollectionChanged += Tags_CollectionChanged;

            Keyframes = new ObservableCollection<ITimelineMarker>();
            Keyframes.CollectionChanged += Keyframes_CollectionChanged;
        }
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

        private void Keyframes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Keyframes.Count > 0)
            {
                var maxTime = Keyframes.Max(x => x.Position);
                if (maxTime > Duration)
                    Duration = maxTime;
            }

            var keyframeMessage = new CollectionChangedMessage<ITimelineMarker>(this, nameof(Keyframes));

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
        #endregion
    }
}