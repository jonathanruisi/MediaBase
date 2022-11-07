using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase
{
    public enum TimeDisplayFormat
    {
        None,
        TimecodeWithFrame,
        TimecodeWithMillis,
        FrameNumber
    }

    public enum BatchAction
    {
        None,
        Delete,
        Copy,
        Move
    }
}