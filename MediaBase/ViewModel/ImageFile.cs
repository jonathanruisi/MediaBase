using System;

using Windows.Storage;

namespace MediaBase
{
	public sealed class ImageFile : MediaTreeFile
	{
		#region Constructors
		public ImageFile() : this(null)
		{
		}

		public ImageFile(StorageFile file) : base(file)
		{
			if (file != null && !file.ContentType.Contains("image"))
				throw new ArgumentException("File must contain image content");
		}
		#endregion
	}
}