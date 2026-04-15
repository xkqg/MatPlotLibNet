// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>v1.2.2 additions to the SignalR interaction dispatcher script: Shift+drag
/// brush-select branch and hover tooltip round-trip branch. Each branch is gated behind a
/// new flag on <c>ServerInteractionBuilder</c> and opts in via the existing
/// <see cref="Figure.EnableSelection"/> / <see cref="Figure.EnableRichTooltips"/> flags
/// (so state is reused, not duplicated).</summary>
public class SvgSignalRInteractionScriptV122Tests
{
    private const string BrushSelectMarker = "mplBrushSelect";
    private const string HoverMarker = "mplHoverRoundtrip";

    [Fact]
    public void BrushSelectBranch_Emitted_WhenEnableBrushSelectSet()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnableBrushSelect())
            .ToSvg();

        Assert.Contains(BrushSelectMarker, svg);
    }

    [Fact]
    public void BrushSelectBranch_NotEmitted_WhenDisabled()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnableZoom())
            .ToSvg();

        Assert.DoesNotContain(BrushSelectMarker, svg);
    }

    [Fact]
    public void HoverBranch_Emitted_WhenEnableHoverSet()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnableHover())
            .ToSvg();

        Assert.Contains(HoverMarker, svg);
    }

    [Fact]
    public void HoverBranch_NotEmitted_WhenDisabled()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnableZoom())
            .ToSvg();

        Assert.DoesNotContain(HoverMarker, svg);
    }

    [Fact]
    public void BothBranches_Emitted_WhenAllEnabled()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.All())
            .ToSvg();

        Assert.Contains(BrushSelectMarker, svg);
        Assert.Contains(HoverMarker, svg);
    }

    [Fact]
    public void StaticFigure_NoBranchesEmitted()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain(BrushSelectMarker, svg);
        Assert.DoesNotContain(HoverMarker, svg);
    }

    [Fact]
    public void V120HandlersStillPresent_BrushSelectOptIn_DoesNotBreakV120()
    {
        // Regression guard: opting into brush-select must not remove the v1.2.0 OnZoom/OnPan/OnReset/OnLegendToggle
        // handlers (they're part of the same IIFE, emitted unconditionally once the figure is in ServerInteraction mode).
        var svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnableBrushSelect())
            .ToSvg();

        Assert.Contains(BrushSelectMarker, svg);
        // v1.2.0 markers still present — the new branch is additive, not a replacement.
        Assert.Contains("OnZoom", svg);
        Assert.Contains("OnPan", svg);
    }
}
