using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.ViewModel;

using Windows.Storage;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents a multimedia file.
    /// </summary>
    public abstract class MediaFile : MBMediaSource
    {
        #region Fields
        private string _path;
        private StorageFile _file;
        #endregion

        #region Properties
        /// <inheritdoc cref="StorageFile.Path"/>
        [ViewModelObject(nameof(Path), XmlNodeType.Element)]
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        /// <summary>
        /// Gets the underlying <see cref="StorageFile"/>
        /// represented by this <see cref="MediaFile"/>.
        /// </summary>
        public StorageFile File
        {
            get => _file;
            set => SetProperty(ref _file, value);
        }
        #endregion

        #region Constructors
        public MediaFile() : this(null) { }

        public MediaFile(StorageFile file)
        {
            _file = file;
            _path = file?.Path;
            Name = file?.DisplayName;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Asynchronously instantiates <see cref="File"/>.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if the file was loaded successfully,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        public async Task<bool> LoadFileFromPathAsync()
        {
            if (string.IsNullOrEmpty(Path))
                return false;

            try
            {
                File = await StorageFile.GetFileFromPathAsync(Path);
                Name = File.DisplayName;
            }
            catch (FileNotFoundException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (ArgumentException) { return false; }

            var contentTypeString = Enum.GetName(ContentType);
            if (!File.ContentType.Contains(contentTypeString.ToLower()))
                throw new InvalidOperationException($"{contentTypeString} file expected");

            return await LoadMediaPropertiesAsync();
        }

        /// <summary>
        /// Asynchronously loads the media properties
        /// associated with this <see cref="MediaFile"/>.
        /// When overridden in a derived class,
        /// use this method to populate media-related properties.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if the properties were loaded successfully,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        public abstract Task<bool> LoadMediaPropertiesAsync();
        #endregion

        #region Method Overrides (System.Object)
        public override string ToString()
        {
            var filename = (bool)(File?.IsAvailable) ? File.Name : "FILE NOT LOADED";
            return $"{base.ToString()} ({filename})";
        }
        #endregion
    }
}