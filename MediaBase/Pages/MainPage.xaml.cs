using System.ComponentModel;
using System.Net.Mime;

using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MediaBase
{
	public sealed partial class MainPage : Page
	{
		#region Properties
		public string AppTitleText
		{
			get => ApplicationView.GetForCurrentView().Title;
			set => ApplicationView.GetForCurrentView().Title = value;
		}

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
		#endregion

		#region Constructor
		public MainPage()
		{
			InitializeComponent();
			InitializeCommands();
		}
		#endregion

		#region Dependency Property Callbacks
		private static void ActiveProjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is MainPage page))
				return;

			if (e.OldValue is Project oldProject)
			{
				oldProject.PropertyChanged -= page.ActiveProject_PropertyChanged;
			}

			switch (e.NewValue)
			{
				case Project newProject:
					newProject.PropertyChanged += page.ActiveProject_PropertyChanged;
					page.AppTitleText          =  newProject.Name;
					break;
				case null:
					page.AppTitleText                    = string.Empty;
					page.MainNavigationView.SelectedItem = null;
					page.MainFrame.NavigateToType(typeof(StartPage), null, null);
					break;
			}
		}
		#endregion

		#region Event Handlers (Page)
		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			AppTitleText = string.Empty;
			MainFrame.NavigateToType(typeof(StartPage), null, null);
		}

		private void NavigationView_SelectionChanged(NavigationView sender,
													 NavigationViewSelectionChangedEventArgs args)
		{
			var pageTag = args.SelectedItemContainer?.Tag?.ToString();
			var navOptions = new FrameNavigationOptions
			{
				IsNavigationStackEnabled = false,
				TransitionInfoOverride   = args.RecommendedNavigationTransitionInfo
			};

			switch (pageTag)
			{
				case "media_library":
					MainFrame.NavigateToType(typeof(MediaLibraryPage), ActiveProject, navOptions);
					break;

				case "preview":
					MainFrame.NavigateToType(typeof(PreviewPage), ActiveProject, navOptions);
					break;

				default:
					MainFrame.NavigateToType(typeof(StartPage), null, null);
					break;
			}
		}
		#endregion

		#region Event Handlers (ActiveProject)
		private void ActiveProject_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ActiveProject.HasUnsavedChanges))
			{
				AppTitleText = $"{ActiveProject.Name}{(ActiveProject.HasUnsavedChanges ? "*" : string.Empty)}";
			}
		}
		#endregion
	}
}