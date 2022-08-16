using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.ViewModel;

namespace MediaBase.ViewModel
{
    [ViewModelType(nameof(MediaFileSource))]
    public class MediaFileSource : MultimediaSource
    {
        #region Fields
        private bool _isReady;
        private MediaFile _file;
        #endregion

        #region Properties
        [ViewModelProperty(nameof(File), XmlNodeType.Element)]
        public MediaFile File
        {
            get => _file;
            set
            {
                if (_file != null)
                    _file.PropertyChanged -= File_PropertyChanged;

                SetProperty(ref _file, value);

                if (_file != null)
                {
                    _file.PropertyChanged += File_PropertyChanged;
                    IsReady = _file.IsReady;
                }
            }
        }

        public override bool IsReady
        {
            get => _isReady;
            protected set
            {
                SetProperty(ref _isReady, value);

                if (_isReady)
                {
                    if (File is ImageFile imageFile)
                    {
                        WidthInPixels = imageFile.WidthInPixels;
                        HeightInPixels = imageFile.HeightInPixels;
                        FramesPerSecond = 0;
                        Duration = 0;
                    }
                    else if (File is VideoFile videoFile)
                    {
                        WidthInPixels = videoFile.WidthInPixels;
                        HeightInPixels = videoFile.HeightInPixels;
                        FramesPerSecond = videoFile.FramesPerSecond;
                        Duration = videoFile.Duration;
                    }
                }
                else
                {
                    WidthInPixels = 0;
                    HeightInPixels = 0;
                    FramesPerSecond = 0;
                    Duration = 0;
                }
            }
        }
        #endregion

        #region Constructors
        public MediaFileSource()
        {
            _isReady = false;
            _file = null;
        }
        #endregion

        #region Event Handlers
        private void File_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MediaFile.IsReady))
                IsReady = (sender as MediaFile).IsReady;
        }
        #endregion
    }
}