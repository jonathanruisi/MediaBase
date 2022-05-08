using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;

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
        /// Gets a collection of optional tags used to describe the media.
        /// </summary>
        /// <remarks>
        /// This collection contains numeric keys for tag values
        /// stored in a separate tag database.
        /// </remarks>
        [ViewModelCollection(nameof(Tags), "Tag")]
        public ObservableCollection<int> Tags { get; }

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
            protected set => SetProperty(ref _duration, value);
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
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Constructs an <see cref="IMediaPlaybackSource"/> from this
        /// <see cref="MBMediaSource"/> so that it can be played
        /// using <see cref="MediaPlayer"/>.
        /// </summary>
        /// <returns></returns>
        public abstract Task<IMediaPlaybackSource> GetMediaSourceAsync();
        #endregion

        #region Event Handlers
        private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var tagMessage = new MediaSourceTagCollectionChangedMessage();

            if (e.OldItems != null)
            {
                foreach (int oldTag in e.OldItems)
                    tagMessage.RemovedTags.Add(oldTag);
            }

            if (e.NewItems != null)
            {
                foreach (int newTag in e.NewItems)
                    tagMessage.AddedTags.Add(newTag);
            }

            Messenger.Send(tagMessage);
            NotifySerializedCollectionChanged(nameof(Tags));
        }
        #endregion

        #region Method Overrides (ViewModelElement)
        protected override object CustomPropertyParser(string propertyName, string content)
        {
            if (propertyName != nameof(Id))
                return null;

            return Guid.Parse(content);
        }
        #endregion
    }

    public sealed class MediaSourceTagCollectionChangedMessage
    {
        public List<int> AddedTags { get; }
        public List<int> RemovedTags { get; }

        public MediaSourceTagCollectionChangedMessage()
        {
            AddedTags = new List<int>();
            RemovedTags = new List<int>();
        }

        public MediaSourceTagCollectionChangedMessage(IList<int> addedTags,
                                                    IList<int> removedTags) : this()
        {
            AddedTags.AddRange(addedTags);
            RemovedTags.AddRange(removedTags);
        }
    }
}