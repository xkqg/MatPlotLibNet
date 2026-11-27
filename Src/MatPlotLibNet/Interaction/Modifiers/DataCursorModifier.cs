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
public sealed class DataCursorModifier : InteractionModifier
{
    /// <summary>Pixel radius within which a click "hits" a data point. Matches the
    /// default cursor-hit threshold used by matplotlib's mplcursors library.</summary>
    public const double HitRadiusPx = 10.0;

    private readonly Figure _figure;
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
        : base(chartId, layout, sink)
    {
        _figure = figure;
        _pan = pan;
    }

    /// <inheritdoc />
    public override bool HandlesPointerPressed(PointerInputArgs args)
    {
        if (args.Modifiers != ModifierKeys.None) return false;
        if (Layout.LeftPressAxes(args) is not { } axesIndex) return false;

        var coords = Layout.PixelToData(args.X, args.Y, axesIndex);

        var nearest = NearestPointFinder.Find(_figure, axesIndex, coords!.Value.DataX, coords.Value.DataY, Layout);
        if (nearest is null) return false;
        if (nearest.PixelDistance > HitRadiusPx) return false;

        _pendingHit = nearest;
        _pendingHitPointer = new Point(args.X, args.Y);
        _pendingHitAxes = axesIndex;
        return true;
    }

    private Point _pendingHitPointer;
    private int _pendingHitAxes;

    /// <inheritdoc />
    public override void OnPointerPressed(PointerInputArgs args)
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
        Emit(new DataCursorEvent(ChartId, _pendingHitAxes, annotation));
    }

    /// <inheritdoc />
    public override void OnPointerMoved(PointerInputArgs args)
    {
        if (!_spanPending) return;

        double travelled = Math.Max(Math.Abs(args.X - _pendingHitPointer.X), Math.Abs(args.Y - _pendingHitPointer.Y));
        if (!_dragging && travelled <= ClickTolerancePx) return;

        _dragging = true;
        _pan!.OnPointerMoved(args);
    }

    /// <inheritdoc />
    public override void OnPointerReleased(PointerInputArgs args)
    {
        if (!_spanPending) return;

        _pan!.OnPointerReleased(args);
        if (!_dragging) Pin(_pendingHit!);

        _spanPending = false;
        _dragging = false;
        _pendingHit = null;
    }
}
