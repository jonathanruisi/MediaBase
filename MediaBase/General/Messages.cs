using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Controls;

namespace MediaBase
{
    public class SetInfoBarMessage
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public InfoBarSeverity Severity { get; set; }
        public bool IsCloseable { get; set; }
        public SetInfoBarMessage()
        {
            Title = string.Empty;
            Message = string.Empty;
            IsCloseable = true;
            Severity = InfoBarSeverity.Informational;
        }
    }
}