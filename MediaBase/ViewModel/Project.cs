using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using MediaBase.ViewModel.Base;

using Windows.Storage;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// MediaBASE project ViewModel
    /// </summary>
    [ViewModelObject("Project", XmlNodeType.Element)]
    public sealed class Project : ViewModelElement
    {
        #region Fields
        public static readonly string[] MediaFileExtensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".avi", ".mov", ".wmv", ".mp4", ".mkv"
        };

        private const string MediaLibraryName = "Media Library";
        private bool _hasUnsavedChanges;
        private StorageFile _file;
        private ViewModelNode _activeProjectNode;
        private MBMediaSource _activeMediaSource;
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating whether or not there are changes to
        /// this <see cref="Project"/> that have yet to be saved.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value, true);
        }

        /// <summary>
        /// Gets or sets a reference to the file in which
        /// this <see cref="Project"/> is stored.
        /// </summary>
        public StorageFile File
        {
            get => _file;
            set => SetProperty(ref _file, value, true);
        }

        /// <summary>
        /// Gets the root of this <see cref="Project"/>'s media library.
        /// </summary>
        [ViewModelObject(MediaLibraryName, XmlNodeType.Element)]
        public MediaFolder MediaLibrary { get; set; }

        /// <summary>
        /// Gets or sets a reference to the currently active project item.
        /// </summary>
        public ViewModelNode ActiveNode
        {
            get => _activeProjectNode;
            set => SetProperty(ref _activeProjectNode, value, true);
        }

        /// <summary>
        /// Gets or sets a reference to the currently active media source.
        /// </summary>
        public MBMediaSource ActiveMediaSource
        {
            get => _activeMediaSource;
            set => SetProperty(ref _activeMediaSource, value, true);
        }
        #endregion

        #region Constructor
        public Project()
        {
            _hasUnsavedChanges = false;
            _file = null;
            _activeProjectNode = null;
            _activeMediaSource = null;
            MediaLibrary = new MediaFolder { Name = MediaLibraryName };
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Saves the XML representation of this <see cref="Project"/>
        /// to its associated <see cref="StorageFile"/>.
        /// </summary>
        public async void Save()
        {
            if (!HasUnsavedChanges)
                return;

            if (File == null)
            {
                throw new FileNotFoundException(
                    "This project is not associated with a StorageFile");
            }

            if (!File.IsAvailable)
            {
                throw new InvalidOperationException(
                    "The StorageFile associated with this project is not available");
            }

            // Create a temporary backup of the current save file,
            // then erase the current save file.
            StorageFile tempBackup = null;
            System.IO.File.Delete(File.Path + ".bak");
            if (System.IO.File.Exists(File.Path))
            {
                tempBackup = await File.CopyAsync(await File.GetParentAsync(), File.Name + ".bak");
                await FileIO.WriteTextAsync(File, string.Empty);
            }

            var success = true;
            XmlWriter writer = null;
            try
            {
                var settings = new XmlWriterSettings
                {
                    Async = true,
                    Indent = true,
                    IndentChars = "\t",
                    OmitXmlDeclaration = true,
                    ConformanceLevel = ConformanceLevel.Document,
                    CloseOutput = true
                };

                writer = XmlWriter.Create(await File.OpenStreamForWriteAsync(), settings);
                WriteXml(writer);
                await writer.FlushAsync();
            }
            catch (Exception)
            {
                success = false;
            }
            finally
            {
                writer?.Close();
            }

            if (success)
            {
                HasUnsavedChanges = false;

                // Delete the temporary backup file
                if (tempBackup != null)
                    await tempBackup.DeleteAsync(StorageDeleteOption.PermanentDelete);

                // TODO: Re-register for view model general change notification
            }
            else
            {
                if (tempBackup != null)
                    await tempBackup.MoveAndReplaceAsync(File);
            }
        }
        #endregion

        #region Private Methods

        #endregion

        #region Method Overrides (ObservableRecipient)
        protected override void OnActivated()
        {
            base.OnActivated();

            // TODO: Register to receive messages
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            // TODO: Unregister message receipt
        }
        #endregion
    }
}