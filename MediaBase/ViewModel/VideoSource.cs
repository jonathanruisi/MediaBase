﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using JLR.Utility.WinUI.Messaging;
using JLR.Utility.WinUI.ViewModel;

using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;

namespace MediaBase.ViewModel
{
    [ViewModelType("Video")]
    public sealed class VideoSource : MultimediaSource
    {
        #region Fields
        private bool _isReady, _areCutsApplied;
        private decimal _trimmedDuration;
        private readonly List<(decimal start, decimal end)> _playableRanges;
        #endregion

        #region Properties
        public override MediaContentType ContentType => MediaContentType.Video;

        public override bool IsReady
        {
            get => _isReady;
            protected set => SetProperty(ref _isReady, value);
        }

        /// <summary>
        /// Gets a value indicating whether or not the time
        /// ranges in <see cref="Cuts"/> have been applied to the
        /// composition generated by this <see cref="VideoSource"/>.
        /// </summary>
        [ViewModelProperty(nameof(AreCutsApplied), XmlNodeType.Element, false, true)]
        public bool AreCutsApplied
        {
            get => _areCutsApplied;
            set => SetProperty(ref _areCutsApplied, value);
        }

        [ViewModelCollection(nameof(Cuts), "Cut", true, true)]
        public ObservableCollection<(decimal start, decimal end)> Cuts { get; }
        #endregion

        #region Constructors
        public VideoSource() : this(null) { }

        public VideoSource(Guid id) : this(id, null) { }

        public VideoSource(IMultimediaItem source) : this(Guid.NewGuid(), source) { }

        public VideoSource(Guid id, IMultimediaItem source) : base(id, source)
        {
            _isReady = false;
            _areCutsApplied = false;
            _trimmedDuration = 0;

            _playableRanges = new List<(decimal start, decimal end)>();

            Cuts = new ObservableCollection<(decimal start, decimal end)>();
            Cuts.CollectionChanged += Cuts_CollectionChanged;
        }
        #endregion

        #region Public Methods
        public void ApplyCuts()
        {
            // If cuts have already been applied, or no cuts exist to apply, do nothing.
            if (AreCutsApplied || _playableRanges.Count == 0)
                return;

            IsReady = false;
            AreCutsApplied = true;
        }

        public void ResetCuts()
        {
            if (Cuts.Count == 0 && !AreCutsApplied)
                return;

            if (AreCutsApplied)
            {
                IsReady = false;
                AreCutsApplied = false;
            }

            _playableRanges.Clear();
            Cuts.Clear();
        }

        public async Task<MediaComposition> BuildMediaCompositionAsync()
        {
            if (!Source.IsReady)
                throw new Exception($"Source is not ready: {Source.Name}");

            MediaComposition composition = null;

            if (Source is VideoFile file)
            {
                composition = new MediaComposition();

                // Apply cuts
                if (_playableRanges.Count > 0 && AreCutsApplied)
                {
                    foreach (var (start, end) in _playableRanges)
                    {
                        var clip = await MediaClip.CreateFromFileAsync(file.File);
                        clip.TrimTimeFromStart = TimeSpan.FromSeconds(decimal.ToDouble(start));
                        clip.TrimTimeFromEnd = TimeSpan.FromSeconds(decimal.ToDouble(file.Duration - end));
                        composition.Clips.Add(clip);
                    }
                }
                else
                {
                    composition.Clips.Add(await MediaClip.CreateFromFileAsync(file.File));
                }
            }
            else if (Source is VideoSource video)
            {
                composition = await video.BuildMediaCompositionAsync();

                // Apply cuts
                if (_playableRanges.Count > 0 && AreCutsApplied)
                {
                    var clipAdjustments = new Dictionary<MediaClip, (TimeSpan start, TimeSpan end)>();

                    // Make note of all clips that need to be removed, adjusted, or split
                    foreach (var (start, end) in Cuts.OrderBy(x => x.start))
                    {
                        var cutStart = TimeSpan.FromSeconds(decimal.ToDouble(start));
                        var cutEnd = TimeSpan.FromSeconds(decimal.ToDouble(end));

                        // Make note of all clips affected by the current cut
                        foreach (var clip in composition.Clips.Where(c => cutStart < c.EndTimeInComposition &&
                                                                          cutEnd > c.StartTimeInComposition))
                        {
                            // Cut overlaps entire clip, remove it
                            if (cutStart <= clip.StartTimeInComposition && cutEnd >= clip.EndTimeInComposition)
                                clipAdjustments.Add(clip, (TimeSpan.Zero, TimeSpan.Zero));
                            // Cut overlaps start of clip
                            else if (cutStart <= clip.StartTimeInComposition && cutEnd < clip.EndTimeInComposition)
                                clipAdjustments.Add(clip, (cutEnd - clip.StartTimeInComposition, TimeSpan.Zero));
                            // Cut overlaps end of clip
                            else if (cutStart > clip.StartTimeInComposition && cutEnd >= clip.EndTimeInComposition)
                                clipAdjustments.Add(clip, (TimeSpan.Zero, clip.EndTimeInComposition - cutStart));
                            // Cut exists in the middle of the clip (clip needs to be split)
                            else if (cutStart > clip.StartTimeInComposition && cutEnd < clip.EndTimeInComposition)
                                clipAdjustments.Add(clip, (clip.EndTimeInComposition - cutStart, cutEnd - clip.StartTimeInComposition));
                        }
                    }

                    // Remove clips
                    foreach (var adjustment in clipAdjustments.Where(a => a.Value.start == TimeSpan.Zero &&
                                                                    a.Value.end == TimeSpan.Zero))
                    {
                        composition.Clips.Remove(adjustment.Key);
                    }

                    // Trim clips
                    foreach (var adjustment in clipAdjustments.Where(a => (a.Value.start == TimeSpan.Zero) ^
                                                                          (a.Value.end == TimeSpan.Zero)))
                    {
                        if (adjustment.Value.start != TimeSpan.Zero)
                            adjustment.Key.TrimTimeFromStart = adjustment.Value.start;
                        else
                            adjustment.Key.TrimTimeFromEnd = adjustment.Value.end;
                    }

                    // Split clips
                    foreach (var adjustment in clipAdjustments.Where(a => a.Value.start != TimeSpan.Zero &&
                                                                          a.Value.end != TimeSpan.Zero))
                    {
                        var index = composition.Clips.IndexOf(adjustment.Key);
                        var duplicateClip = composition.Clips[index].Clone();
                        adjustment.Key.TrimTimeFromEnd = adjustment.Value.start - adjustment.Key.TrimTimeFromEnd;
                        duplicateClip.TrimTimeFromStart = adjustment.Value.end - adjustment.Key.TrimTimeFromStart;
                        composition.Clips.Insert(index + 1, duplicateClip);
                    }
                }
            }

            return composition;
        }

