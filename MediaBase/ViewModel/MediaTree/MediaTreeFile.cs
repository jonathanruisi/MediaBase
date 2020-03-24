using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

using Windows.Storage;

namespace MediaBase
{
	public abstract class MediaTreeFile : MediaTreeNode, IMediaDescriptor
	{
		#region Fields
		private string      _path;
		private int         _rating;
		private StorageFile _storageFile;
		#endregion

		#region Properties
		/// <summary>
		/// <inheritdoc cref="Windows.Storage.StorageFile.Path"/>
		/// </summary>
		public string Path
		{
			get => _path;
			protected set => Set(ref _path, value);
		}

		/// <summary>
		/// Gets the <see cref="Windows.Storage.StorageFile"/>
		/// represented by this <see cref="MediaTreeFile"/>
		/// </summary>
		public StorageFile StorageFile
		{
			get => _storageFile;
			private set => Set(ref _storageFile, value);
		}

		public int Rating
		{
			get => _rating;
			set =>
				Set(value, () => _rating, newValue =>
				{
					if (newValue < 0 || newValue > 10)
					{
						throw new ArgumentOutOfRangeException(
							nameof(value),
							"Value must be between 1 and 10 (or zero for unrated)");
					}

					_rating = newValue;
				});
		}

		public ObservableCollection<string> Tags { get; }
		#endregion

		#region Constructor
		protected MediaTreeFile(StorageFile file)
		{
			_storageFile = file;
			Name         = file?.DisplayName;
			_path        = file?.Path;
			_rating      = 0;
			Tags         = new ObservableCollection<string>();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Sets an internal reference to the <see cref="StorageFile"/>
		/// located at <see cref="Path"/>.
		/// </summary>
		/// <returns><b>true</b> if successful, <b>false</b> otherwise</returns>
		public async Task<bool> GetFileFromPathAsync()
		{
			if (string.IsNullOrEmpty(Path))
				return false;

			try
			{
				StorageFile = await StorageFile.GetFileFromPathAsync(Path);
			}
			catch (FileNotFoundException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}

			return true;
		}
		#endregion

		#region Method Overrides (XmlViewModel)
		protected override void ReadAttribute(string name, string value)
		{
			base.ReadAttribute(name, value);

			if (name == nameof(Rating) && int.TryParse(value, NumberStyles.Integer, null, out var rating))
			{
				Rating = rating;
			}
		}

		protected override void ReadElement(string name, XmlReader reader)
		{
			base.ReadElement(name, reader);

			switch (name)
			{
				case nameof(Path):
					Path = reader.ReadElementContentAsString();
					break;
				case "Tag":
					Tags.Add(reader.ReadElementContentAsString());
					break;
			}
		}

		protected override void WriteAttributes(XmlWriter writer)
		{
			base.WriteAttributes(writer);

			writer.WriteAttributeString(nameof(Rating), Rating.ToString());
		}

		protected override void WriteElements(XmlWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(nameof(Path), Path);
			foreach (var tag in Tags)
			{
				writer.WriteElementString("Tag", tag);
			}
		}
		#endregion
	}
}