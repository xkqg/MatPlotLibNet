// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>The name of every method of the chart hub, written once: the events a page or a native control sends (the
/// <c>On</c> names), the messages the server sends a viewer and the calls a viewer joins and leaves a chart with. The sink a
/// native control sends through, the hub of the server, the script a figure carries, the Interactive page and the Blazor client
/// all read their names from here, or are held to it by a test, so a method cannot be renamed or added on one side and be
/// missed by the others. A page of your own that talks to the hub reads the same names.</summary>
public static class HubMethodNames
{
    /// <summary>The method that carries a <see cref="ZoomEvent"/>.</summary>
    public const string Zoom = "OnZoom";

    /// <summary>The method that carries a <see cref="PanEvent"/>.</summary>
    public const string Pan = "OnPan";

    /// <summary>The method that carries a <see cref="ResetEvent"/>.</summary>
    public const string Reset = "OnReset";

    /// <summary>The method that carries a <see cref="LegendToggleEvent"/>.</summary>
    public const string LegendToggle = "OnLegendToggle";

    /// <summary>The method that carries a <see cref="BrushSelectEvent"/>.</summary>
    public const string BrushSelect = "OnBrushSelect";

    /// <summary>The method that carries a <see cref="HoverEvent"/>.</summary>
    public const string Hover = "OnHover";

    /// <summary>The method that carries a <see cref="DataCursorEvent"/>.</summary>
    public const string DataCursor = "OnDataCursor";

    /// <summary>The method that carries a <see cref="RectangleZoomEvent"/>.</summary>
    public const string RectangleZoom = "OnRectangleZoom";

    /// <summary>The method that carries a <see cref="SpanSelectEvent"/>.</summary>
    public const string SpanSelect = "OnSpanSelect";

    /// <summary>The method that carries a <see cref="Rotate3DEvent"/>.</summary>
    public const string Rotate3D = "OnRotate3D";

    /// <summary>The method that carries an <see cref="AddTrendlineEvent"/>.</summary>
    public const string AddTrendline = "OnAddTrendline";

    /// <summary>The method that carries an <see cref="AddHorizontalLevelEvent"/>.</summary>
    public const string AddHorizontalLevel = "OnAddHorizontalLevel";

    /// <summary>The method that carries an <see cref="AddFibonacciRetracementEvent"/>.</summary>
    public const string AddFibonacciRetracement = "OnAddFibonacciRetracement";

    /// <summary>The method that carries where a viewer's pointer is (<c>CursorEventPayload</c>); not an event of the figure,
    /// so no event of the figure maps to it.</summary>
    public const string Cursor = "OnCursor";

    /// <summary>The message that carries a chart's figure as JSON to its viewers.</summary>
    public const string UpdateChart = "UpdateChart";

    /// <summary>The message that carries a chart's whole SVG to its viewers.</summary>
    public const string UpdateChartSvg = "UpdateChartSvg";

    /// <summary>The message that carries only the parts of a chart's SVG that changed to the viewers that patch.</summary>
    public const string UpdateChartParts = "UpdateChartParts";

    /// <summary>The message that carries the tooltip the server worked out for a viewer's hover.</summary>
    public const string ReceiveTooltipContent = "ReceiveTooltipContent";

    /// <summary>The message that tells a viewer where another viewer's pointer is.</summary>
    public const string ReceiveCursor = "ReceiveCursor";

    /// <summary>The message that tells a viewer another viewer moved the view.</summary>
    public const string ViewChanged = "ViewChanged";

    /// <summary>The message that tells a viewer another viewer left the chart.</summary>
    public const string ViewerLeft = "ViewerLeft";

    /// <summary>The call a viewer joins a chart with, to receive its whole SVG on every change.</summary>
    public const string Subscribe = "Subscribe";

    /// <summary>The call a viewer that patches joins a chart with, to receive the whole SVG once and then only the parts that
    /// change.</summary>
    public const string SubscribeToParts = "SubscribeToParts";

    /// <summary>The call a viewer leaves a chart with.</summary>
    public const string Unsubscribe = "Unsubscribe";

    /// <summary>The events that are not sent over a wire, each with why.</summary>
    internal static IReadOnlyDictionary<Type, string> NotRouted { get; } = new Dictionary<Type, string>
    {
        [typeof(RowSelectionEvent)] =
            "it is worked out where the figure is, from a brush (RowSelectionExtensions.RowSelectionOf), and given to the handler right "
            + "after that brush; a client that sent one would be naming rows the server never checked against its own data.",
        [typeof(RemoveDrawingToolEvent)] =
            "it carries the drawing as an object; across a wire the server receives a JSON element it cannot match to a "
            + "drawing it holds, so removal would silently remove nothing. Removing a drawing needs a typed payload first.",
    };

    /// <summary>The name of the hub method that carries <paramref name="evt"/>, or <see langword="null"/> for an event that is
    /// not sent over a wire (<see cref="NotRouted"/>).</summary>
    /// <param name="evt">The event.</param>
    internal static string? For(FigureInteractionEvent evt) => evt switch
    {
        ZoomEvent => Zoom,
        PanEvent => Pan,
        ResetEvent => Reset,
        LegendToggleEvent => LegendToggle,
        BrushSelectEvent => BrushSelect,
        HoverEvent => Hover,
        DataCursorEvent => DataCursor,
        RectangleZoomEvent => RectangleZoom,
        SpanSelectEvent => SpanSelect,
        Rotate3DEvent => Rotate3D,
        AddTrendlineEvent => AddTrendline,
        AddHorizontalLevelEvent => AddHorizontalLevel,
        AddFibonacciRetracementEvent => AddFibonacciRetracement,
        _ => null,
    };
}
