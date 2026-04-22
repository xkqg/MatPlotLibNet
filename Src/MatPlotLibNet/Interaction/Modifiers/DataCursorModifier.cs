// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Phase H.4 of the v1.7.2 follow-on plan — click-to-pin data cursor.
/// On a plain left-click near a data point (within <see cref="HitRadiusPx"/>),
/// emits a <see cref="DataCursorEvent"/> carrying a <see cref="PinnedAnnotation"/>.
/// When the click doesn't hit any point, defers to the next modifier
/// (typically pan) — so normal drag-pan gestures still work.
///
/// <para>Pre-H.4 the <c>ToolbarButton("cursor", "Data Cursor (click)")</c>
/// button existed, <see cref="DataCursorEvent"/> + <see cref="PinnedAnnotation"/>
/// records existed, but <b>no modifier implemented the click handler</b> — the
/// toolbar button was orphaned. H.4 wires it up.</para></summary>
public sealed class DataCursorModifier : IInteractionModifier
{
    /// <summary>Pixel radius within which a click "hits" a data point. Matches the
    /// default cursor-hit threshold used by matplotlib's mplcursors library.</summary>
    public const double HitRadiusPx = 10.0;

    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Figure _figure;
    private readonly Action<FigureInteractionEvent> _sink;
    private NearestPointResult? _pendingHit;

    /// <summary>Creates a data-cursor modifier.</summary>
    public DataCursorModifier(string chartId, IChartLayout layout, Figure figure, Action<FigureInteractionEvent> sink)
    {
        _chartId = chartId;
        _layout = layout;
        _figure = figure;
        _sink = sink;
    }

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args)
    {
        if (args.Button != PointerButton.Left) return false;
        if (args.Modifiers != ModifierKeys.None) return false;

        var axesIndex = _layout.HitTestAxes(args.X, args.Y);
        if (axesIndex is null) return false;

        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, axesIndex.Value);

        var nearest = NearestPointFinder.Find(_figure, axesIndex.Value, coords!.Value.DataX, coords.Value.DataY, _layout);
        if (nearest is null) return false;
        if (nearest.PixelDistance > HitRadiusPx) return false;

        _pendingHit = nearest;
        _pendingHitPointer = new Point(args.X, args.Y);
        _pendingHitAxes = axesIndex.Value;
        return true;
    }

    private Point _pendingHitPointer;
    private int _pendingHitAxes;

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        if (_pendingHit is null) return;
        var annotation = new PinnedAnnotation(
            _pendingHit.SeriesLabel,
            _pendingHit.DataX, _pendingHit.DataY,
            _pendingHitPointer.X, _pendingHitPointer.Y,
            _pendingHitAxes);
        _sink(new DataCursorEvent(_chartId, _pendingHitAxes, annotation));
        _pendingHit = null;
    }

    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args) { }
    /// <inheritdoc />
    public void OnPointerReleased(PointerInputArgs args) { }
    /// <inheritdoc />
    public bool HandlesScroll(ScrollInputArgs args) => false;
    /// <inheritdoc />
    public void OnScroll(ScrollInputArgs args) { }
    /// <inheritdoc />
    public bool HandlesKeyDown(KeyInputArgs args) => false;
    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args) { }
}
