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

        #region Constructors
        public MediaFile() : this(null) { }

        public MediaFile(StorageFile file)
        {
            _file = file;
            _path = file?.Path;
            Name = file?.DisplayName;
            FramesPerSecond = double.NaN;
        }
        #endregion

        #region Public Methods
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