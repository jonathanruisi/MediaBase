using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase.ViewModel
{
    public abstract class MediaClip : MediaSource
    {
        #region Fields
        private Guid _sourceId;
        private MediaSource _source;
        #endregion

        #region Properties
        public Guid SourceId
        {
            get => _sourceId;
            set => SetProperty(ref _sourceId, value);
        }
        #endregion
    }
}