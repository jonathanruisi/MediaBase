using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

			/*return rating switch
			{
				10 => new SolidColorBrush(Color.FromArgb(255, 0, 255, 0)),
				9 => new SolidColorBrush(Color.FromArgb(255, 51, 255, 0)),
				8 => new SolidColorBrush(Color.FromArgb(255, 102, 255, 0)),
				7 => new SolidColorBrush(Color.FromArgb(255, 153, 255, 0)),
				6 => new SolidColorBrush(Color.FromArgb(255, 204, 255, 0)),
				5 => new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)),
				4 => new SolidColorBrush(Color.FromArgb(255, 255, 192, 0)),
				3 => new SolidColorBrush(Color.FromArgb(255, 255, 128, 0)),
				2 => new SolidColorBrush(Color.FromArgb(255, 255, 64, 0)),
				1 => new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
				_ => new SolidColorBrush(Colors.Transparent)
			};*/

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
}