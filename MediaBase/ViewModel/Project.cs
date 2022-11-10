using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JLR.Utility.WinUI.ViewModel;

using CommunityToolkit.Mvvm.Messaging;

using Windows.Storage;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// MediaBASE project ViewModel.
    /// </summary>
    [ViewModelType("Project")]
    public sealed class Project : ViewModelNode
    {
        #region Fields
        private StorageFile _file;
        private string _path;
        private bool _hasUnsavedChanges;
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
            set
            {
                SetProperty(ref _file, value, true);
                if (File is not null && File.IsAvailable)
                    Path = File.Path;
            }
        }

        /// <summary>
        /// Gets or sets the path of the file used to save this <see cref="Project"/>.
        /// </summary>
        /// <remarks>
        /// This property is used by <see cref="ProjectManager"/> during deserialization.
        /// </remarks>
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        [ViewModelCollection(nameof(MediaFileDictionary), "FileReference", true)]
        public Dictionary<string, Guid> MediaFileDictionary { get; }
        #endregion

        #region Constructors
        public Project() : this(string.Empty) { }

        public Project(string name)
        {
            Name = name;
            _file = null;
            _path = string.Empty;
            MediaFileDictionary = new Dictionary<string, Guid>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Saves the XML representation of this <see cref="Project"/>
        /// to the <see cref="StorageFile"/> pointed to by <see cref="File"/>.
        /// </summary>
        public async Task SaveAsync()
        {
            if (!HasUnsavedChanges)
                return;

            if (await SaveAsync(File))
            {
                HasUnsavedChanges = false;
                RegisterForViewModelSerializedPropertyChangeNotification();
            }
        }

        /// <summary>
        /// Alerts the user that unsaved changes exist,
        /// asking whether or not to save those changes.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if the user chose either <b>Yes</b> or <b>No</b>,
        /// <b><c>false</c></b> if the user chose <b>Cancel</b>.
        /// </returns>
        public async Task<bool> PromptSaveChanges()
        {
            if (!IsActive || !HasUnsavedChanges)
                return true;

            var dlg = new ContentDialog
            {
                Title = "Unsaved Changes",
                Content = $"Save changes to project {Name}?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                CloseButtonText = "Cancel",
                XamlRoot = App.Window.Content.XamlRoot
            };

            var choice = await dlg.ShowAsync();

            // Cancel
            if (choice == ContentDialogResult.None)
                return false;

            // Yes
            if (choice == ContentDialogResult.Primary)
            {
                if (File == null || !File.IsAvailable)
                {
                    if (await PromptSaveLocation() == false)
                        return false;
                }

                await SaveAsync();
            }

            return true;
        }

        /// <summary>
        /// Prompts the user to choose a location and a
        /// filename to which the project will be saved.
        /// The <see cref="File"/> property will be updated
        /// if a file is chosen.
        /// </summary>
        /// <returns>
        /// <b><c>true</c></b> if a file is chosen,
        /// <b><c>false</c></b> otherwise.
        /// </returns>
        public async Task<bool> PromptSaveLocation()
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                CommitButtonText = "Save",
                SuggestedFileName = Name
            };

            picker.FileTypeChoices.Add("MediaBase Project Files", new List<string> { ".mbp" });
            InitializeWithWindow.Initialize(picker, App.WindowHandle);

            var file = await picker.PickSaveFileAsync();
            if (file != null && file.IsAvailable)
            {
                File = file;
                return true;
            }

            return false;
        }
        #endregion

        #region Method Overrides (ViewModelElement)
        protected override object CustomPropertyParser(string propertyName, string content, params string[] args)
        {
            if (propertyName == nameof(MediaFileDictionary) &&
                args.Length > 0 &&
                args[0] == "FileReference")
            {
                var kvpStrings = content[1..^1].Split(',', StringSplitOptions.TrimEntries);
                return KeyValuePair.Create((object)kvpStrings[0], (object)Guid.Parse(kvpStrings[1]));
            }

            return null;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            RegisterForViewModelSerializedPropertyChangeNotification();
        }

        protected override void OnDeactivated()
        {
            Messenger.Unregister<SerializedPropertyChangedMessage>(this);
            base.OnDeactivated();
        }
        #endregion

        #region Private Methods
        private void RegisterForViewModelSerializedPropertyChangeNotification()
        {
            Messenger.Register<SerializedPropertyChangedMessage>(this, (r, m) =>
            {
                if (m.Sender is ViewModelElement node && node.Root == r)
                {
                    ((Project)r).HasUnsavedChanges = true;

                    // Unregister from further messages.
                    // We will re-register when project is saved.
                    Messenger.Unregister<SerializedPropertyChangedMessage>(r);
                }
            });
        }
        #endregion
    }
}