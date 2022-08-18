using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents a multimedia object with a unique ID.
    /// </summary>
    public interface IMultimediaItem
    {
        /// <summary>
        /// Gets or sets a <see cref="Guid"/> which uniquely identifies this object.
        /// </summary>
        Guid Id { get; set; }

        /// <summary>
        /// Gets a value indicating the type of media represented
        /// by this <see cref="IMultimediaItem"/>.
        /// </summary>
        MediaContentType ContentType { get; }

        /// <summary>
        /// Gets a value indicating whether or not this object is ready for use.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Asynchronously performs the tasks necessary
        /// to ready this object for use.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if all tasks completed successfully,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        Task<bool> MakeReady();
    }
}