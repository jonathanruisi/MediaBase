using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediaBase.ViewModel;

using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

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

	public class RatingToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is not int rating)
				throw new ArgumentException("Object must be an integer", nameof(value));

			return rating switch
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

	public class MediaTypeToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is null)
				return string.Empty;

			if (value is not MBMediaSource media)
				throw new ArgumentException("Object must be an MBMediaSource", nameof(value));

			if (media.ContentType == MediaContentType.Image)
				return media.Duration > 0 ? "Animated Image" : "Image";
			else if (media.ContentType == MediaContentType.Video)
				return "Video";
			else throw new ArgumentException("Unrecognized MBMediaSource", nameof(value));
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}