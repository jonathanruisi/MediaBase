using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JLR.Utility.WinUI;

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
                if (node.Depth == 0 && folder.Name.Contains(':'))
                    return DriveTemplate;
                else
                    return FolderTemplate;
            }
            else if (node.Content is StorageFile storageFile)
            {
                var extension = storageFile.GetFileExtension();
                if (extension == ProjectManager.WorkspaceFileExtension)
                    return WorkspaceFileTemplate;
                if (extension == ProjectManager.ProjectFileExtension)
                    return ProjectFileTemplate;
            }
            else if (node.Content is MultimediaSource mediaSource)
            {
                if (mediaSource.ContentType == MediaContentType.Image)
                    return ImageFileTemplate;
                else if (mediaSource.ContentType == MediaContentType.Video)
                    return VideoFileTemplate;
            }

            return DefaultTemplate;
        }
    }

    public class WorkspaceItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate VideoTemplate { get; set; }
        public DataTemplate PlaylistTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return item switch
            {
                MediaFolder => FolderTemplate,
                ImageSource => ImageTemplate,
                VideoSource => VideoTemplate,
                Playlist    => PlaylistTemplate,
                _ => null
            };
        }
    }
}