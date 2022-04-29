using System;
using System.Collections;
using System.Collections.Generic;

namespace MediaBase.ViewModel
{
    public sealed class ViewModelTagCollectionChangedMessage
    {
        public List<int> AddedTags { get; }
        public List<int> RemovedTags { get; }

        public ViewModelTagCollectionChangedMessage()
        {
            AddedTags = new List<int>();
            RemovedTags = new List<int>();
        }

        public ViewModelTagCollectionChangedMessage(IList<int> addedTags, IList<int> removedTags) : this()
        {
            AddedTags.AddRange(addedTags);
            RemovedTags.AddRange(removedTags);
        }
    }
}