// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Click-to-pin data cursor, the handler of the toolbar's
/// <c>ToolbarButton("cursor", "Data Cursor (click)")</c>.
/// On a plain left-click near a data point (within <see cref="HitRadiusPx"/>),
/// emits a <see cref="DataCursorEvent"/> carrying a <see cref="PinnedAnnotation"/>.
/// When the click doesn't hit any point, defers to the next modifier
/// (typically pan) — so normal drag-pan gestures still work.</summary>
public sealed class DataCursorModifier : IInteractionModifier
{
    /// <summary>Pixel radius within which a click "hits" a data point. Matches the
    /// default cursor-hit threshold used by matplotlib's mplcursors library.</summary>
    public const double HitRadiusPx = 10.0;

    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Figure _figure;
    private readonly Action<FigureInteractionEvent> _sink;
    private readonly PanModifier? _pan;
    private NearestPointResult? _pendingHit;
    private bool _spanPending;
    private bool _dragging;

    /// <summary>How far the pointer may travel between press and release and still count as a click on a span.</summary>
    public const double ClickTolerancePx = 4.0;

    /// <summary>Creates a data-cursor modifier.</summary>
    public DataCursorModifier(string chartId, IChartLayout layout, Figure figure, Action<FigureInteractionEvent> sink)
        : this(chartId, layout, figure, sink, null)
    {
    }

    /// <summary>Creates a data-cursor modifier that can hand a drag to <paramref name="pan"/>.
    /// <para>A span (a timeline segment) fills most of its axes, so a press on one may be the start of a drag as
    /// easily as a click. With a pan modifier the press waits: a release within <see cref="ClickTolerancePx"/> is
    /// the click, and anything further pans, anchored where the button went down. Without one a span is clicked on
    /// press, like a point.</para></summary>
    public DataCursorModifier(string chartId, IChartLayout layout, Figure figure, Action<FigureInteractionEvent> sink, PanModifier? pan)
    {
        _chartId = chartId;
        _layout = layout;
        _figure = figure;
        _sink = sink;
        _pan = pan;
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

        if (_pan is not null && _pendingHit.Detail is not null)
        {
            _spanPending = true;
            _dragging = false;
            _pan.OnPointerPressed(args);
            return;
        }

        Pin(_pendingHit);
        _pendingHit = null;
    }

    private void Pin(NearestPointResult hit)
    {
        var annotation = new PinnedAnnotation(
            hit.SeriesLabel,
            hit.DataX, hit.DataY,
            _pendingHitPointer.X, _pendingHitPointer.Y,
            _pendingHitAxes)
        {
            // Both indices were resolved by the search; passing on only the label would make the application
            // search its own data for a matching pair of doubles to find the row the reader clicked.
            PointIndex = hit.PointIndex,
            SeriesIndex = hit.SeriesIndex,
        };
        _sink(new DataCursorEvent(_chartId, _pendingHitAxes, annotation));
    }

    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args)
    {
        if (!_spanPending) return;

        double travelled = Math.Max(Math.Abs(args.X - _pendingHitPointer.X), Math.Abs(args.Y - _pendingHitPointer.Y));
        if (!_dragging && travelled <= ClickTolerancePx) return;

        _dragging = true;
        _pan!.OnPointerMoved(args);
    }

    /// <inheritdoc />
    public void OnPointerReleased(PointerInputArgs args)
    {
        if (!_spanPending) return;

        _pan!.OnPointerReleased(args);
        if (!_dragging) Pin(_pendingHit!);

        _spanPending = false;
        _dragging = false;
        _pendingHit = null;
    }
    /// <inheritdoc />
    public bool HandlesScroll(ScrollInputArgs args) => false;
    /// <inheritdoc />
    public void OnScroll(ScrollInputArgs args) { }
    /// <inheritdoc />
    public bool HandlesKeyDown(KeyInputArgs args) => false;
    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args) { }
}
