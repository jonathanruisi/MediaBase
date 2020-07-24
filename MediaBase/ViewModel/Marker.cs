using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml;

using JLR.Utility.NET;
using JLR.Utility.UWP.Controls;
using JLR.Utility.UWP.ViewModel;

namespace MediaBase
{
	/// <summary>
	/// Represents a specific time and duration (in seconds) within a media file.
	/// </summary>
	public sealed class Marker : XmlViewModel, IMediaDescriptor, MediaSlider.IMediaMarker
	{
		#region Fields
		private string  _name;
		private int     _rating;
		private decimal _position, _duration;
		#endregion

		#region Properties
		public string Name
		{
			get => _name;
			set => Set(ref _name, value);
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

		/// <summary>
		/// Gets or sets the position of the marker (in seconds)
		/// </summary>
		public decimal Position
		{
			get => _position;
			set => Set(ref _position, value);
		}

		/// <summary>
		/// Gets or sets the duration of the marker (in seconds)
		/// </summary>
		public decimal Duration
		{
			get => _duration;
			set => Set(ref _duration, value);
		}

		public ObservableCollection<string> Tags { get; }
		#endregion

		#region Constructor
		public Marker()
		{
			_name     = string.Empty;
			_rating   = 0;
			_position = 0;
			_duration = 0;
			Tags      = new ObservableCollection<string>();
		}
		#endregion

		#region Method Overrides (XmlViewModel)
		protected override void ReadAttribute(string name, string value)
		{
			base.ReadAttribute(name, value);

			switch (name)
			{
				case nameof(Name):
					Name = value;
					break;
				case nameof(Rating):
					if (int.TryParse(value, NumberStyles.Integer, null, out var rating))
						Rating = rating;
					break;
			}
		}

		protected override void ReadElement(string name, XmlReader reader)
		{
			base.ReadElement(name, reader);

			switch (name)
			{
				case nameof(Position):
					Position = ParseHexString(reader.ReadElementContentAsString());
					break;
				case nameof(Duration):
					Duration = ParseHexString(reader.ReadElementContentAsString());
					break;
				case "Tag":
					Tags.Add(reader.ReadElementContentAsString());
					break;
			}

			decimal ParseHexString(string valueString)
			{
				var      bits  = new int[4];
				string[] words = valueString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				for (var i = 0; i < 4; i++)
				{
					bits[3 - i] = int.Parse(words[i], NumberStyles.HexNumber);
				}

				return new decimal(bits);
			}
		}

		protected override void WriteAttributes(XmlWriter writer)
		{
			base.WriteAttributes(writer);

			writer.WriteAttributeString(nameof(Name), Name);
			writer.WriteAttributeString(nameof(Rating), Rating.ToString());
		}

		protected override void WriteElements(XmlWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(nameof(Position), Position.ToHexString());
			writer.WriteElementString(nameof(Duration), Duration.ToHexString());
			foreach (var tag in Tags)
			{
				writer.WriteElementString("Tag", tag);
			}
		}
		#endregion

		#region Method Overrides (System.Object)
		public override string ToString()
		{
			return
				$"{(Duration == 0 ? "Marker" : "Clip")} @{Position:#.000}{(Duration > 0 ? $" (+{Duration:#.000})" : string.Empty)} ({Name})";
		}
		#endregion
	}
}