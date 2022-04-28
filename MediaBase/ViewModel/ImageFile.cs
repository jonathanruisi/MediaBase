using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents an image file.
    /// </summary>
    [ViewModelObject("Image", XmlNodeType.Element)]
    public sealed class ImageFile : MediaFile
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
        public override MediaContentType ContentType => MediaContentType.Image;
        #endregion

        #region Constructor
        public ImageFile()
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

            // Query image properties
            try
            {
                var strWidth = "System.Image.HorizontalSize";
                var strHeight = "System.Image.VerticalSize";
                var propRequestList = new List<string> { strWidth, strHeight };
                var propResultList = await File.Properties.RetrievePropertiesAsync(propRequestList);
                WidthInPixels = (uint)propResultList[strWidth];
                HeightInPixels = (uint)propResultList[strHeight];
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to retrieve image properties", ex);
            }

            return true;
        }
        #endregion

        #region Method Overrides (MediaSource)
        public override async Task<IMediaPlaybackSource> GetMediaPlaybackSourceAsync()
        {
            if (File == null || !File.IsAvailable)
                throw new InvalidOperationException(
                    "Unable to provide a playback source because the image file has not been loaded.");

            if (Duration == 0)
                throw new InvalidOperationException("Duration cannot be equal to zero for image files");

            var composition = new MediaComposition();
            var clip = await MediaClip.CreateFromImageFileAsync(File,
                TimeSpan.FromSeconds(decimal.ToDouble(Duration)));
            composition.Clips.Add(clip);

            var encodingProfile = MediaEncodingProfile.CreateHevc(VideoEncodingQuality.Uhd2160p);
            var mediaStreamSource = composition.GenerateMediaStreamSource(encodingProfile);
            return Windows.Media.Core.MediaSource.CreateFromMediaStreamSource(mediaStreamSource);
        }
        #endregion
    }
}