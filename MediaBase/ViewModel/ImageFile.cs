﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.ViewModel;

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
    [ViewModelType("Image")]
    public class ImageFile : MBMediaSource, IMediaFile
    {
        #region Fields
        private string _path;
        private StorageFile _file;
        private bool _isReady;
        #endregion

        #region Properties
        public override MediaContentType ContentType => MediaContentType.Image;

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
        public ImageFile() : this(null) { }

        public ImageFile(StorageFile file)
        {
            _file = file;
            _path = file?.Path;
            _isReady = false;
            Name = file?.DisplayName;
            FramesPerSecond = App.RefreshRate;  // TODO: Refresh rate should ultimately be equal to monitor
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

        #region Method Overrides (System.Object)
        public override string ToString()
        {
            var filename = IsReady ? File.Name : "FILE NOT LOADED";
            return $"{base.ToString()} ({filename})";
        }
        #endregion
    }
}