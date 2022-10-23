using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents metadata used to further describe a multimedia object.
    /// </summary>
    public interface IMediaMetadata
    {
        /// <inheritdoc cref="ViewModelElement.Name"/>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the media's rating on a scale of 1 to 5.
        /// </summary>
        /// <remarks>A value of <b>zero</b> indicates the media is not rated.</remarks>
        int Rating { get; set; }

        /// <summary>
        /// Gets a collection of user-created tags used to describe the media.
        /// </summary>
        ObservableCollection<string> Tags { get; }

        /// <summary>
        /// Gets or sets a value where each bit represents a group.
        /// </summary>
        /// <remarks>
        /// The meaning of "group" is arbitrary and has no effect on the
        /// functionality of this object.
        /// </remarks>
        int GroupFlags { get; set; }
    }
}