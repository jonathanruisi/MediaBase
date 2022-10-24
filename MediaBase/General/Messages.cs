using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Messaging.Messages;

using MediaBase.ViewModel;

using Microsoft.UI.Xaml.Controls;

namespace MediaBase
{
    public class GeneralActionMessage { }

    public class GeneralInfoMessage<T>
    {
        public T Info { get; }

        public GeneralInfoMessage(T info)
        {
            Info = info;
        }
    }

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

    public class MediaLookupRequestMessage : RequestMessage<IMultimediaItem>
    {
        public Guid Id { get; set; }

        public MediaLookupRequestMessage(Guid id)
        {
            Id = id;
        }
    }
}