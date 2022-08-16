﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.ViewModel;

using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.Storage;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Contains properties and methods needed for accessing video files
    /// </summary>
    [ViewModelType("Video")]
    public class VideoFile : VideoSource, IMediaFile
    {
        #region Fields
        private string _path;
        private StorageFile _file;
        private bool _isReady;
        #endregion

        #region Properties
        [ViewModelProperty(nameof(Path), XmlNodeType.Element)]
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public StorageFile File
        {
            get => _file;
            set => SetProperty(ref _file, value);
        }

        public override bool IsReady
        {
            get => _isReady;
            protected set => SetProperty(ref _isReady, value);
        }
        #endregion

        #region Constructors
        public VideoFile() : this(null) { }

        public VideoFile(StorageFile file)
        {
            _file = file;
            _path = file?.Path;
            _isReady = false;
            Name = file?.DisplayName;
        }
        #endregion

        #region Public Methods
        public async Task<bool> LoadFileFromPathAsync()
        {
            if (string.IsNullOrEmpty(Path))
            {
                IsReady = false;
                return false;
            }

            try
            {
                File = await StorageFile.GetFileFromPathAsync(Path);
                Name = File.DisplayName;
            }
            catch (FileNotFoundException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (ArgumentException) { return false; }

            var contentTypeString = Enum.GetName(ContentType);
            if (!File.ContentType.Contains(contentTypeString.ToLower()))
            {
                IsReady = false;
                throw new InvalidOperationException($"{contentTypeString} file expected");
            }
                
            return await ReadPropertiesFromFileAsync();
        }

        public async Task<bool> ReadPropertiesFromFileAsync()
        {
            if (File?.IsAvailable == false)
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

        public override async Task<IMediaPlaybackSource> GetPlaybackSourceAsync()
        {
            var composition = new MediaComposition();

            if (PlayableRanges.Count > 0 && AreCutsApplied)
            {
                foreach (var (start, end) in PlayableRanges)
                {
                    var clip = await MediaClip.CreateFromFileAsync(File);
                    clip.TrimTimeFromStart = TimeSpan.FromSeconds(decimal.ToDouble(start));
                    clip.TrimTimeFromEnd = TimeSpan.FromSeconds(decimal.ToDouble(Duration - end));
                    composition.Clips.Add(clip);
                }
            }
            else
            {
                composition.Clips.Add(await MediaClip.CreateFromFileAsync(File));
            }

            var encodingProfile = composition.CreateDefaultEncodingProfile();
            var mediaStreamSource = composition.GenerateMediaStreamSource(encodingProfile);
            return MediaSource.CreateFromMediaStreamSource(mediaStreamSource);
        }
        #endregion

        #region Method Overrides (System.Object)
        public override string ToString()
        {
            var filename = IsReady ? File.Name : "FILE NOT LOADED";
            return $"{base.ToString()} ({filename})";
        }
        #endregion
    }
}