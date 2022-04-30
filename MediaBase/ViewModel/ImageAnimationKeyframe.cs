using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using MediaBase.ViewModel.Base;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Defines an image's scale and translation at
    /// a specific point in time.
    /// </summary>
    [ViewModelObject("Image Keyframe", XmlNodeType.Element)]
    public sealed class ImageAnimationKeyframe : ViewModelElement
    {
        #region Fields
        private decimal _time;
        private double _scale, _offsetX, _offsetY;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the time where the keyframe occurs.
        /// </summary>
        [ViewModelObject(nameof(Time), XmlNodeType.Attribute)]
        public decimal Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        /// <summary>
        /// Gets or sets the image scale.
        /// </summary>
        /// <remarks>
        /// This is a log-normalized value.
        /// <b>Zero</b> is equivalent to the original image size,
        /// values <b>greater than zero</b> are larger than the original size,
        /// and values <b>less than zero</b> are smaller than the original size.
        /// </remarks>
        [ViewModelObject(nameof(Scale), XmlNodeType.Element)]
        public double Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value);
        }

        /// <summary>
        /// Gets or sets the horizontal translation offset, in pixels.
        /// </summary>
        [ViewModelObject(nameof(OffsetX), XmlNodeType.Element)]
        public double OffsetX
        {
            get => _offsetX;
            set => SetProperty(ref _offsetX, value);
        }

        /// <summary>
        /// Gets or sets the vertical translation offset, in pixels.
        /// </summary>
        [ViewModelObject(nameof(OffsetY), XmlNodeType.Element)]
        public double OffsetY
        {
            get => _offsetY;
            set => SetProperty(ref _offsetY, value);
        }
        #endregion

        #region Constructor
        public ImageAnimationKeyframe()
        {
            Name = string.Empty;
            _time = 0;
            _scale = 0;
            _offsetX = 0;
            _offsetY = 0;
        }
        #endregion

        #region Method Overrides (System.Object)
        public override string ToString()
        {
            return $"{Time:0.0####},{Scale:0.0####},{OffsetX:0.0####},{OffsetY:0.0####}";
        }
        #endregion
    }
}