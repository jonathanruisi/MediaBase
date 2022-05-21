using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.ViewModel;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Defines the scale and translation of a media
    /// object at a specific point in time.
    /// </summary>
    [ViewModelObject(nameof(PanAndZoomKeyframe), XmlNodeType.Element)]
    public sealed class PanAndZoomKeyframe : Keyframe
    {
        private double _scale, _offsetX, _offsetY;

        /// <summary>
        /// Gets or sets the scale.
        /// </summary>
        /// <remarks>
        /// This is a log-normalized value.
        /// <b>Zero</b> is equivalent to the original size,
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

        public PanAndZoomKeyframe()
        {
            _scale = 0;
            _offsetX = 0;
            _offsetY = 0;
        }

        public override string ToString()
        {
            return $"{base.ToString()} (Scale={Scale:0.000}, X={OffsetX:0.000}, Y={OffsetY:0.000})";
        }
    }
}