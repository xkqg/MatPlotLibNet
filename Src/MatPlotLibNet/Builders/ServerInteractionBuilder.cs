// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Builders;

/// <summary>Fluent selector for which server-authoritative interaction events a figure opts
/// into. Each <c>Enable*</c> method flips an internal flag consumed by
/// <see cref="FigureBuilder.WithServerInteraction"/> at <c>Build()</c> time to route the matching
/// existing <c>Figure.Enable*</c> flag on the resulting figure. Method names mirror the existing
/// <c>Enable*</c> convention — no new vocabulary for users to learn.</summary>
public sealed class ServerInteractionBuilder
{
    internal bool Zoom { get; private set; }
    internal bool Pan { get; private set; }
    internal bool Reset { get; private set; }
    internal bool LegendToggle { get; private set; }
    internal bool BrushSelect { get; private set; }
    internal bool Hover { get; private set; }
    internal bool DataCursor { get; private set; }
    internal bool ViewerCursors { get; private set; }

    /// <summary>Opt in to <c>ZoomEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnableZoom() { Zoom = true; return this; }

    /// <summary>Opt in to <c>PanEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnablePan() { Pan = true; return this; }

    /// <summary>Opt in to <c>ResetEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnableReset() { Reset = true; return this; }

    /// <summary>Opt in to <c>LegendToggleEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnableLegendToggle() { LegendToggle = true; return this; }

    /// <summary>Opt in to <c>BrushSelectEvent</c> round-trips. Shift+drag in the browser
    /// becomes a server-side notification delivered to the chart's brush-select handler.</summary>
    public ServerInteractionBuilder EnableBrushSelect() { BrushSelect = true; return this; }

    /// <summary>Opt in to <c>HoverEvent</c> round-trips. Cursor movement over the plot
    /// becomes a server-side query whose response is delivered as tooltip HTML to the
    /// originating client only.</summary>
    public ServerInteractionBuilder EnableHover() { Hover = true; return this; }

    /// <summary>Opt in to <c>DataCursorEvent</c> round-trips. A click on a marker becomes a server-side
    /// notification delivered to the chart's data-cursor handler, naming the series the reader clicked and
    /// where on the axes they clicked it.</summary>
    public ServerInteractionBuilder EnableDataCursor() { DataCursor = true; return this; }

    /// <summary>Opt in to showing each viewer the pointers of the others. Not a gesture the server applies and not part
    /// of <see cref="All"/>: every viewer sends where their pointer is while it moves, and the server relays it to
    /// the other viewers of the chart.</summary>
    public ServerInteractionBuilder EnableViewerCursors() { ViewerCursors = true; return this; }

    /// <summary>Opt in to every event type.</summary>
    public ServerInteractionBuilder All()
    {
        Zoom = true;
        Pan = true;
        Reset = true;
        LegendToggle = true;
        BrushSelect = true;
        Hover = true;
        DataCursor = true;
        return this;
    }
}
