using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JLR.Utility.WinUI.ViewModel;

namespace MediaBase.ViewModel
{
    [ViewModelType("Image")]
    public sealed class ImageSource : MultimediaSource
    {
        #region Fields
        private bool _isAnimated, _isReady;
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating whether or not this
        /// <see cref="ImageSource"/> contains animation keyframes.
        /// </summary>
        public bool IsAnimated
        {
            get => _isAnimated;
            private set => SetProperty(ref _isAnimated, value);
        }

        public override bool IsReady
        {
            get => _isReady;
            protected set => SetProperty(ref _isReady, value);
        }

        public override MediaContentType ContentType => MediaContentType.Image;
        #endregion

        #region Constructors
        public ImageSource() : this(null) { }

        public ImageSource(Guid id) : this(id, null) { }

        public ImageSource(IMultimediaItem source) : this(Guid.NewGuid(), source) { }

        public ImageSource(Guid id, IMultimediaItem source) : base(id, source)
        {
            _isAnimated = false;
            _isReady = false;
        }
        #endregion

        #region Public Methods
        public void Animate(decimal duration)
        {
            if (duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration), "Value must be greater than zero");

            Duration = duration;
            FramesPerSecond = App.RefreshRate;
            IsAnimated = true;
        }

        protected override void OnReadXmlComplete()
        {
            if (Keyframes.Any())
                Animate(Keyframes.Max(x => x.Position));
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