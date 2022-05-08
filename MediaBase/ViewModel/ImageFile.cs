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
    [ViewModelObject("Image", XmlNodeType.Element)]
    public class ImageFile : MediaFile, IImageSource
    {
        #region Properties
        [ViewModelCollection(nameof(Keyframes), "Keyframe")]
        public ObservableCollection<ImageAnimationKeyframe> Keyframes { get; }

        public override decimal Duration
        {
            get => base.Duration;
            protected set
            {
                if (value == 0)
                    Keyframes.Clear();

                base.Duration = value;
            }
        }

        public override MediaContentType ContentType => MediaContentType.Image;
        #endregion

        #region Constructors
        public ImageFile() : this(null) { }

        public ImageFile(StorageFile file) : base(file)
        {
            Keyframes = new ObservableCollection<ImageAnimationKeyframe>();
            Keyframes.CollectionChanged += Keyframes_CollectionChanged;
        }
        #endregion

        #region Event Handlers
        private void Keyframes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifySerializedCollectionChanged(nameof(Keyframes));

            if (Keyframes.Count == 0)
                return;

            var maxTime = Keyframes.Max(x => x.Time);
            if (maxTime > Duration)
                Duration = maxTime;
        }
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

            var encodingProfile = MediaEncodingProfile.CreateHevc(VideoEncodingQuality.Uhd2160p);
            var mediaStreamSource = composition.GenerateMediaStreamSource(encodingProfile);
            return MediaSource.CreateFromMediaStreamSource(mediaStreamSource);
        }
        #endregion
    }
}