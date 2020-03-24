using System.Collections.ObjectModel;

namespace MediaBase
{
	/// <summary>
	/// Represents an object which contains a collection of <see cref="Marker"/>s.
	/// </summary>
	public interface IMarkable
	{
		/// <summary>
		/// Gets a collection of <see cref="Marker"/>s associated with this object
		/// </summary>
		ObservableCollection<Marker> Markers { get; }
	}
}