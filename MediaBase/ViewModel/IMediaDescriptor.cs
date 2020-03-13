using System.Collections.ObjectModel;

namespace MediaBase
{
	/// <summary>
	/// Represents a media object by its name, rating, and a set of tags.
	/// </summary>
	public interface IMediaDescriptor
	{
		/// <summary>
		/// Gets or sets the name of this item
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Gets or sets a rating for this item.
		/// Valid range is between 1 (worst) and 10 (best).
		/// A value of zero indicates that this item is not rated.
		/// </summary>
		int Rating { get; set; }

		/// <summary>
		/// Gets a collection of tags associated with this item
		/// </summary>
		ObservableCollection<string> Tags { get; }
	}
}