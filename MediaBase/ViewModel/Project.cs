using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Toolkit.Mvvm.Messaging;

using JLR.Utility.WinUI.ViewModel;
using System.Xml;

namespace MediaBase.ViewModel
{
    /// <summary>
    /// MediaBASE project ViewModel
    /// </summary>
    [ViewModelObject("Project", XmlNodeType.Element)]
    public sealed class Project : ViewModelElement
    {
        #region Fields
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
        #endregion

        #region Constructor

        #endregion

        #region Private Methods
        
        #endregion
    }
}