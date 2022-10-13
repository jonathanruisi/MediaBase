﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
    public abstract class MultimediaSource : ViewModelNode, IMultimediaItem, IMediaMetadata, IVideoProperties
    {
        #region Constants
        /// <summary>
        /// Indicates the default refresh rate of the current display.
        /// </summary>
        /// <remarks>
        /// TODO: That value should be retrievable using an API call.
        /// </remarks>
        public static readonly int DefaultFrameRate = 120;
        #endregion

        #region Fields
        private Guid _id, _sourceId;
        private IMultimediaItem _source;
        private int _rating;
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
            set => SetProperty(ref _duration, value);
        }

        public abstract MediaContentType ContentType { get; }

        public abstract bool IsReady { get; protected set; }

        /// <summary>
        /// Gets the total number of frames in the media.
        /// </summary>
        public int TotalFrames => (int)(Duration * (decimal)FramesPerSecond);

        [ViewModelCollection(nameof(Tags), "Tag")]
        public ObservableCollection<string> Tags { get; }
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
            Name = _source?.Name;

            Tags = new ObservableCollection<string>();
            Tags.CollectionChanged += Tags_CollectionChanged;
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
        #endregion

        #region Interface Implementation (IMultimediaItem)
        public virtual async Task<bool> MakeReady()
        {
            // Retrieve source object
            if (Source == null && SourceId != Guid.Empty)
            {
                var response = Messenger.Send(new MediaLookupRequestMessage(SourceId));
                if (response == null)
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
        protected override object CustomPropertyParser(string propertyName, string content)
        {
            if (propertyName is (nameof(Id)) or (nameof(SourceId)))
                return Guid.Parse(content);

            return null;
        }
        #endregion
    }
}