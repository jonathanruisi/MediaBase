using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase.ViewModel
{
    [Flags]
    public enum MediaContentType : short
    {
        None = 0,
        Audio = 1,
        Image = 2,
        Video = 4
    }
}