using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

using Windows.Storage;
using Windows.UI.Xaml.Automation;

using JLR.Utility.UWP.ViewModel;

namespace MediaBase
{
	/// <summary>
	/// Contains and manages all elements of a <see cref="MediaBase"/> project.
	/// </summary>
	public sealed class Project : XmlViewModel
	{
		#region Fields
		private const string      MediaLibraryRootName = "Media Library Root";
		private       string      _name;
		private       bool        _hasUnsavedChanges;
		private       StorageFile _storageFile;
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets the name of this project.
		/// </summary>
		public string Name
		{
			get => _name;
			set => Set(ref _name, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not
		/// changes have been made to the <see cref="Project"/>'s content.
		/// </summary>
		public bool HasUnsavedChanges
		{
			get => _hasUnsavedChanges;
			set => Set(ref _hasUnsavedChanges, value);
		}

		/// <summary>
		/// Gets or sets a reference to the file in which
		/// this <see cref="Project"/> is stored.
		/// </summary>
		public StorageFile StorageFile
		{
			get => _storageFile;
			set => Set(ref _storageFile, value);
		}

		/// <summary>
		/// Gets the root of this project's media library hierarchy.
		/// </summary>
		public MediaTreeFolder MediaLibrary { get; private set; }
		#endregion

		#region Constructors
		public Project()
		{
			_name              = string.Empty;
			_hasUnsavedChanges = false;
			_storageFile       = null;
			MediaLibrary       = new MediaTreeFolder {Name = MediaLibraryRootName};
		}
		#endregion

		#region Public Methods
		public IEnumerable<T> GetAllMediaFiles<T>() where T : MediaTreeFile
		{
			return
				from node in MediaLibrary.OfType<T>()
				select node;
		}

		/// <summary>
		/// Saves this <see cref="Project"/> to its associated
		/// <see cref="Windows.Storage.StorageFile"/> as XML.
		/// </summary>
		public async void Save()
		{
			if (StorageFile == null)
			{
				throw new NullReferenceException(
					"This project instance is not associated with a StorageFile");
			}

			if (!StorageFile.IsAvailable)
			{
				throw new ElementNotAvailableException(
					"The StorageFile associated with this project instance is not available");
			}

			await FileIO.WriteTextAsync(StorageFile, string.Empty);

			XmlWriter writer = null;

			try
			{
				var settings = new XmlWriterSettings
				{
					Indent             = true,
					IndentChars        = "\t",
					OmitXmlDeclaration = false,
					ConformanceLevel   = ConformanceLevel.Document,
					CloseOutput        = true
				};

				writer = XmlWriter.Create(await StorageFile.OpenStreamForWriteAsync(), settings);
				WriteXml(writer);
				writer.Flush();

				HasUnsavedChanges = false;
			}
			finally
			{
				writer?.Close();
			}
		}
		#endregion

		#region Static Methods
		/// <summary>
		/// Instantiates a <see cref="Project"/> from a
		/// <see cref="Windows.Storage.StorageFile"/>
		/// containing its XML representation.
		/// </summary>
		/// <param name="file">
		/// The file from which this <see cref="Project"/> will be read.
		/// </param>
		/// <returns>
		/// A <see cref="Project"/> object.
		/// </returns>
		public static async Task<Project> Load(StorageFile file)
		{
			if (!file.IsAvailable)
				return null;

			XmlReader reader  = null;
			var       project = new Project();

			try
			{
				var settings = new XmlReaderSettings
				{
					IgnoreComments               = true,
					IgnoreProcessingInstructions = true,
					IgnoreWhitespace             = true
				};

				reader = XmlReader.Create(await file.OpenStreamForReadAsync(), settings);
				reader.MoveToContent();
				project.ReadXml(reader);
				project.StorageFile = file;
			}
			finally
			{
				reader?.Close();
			}

			project.HasUnsavedChanges = false;
			return project;
		}

		/// <summary>
		/// This method performs initialization necessary for
		/// serialization/deserialization of a <see cref="Project"/> to/from XML.
		/// This method only needs to be called once per execution,
		/// however it must be called prior to serialization/deserialization.
		/// </summary>
		public static void RegisterAllProjectTypes()
		{
			RegisterXmlSerializationInfo<Project>("Project");
			RegisterXmlSerializationInfo<MediaTreeFolder>("Folder");
			RegisterXmlSerializationInfo<ImageFile>("Image");
			RegisterXmlSerializationInfo<VideoFile>("Video");
			RegisterXmlSerializationInfo<Marker>("Marker");
		}
		#endregion

		#region Method Overrides (XmlViewModel)
		protected override void ReadAttribute(string name, string value)
		{
			base.ReadAttribute(name, value);

			if (name == nameof(Name))
				Name = value;
		}

		protected override void WriteAttributes(XmlWriter writer)
		{
			base.WriteAttributes(writer);

			writer.WriteAttributeString(nameof(Name), Name);
		}

		protected override void WriteElements(XmlWriter writer)
		{
			base.WriteElements(writer);

			MediaLibrary.WriteXml(writer);
		}

		protected override void ProcessMember(XmlViewModel element, string param)
		{
			base.ProcessMember(element, param);

			if (element is MediaTreeFolder folder && folder.Name == MediaLibraryRootName)
				MediaLibrary = folder;
		}
		#endregion

		#region Method Overrides (System.Object)
		public override string ToString()
		{
			return Name;
		}
		#endregion
	}
}