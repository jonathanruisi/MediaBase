using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using CommunityToolkit.Common;

using JLR.Utility.WinUI.ViewModel;

namespace MediaBase.ViewModel
{
    [ViewModelObject("Folder", XmlNodeType.Element)]
    public sealed class MediaFolder : ViewModelNode
    {
        #region Fields
        private List<List<ViewModelNode>> _sequenceList;
        #endregion

        #region Constructors
        public MediaFolder() : this(string.Empty) { }

        public MediaFolder(string name)
        {
            Name = name;
            _sequenceList = new List<List<ViewModelNode>>();
        }
        #endregion

        #region Public Methods
        public bool IsSequenceStart(ViewModelNode node)
        {
            foreach(var sequence in _sequenceList)
            {
                if (sequence.First().Equals(node))
                    return true;
            }

            return false;
        }

        public bool IsSequenceEnd(ViewModelNode node)
        {
            foreach (var sequence in _sequenceList)
            {
                if (sequence.Last().Equals(node))
                    return true;
            }

            return false;
        }

        public bool IsWithinSequence(ViewModelNode node)
        {
            foreach (var sequence in _sequenceList)
            {
                if (sequence.Contains(node) && !sequence.First().Equals(node) && !sequence.Last().Equals(node))
                    return true;
            }

            return false;
        }
        #endregion

        #region Method Overrides (ViewModelNode)
        protected override void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.Children_CollectionChanged(sender, e);
            UpdateSequentialNamesList();
        }
        #endregion

        #region Private Methods
        private void UpdateSequentialNamesList()
        {
            _sequenceList.Clear();
            var digitsAtEndOfName = 0;
            var lastNumber = 0;
            var sequenceInterval = 0;

            foreach (var child in Children)
            {
                var nameWithoutExtension = child.Name;
                if (nameWithoutExtension.Contains('.'))
                    nameWithoutExtension = nameWithoutExtension.Split('.')[0];

                if (digitsAtEndOfName == 0)
                {
                    var i = nameWithoutExtension.Length - 1;
                    while (nameWithoutExtension[i..].IsNumeric())
                        i--;
                    digitsAtEndOfName = nameWithoutExtension.Length - 1 - i;
                }

                if (digitsAtEndOfName > 0 && int.TryParse(nameWithoutExtension.AsSpan(nameWithoutExtension.Length - digitsAtEndOfName + 1), out int number))
                {
                    if (_sequenceList.Count == 0)
                        _sequenceList.Add(new List<ViewModelNode>());

                    if (_sequenceList[^1].Count == 0 || number - sequenceInterval == lastNumber)
                    {
                        sequenceInterval = number - lastNumber;
                        _sequenceList[^1].Add(child);
                        lastNumber = number;
                    }
                    else
                    {
                        if (_sequenceList[^1].Count == 1)
                            _sequenceList.RemoveAt(_sequenceList.Count - 1);

                        digitsAtEndOfName = 0;
                        lastNumber = 0;
                        sequenceInterval = 0;
                    }
                }
            }
        }
        #endregion
    }
}