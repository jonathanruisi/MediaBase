using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using JLR.Utility.WinUI.ViewModel;

using MediaBase.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MediaBase.Controls
{
    public sealed partial class WorkspaceBrowser : UserControl
    {
        #region Properties
        public ProjectManager ViewModel => (ProjectManager)DataContext;
        #endregion

        #region Constructor
        public WorkspaceBrowser()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ProjectManager>();

            InitializeCommands();
        }
        #endregion

        #region Event Handlers (Commands - CanExecuteRequested)

        #endregion

        #region Event Handlers (Commands - ExecuteRequested)

        #endregion

        #region Event Handlers (TreeView)
        private void WorkspaceBrowserTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is not MultimediaSource mediaSource || mediaSource.IsReady == false)
                return;

            ViewModel.ActiveMediaSource = mediaSource;
        }

        private void WorkspaceBrowserTreeView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is TreeViewItem item)
                ViewModel.ActiveNode = item.DataContext as ViewModelNode;
            else
                ViewModel.ActiveNode = null;

            e.Handled = true;
        }

        private void WorkspaceBrowserTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {

        }
        #endregion

        #region Private Methods
        private void InitializeCommands()
        {
            
        }
        #endregion
    }
}