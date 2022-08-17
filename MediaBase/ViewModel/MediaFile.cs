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
    [ViewModelType(nameof(MediaFile))]
    public abstract class MediaFile : ViewModelElement
    {
        #region Fields
        private string _path;
        private StorageFile _file;
        private bool _isReady;
        #endregion

        #region Properties
        /// <inheritdoc cref="StorageFile.Path"/>
        [ViewModelProperty(nameof(Path), XmlNodeType.Element)]
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

        /// <summary>
        /// Gets a value indicating whether or not
        /// this file has been loaded and its
        /// relevant media properties have been read.
        /// </summary>
        public bool IsReady
        {
            get => _isReady;
            protected set => SetProperty(ref _isReady, value);
        }

        /// <summary>
        /// Gets a value indicating the type of media
        /// contained in this file.
        /// </summary>
        protected abstract MediaContentType ContentType { get; }
        #endregion

        #region Constructors
        protected MediaFile() : this(null) { }

        protected MediaFile(StorageFile file)
        {
            _isReady = false;
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
            {
                IsReady = false;
                return false;
            }

            if (File?.Path == Path)
                return true;

            try
            {
                File = await StorageFile.GetFileFromPathAsync(Path);
                Name = File.DisplayName;
            }
            catch (FileNotFoundException)
            {
                IsReady = false;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                IsReady = false;
                return false;
            }
            catch (ArgumentException)
            {
                IsReady = false;
                return false;
            }

            var contentTypeString = Enum.GetName(ContentType);
            if (!File.ContentType.Contains(contentTypeString.ToLower()))
            {
                IsReady = false;
                throw new InvalidOperationException($"{contentTypeString} file expected");
            }

            return true;
        }

        /// <summary>
        /// Asynchronously reads any data from <see cref="File"/>
        /// that is needed by this <see cref="MediaFile"/>.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if all needed information
        /// was successfully read from the file,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        public abstract Task<bool> ReadPropertiesFromFileAsync();
        #endregion

        #region Method Overrides (System.Object)
        public override string ToString()
        {
            var filename = IsReady ? File.Name : "FILE NOT READY";
            return $"{base.ToString()} ({filename})";
        }
        #endregion
    }
}