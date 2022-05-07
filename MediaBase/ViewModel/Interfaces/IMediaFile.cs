using System.Threading.Tasks;

using Windows.Storage;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents a multimedia file.
    /// </summary>
    public interface IMediaFile
    {
        /// <inheritdoc cref="StorageFile.Path"/>
        string Path { get; }

        /// <summary>
        /// Gets the underlying <see cref="StorageFile"/>
        /// represented by this <see cref="IMediaFile"/>.
        /// </summary>
        StorageFile File { get; }

        /// <summary>
        /// Asynchronously instantiates <see cref="File"/>.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if the file was loaded successfully,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        Task<bool> LoadFileFromPathAsync();

        /// <summary>
        /// Asynchronously loads the media properties
        /// associated with this <see cref="IMediaFile"/>.
        /// When overridden in a derived class,
        /// use this method to populate media-related properties.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if the properties were loaded successfully,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        Task<bool> LoadMediaPropertiesAsync();
    }
}