using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediaBase.ViewModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaBase
{
	public class ProjectItemTemplateSelector : DataTemplateSelector
	{
		public DataTemplate ProjectFileTemplate { get; set; }
		public DataTemplate FolderTemplate { get; set; }
		public DataTemplate ImageFileTemplate { get; set; }
		public DataTemplate VideoFileTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(object item)
		{
            return item switch
            {
                Project     => ProjectFileTemplate,
                MediaFolder => FolderTemplate,
                ImageFile   => ImageFileTemplate,
                VideoFile   => VideoFileTemplate,
                _           => null
            };
        }
	}
}