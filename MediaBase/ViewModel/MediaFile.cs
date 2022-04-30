using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;

namespace MediaBase.ViewModel
{
    /// <inheritdoc cref="IMediaFile"/>
    public abstract class MediaFile : MBMediaSource, IMediaFile
    {
        #region Fields
        private string _path;
        private StorageFile _file;
        #endregion

        #region Properties
        [ViewModelObject(nameof(Path), System.Xml.XmlNodeType.Element)]
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public StorageFile File
        {
            get => _file;
            set => SetProperty(ref _file, value);
        }
        #endregion

        #region Constructor
        public MediaFile()
        {
            _path = string.Empty;
            _file = null;
            FramesPerSecond = double.NaN;
        }
        #endregion

        #region Public Methods
        public virtual async Task<bool> LoadFileFromPathAsync()
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

            var contentTypeString = Enum.GetName(ContentType);
            if (!File.ContentType.Contains(contentTypeString.ToLower()))
                throw new InvalidOperationException($"{contentTypeString} file expected");

            return true;
        }
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