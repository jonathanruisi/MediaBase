using System.Xml;

using JLR.Utility.UWP.ViewModel;

namespace MediaBase
{
	public abstract class MediaTreeNode : NodeViewModel
	{
		#region Fields
		private string _name;
		#endregion

		#region Properties
		public string Name
		{
			get => _name;
			set => Set(ref _name, value);
		}
		#endregion

		#region Constructor
		protected MediaTreeNode()
		{
			_name = string.Empty;
		}
		#endregion

		#region Method Overrides (XmlViewModel)
		protected override void ReadAttribute(string name, string value)
		{
			if (name == nameof(Name))
				Name = value;
		}

		protected override void WriteAttributes(XmlWriter writer)
		{
			writer.WriteAttributeString(nameof(Name), Name);

			base.WriteAttributes(writer);
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