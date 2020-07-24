using System.IO;
using System.Linq;

using Windows.Media.Core;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using JLR.Utility.UWP.Controls;

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

		private static void ActiveDescriptorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is MediaLibraryPage page))
				return;

			if (e.NewValue is IMediaDescriptor descriptor)
			{
				// Set the content frame's contents to a MediaPlayerPage (if not already)
				if (!(page.ContentFrame.Content is MediaPlayerPage))
				{
					var navOptions = new FrameNavigationOptions
					{
						IsNavigationStackEnabled = false,
						TransitionInfoOverride   = new SuppressNavigationTransitionInfo()
					};

					if (page.ContentFrame.NavigateToType(typeof(MediaPlayerPage), null, navOptions) &&
						page.ContentFrame.Content is MediaPlayerPage mediaPlayerPage)
					{
						mediaPlayerPage.SelectedMarkerChanged += page.MediaPlayerPage_SelectedMarkerChanged;
					}
				}
				
				// Set the media player's PlayableSource
				if (descriptor is IPlayable playable && page.ContentFrame.Content is MediaPlayerPage playerPage)
				{
					playerPage.PlayableSource = playable;
				}

				// Set ListView item sources
				page.ListViewTags.ItemsSource = descriptor.Tags;

				if (descriptor is IMarkable markable)
				{
					page.ListViewMarkers.ItemsSource = markable.Markers;
				}
			}
			else
			{
				page.ContentFrame.NavigateToType(typeof(StartPage), null, null);
				page.ListViewTags.ItemsSource    = null;
				page.ListViewMarkers.ItemsSource = null;
			}
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

		#region Event Handlers (ListView)
		private void ListViewMarkers_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!(ContentFrame.Content is MediaPlayerPage page))
				return;

			if (e.AddedItems.Count > 0)
				page.SelectedMarker = e.AddedItems[0] as Marker;
			else if (e.RemovedItems.Count > 0)
				page.SelectedMarker = null;
		}

		private void MediaPlayerPage_SelectedMarkerChanged(object sender, MediaSlider.IMediaMarker e)
		{
			ListViewMarkers.SelectedItem = e;
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