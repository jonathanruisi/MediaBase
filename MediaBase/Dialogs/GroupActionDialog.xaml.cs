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
using Windows.Storage;
using Windows.Storage.Pickers;

using WinRT.Interop;

namespace MediaBase.Dialogs
{
    public sealed partial class GroupActionDialog : ContentDialog
    {
        #region Properties
        public ProjectManager ViewModel => (ProjectManager)DataContext;

        public bool ActOnGroup1
        {
            get => (bool)GetValue(ActOnGroup1Property);
            set => SetValue(ActOnGroup1Property, value);
        }

        public static readonly DependencyProperty ActOnGroup1Property =
            DependencyProperty.Register("ActOnGroup1",
                                        typeof(bool),
                                        typeof(GroupActionDialog),
                                        new PropertyMetadata(false));

        public bool ActOnGroup2
        {
            get => (bool)GetValue(ActOnGroup2Property);
            set => SetValue(ActOnGroup2Property, value);
        }

        public static readonly DependencyProperty ActOnGroup2Property =
            DependencyProperty.Register("ActOnGroup2",
                                        typeof(bool),
                                        typeof(GroupActionDialog),
                                        new PropertyMetadata(false));

        public bool ActOnGroup3
        {
            get => (bool)GetValue(ActOnGroup3Property);
            set => SetValue(ActOnGroup3Property, value);
        }

        public static readonly DependencyProperty ActOnGroup3Property =
            DependencyProperty.Register("ActOnGroup3",
                                        typeof(bool),
                                        typeof(GroupActionDialog),
                                        new PropertyMetadata(false));

        public bool ActOnGroup4
        {
            get => (bool)GetValue(ActOnGroup4Property);
            set => SetValue(ActOnGroup4Property, value);
        }

        public static readonly DependencyProperty ActOnGroup4Property =
            DependencyProperty.Register("ActOnGroup4",
                                        typeof(bool),
                                        typeof(GroupActionDialog),
                                        new PropertyMetadata(false));

        public int Group1Count
        {
            get => (int)GetValue(Group1CountProperty);
            set => SetValue(Group1CountProperty, value);
        }

        public static readonly DependencyProperty Group1CountProperty =
            DependencyProperty.Register("Group1Count",
                                        typeof(int),
                                        typeof(GroupActionDialog),
                                        new PropertyMetadata(0));

        public int Group2Count
        {
            get => (int)GetValue(Group2CountProperty);
            set => SetValue(Group2CountProperty, value);
        }

        public static readonly DependencyProperty Group2CountProperty =
            DependencyProperty.Register("Group2Count",
                                        typeof(int),
                                        typeof(GroupActionDialog),
                                        new PropertyMetadata(0));

        public int Group3Count
        {
            get => (int)GetValue(Group3CountProperty);
            set => SetValue(Group3CountProperty, value);
        }

        public static readonly DependencyProperty Group3CountProperty =
            DependencyProperty.Register("Group3Count",
                                        typeof(int),
                                        typeof(GroupActionDialog),
                                        new PropertyMetadata(0));

        public int Group4Count
        {
            get => (int)GetValue(Group4CountProperty);
            set => SetValue(Group4CountProperty, value);
        }

        public static readonly DependencyProperty Group4CountProperty =
            DependencyProperty.Register("Group4Count",
                                        typeof(int),
                                        typeof(GroupActionDialog),
                                        new PropertyMetadata(0));

        public BatchAction Action
        {
            get => (BatchAction)GetValue(ActionProperty);
            set => SetValue(ActionProperty, value);
        }

        public static readonly DependencyProperty ActionProperty =
            DependencyProperty.Register("Action",
                                        typeof(BatchAction),
                                        typeof(GroupActionDialog),
                                        new PropertyMetadata(BatchAction.None));

        public StorageFolder TargetFolder
        {
            get => (StorageFolder)GetValue(TargetFolderProperty);
            set => SetValue(TargetFolderProperty, value);
        }

        public static readonly DependencyProperty TargetFolderProperty =
            DependencyProperty.Register("TargetFolder",
                                        typeof(StorageFolder),
                                        typeof(GroupActionDialog),
                                        new PropertyMetadata(null));
        #endregion

        #region Constructor
        public GroupActionDialog()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ProjectManager>();
        }
        #endregion

        #region Event Handlers (ContentDialog)
        private void ContentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Group1Count = ViewModel.ActiveSystemBrowserNode.DepthFirstEnumerable().Count(x => x.CheckGroupFlag(1));
            Group2Count = ViewModel.ActiveSystemBrowserNode.DepthFirstEnumerable().Count(x => x.CheckGroupFlag(2));
            Group3Count = ViewModel.ActiveSystemBrowserNode.DepthFirstEnumerable().Count(x => x.CheckGroupFlag(3));
            Group4Count = ViewModel.ActiveSystemBrowserNode.DepthFirstEnumerable().Count(x => x.CheckGroupFlag(4));

            if (Group1Count == 0)
                Group1ToggleButton.IsEnabled = false;

            if (Group2Count == 0)
                Group2ToggleButton.IsEnabled = false;

            if (Group3Count == 0)
                Group3ToggleButton.IsEnabled = false;

            if (Group4Count == 0)
                Group4ToggleButton.IsEnabled = false;
        }

        private void GroupActionRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupActionRadioButtons.SelectedItem == null)
                return;

            Action = (BatchAction)Enum.Parse(typeof(BatchAction), (string)GroupActionRadioButtons.Items[GroupActionRadioButtons.SelectedIndex]);
        }

        private async void FolderBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                CommitButtonText = "Set Target"
            };

            InitializeWithWindow.Initialize(picker, App.WindowHandle);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
                TargetFolder = folder;
        }
        #endregion
    }
}