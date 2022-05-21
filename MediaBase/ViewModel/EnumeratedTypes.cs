using System;

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