using System.Text;
using System.Xml;

using JLR.Utility.WinUI.Controls;

using MediaBase.General;

namespace MediaBase.ViewModel
{
    [ViewModelObject("Marker", XmlNodeType.Element)]
    public sealed class Marker : ViewModelElement, ITimelineMarker
    {
        #region Fields
        private decimal _position, _duration;
        private int _track;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the offset within the timeline where the marker occurs, in seconds.
        /// </summary>
        [ViewModelObject(nameof(Position), XmlNodeType.Element)]
        public decimal Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        /// <summary>
        /// Gets or sets the duration of the marker within the timeline, in seconds.
        /// </summary>
        [ViewModelObject(nameof(Duration), XmlNodeType.Element)]
        public decimal Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        /// <summary>
        /// Gets or sets a value which identifies the collection (category or track)
        /// to which the marker belongs.
        /// </summary>
        [ViewModelObject(nameof(Track), XmlNodeType.Attribute)]
        public int Track
        {
            get => _track;
            set => SetProperty(ref _track, value);
        }
        #endregion

        #region Constructor
        public Marker()
        {
            _position = 0;
            _duration = 0;
            _track = 0;
        }
        #endregion

        #region Method Overrides (System.Object)
        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int framesPerSecond)
        {
            var builder = new StringBuilder($"{Name} @{Position.ToTimecodeString(framesPerSecond, framesPerSecond <= 0)}");
            if (Duration > 0)
                builder.Append($" (Duration: {Duration.ToTimecodeString(framesPerSecond, framesPerSecond <= 0)})");
            return builder.ToString();
        }
        #endregion
    }
}