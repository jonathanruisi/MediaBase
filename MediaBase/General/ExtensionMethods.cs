using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase
{
    public static class ExtensionMethods
    {
        #region Timecode String Formatting
        public static string ToTimecodeString(this double value,
                                              int framesPerSecond = 30,
                                              TimeDisplayFormat format = TimeDisplayFormat.TimecodeWithFrame)
        {
            return ToTimecodeString((decimal)value, framesPerSecond, format);
        }

        public static string ToTimecodeString(this decimal value,
                                              int framesPerSecond = 30,
                                              TimeDisplayFormat format = TimeDisplayFormat.TimecodeWithFrame)
        {
            if (framesPerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(framesPerSecond),
                    "This value must be greater than zero");

            if (format == TimeDisplayFormat.FrameNumber)
                return $"Frame {(int)(value * framesPerSecond)}";

            var hour = (int)(value / 3600);
            value -= hour * 3600;
            var minute = (int)(value / 60);
            value -= minute * 60;
            var second = (int)value;
            value -= second;
            var frame = (int)(value / (1M / framesPerSecond));

            var builder = new StringBuilder();
            if (hour > 0) builder.Append($"{hour:00}:");
            if (minute > 0) builder.Append($"{minute:00}:");
            builder.Append($"{second:00}");
            if (format == TimeDisplayFormat.TimecodeWithMillis)
                builder.Append($".{value:000}");
            else if (framesPerSecond > 100)
                builder.Append($";{frame:000}");
            else if (framesPerSecond > 10)
                builder.Append($";{frame:00}");
            else
                builder.Append($";{frame:0}");
            return builder.ToString();
        }
        #endregion
    }
}