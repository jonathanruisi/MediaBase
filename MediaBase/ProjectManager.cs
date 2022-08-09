using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JLR.Utility.WinUI.ViewModel;

using MediaBase.ViewModel;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Input;

namespace MediaBase
{
    public sealed class ProjectManager : ObservableRecipient
    {
        #region Fields
        public readonly string[] MediaFileExtensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".avi", ".mov", ".wmv", ".mp4", ".mkv"
        };

        private string _displayName;
        private bool _hasUnsavedChanges;
        #endregion

        #region Properties
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value, true);
        }

        public Dictionary<Guid, IMediaFile> MediaFileDatabase { get; }

        public HashSet<string> TagDatabase { get; }
        #endregion

        #region Commands
        // General
        public XamlUICommand GeneralPreviousCommand { get; private set; }
        public XamlUICommand GeneralNextCommand { get; private set; }

        // Project
        public XamlUICommand ProjectNewCommand { get; private set; }
        public XamlUICommand ProjectOpenCommand { get; private set; }
        public XamlUICommand ProjectSaveCommand { get; private set; }
        public XamlUICommand ProjectSaveAsCommand { get; private set; }
        public XamlUICommand ProjectCloseCommand { get; private set; }
        public XamlUICommand ProjectNewFolderCommand { get; private set; }
        public XamlUICommand ProjectImportFilesCommand { get; private set; }
        public XamlUICommand ProjectImportFolderCommand { get; private set; }
        public XamlUICommand ProjectRemoveItemCommand { get; private set; }
        public XamlUICommand ProjectRemoveSelectedCommand { get; private set; }
        public XamlUICommand ProjectRemoveAllCommand { get; private set; }
        public XamlUICommand ProjectRenameItemCommand { get; private set; }
        public XamlUICommand ProjectSelectMultipleCommand { get; private set; }
        public XamlUICommand ProjectDeleteMarkerCommand { get; private set; }

        // Tools
        public XamlUICommand ToolsMark1Command { get; private set; }
        public XamlUICommand ToolsMark2Command { get; private set; }
        public XamlUICommand ToolsMark3Command { get; private set; }
        public XamlUICommand ToolsMark4Command { get; private set; }
        #endregion

        #region Constructor
        public ProjectManager()
        {
            _displayName = null;
            _hasUnsavedChanges = false;

            MediaFileDatabase = new Dictionary<Guid, IMediaFile>();
            TagDatabase = new HashSet<string>();

            InitializeCommands();
            RegisterMessages();
        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {

        }

        private void RegisterMessages()
        {

        }

        private void RegisterForViewModelSerializedPropertyChangeNotification()
        {
            Messenger.Register<SerializedPropertyChangedMessage>(this, (r, m) =>
            {
                HasUnsavedChanges = true;

                // Unregister from further messages.
                // We will re-register when project is saved.
                Messenger.Unregister<SerializedPropertyChangedMessage>(this);
            });
        }
        #endregion
    }
}