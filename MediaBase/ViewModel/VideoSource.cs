using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JLR.Utility.WinUI.ViewModel;

namespace MediaBase.ViewModel
{
    [ViewModelType("Video")]
    public sealed class VideoSource : MultimediaSource
    {
        #region Fields
        private bool _isReady;
        #endregion

        #region Properties
        public override bool IsReady
        {
            get => _isReady;
            protected set => SetProperty(ref _isReady, value);
        }

        public override MediaContentType ContentType => MediaContentType.Video;
        #endregion

        #region Constructors
        public VideoSource() : this(null) { }

        public VideoSource(Guid id) : this(id, null) { }

        public VideoSource(IMultimediaItem source) : this(Guid.NewGuid(), source) { }

        public VideoSource(Guid id, IMultimediaItem source) : base(id, source)
        {
            _isReady = false;
        }
        #endregion

        #region Interface Implementation (IMultimediaItem)
        public override async Task<bool> MakeReady()
        {
            IsReady = await base.MakeReady();
            return IsReady;
        }
        #endregion
    }
}