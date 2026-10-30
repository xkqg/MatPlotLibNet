// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Skia;
using SkiaSharp;
using MplRect  = MatPlotLibNet.Rendering.Rect;
using MplPoint = MatPlotLibNet.Rendering.Point;

namespace MatPlotLibNet.Avalonia;

/// <summary>
/// Custom Avalonia draw operation that renders a <see cref="Figure"/> onto the Skia canvas
/// provided by Avalonia's Skia rendering backend.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Runs on Avalonia's render thread, against a Skia lease only a running Avalonia application gives.")]
internal sealed class MplChartDrawOperation : ICustomDrawOperation
{
    private readonly Figure _figure;
    private readonly IInteractiveChart? _owner;

    internal MplChartDrawOperation(Rect bounds, Figure figure, IInteractiveChart? owner)
    {
        Bounds = bounds;
        _figure = figure;
        _owner = owner;
    }

    /// <inheritdoc />
    public Rect Bounds { get; }

    /// <inheritdoc />
    public bool HitTest(Point p) => Bounds.Contains(p);

    /// <inheritdoc />
    public bool Equals(ICustomDrawOperation? other) => false;

    /// <inheritdoc />
    public void Dispose() { }

    /// <inheritdoc />
    public void Render(ImmediateDrawingContext context)
    {
        var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (feature is null) return;

        using var lease = feature.Lease();
        if (lease is null) return;

        var skiaCtx = new SkiaRenderContext(lease.SkCanvas);
        MatPlotLibNet.ChartRendererDefault.Current.Render(_figure, skiaCtx);

        if (_owner is { IsInteractive: true })
        {
            var layoutResult = MatPlotLibNet.ChartRendererDefault.Current.ComputeLayout(_figure, skiaCtx);
            _owner.OnRenderCompleted(_figure, layoutResult);
        }

        // Draw rubber-band selection rectangle if a brush select drag is in progress.
        var brushState = _owner?.ActiveBrushSelect;
        if (brushState is { } bs)
            lease.SkCanvas.DrawBrushSelect(bs);
    }
}
