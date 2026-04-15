// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies that <c>SvgSignalRInteractionScript</c> is emitted only when
/// <c>Figure.ServerInteraction</c> is set, that <c>data-chart-id</c> lands on the root SVG in
/// that mode, and that the local client-side IIFE scripts (Zoom/Pan/Legend-Toggle) are
/// replaced by the SignalR dispatcher — never duplicated.</summary>
public class SvgSignalRInteractionScriptTests
{
    private const string SignalRMarker = "mplSignalRInteraction";

    [Fact]
    public void ServerInteractionFalse_Default_ScriptIsNotEmitted()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .ToSvg();

        Assert.DoesNotContain(SignalRMarker, svg);
        Assert.DoesNotContain("data-chart-id", svg);
    }

    [Fact]
    public void ServerInteractionTrue_ScriptIsEmitted()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .WithServerInteraction("live-7", i => i.EnableZoom())
            .ToSvg();

        Assert.Contains(SignalRMarker, svg);
    }

    [Fact]
    public void ServerInteractionTrue_RootSvg_Has_DataChartIdAttribute()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .WithServerInteraction("live-42", i => i.EnableZoom())
            .ToSvg();

        Assert.Contains("data-chart-id=\"live-42\"", svg);
    }

    [Fact]
    public void ServerInteractionTrue_LocalZoomPanScript_IsSuppressed()
    {
        // The existing SvgInteractivityScript (client-side viewBox manipulation) must NOT be
        // emitted when ServerInteraction is on — the two would double-handle the same wheel/drag
        // events. The SignalR dispatcher replaces it.
        var svg = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .WithServerInteraction("c", i => i.EnableZoom().EnablePan())
            .ToSvg();

        // The local script's signature comment or known substring
        Assert.DoesNotContain("vb.slice()", svg); // unique to SvgInteractivityScript
    }

    [Fact]
    public void ServerInteractionTrue_LocalLegendToggleScript_IsSuppressed()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .WithServerInteraction("c", i => i.EnableLegendToggle())
            .ToSvg();

        // SvgLegendToggleScript has a unique substring we can pivot on.
        Assert.DoesNotContain("data-legend-index", svg);
    }

    [Fact]
    public void ServerInteractionFalse_WithZoomPan_StillEmitsLocalScript()
    {
        // Regression: ServerInteraction = false path must be byte-identical to v1.1.4.
        // EnableZoomPan alone (the existing way) still produces the local client script.
        var svg = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
            .WithZoomPan()
            .ToSvg();

        Assert.Contains("vb.slice()", svg);
        Assert.DoesNotContain(SignalRMarker, svg);
    }
}
