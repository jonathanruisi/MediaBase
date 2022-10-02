using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediaBase.ViewModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.Storage;

namespace MediaBase
{
    public class ExplorerItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate DriveTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate WorkspaceFileTemplate { get; set; }
        public DataTemplate ProjectFileTemplate { get; set; }
        public DataTemplate ImageFileTemplate { get; set; }
        public DataTemplate VideoFileTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is not TreeViewNode node)
                return DefaultTemplate;

            if (node.Content is StorageFolder folder)
            {
                if (node.Depth == 0 && folder.DisplayName.Contains(':'))
                    return DriveTemplate;
                else
                    return FolderTemplate;
            }
            else if (node.Content is StorageFile file)
            {
                if (file.ContentType.ToLower().Contains("image"))
                    return ImageFileTemplate;
                if (file.ContentType.ToLower().Contains("video"))
                    return VideoFileTemplate;

                var extension = file.Name.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last().ToLower();
                if (extension == "mbw")
                    return WorkspaceFileTemplate;
                if (extension == "mbp")
                    return ProjectFileTemplate;
            }

            return DefaultTemplate;
        }
    }

    public class WorkspaceItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ProjectTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate VideoTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return item switch
            {
                Project => ProjectTemplate,
                MediaFolder => FolderTemplate,
                ImageSource => ImageTemplate,
                VideoSource => VideoTemplate,
                _ => null,
            };
        }
    }
}