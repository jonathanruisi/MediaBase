using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.ViewModel;

namespace MediaBase.ViewModel
{
    [ViewModelType("Folder")]
    public sealed class MediaFolder : ViewModelNode
    {
        public MediaFolder() : this(string.Empty) { }

        public MediaFolder(string name)
        {
            Name = name;
        }
    }
}