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
    [ViewModelType("Video")]
    public sealed class VideoFile : MediaFile
    {
        #region Fields
        private uint _widthInPixels, _heightInPixels;
        private double _framesPerSecond;
        private decimal _duration;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the width of the video, in pixels.
        /// </summary>
        public uint WidthInPixels
        {
            get => _widthInPixels;
            private set => SetProperty(ref _widthInPixels, value);
        }

        /// <summary>
        /// Gets the height of the video, in pixels.
        /// </summary>
        public uint HeightInPixels
        {
            get => _heightInPixels;
            private set => SetProperty(ref _heightInPixels, value);
        }

        /// <summary>
        /// Gets the frame rate of the video, in frames/second.
        /// </summary>
        public double FramesPerSecond
        {
            get => _framesPerSecond;
            private set => SetProperty(ref _framesPerSecond, value);
        }

        /// <summary>
        /// Gets the duration of the video, in seconds.
        /// </summary>
        public decimal Duration
        {
            get => _duration;
            private set => SetProperty(ref _duration, value);
        }

        protected override MediaContentType ContentType => MediaContentType.Video;
        #endregion

        #region Constructors
        public VideoFile() : this(null) { }

        public VideoFile(StorageFile file) : base(file)
        {
            _widthInPixels = 0;
            _heightInPixels = 0;
            _framesPerSecond = 0;
            _duration = 0;
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