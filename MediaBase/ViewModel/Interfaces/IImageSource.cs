using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// Represents an object that contains an image.
    /// </summary>
    public interface IImageSource
    {
        ObservableCollection<ImageAnimationKeyframe> Keyframes { get; }
    }
}