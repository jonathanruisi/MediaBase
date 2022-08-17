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
    public class WorkspaceItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ProjectTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate VideoTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            switch (item)
            {
                case Project:
                    return ProjectTemplate;

                case MediaFolder:
                    return FolderTemplate;

                case MediaFileSource mediaFile:
                    if (mediaFile.File is ImageFile)
                        return ImageTemplate;
                    if (mediaFile.File is VideoFile)
                        return VideoTemplate;
                    return null;

                default:
                    return null;
            }
        }
    }
}