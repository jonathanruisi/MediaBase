using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

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

namespace MediaBase.Dialogs
{
    public sealed partial class BatchActionDialog : ContentDialog
    {
        #region Properties
        public Project ViewModel => (Project)DataContext;

        public bool ActOnCategory1
        {
            get => (bool)GetValue(ActOnCategory1Property);
            set => SetValue(ActOnCategory1Property, value);
        }

        public static readonly DependencyProperty ActOnCategory1Property =
            DependencyProperty.Register("ActOnCategory1",
                                        typeof(bool),
                                        typeof(BatchActionDialog),
                                        new PropertyMetadata(false));

        public bool ActOnCategory2
        {
            get => (bool)GetValue(ActOnCategory2Property);
            set => SetValue(ActOnCategory2Property, value);
        }

        public static readonly DependencyProperty ActOnCategory2Property =
            DependencyProperty.Register("ActOnCategory2",
                                        typeof(bool),
                                        typeof(BatchActionDialog),
                                        new PropertyMetadata(false));

        public bool ActOnCategory3
        {
            get => (bool)GetValue(ActOnCategory3Property);
            set => SetValue(ActOnCategory3Property, value);
        }

        public static readonly DependencyProperty ActOnCategory3Property =
            DependencyProperty.Register("ActOnCategory3",
                                        typeof(bool),
                                        typeof(BatchActionDialog),
                                        new PropertyMetadata(false));

        public bool ActOnCategory4
        {
            get => (bool)GetValue(ActOnCategory4Property);
            set => SetValue(ActOnCategory4Property, value);
        }

        public static readonly DependencyProperty ActOnCategory4Property =
            DependencyProperty.Register("ActOnCategory4",
                                        typeof(bool),
                                        typeof(BatchActionDialog),
                                        new PropertyMetadata(false));

        public int Category1Count
        {
            get => (int)GetValue(Category1CountProperty);
            set => SetValue(Category1CountProperty, value);
        }

        public static readonly DependencyProperty Category1CountProperty =
            DependencyProperty.Register("Category1Count",
                                        typeof(int),
                                        typeof(BatchActionDialog),
                                        new PropertyMetadata(0));

        public int Category2Count
        {
            get => (int)GetValue(Category2CountProperty);
            set => SetValue(Category2CountProperty, value);
        }

        public static readonly DependencyProperty Category2CountProperty =
            DependencyProperty.Register("Category2Count",
                                        typeof(int),
                                        typeof(BatchActionDialog),
                                        new PropertyMetadata(0));

        public int Category3Count
        {
            get => (int)GetValue(Category3CountProperty);
            set => SetValue(Category3CountProperty, value);
        }

        public static readonly DependencyProperty Category3CountProperty =
            DependencyProperty.Register("Category3Count",
                                        typeof(int),
                                        typeof(BatchActionDialog),
                                        new PropertyMetadata(0));

        public int Category4Count
        {
            get => (int)GetValue(Category4CountProperty);
            set => SetValue(Category4CountProperty, value);
        }

        public static readonly DependencyProperty Category4CountProperty =
            DependencyProperty.Register("Category4Count",
                                        typeof(int),
                                        typeof(BatchActionDialog),
                                        new PropertyMetadata(0));

        public BatchAction Action
        {
            get => (BatchAction)GetValue(ActionProperty);
            set => SetValue(ActionProperty, value);
        }

        public static readonly DependencyProperty ActionProperty =
            DependencyProperty.Register("Action",
                                        typeof(BatchAction),
                                        typeof(BatchActionDialog),
                                        new PropertyMetadata(BatchAction.Delete));
        #endregion

        #region Constructor
        public BatchActionDialog()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<Project>();
        }
        #endregion

        #region Event Handlers (ContentDialog)
        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Category1Count = ViewModel.MediaLibrary.DepthFirstEnumerable().OfType<MBMediaSource>().Count(x => x.IsCategory1);
            Category2Count = ViewModel.MediaLibrary.DepthFirstEnumerable().OfType<MBMediaSource>().Count(x => x.IsCategory2);
            Category3Count = ViewModel.MediaLibrary.DepthFirstEnumerable().OfType<MBMediaSource>().Count(x => x.IsCategory3);
            Category4Count = ViewModel.MediaLibrary.DepthFirstEnumerable().OfType<MBMediaSource>().Count(x => x.IsCategory4);

            if (Category1Count == 0)
                Category1ToggleButton.IsEnabled = false;

            if (Category2Count == 0)
                Category2ToggleButton.IsEnabled = false;

            if (Category3Count == 0)
                Category3ToggleButton.IsEnabled = false;

            if (Category4Count == 0)
                Category4ToggleButton.IsEnabled = false;
        }

        private void BatchActionRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                Action = (BatchAction)Enum.Parse(typeof(BatchAction), (string)e.AddedItems[0]);
            }
        }
        #endregion
    }
}