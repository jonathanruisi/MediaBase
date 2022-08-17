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
    public sealed class ImageFile : MediaFile
    {
        #region Fields
        private uint _widthInPixels, _heightInPixels;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the width of the image, in pixels.
        /// </summary>
        public uint WidthInPixels
        {
            get => _widthInPixels;
            private set => SetProperty(ref _widthInPixels, value);
        }

        /// <summary>
        /// Gets the height of the image, in pixels.
        /// </summary>
        public uint HeightInPixels
        {
            get => _heightInPixels;
            private set => SetProperty(ref _heightInPixels, value);
        }

        protected override MediaContentType ContentType => MediaContentType.Image;
        #endregion

        #region Constructors
        public ImageFile() : this(null) { }

        public ImageFile(StorageFile file) : base(file)
        {
            _widthInPixels = 0;
            _heightInPixels = 0;
        }
        #endregion

        #region Method Overrides (MediaFile)
        public override async Task<bool> ReadPropertiesFromFileAsync()
        {
            if (File?.IsAvailable == false || File?.Path != Path)
            {
                IsReady = false;
                return false;
            }

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