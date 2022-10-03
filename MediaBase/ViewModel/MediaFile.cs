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
    public abstract class MediaFile : ViewModelElement, IMultimediaItem
    {
        #region Fields
        private Guid _id;
        private string _path;
        private StorageFile _file;
        private bool _isReady;
        #endregion

        #region Properties
        [ViewModelProperty(nameof(Id), XmlNodeType.Element, true)]
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

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

        public bool IsReady
        {
            get => _isReady;
            protected set => SetProperty(ref _isReady, value);
        }

        public abstract MediaContentType ContentType { get; }
        #endregion

        #region Constructors
        protected MediaFile() : this(file: null) { }

        protected MediaFile(string path)
        {
            _id = Guid.NewGuid();
            _isReady = false;
            _file = null;
            _path = path;
            Name = null;
        }

        protected MediaFile(StorageFile file)
        {
            _id = Guid.NewGuid();
            _isReady = false;
            _file = file;
            _path = file?.Path;
            Name = file?.DisplayName;
        }
        #endregion

        #region Interface Implementation (IMultimediaItem)
        /// <summary>
        /// Asynchronously instantiates <see cref="File"/>.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if the file was loaded successfully,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        public virtual async Task<bool> MakeReady()
        {
            if (string.IsNullOrEmpty(Path) && File == null)
            {
                IsReady = false;
                return false;
            }

            if (string.IsNullOrEmpty(Path) && File?.IsAvailable == true)
                Path = File.Path;

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
        #endregion

        #region Method Overrides (ViewModelElement)
        protected override object CustomPropertyParser(string propertyName, string content)
        {
            if (propertyName == nameof(Id))
                return Guid.Parse(content);

            return null;
        }
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