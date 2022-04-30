using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents an object that contains video.
    /// </summary>
    public interface IVideoSource
    {
        /// <summary>
        /// Gets a collection used to specify spans of time
        /// intended to be cut from the video.
        /// </summary>
        ObservableCollection<(decimal start, decimal end)> Cuts { get; }

        /// <summary>
        /// Gets a collection of <see cref="Marker"/> objects
        /// used to define chapters and clips.
        /// </summary>
        ObservableCollection<Marker> Markers { get; }
    }
}