using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

using Windows.Storage;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents a multimedia file
    /// </summary>
    public abstract class MediaFile : MediaSource
    {
        #region Fields
        private string _path;
        private StorageFile _file;
        #endregion

        #region Properties
        /// <summary>
        /// <inheritdoc cref="StorageFile.Path"/>
        /// </summary>
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
            private set => SetProperty(ref _file, value);
        }
        #endregion

        #region Constructor
        protected MediaFile()
        {
            _path = string.Empty;
            _file = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Asynchronously instantiates <see cref="File"/>.
        /// </summary>
        /// <param name="setNameFromFilename">
        /// If set to <b><c>true</c></b> and the file
        /// is loaded successfully, this method will set this
        /// element's <see cref="ViewModelElement.Name"/> using
        /// <see cref="StorageFile.DisplayName"/>.
        /// </param>
        /// <returns>
        /// <b><c>true</c></b> if the file was loaded successfully,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        public virtual async Task<bool> LoadFileFromPathAsync(bool setNameFromFilename = true)
        {
            if (string.IsNullOrEmpty(Path))
                return false;

            try
            {
                File = await StorageFile.GetFileFromPathAsync(Path);
            }
            catch (FileNotFoundException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (ArgumentException) { return false; }

            Name = File.DisplayName;
            return true;
        }
        #endregion

        #region Method Overrides (System.Object)
        public override string ToString()
        {
            var filename = File != null ? File.Name : "NOT LOADED";
            return $"{base.ToString()} ({filename})";
        }
        #endregion
    }
}