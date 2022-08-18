using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents an item containing width and height properties.
    /// </summary>
    public interface IMediaDimensions
    {
        /// <summary>
        /// The width of the media, in pixels.
        /// </summary>
        uint WidthInPixels { get; }

        /// <summary>
        /// The height of the media, in pixels.
        /// </summary>
        uint HeightInPixels { get; }
    }
}