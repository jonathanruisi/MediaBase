using System;

using Windows.Media.Editing;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace MediaBase
{
	public sealed partial class MediaLibraryPage : Page
	{
		#region Properties
		public Project ActiveProject
		{
			get => (Project) GetValue(ActiveProjectProperty);
			set => SetValue(ActiveProjectProperty, value);
		}

		private static readonly DependencyProperty ActiveProjectProperty =
			DependencyProperty.Register("ActiveProject",
										typeof(Project),
										typeof(MainPage),
										new PropertyMetadata(null, ActiveProjectChanged));

		public MediaTreeNode ActiveNode
		{
			get => (MediaTreeNode) GetValue(ActiveNodeProperty);
			set => SetValue(ActiveNodeProperty, value);
		}

		private static readonly DependencyProperty ActiveNodeProperty =
			DependencyProperty.Register("ActiveNode",
										typeof(MediaTreeNode),
										typeof(MediaLibraryPage),
										new PropertyMetadata(null, ActiveNodeChanged));

		public IMediaDescriptor ActiveDescriptor
		{
			get => (IMediaDescriptor) GetValue(ActiveDescriptorProperty);
			set => SetValue(ActiveDescriptorProperty, value);
		}

		private static readonly DependencyProperty ActiveDescriptorProperty =
			DependencyProperty.Register("ActiveDescriptor",
										typeof(IMediaDescriptor),
										typeof(MediaLibraryPage),
										new PropertyMetadata(null, ActiveDescriptorChanged));
		#endregion

		#region Constructor
		public MediaLibraryPage()
		{
			InitializeComponent();
			InitializeCommands();
		}
		#endregion

		#region Dependency Property Callbacks
		private static void ActiveProjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is MediaLibraryPage page))
				return;

			page.ActiveDescriptor = null;
			page.ActiveNode       = null;
		}

		private static void ActiveNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is MediaLibraryPage page))
				return;
		}

		private static async void ActiveDescriptorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is MediaLibraryPage page))
				return;

			/*if (e.NewValue is IMediaDescriptor descriptor)
			{
				if (descriptor is MediaTreeFile mediaFile)
				{
					// Set the content frame's contents to a CompositionPlayerPage (if not already)
					if (!(page.ContentFrame.Content is CompositionPlayerPage))
					{
						var navOptions = new FrameNavigationOptions
						{
							IsNavigationStackEnabled = false,
							TransitionInfoOverride   = new SuppressNavigationTransitionInfo()
						};

						page.ContentFrame.NavigateToType(typeof(CompositionPlayerPage), null, navOptions);
					}

					// Create clip from media file
					MediaClip clip = null;
					if (mediaFile is ImageFile)
						clip = await MediaClip.CreateFromImageFileAsync(mediaFile.StorageFile, TimeSpan.FromSeconds(5));
					else if (mediaFile is VideoFile)
						clip = await MediaClip.CreateFromFileAsync(mediaFile.StorageFile);

					// Create media composition
					var comp = new MediaComposition();
					comp.Clips.Add(clip);

					// Set the composition player page's active composition
					if (page.ContentFrame.Content is CompositionPlayerPage playerPage)
						playerPage.ActiveComposition = comp;
				}

				page.ListViewTags.ItemsSource = descriptor.Tags;
			}
			else
			{
				page.ContentFrame.NavigateToType(typeof(StartPage), null, null);
				page.ListViewTags.ItemsSource = null;
			}*/
		}
		#endregion

		#region Event Handlers (Page)
		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			TreeViewMediaLibrary.ItemsSource = ActiveProject.MediaLibrary.Children;
			ContentFrame.NavigateToType(typeof(StartPage), null, null);
		}
		#endregion

		#region Event Handlers (TreeView)
		private void TreeViewMediaLibrary_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (sender is TreeViewItem item)
				ActiveNode = item.DataContext as MediaTreeNode;
			else
				ActiveNode = ActiveProject.MediaLibrary;

			e.Handled = true;
		}

		private void TreeViewMediaLibrary_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
		{
			if (!(args.InvokedItem is MediaTreeFile file))
				return;

			ActiveDescriptor = file;
		}
		#endregion

		#region Method Overrides (Page)
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			ActiveProject = e.Parameter as Project;
		}
		#endregion
	}
}