        public async Task<MediaSource> BuildMediaSourceAsync(MediaEncodingProfile profile = null)
        {
            var composition = await BuildMediaCompositionAsync();

            if (composition == null)
                throw new Exception("Unable to build MediaSource: Composition failure");

            var encodingProfile = profile ?? composition.CreateDefaultEncodingProfile();
            var mediaStreamSource = composition.GenerateMediaStreamSource(encodingProfile);
            return MediaSource.CreateFromMediaStreamSource(mediaStreamSource);
        }
        #endregion

        #region Event Handlers
        private void Cuts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EvaluateCuts();

            var cutMessage = new CollectionChangedMessage<(decimal, decimal)>(this, nameof(Cuts), e.Action)
            {
                OldStartingIndex = e.OldStartingIndex,
                NewStartingIndex = e.NewStartingIndex
            };

            if (e.OldItems != null)
            {
                foreach ((decimal, decimal) oldCut in e.OldItems)
                    cutMessage.OldValue.Add(oldCut);
            }

            if (e.NewItems != null)
            {
                foreach ((decimal, decimal) newCut in e.NewItems)
                    cutMessage.NewValue.Add(newCut);
            }

            Messenger.Send(cutMessage, nameof(Cuts));
            NotifySerializedCollectionChanged(nameof(Cuts));
        }
        #endregion

        #region Interface Implementation (IMultimediaItem)
        public override async Task<bool> MakeReady()
        {
            if (await base.MakeReady() == false)
            {
                IsReady = false;
                return false;
            }

            if (AreCutsApplied)
            {
                EvaluateCuts();
                Duration = _trimmedDuration;
            }

            IsReady = true;
            return true;
        }
        #endregion

        #region Method Overrides (ViewModelElement)
        protected override object CustomPropertyParser(string propertyName, string content)
        {
            var baseProperty = base.CustomPropertyParser(propertyName, content);
            if (baseProperty != null)
                return baseProperty;

            if (propertyName == "Cut")
            {
                var cutStrings = content.Split(':');
                return (decimal.Parse(cutStrings[0]), decimal.Parse(cutStrings[1]));
            }

            return null;
        }

        protected override string CustomPropertyWriter(string propertyName, object value)
        {
            var baseProperty = base.CustomPropertyWriter(propertyName, value);
            if (baseProperty != null)
                return baseProperty;

            if (propertyName == "Cut")
            {
                var (start, end) = ((decimal start, decimal end))value;
                return $"{start}:{end}";
            }

            if (propertyName == nameof(AreCutsApplied))
            {
                return AreCutsApplied ? 1.ToString() : 0.ToString();
            }

            return null;
        }
        #endregion

        #region Private Methods
        private void EvaluateCuts()
        {
            decimal lastStart = 0;
            _playableRanges.Clear();

            foreach (var (start, end) in Cuts.OrderBy(x => x.start))
            {
                if (lastStart >= Duration)
                    break;

                if (start <= 0)
                {
                    lastStart = end;
                    continue;
                }

                _playableRanges.Add((lastStart, start));
                lastStart = end;
            }

            if (lastStart > 0 && lastStart < Duration)
                _playableRanges.Add((lastStart, Duration));

            if (_playableRanges.Count == 0)
                _trimmedDuration = Duration;
            else
            {
                _trimmedDuration = 0;
                foreach (var (start, end) in _playableRanges)
                {
                    _trimmedDuration += end - start;
                }
            }
        }
        #endregion
    }
}