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

    /// <summary>Opt in to <c>ZoomEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnableZoom() { Zoom = true; return this; }

    /// <summary>Opt in to <c>PanEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnablePan() { Pan = true; return this; }

    /// <summary>Opt in to <c>ResetEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnableReset() { Reset = true; return this; }

    /// <summary>Opt in to <c>LegendToggleEvent</c> round-trips.</summary>
    public ServerInteractionBuilder EnableLegendToggle() { LegendToggle = true; return this; }

    /// <summary>Opt in to all four event types.</summary>
    public ServerInteractionBuilder All()
    {
        Zoom = true;
        Pan = true;
        Reset = true;
        LegendToggle = true;
        return this;
    }
}
