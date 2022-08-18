using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.ViewModel;

using Windows.Storage;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Contains properties and methods needed for accessing image files.
    /// </summary>
    [ViewModelType(nameof(ImageFile))]
    public sealed class ImageFile : MediaFile, IMediaDimensions
    {
        #region Fields
        private uint _widthInPixels, _heightInPixels;
        #endregion

        #region Properties
        public uint WidthInPixels
        {
            get => _widthInPixels;
            private set => SetProperty(ref _widthInPixels, value);
        }

        public uint HeightInPixels
        {
            get => _heightInPixels;
            private set => SetProperty(ref _heightInPixels, value);
        }

        public override MediaContentType ContentType => MediaContentType.Image;
        #endregion

        #region Constructors
        public ImageFile() : this(file: null) { }

        public ImageFile(string path) : base(path)
        {
            _widthInPixels = 0;
            _heightInPixels = 0;
        }

        public ImageFile(StorageFile file) : base(file)
        {
            _widthInPixels = 0;
            _heightInPixels = 0;
        }
        #endregion

        #region Method Overrides (MediaFile)
        /// <summary>
        /// Asynchronously loads <see cref="File"/> and reads
        /// all properties needed by this <see cref="ImageFile"/>.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if all needed information
        /// was successfully read from the file,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        public override async Task<bool> MakeReady()
        {
            // Load file from path
            if (await base.MakeReady() == false)
                return false;

            // Read image file properties
            try
            {
                var strWidth = "System.Image.HorizontalSize";
                var strHeight = "System.Image.VerticalSize";
                var propRequestList = new List<string> { strWidth, strHeight };
                var propResultList = await File.Properties.RetrievePropertiesAsync(propRequestList);

                WidthInPixels = (uint)propResultList[strWidth];
                HeightInPixels = (uint)propResultList[strHeight];
            }
            catch (Exception)
            {
                IsReady = false;
                return false;
            }

            IsReady = true;
            return true;
        }
        #endregion
    }
}