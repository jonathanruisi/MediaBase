using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents metadata associated with a media object.
    /// </summary>
    public interface IMediaSource
    {
        /// <summary>
        /// Uniquely identifies this object.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets or sets the name of the media.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the media's rating on a scale of 1 to 10.
        /// </summary>
        /// <remarks>A value of <b>zero</b> indicates the media is not rated.</remarks>
        int Rating { get; }

        /// <summary>
        /// Defines the type of media that is expected
        /// to be provided by this <see cref="IMediaSource"/>.
        /// </summary>
        MediaContentType ContentType { get; }

        /// <summary>
        /// Gets the duration of the media, in seconds.
        /// </summary>
        decimal Duration { get; }

        /// <summary>
        /// Gets the width of the media, in pixels.
        /// </summary>
        uint WidthInPixels { get; }

        /// <summary>
        /// Gets the height of the media, in pixels.
        /// </summary>
        uint HeightInPixels { get; }

        /// <summary>
        /// Gets the refresh rate of the media.
        /// </summary>
        double FramesPerSecond { get; }

        /// <summary>
        /// Gets a collection of optional tags used to describe the media.
        /// </summary>
        /// <remarks>
        /// This collection contains numeric keys for tag values
        /// stored in a separate tag database.
        /// </remarks>
        ObservableCollection<int> Tags { get; }
    }
}