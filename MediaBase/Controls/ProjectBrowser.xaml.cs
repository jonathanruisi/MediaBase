using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using MediaBase.ViewModel;
using MediaBase.ViewModel.Base;

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
    public sealed partial class ProjectBrowser : UserControl
    {
        #region Properties
        public Project ViewModel => (Project)DataContext;
        #endregion

        #region Constructor
        public ProjectBrowser()
        {
            InitializeComponent();

            DataContext = App.Current.Services.GetService<Project>();
        }
        #endregion

        #region Event Handlers
        private void ProjectBrowserTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (!(args.InvokedItem is MBMediaSource mediaSource))
                return;

            ViewModel.ActiveMediaSource = mediaSource;
        }

        private void ProjectBrowserTreeView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is TreeViewItem item)
                ViewModel.ActiveNode = item.DataContext as ViewModelNode;
            else
                ViewModel.ActiveNode = ViewModel.MediaLibrary;

            e.Handled = true;
        }
        #endregion
    }
}