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
    /// Contains properties and methods needed for accessing video files.
    /// </summary>
    [ViewModelType(nameof(VideoFile))]
    public sealed class VideoFile : MediaFile, IVideoProperties
    {
        #region Fields
        private uint _widthInPixels, _heightInPixels;
        private double _framesPerSecond;
        private decimal _duration;
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

        public double FramesPerSecond
        {
            get => _framesPerSecond;
            private set => SetProperty(ref _framesPerSecond, value);
        }

        public decimal Duration
        {
            get => _duration;
            private set => SetProperty(ref _duration, value);
        }

        public override MediaContentType ContentType => MediaContentType.Video;
        #endregion

        #region Constructors
        public VideoFile() : this(file: null) { }

        public VideoFile(string path) : base(path)
        {
            _widthInPixels = 0;
            _heightInPixels = 0;
            _framesPerSecond = 0;
            _duration = 0;
        }

        public VideoFile(StorageFile file) : base(file)
        {
            _widthInPixels = 0;
            _heightInPixels = 0;
            _framesPerSecond = 0;
            _duration = 0;
        }
        #endregion

        #region Method Overrides (MediaFile)
        /// <summary>
        /// Asynchronously loads <see cref="File"/> and reads
        /// all properties needed by this <see cref="VideoFile"/>.
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

            // Read video file properties
            try
            {
                var strWidth = "System.Video.FrameWidth";
                var strHeight = "System.Video.FrameHeight";
                var strFps = "System.Video.FrameRate";
                var strDuration = "System.Media.Duration";
                var propRequestList = new List<string> { strWidth, strHeight, strFps, strDuration };
                var propResultList = await File.Properties.RetrievePropertiesAsync(propRequestList);

                WidthInPixels = (uint)propResultList[strWidth];
                HeightInPixels = (uint)propResultList[strHeight];
                FramesPerSecond = (uint)propResultList[strFps] / 1000.0;
                Duration = (ulong)propResultList[strDuration] / 10000000.0M;
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