// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Frames;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>The paint of a native control that draws with Skia (WPF, Avalonia, Uno): each paint draws the figure onto the canvas
/// it is handed through the library's one paint (<see cref="FrameTargetPaintExtensions"/>), and keeps, between paints, a recording
/// of every subplot that did not change, so a paint draws only the subplots that did. One painter belongs to one control. The
/// recordings depend on the canvas' scale and on the figure, so another scale draws again and another figure starts over.</summary>
/// <remarks>A paint and <see cref="Dispose"/> may come from different threads (a control's render thread and its UI thread), so
/// both take the painter's lock.</remarks>
internal sealed class SkiaChartPainter : IDisposable
{
    private readonly object _gate = new();
    private Figure? _figure;
    private KeptAxesDraw<SKPicture>[]? _kept;

    /// <summary>The recording the painter holds of each subplot after its last paint, <see langword="null"/> for a subplot it holds
    /// none of; <see langword="null"/> as a whole before the first paint and after <see cref="Dispose"/>.</summary>
    internal SKPicture?[]? Recordings
    {
        get
        {
            lock (_gate)
            {
                if (_kept is null) return null;
                var recordings = new SKPicture?[_kept.Length];
                for (int i = 0; i < recordings.Length; i++) recordings[i] = _kept[i].Layer;
                return recordings;
            }
        }
    }

    /// <summary>Paints <paramref name="figure"/> onto <paramref name="canvas"/>, with the band of a brush-select drag in progress
    /// over it.</summary>
    /// <param name="canvas">The control's canvas for this paint.</param>
    /// <param name="figure">The figure the control shows.</param>
    /// <param name="band">The drag in progress, or <see langword="null"/> for none.</param>
    /// <returns>The layout of the paint, for the control's interaction host.</returns>
    internal LayoutResult Paint(SKCanvas canvas, Figure figure, BrushSelectState? band)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(figure, _figure))
            {
                Free();
                _figure = figure;
            }

            var target = new SkiaFrameTarget(canvas, _kept);
            var layout = target.Paint(figure, band);
            _kept = target.ToKeep();
            return layout;
        }
    }

    /// <summary>Frees every recording the painter kept. The painter can still paint: it starts again from scratch.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            Free();
            _figure = null;
        }
    }

    private void Free()
    {
        if (_kept is null) return;
        foreach (var draw in _kept) draw.Layer?.Dispose();
        _kept = null;
    }
}
