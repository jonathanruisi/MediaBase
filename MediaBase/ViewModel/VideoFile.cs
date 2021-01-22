using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;

using JLR.Utility.UWP.ViewModel;

namespace MediaBase
{
	public sealed class VideoFile : MediaTreeFile, IMarkable, IPlayable
	{
		#region Properties
		public ObservableCollection<Marker> Markers { get; }
		#endregion

		#region Constructors
		public VideoFile() : this(null)
		{
		}

		public VideoFile(StorageFile file) : base(file)
		{
			if (file != null && !file.ContentType.Contains("video"))
				throw new ArgumentException("File must contain video content");

			Markers = new ObservableCollection<Marker>();
		}
		#endregion

		#region Interface Implementation (IPlayable)
		public async Task<MediaSource> GetMediaSourceAsync()
		{
			// Query video frame rate
			double frameRate = 0;
			try
			{
				var propRequestList = new List<string> {"System.Video.FrameRate"};
				var propResultList  = await StorageFile.Properties.RetrievePropertiesAsync(propRequestList);
				frameRate = Math.Ceiling((uint) propResultList["System.Video.FrameRate"] / 1000.0);
			}
			catch (Exception)
			{
				Debug.WriteLine("ERROR retrieving frame rate");
				frameRate = 30;
			}

			// Create MediaSource
			var stream = await StorageFile.OpenReadAsync();
			var source = MediaSource.CreateFromStream(stream, stream.ContentType);
			source.CustomProperties.Add("FPS", frameRate);
			return source;
			
			/*var stream = new InMemoryRandomAccessStream();
			using var fileStream = await StorageFile.OpenStreamForReadAsync();
			await fileStream.CopyToAsync(stream.AsStreamForWrite(), 102400);

			var source = MediaSource.CreateFromStream(stream, StorageFile.ContentType);
			source.CustomProperties.Add("FPS", frameRate);
			return source;*/
		}
		#endregion

		#region Method Overrides (XmlViewModel)
		protected override void WriteElements(XmlWriter writer)
		{
			base.WriteElements(writer);

			foreach (var marker in Markers.OrderBy(x => x.Position))
			{
				marker.WriteXml(writer);
			}
		}

		protected override void ProcessMember(XmlViewModel element, string param)
		{
			if (element is Marker marker)
				Markers.Add(marker);
		}
		#endregion
	}
}