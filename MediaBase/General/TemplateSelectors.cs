using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MediaBase
{
	public class MediaItemTemplateSelector : DataTemplateSelector
	{
		public DataTemplate FolderTemplate { get; set; }
		public DataTemplate ImageFileTemplate { get; set; }
		public DataTemplate VideoFileTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(object item)
		{
			switch (item)
			{
				case MediaTreeFolder _:
					return FolderTemplate;
				case ImageFile _:
					return ImageFileTemplate;
				case VideoFile _:
					return VideoFileTemplate;
				default:
					return null;
			}
		}
	}
}