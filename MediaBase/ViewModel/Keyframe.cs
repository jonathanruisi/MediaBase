﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JLR.Utility.WinUI.ViewModel;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Indicates the type of adjustment to be made by a keyframe.
    /// </summary>
    public enum KeyframeAdjustment
    {
        #region Parameterless adjustments

        #endregion

        #region Adjustments with one or more parameter
        /// <summary>
        /// Media is scaled relative to its original size.
        /// <para>
        /// Value type: Floating-point<br/>
        /// Value range: -∞ to +∞, or Fit
        /// </para>
        /// </summary>
        /// <remarks>
        /// This is a log-normalized value.
        /// <b>Zero</b> is equivalent to the original size,
        /// values <b>greater than zero</b> are larger than the original size,
        /// and values <b>less than zero</b> are smaller than the original size.
        /// </remarks>
        Scale,

        /// <summary>
        /// Media is translated horizontally by a specified number of pixels.
        /// <para>
        /// Value type: Floating-point<br/>
        /// Value range: -∞ to +∞
        /// </para>
        /// </summary>
        OffsetX,

        /// <summary>
        /// Media is translated vertically by a specified number of pixels.
        /// <para>
        /// Value type: Floating-point<br/>
        /// Value range: -∞ to +∞
        /// </para>
        /// </summary>
        OffsetY,

        /// <summary>
        /// Sets the opacity of the media to a specified value.
        /// <para>
        /// Value type: Floating-point<br/>
        /// Value range: 0.0 (transparent) to 1.0 (opaque)
        /// </para>
        /// </summary>
        Opacity,

        /// <summary>
        /// Sets the playback rate of the media to a specified value.
        /// <para>
        /// Value type: Floating-point<br/>
        /// Value range: 0.5 to 3.0 for video, 0.25 to 4.0 for animated images
        /// </para>
        /// </summary>
        PlaybackRate
        #endregion
    }

    public enum PositionRelativeToKeyframe
    {
        None,
        BeforeFirst,
        AtOrAfterLast,
        Between
    }

    [ViewModelType(nameof(Keyframe))]
    public sealed class Keyframe : Marker
    {
        private const string DefaultStyleString = "KeyframeDefault";

        [ViewModelCollection(nameof(Adjustments), "Adjustment")]
        public Dictionary<KeyframeAdjustment, string> Adjustments { get; }

        public Keyframe() : this(0) { }

        public Keyframe(decimal position, string group = null, string style = DefaultStyleString) : base(position, 0, group, style)
        {
            Name = nameof(Keyframe);
            Adjustments = new Dictionary<KeyframeAdjustment, string>();
        }
    }

    public sealed class KeyframeStatus
    {
        public List<Keyframe> ScaleKeyframes { get; set; }
        public List<Keyframe> OffsetXKeyframes { get; set; }
        public List<Keyframe> OffsetYKeyframes { get; set; }
        public List<Keyframe> OpacityKeyframes { get; set; }
        public List<Keyframe> PlaybackRateKeyframes { get; set; }

        public KeyframeStatus()
        {
            ScaleKeyframes = new List<Keyframe>();
            OffsetXKeyframes = new List<Keyframe>();
            OffsetYKeyframes = new List<Keyframe>();
            OpacityKeyframes = new List<Keyframe>();
            PlaybackRateKeyframes = new List<Keyframe>();
        }
    }
}