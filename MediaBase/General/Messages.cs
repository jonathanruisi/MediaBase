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
    public class GeneralMessage { }

    public class GeneralMessage<T>
    {
        public List<T> Content { get; }

        public GeneralMessage(params T[] content)
        {
            Content = new List<T>();
            if (content != null && content.Length > 0)
                Content.AddRange(content);
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