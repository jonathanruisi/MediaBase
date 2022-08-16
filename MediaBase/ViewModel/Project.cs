using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Toolkit.Mvvm.Messaging;

using JLR.Utility.WinUI.ViewModel;
using System.Xml;
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

        #endregion

        #region Private Methods

        #endregion
    }
}