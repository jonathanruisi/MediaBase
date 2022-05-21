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
    /// Represents a point in time in a media object at which an event takes place.
    /// </summary>
    public abstract class Keyframe : ViewModelElement
    {
        private decimal _time;

        /// <summary>
        /// Gets or sets the time where the keyframe occurs.
        /// </summary>
        [ViewModelObject(nameof(Time), XmlNodeType.Attribute)]
        public decimal Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        protected Keyframe()
        {
            Name = string.Empty;
            _time = 0;
        }

        public override string ToString()
        {
            return $"{Time:0.0####}";
        }
    }
}