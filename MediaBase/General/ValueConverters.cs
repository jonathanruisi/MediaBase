using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediaBase.ViewModel;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace MediaBase
{
    public class RatingAdjustmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not int rating)
                throw new ArgumentException("Object must be an integer", nameof(value));

            return (double)(rating == 0 ? -1 : rating);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is not double rating)
                throw new ArgumentException("Object must be a double", nameof(value));

            return (int)(rating < 0 ? 0 : rating);
        }
    }

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

    public class GroupMaskToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!int.TryParse(parameter as string, out int group))
                throw new ArgumentNullException(nameof(parameter));

            group--;
            if (group is < 0 or > 7)
                throw new ArgumentOutOfRangeException(nameof(parameter));

            return ((int)value & (1 << group)) != 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class GroupMaskToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!int.TryParse(parameter as string, out int group))
                throw new ArgumentNullException(nameof(parameter));

            group--;
            if (group is < 0 or > 7)
                throw new ArgumentOutOfRangeException(nameof(parameter));

            return ((int)value & (1 << group)) != 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class MediaTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
                return string.Empty;

            if (value is not MultimediaSource mediaSource)
                throw new ArgumentException("Object must be a MultimediaSource", nameof(value));

            if (mediaSource is ViewModel.ImageSource imageSource)
                return imageSource.IsAnimated ? "Animated Image" : "Image";
            else if (mediaSource.ContentType == MediaContentType.Video)
                return "Video";
            else
                throw new ArgumentException("Unrecognized MultimediaSource", nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}