// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>The name of the hub method that carries each interaction event, written once. The sink a native control sends
/// through, the hub of the server that receives and the script of a page that invokes all read their names from here, or
/// are held to it by a test, so an event cannot be added to one of them and be dropped by the others.</summary>
internal static class HubMethodNames
{
    /// <summary>The method that carries a <see cref="ZoomEvent"/>.</summary>
    internal const string Zoom = "OnZoom";

    /// <summary>The method that carries a <see cref="PanEvent"/>.</summary>
    internal const string Pan = "OnPan";

    /// <summary>The method that carries a <see cref="ResetEvent"/>.</summary>
    internal const string Reset = "OnReset";

    /// <summary>The method that carries a <see cref="LegendToggleEvent"/>.</summary>
    internal const string LegendToggle = "OnLegendToggle";

    /// <summary>The method that carries a <see cref="BrushSelectEvent"/>.</summary>
    internal const string BrushSelect = "OnBrushSelect";

    /// <summary>The method that carries a <see cref="HoverEvent"/>.</summary>
    internal const string Hover = "OnHover";

    /// <summary>The method that carries a <see cref="DataCursorEvent"/>.</summary>
    internal const string DataCursor = "OnDataCursor";

    /// <summary>The method that carries a <see cref="RectangleZoomEvent"/>.</summary>
    internal const string RectangleZoom = "OnRectangleZoom";

    /// <summary>The method that carries a <see cref="SpanSelectEvent"/>.</summary>
    internal const string SpanSelect = "OnSpanSelect";

    /// <summary>The method that carries a <see cref="Rotate3DEvent"/>.</summary>
    internal const string Rotate3D = "OnRotate3D";

    /// <summary>The method that carries an <see cref="AddTrendlineEvent"/>.</summary>
    internal const string AddTrendline = "OnAddTrendline";

    /// <summary>The method that carries an <see cref="AddHorizontalLevelEvent"/>.</summary>
    internal const string AddHorizontalLevel = "OnAddHorizontalLevel";

    /// <summary>The method that carries an <see cref="AddFibonacciRetracementEvent"/>.</summary>
    internal const string AddFibonacciRetracement = "OnAddFibonacciRetracement";

    /// <summary>The method that carries where a viewer's pointer is (<c>CursorEventPayload</c>); not an event of the figure,
    /// so <see cref="For"/> has no arm for it.</summary>
    internal const string Cursor = "OnCursor";

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
