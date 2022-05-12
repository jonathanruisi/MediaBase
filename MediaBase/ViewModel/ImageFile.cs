using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.ViewModel;

using Microsoft.Toolkit.Mvvm.Messaging;

using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Contains properties and methods needed for accessing image files
    /// </summary>
    [ViewModelObject("Image", XmlNodeType.Element)]
    public class ImageFile : MediaFile
    {
        #region Properties
        public override MediaContentType ContentType => MediaContentType.Image;
        #endregion

        #region Constructors
        public ImageFile() : this(null) { }

        public ImageFile(StorageFile file) : base(file) { }
        #endregion

        #region Method Overrides (MediaFile)
        public override async Task<bool> LoadMediaPropertiesAsync()
        {
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
                return false;
            }

            return true;
        }
        #endregion

        #region Method Overrides (MBMediaSource)
        public override async Task<IMediaPlaybackSource> GetMediaSourceAsync()
        {
            var composition = new MediaComposition();
            var clip = await MediaClip.CreateFromImageFileAsync(File,
                TimeSpan.FromSeconds(decimal.ToDouble(Duration)));
            composition.Clips.Add(clip);

            var encodingProfile = MediaEncodingProfile.CreateHevc(VideoEncodingQuality.Uhd4320p);
            encodingProfile.Video.Width = WidthInPixels;
            encodingProfile.Video.Height = HeightInPixels;
            var mediaStreamSource = composition.GenerateMediaStreamSource(encodingProfile);
            return MediaSource.CreateFromMediaStreamSource(mediaStreamSource);
        }
        #endregion
    }
}