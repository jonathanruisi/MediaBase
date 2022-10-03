using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace MediaBase
{
    public class RatingToSolidColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (int)value switch
            {
                5 => new SolidColorBrush(Colors.Orange),
                4 => new SolidColorBrush(Colors.HotPink),
                3 => new SolidColorBrush(Colors.CornflowerBlue),
                2 => new SolidColorBrush(Colors.LimeGreen),
                1 => new SolidColorBrush(Colors.LightGray),
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}