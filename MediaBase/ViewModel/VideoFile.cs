using System;

using Windows.Storage;

namespace MediaBase
{
	public sealed class VideoFile : MediaTreeFile
	{
		#region Constructors
		public VideoFile() : this(null)
		{
		}

		public VideoFile(StorageFile file) : base(file)
		{
			if (file != null && !file.ContentType.Contains("video"))
				throw new ArgumentException("File must contain video content");
		}
		#endregion
	}
}