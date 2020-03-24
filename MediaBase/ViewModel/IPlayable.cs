using System.Threading.Tasks;

using Windows.Media.Core;

namespace MediaBase
{
	/// <summary>
	/// Represents a media object capable of generating a
	/// <see cref="MediaSource"/> for playback.
	/// </summary>
	public interface IPlayable
	{
		/// <summary>
		/// Asynchronous method which generates a <see cref="MediaSource"/>
		/// for playback of this media object.
		/// </summary>
		/// <returns>A <see cref="MediaSource"/> object</returns>
		Task<MediaSource> GetMediaSourceAsync();
	}
}