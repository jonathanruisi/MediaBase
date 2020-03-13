using System;

using Windows.UI.Xaml.Data;

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
}