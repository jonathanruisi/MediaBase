using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

using CommunityToolkit.Mvvm.Messaging;

using JLR.Utility.WinUI.Messaging;
using JLR.Utility.WinUI.ViewModel;

namespace MediaBase.ViewModel
{
    [ViewModelType(nameof(Playlist))]
    [ViewModelCollectionOverride(nameof(Children), nameof(Children), null, false, false, true)]
    public sealed class Playlist : ViewModelNode, IMultimediaItem, IMediaMetadata
    {
        #region Fields
        private Guid _id;
        private MediaContentType _contentType;
        private int _rating, _groupFlags;
        private bool _isReady;
        #endregion

        #region Properties
        [ViewModelProperty(nameof(Id), XmlNodeType.Element, true)]
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [ViewModelProperty(nameof(Rating), XmlNodeType.Element)]
        public int Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        [ViewModelProperty(nameof(GroupFlags), XmlNodeType.Element)]
        public int GroupFlags
        {
            get => _groupFlags;
            set => SetProperty(ref _groupFlags, value);
        }

        public bool IsReady
        {
            get => Children.Count == 0 || _isReady;
            set => SetProperty(ref _isReady, value);
        }

        public MediaContentType ContentType
        {
            get => _contentType;
            private set => SetProperty(ref _contentType, value);
        }

        [ViewModelCollection(nameof(Tags), "Tag")]
        public ObservableCollection<string> Tags { get; }
        #endregion

        #region Constructors
        public Playlist() : this(null, Guid.NewGuid()) { }

        public Playlist(string name) : this(name, Guid.NewGuid()) { }

        public Playlist(Guid id) : this(null, id) { }

        public Playlist(string name, Guid id)
        {
            _id = id;
            _contentType = MediaContentType.None;
            _rating = 0;
            _groupFlags = 0;
            Name = name;

            Tags = new ObservableCollection<string>();
            Tags.CollectionChanged += Tags_CollectionChanged;
        }
        #endregion

        #region Event Handlers
        private void Tags_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var tagMessage = new CollectionChangedMessage<string>(this, nameof(Tags), e.Action)
            {
                OldStartingIndex = e.OldStartingIndex,
                NewStartingIndex = e.NewStartingIndex
            };

            if (e.OldItems != null)
            {
                foreach (string oldTag in e.OldItems)
                    tagMessage.OldValue.Add(oldTag);
            }

            if (e.NewItems != null)
            {
                foreach (string newTag in e.NewItems)
                    tagMessage.NewValue.Add(newTag);
            }

            Messenger.Send(tagMessage, nameof(Tags));
            NotifySerializedCollectionChanged(nameof(Tags));
        }
        #endregion

        #region Interface Implementation (IMultimediaItem)
        public async Task<bool> MakeReady()
        {
            var isReady = true;

            for (var i = 0; i < Children.Count; i++)
            {
                if (Children[i] is not IMultimediaItem)
                    continue;

                var response = Messenger.Send(new MediaLookupRequestMessage(((IMultimediaItem)Children[i]).Id));
                if (response != null && response.Response != null)
                    Children[i] = (ViewModelElement)response.Response;

                if (await ((IMultimediaItem)Children[i]).MakeReady())
                    ContentType |= ((IMultimediaItem)Children[i]).ContentType;
                else
                    isReady = false;
            }

            IsReady = isReady;
            return IsReady;
        }
        #endregion

        #region Interface Implementation (IGroupable)
        public bool CheckGroupFlag(int group)
        {
            group--;
            if (group is < 0 or > 7)
                throw new ArgumentOutOfRangeException(nameof(group));

            return (GroupFlags & (1 << group)) != 0;
        }

        public void SetGroupFlag(int group)
        {
            group--;
            if (group is < 0 or > 7)
                throw new ArgumentOutOfRangeException(nameof(group));

            GroupFlags |= 1 << group;
        }

        public void ClearGroupFlag(int group)
        {
            group--;
            if (group is < 0 or > 7)
                throw new ArgumentOutOfRangeException(nameof(group));

            GroupFlags &= ~(1 << group);
        }

        public void ToggleGroupFlag(int group)
        {
            group--;
            if (group is < 0 or > 7)
                throw new ArgumentOutOfRangeException(nameof(group));

            GroupFlags ^= (1 << group);
        }
        #endregion

        #region Method Overrides (ViewModelElement)
        protected override object CustomPropertyParser(string propertyName, string content, params string[] args)
        {
            if (propertyName != nameof(Id))
                return null;

            return Guid.Parse(content);
        }

        protected override object HijackDeserialization(string propertyName,
                                                        ref XmlReader reader,
                                                        params string[] args)
        {
            if (propertyName == nameof(Children))
            {
                var typeString = reader.Name;

                reader.MoveToFirstAttribute();
                var id = Guid.Parse(reader.ReadContentAsString());

                var result = (IMultimediaItem)InstantiateObjectFromXmlTagName(typeString);
                reader.Read();
                result.Id = id;
                return result;
            }

            return null;
        }

        protected override void HijackSerialization(string propertyName,
                                                    object value,
                                                    ref XmlWriter writer,
                                                    params string[] args)
        {
            if (propertyName == nameof(Children) && args.Length > 0)
            {
                if (value is not IMultimediaItem media)
                    throw new Exception(
                        "Argument passed to custom serializer could not be cast to IMultimediaItem");

                var xmlTag = GetXmlTagForType(value.GetType());
                if (string.IsNullOrEmpty(xmlTag))
                    throw new Exception(
                        "Argument passed to custom serializer is not recognized as a ViewModelElement serializable type");

                writer.WriteStartElement(xmlTag);
                writer.WriteAttributeString(nameof(media.Id), media.Id.ToString());
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}