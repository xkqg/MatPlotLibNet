// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>What a draw of an axes reads beyond its own change mark: the objects that hold data in collections (every series
/// on every axis, every patch and overlay, every inset with its own), and the marks of the axes it shares a scale with.</summary>
internal static class AxesDrawnDataExtensions
{
    extension(Axes axes)
    {
        /// <summary>Every object a draw of this axes reads data from, in a fixed order: the series of every slot and of the
        /// top axis, the overlays, the patches, then each inset's own, depth first; then the same of every axes this one shares
        /// a scale with. A value changed in place inside any of them changes what the axes draws.</summary>
        internal IEnumerable<object> DataHolders()
        {
            foreach (var holder in axes.OwnDataHolders()) yield return holder;
            if (!axes.SharesAScale) yield break;
            foreach (var shared in axes.SharedScaleAxes())
                foreach (var holder in shared.OwnDataHolders()) yield return holder;
        }

        /// <summary>The newest change mark of this axes and of every axes it shares a scale with: a shared range is folded from
        /// the data of all of them.</summary>
        internal long LatestChangeWithShared()
        {
            long latest = axes.LatestChange;
            if (!axes.SharesAScale) return latest;
            foreach (var shared in axes.SharedScaleAxes()) latest = ChangeClock.Latest(latest, shared.LatestChange);
            return latest;
        }

        /// <summary>Whether this axes takes a scale from another (the walk over the axes it shares with is then needed).</summary>
        private bool SharesAScale => axes.ShareXWith is not null || axes.ShareYWith is not null;

        /// <summary>The axes this one shares its X or its Y scale with, each link followed to its end the way the range is
        /// folded (<c>LinkedContributions</c>: the X links through <see cref="Axes.ShareXWith"/>, the Y links through
        /// <see cref="Axes.ShareYWith"/>), each axes once.</summary>
        internal IEnumerable<Axes> SharedScaleAxes()
        {
            var yielded = new HashSet<Axes>(ReferenceEqualityComparer.Instance) { axes };
            foreach (var linked in axes.Chain(a => a.ShareXWith).Concat(axes.Chain(a => a.ShareYWith)))
                if (yielded.Add(linked)) yield return linked;
        }

        private IEnumerable<object> OwnDataHolders()
        {
            foreach (var series in axes.AllSeries) yield return series;
            foreach (var item in axes.Overlays.Items()) yield return item;
            foreach (var inset in axes.Insets)
                foreach (var holder in inset.OwnDataHolders()) yield return holder;
        }

        /// <summary>The axes reached from this one through <paramref name="link"/>, each once: a link that comes back round
        /// ends the chain.</summary>
        private IEnumerable<Axes> Chain(Func<Axes, Axes?> link)
        {
            var walked = new HashSet<Axes>(ReferenceEqualityComparer.Instance) { axes };
            for (var linked = link(axes); linked is not null && walked.Add(linked); linked = link(linked))
                yield return linked;
        }
    }
}
