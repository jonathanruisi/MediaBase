using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MediaBase.ViewModel
{
    [ViewModelObject("Video File", XmlNodeType.Element)]
    public class VideoFile : MediaFile, IVideoSource
    {
        #region Fields
        private decimal _trimmedDuration;
        private List<(decimal start, decimal end)> _playableRanges;
        #endregion

        #region Properties
        [ViewModelCollection(nameof(Markers), "Marker")]
        public ObservableCollection<Marker> Markers { get; }

        [ViewModelCollection(nameof(Cuts), "Cut", true, true)]
        public ObservableCollection<(decimal start, decimal end)> Cuts { get; }

        /// <summary>
        /// Gets a value indicating what the duration of the
        /// video will be after all cuts are applied.
        /// </summary>
        public decimal TrimmedDuration
        {
            get => _trimmedDuration;
            private set => SetProperty(ref _trimmedDuration, value);
        }

        public override MediaContentType ContentType => MediaContentType.Video;
        #endregion

        #region Constructors
        public VideoFile() : this(null) { }

        public VideoFile(StorageFile file) : base(file)
        {
            _trimmedDuration = 0;
            _playableRanges = new List<(decimal start, decimal end)>();

            Markers = new ObservableCollection<Marker>();
            Markers.CollectionChanged += Markers_CollectionChanged;

            Cuts = new ObservableCollection<(decimal start, decimal end)>();
            Cuts.CollectionChanged += Cuts_CollectionChanged;
        }
        #endregion

        #region Public Methods
        // TODO: Implement a static method to create a new IVideoSource (like a VideoClip or something) from the cuts applied to this VideoFile
        #endregion

        #region Event Handlers
        private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

        }

        private void Cuts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EvaluateCuts();
        }
        #endregion

        #region Method Overrides (MediaFile)
        public override async Task<bool> LoadMediaPropertiesAsync()
        {
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
                return false;
            }

            return true;
        }
        #endregion

        #region Method Overrides (MBMediaSource)
        public override async Task<IMediaPlaybackSource> GetMediaSourceAsync()
        {
            var composition = new MediaComposition();

            if (_playableRanges.Count == 0)
            {
                composition.Clips.Add(await MediaClip.CreateFromFileAsync(File));
            }
            else
            {
                foreach (var range in _playableRanges)
                {
                    var clip = await MediaClip.CreateFromFileAsync(File);
                    clip.TrimTimeFromStart = TimeSpan.FromSeconds(decimal.ToDouble(range.start));
                    clip.TrimTimeFromEnd = TimeSpan.FromSeconds(decimal.ToDouble(Duration - range.end));
                    composition.Clips.Add(clip);
                }
            }

            var encodingProfile = MediaEncodingProfile.CreateHevc(VideoEncodingQuality.Uhd2160p);
            var mediaStreamSource = composition.GenerateMediaStreamSource(encodingProfile);
            return MediaSource.CreateFromMediaStreamSource(mediaStreamSource);
        }
        #endregion

        #region Private Methods
        private void EvaluateCuts()
        {
            decimal lastStart = 0;
            _playableRanges.Clear();

            foreach (var cut in Cuts.OrderBy(x => x.start))
            {
                if (lastStart >= Duration)
                    break;

                if (cut.start <= 0)
                {
                    lastStart = cut.end;
                    continue;
                }

                _playableRanges.Add((lastStart, cut.start));
                lastStart = cut.end;
            }

            if (lastStart > 0 && lastStart < Duration)
                _playableRanges.Add((lastStart, Duration));

            if (_playableRanges.Count == 0)
                TrimmedDuration = Duration;
            else
            {
                decimal trimmedDuration = 0;
                foreach (var range in _playableRanges)
                {
                    trimmedDuration += range.end - range.start;
                }

                TrimmedDuration = trimmedDuration;
            }
        }
        #endregion
    }
}