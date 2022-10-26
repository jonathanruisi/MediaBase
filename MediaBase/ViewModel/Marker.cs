using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.Controls;
using JLR.Utility.WinUI.ViewModel;

namespace MediaBase.ViewModel
{
    [ViewModelType(nameof(Marker))]
    public class Marker : ViewModelElement, ITimelineMarker
    {
        #region Fields
        private const string DefaultStyleString = "MarkerDefault";
        private decimal _position, _duration;
        private int _group;
        private string _style;
        #endregion

        #region Properties
        [ViewModelProperty(nameof(Position), XmlNodeType.Element)]
        public decimal Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        [ViewModelProperty(nameof(Duration), XmlNodeType.Element)]
        public decimal Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        [ViewModelProperty(nameof(Group), XmlNodeType.Attribute)]
        public int Group
        {
            get => _group;
            set => SetProperty(ref _group, value);
        }

        [ViewModelProperty(nameof(Style), XmlNodeType.Element)]
        public string Style
        {
            get => _style;
            set => SetProperty(ref _style, value);
        }
        #endregion

        #region Constructors
        public Marker() : this(0) { }

        public Marker(decimal position, decimal duration = 0, int group = 0, string style = DefaultStyleString)
        {
            _position = position;
            _duration = duration;
            _group = group;
            _style = style;
        }
        #endregion

        #region Method Overrides (System.Object)
        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int framesPerSecond)
        {
            var builder = new StringBuilder($"{Name} @{Position.ToTimecodeString(framesPerSecond, framesPerSecond == 0 ? TimeDisplayFormat.TimecodeWithMillis : TimeDisplayFormat.TimecodeWithFrame)}");
            if (Duration > 0)
                builder.Append($" (Duration: {Duration.ToTimecodeString(framesPerSecond, framesPerSecond == 0 ? TimeDisplayFormat.TimecodeWithMillis : TimeDisplayFormat.TimecodeWithFrame)})");
            return builder.ToString();
        }
        #endregion
    }
}