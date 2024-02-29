using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.ViewModel;

using Microsoft.Graphics.Canvas;

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
        private bool _isCached;
        private uint _widthInPixels, _heightInPixels;
        private CanvasBitmap _bitmap;
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

        public bool IsCached
        {
            get => _isCached;
            private set => SetProperty(ref _isCached, value);
        }

        public CanvasBitmap Bitmap
        {
            get => _bitmap;
            private set => SetProperty(ref _bitmap, value);
        }

        public override MediaContentType ContentType => MediaContentType.Image;
        #endregion

        #region Constructors
        public ImageFile() : this(file: null) { }

        public ImageFile(string path) : base(path)
        {
            _widthInPixels = 0;
            _heightInPixels = 0;
            _isCached = false;
            _bitmap = null;
        }

        public ImageFile(StorageFile file) : base(file)
        {
            _widthInPixels = 0;
            _heightInPixels = 0;
            _isCached = false;
            _bitmap = null;
        }
        #endregion

        #region Public Methods
        public async Task<bool> Cache(ICanvasResourceCreator resourceCreator)
        {
            if (await MakeReady() == false)
                return false;

            try
            {
                Bitmap = await CanvasBitmap.LoadAsync(resourceCreator, await File.OpenReadAsync());
            }
            catch (Exception)
            {
                IsCached = false;
                return false;
            }

            IsCached = true;
            return true;
        }

        public void FreeCache()
        {
            if (Bitmap != null)
            {
                Bitmap.Dispose();
                Bitmap = null;
            }

            IsCached = false;
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
            {
                IsReady = false;
                return false;
            }

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