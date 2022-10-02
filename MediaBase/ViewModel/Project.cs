using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JLR.Utility.WinUI.ViewModel;

using CommunityToolkit.Mvvm.Messaging;

using Windows.Storage;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// MediaBASE project ViewModel.
    /// </summary>
    [ViewModelType("Project")]
    public sealed class Project : MediaFolder
    {
        #region Fields
        private StorageFile _file;
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
            set => SetProperty(ref _file, value, true);
        }
        #endregion

        #region Constructor
        public Project() : this(string.Empty) { }

        public Project(string name)
        {
            Name = name;
        }
        #endregion

        #region Method Overrides (ObservableRecipient)
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
                if (m.Sender is ViewModelNode node && node.Root == this)
                {
                    HasUnsavedChanges = true;

                    // Unregister from further messages.
                    // We will re-register when project is saved.
                    Messenger.Unregister<SerializedPropertyChangedMessage>(this);
                }
            });
        }
        #endregion
    }
}