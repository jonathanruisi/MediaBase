using System;

using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace MediaBase
{
	public class ObjectToFriendlyTypeStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			switch (value)
			{
				case ImageFile _:
					return "Image";
				case VideoFile _:
					return "Video";
				case Marker marker:
					return marker.Duration > 0 ? "Clip" : "Marker";
				default:
					return "?";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}

	public class RatingAdjustmentConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (!(value is int rating))
				throw new ArgumentException("Object must be an integer", nameof(value));

			return (double) (rating == 0 ? -1 : rating);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (!(value is double rating))
				throw new ArgumentException("Object must be a double", nameof(value));

			return (int) (rating < 0 ? 0 : rating);
		}
	}

	public class RatingToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (!(value is int rating))
				throw new ArgumentException("Object must be an integer", nameof(value));

			switch(rating)
			{
				case 10:
					return new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
				case 9:
					return new SolidColorBrush(Color.FromArgb(255, 51, 255, 0));
				case 8:
					return new SolidColorBrush(Color.FromArgb(255, 102, 255, 0));
				case 7:
					return new SolidColorBrush(Color.FromArgb(255, 153, 255, 0));
				case 6:
					return new SolidColorBrush(Color.FromArgb(255, 204, 255, 0));
				case 5:
					return new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
				case 4:
					return new SolidColorBrush(Color.FromArgb(255, 255, 192, 0));
				case 3:
					return new SolidColorBrush(Color.FromArgb(255, 255, 128, 0));
				case 2:
					return new SolidColorBrush(Color.FromArgb(255, 255, 64, 0));
				case 1:
					return new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
				default:
					return new SolidColorBrush(Colors.Transparent);
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}