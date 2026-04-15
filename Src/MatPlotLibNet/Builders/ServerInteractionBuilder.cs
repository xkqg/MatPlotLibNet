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

    /// <summary>Opt in to <c>ZoomEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnableZoom() { Zoom = true; return this; }

    /// <summary>Opt in to <c>PanEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnablePan() { Pan = true; return this; }

    /// <summary>Opt in to <c>ResetEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnableReset() { Reset = true; return this; }

    /// <summary>Opt in to <c>LegendToggleEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnableLegendToggle() { LegendToggle = true; return this; }

    /// <summary>Opt in to <c>BrushSelectEvent</c> round-trips (v1.2.2). Shift+drag in the browser
    /// becomes a server-side notification delivered to the chart's brush-select handler.</summary>
    public ServerInteractionBuilder EnableBrushSelect() { BrushSelect = true; return this; }

    /// <summary>Opt in to <c>HoverEvent</c> round-trips (v1.2.2). Cursor movement over the plot
    /// becomes a server-side query whose response is delivered as tooltip HTML to the
    /// originating client only.</summary>
    public ServerInteractionBuilder EnableHover() { Hover = true; return this; }

    /// <summary>Opt in to every event type (v1.2.0 + v1.2.2).</summary>
    public ServerInteractionBuilder All()
    {
        Zoom = true;
        Pan = true;
        Reset = true;
        LegendToggle = true;
        BrushSelect = true;
        Hover = true;
        return this;
    }
}
