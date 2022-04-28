using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;

using MediaBase.ViewModel.Base;

using Microsoft.Toolkit.Mvvm.Messaging;

using Windows.Media.Playback;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents a multimedia object
    /// </summary>
    public abstract class MediaSource : ViewModelNode, IMediaSource
    {
        #region Fields
        private Guid _id;
        private int _rating;
        #endregion

        #region Properties
        [ViewModelObject(nameof(Id), XmlNodeType.Attribute, true)]
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [ViewModelObject(nameof(Rating), XmlNodeType.Element)]
        public int Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        [ViewModelCollection(nameof(Tags), "Tag")]
        public ObservableCollection<int> Tags { get; }

        public abstract MediaContentType ContentType { get; }

        public abstract decimal Duration { get; protected set; }

        public abstract uint WidthInPixels { get; protected set; }

        public abstract uint HeightInPixels { get; protected set; }

        public abstract double FramesPerSecond { get; protected set; }
        #endregion

        #region Constructor
        protected MediaSource()
        {
            _id = Guid.NewGuid();
            _rating = 0;
            Tags = new ObservableCollection<int>();
            Tags.CollectionChanged += Tags_CollectionChanged;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Represents this media source in a form
        /// which is compatible with <see cref="MediaPlayer"/>.
        /// </summary>
        /// <returns>
        /// Upon completion, this method returns an object
        /// that can be played using <see cref="MediaPlayer"/>.
        /// </returns>
        public abstract Task<IMediaPlaybackSource> GetMediaPlaybackSourceAsync();
        #endregion

        #region Event Handlers
        private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var tagMessage = new ViewModelTagCollectionChangedMessage();

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
}