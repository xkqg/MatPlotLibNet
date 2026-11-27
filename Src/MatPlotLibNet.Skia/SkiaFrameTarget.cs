// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Frames;
using SkiaSharp;

namespace MatPlotLibNet.Skia;

/// <summary>The target a native control's paint goes onto: one Skia canvas, on which a subplot is drawn directly, or, from its
/// second paint under one key, recorded into an <see cref="SKPicture"/> that later paints replay. A picture is recorded under the
/// canvas' own matrix and replayed with the matrix set aside, so it rasterises exactly as a direct draw does (a hatch tile is
/// built for the pixels the canvas has, text lands on the same device pixels), and the matrix is part of what the recording
/// depends on: a canvas at another scale draws it again.</summary>
internal sealed class SkiaFrameTarget : KeepingFrameTarget<SKPicture>
{
    private readonly SKCanvas _canvas;
    private readonly SkiaRenderContext _surface;
    private readonly SKMatrix _matrix;

    /// <summary>A target for a paint onto <paramref name="canvas"/> after the one that left <paramref name="kept"/>.</summary>
    internal SkiaFrameTarget(SKCanvas canvas, KeptAxesDraw<SKPicture>[]? kept) : base(kept)
    {
        _canvas = canvas;
        _surface = new SkiaRenderContext(canvas);
        _matrix = canvas.TotalMatrix;
    }

    /// <inheritdoc />
    internal override IRenderContext Background => _surface;

    /// <inheritdoc />
    internal override IRenderContext Overlay => _surface;

    /// <summary>The canvas' total matrix: a recording made at one scale is never replayed at another.</summary>
    protected override object? LayerKey => _matrix;

    /// <summary>Draws the subplot straight onto the canvas, or, when it is kept, records it under the canvas' matrix and replays
    /// the recording onto the canvas at once.</summary>
    protected override LayerDraw<SKPicture> DrawLayer(in AxesDrawRequest request, bool keep,
        Func<IRenderContext, IReadOnlyList<LegendItemBounds>> draw)
    {
        if (!keep) return new LayerDraw<SKPicture>(null, draw(_surface));

        var bounds = _canvas.DeviceClipBounds;
        using var recorder = new SKPictureRecorder();
        var recording = recorder.BeginRecording(new SKRect(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom));
        recording.SetMatrix(_matrix);
        var legend = draw(new SkiaRenderContext(recording));
        var picture = recorder.EndRecording();
        Place(request.Index, picture);
        return new LayerDraw<SKPicture>(picture, legend);
    }

    /// <summary>Replays a recording onto the canvas with the canvas' matrix set aside: the recording carries the matrix it was
    /// made under.</summary>
    protected override void Place(int index, SKPicture layer)
    {
        _canvas.Save();
        _canvas.ResetMatrix();
        _canvas.DrawPicture(layer);
        _canvas.Restore();
    }

    /// <summary>Frees a recording a new draw replaced: a picture holds native memory.</summary>
    protected override void Release(SKPicture layer) => layer.Dispose();
}
