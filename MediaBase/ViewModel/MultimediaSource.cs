using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
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
    public abstract class MultimediaSource : ViewModelNode, IMediaMetadata
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
        /// Gets a value indicating whether or not
        /// the <see cref="MultimediaSource"/> is ready to be consumed.
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
            set => SetProperty(ref _duration, value);
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
        #endregion

        #region Constructor
        protected MultimediaSource()
        {
            _id = Guid.NewGuid();
            _rating = 0;
            _duration = 0;
            _widthInPixels = 0;
            _heightInPixels = 0;
            _framesPerSecond = 0;

            Tags = new ObservableCollection<string>();
            Tags.CollectionChanged += Tags_CollectionChanged;
        }
        #endregion

        #region Public Methods
        
        #endregion

        #region Event Handlers
        private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var tagMessage = new CollectionChangedMessage<string>(this, nameof(Tags));

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