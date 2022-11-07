using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents an item containing video properties.
    /// </summary>
    public interface IVideoProperties : IMediaDimensions
    {
        /// <summary>
        /// Gets the frame rate of the video, in frames/second.
        /// </summary>
        double FramesPerSecond { get; }

        /// <summary>
        /// Gets the duration of the video, in seconds.
        /// </summary>
        decimal Duration { get; }
    }
}