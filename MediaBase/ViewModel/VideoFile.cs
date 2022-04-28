using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Windows.Media.Playback;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents a video file.
    /// </summary>
    [ViewModelObject("Video", XmlNodeType.Element)]
    public sealed class VideoFile : MediaFile
    {
        #region Fields
        private decimal _duration;
        private double _fps;
        private uint _widthInPixels, _heightInPixels;
        #endregion

        #region Properties
        public override decimal Duration
        {
            get => _duration;
            protected set => SetProperty(ref _duration, value);
        }

        public override uint WidthInPixels
        {
            get => _widthInPixels;
            protected set => SetProperty(ref _widthInPixels, value);
        }

        public override uint HeightInPixels
        {
            get => _heightInPixels;
            protected set => SetProperty(ref _heightInPixels, value);
        }

        public override double FramesPerSecond
        {
            get => _fps;
            protected set => SetProperty(ref _fps, value);
        }

        public override MediaContentType ContentType => MediaContentType.Video;
        #endregion

        #region Constructor
        public VideoFile()
        {
            _duration = 0;
            _widthInPixels = 0;
            _heightInPixels = 0;
            _fps = 0;
        }
        #endregion

        #region Method Overrides (MediaFile)
        public override async Task<bool> LoadFileFromPathAsync(bool setNameFromFilename = true)
        {
            if (await base.LoadFileFromPathAsync(setNameFromFilename == false))
                return false;

            // Query video properties
            try
            {
                var strFps = "System.Video.FrameRate";
                var strDuration = "System.Media.Duration";
                var strWidth = "System.Video.FrameWidth";
                var strHeight = "System.Video.FrameHeight";
                var propRequestList = new List<string>() { strFps, strDuration, strWidth, strHeight };
                var propResultList = await File.Properties.RetrievePropertiesAsync(propRequestList);
                FramesPerSecond = (uint)propResultList[strFps] / 1000.0;
                Duration = (ulong)propResultList[strDuration] / 10000000.0M;
                WidthInPixels = (uint)propResultList[strWidth];
                HeightInPixels = (uint)propResultList[strHeight];
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to retrieve video properties", ex);
            }

            return true;
        }
        #endregion

        #region Method Overrides (MediaSource)
        public async override Task<IMediaPlaybackSource> GetMediaPlaybackSourceAsync()
        {
            if (File == null || !File.IsAvailable)
                throw new InvalidOperationException(
                    "Unable to provide a playback source because the video file has not been loaded.");

            return await Task.Run<IMediaPlaybackSource>(() => Windows.Media.Core.MediaSource.CreateFromStorageFile(File));
        }
        #endregion
    }
}