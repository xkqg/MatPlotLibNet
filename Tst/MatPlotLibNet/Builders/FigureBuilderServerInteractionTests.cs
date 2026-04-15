// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Verifies the fluent <see cref="FigureBuilder.WithServerInteraction"/> entry point
/// and <see cref="ServerInteractionBuilder"/> semantics: sets <c>ChartId</c>, flips
/// <c>ServerInteraction</c>, and routes each opted-in event type to the matching existing
/// <c>Enable*</c> flag on <see cref="Models.Figure"/>.</summary>
public class FigureBuilderServerInteractionTests
{
    [Fact]
    public void WithServerInteraction_SetsChartIdAndServerInteractionFlag()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("live-1", i => i.EnableZoom())
            .Build();

        Assert.Equal("live-1", figure.ChartId);
        Assert.True(figure.ServerInteraction);
    }

    [Fact]
    public void WithServerInteraction_EnableZoom_FlipsEnableZoomPan()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnableZoom())
            .Build();

        Assert.True(figure.EnableZoomPan);
    }

    [Fact]
    public void WithServerInteraction_EnablePan_FlipsEnableZoomPan()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnablePan())
            .Build();

        Assert.True(figure.EnableZoomPan);
    }

    [Fact]
    public void WithServerInteraction_EnableLegendToggle_FlipsEnableLegendToggle()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnableLegendToggle())
            .Build();

        Assert.True(figure.EnableLegendToggle);
    }

    [Fact]
    public void WithServerInteraction_All_FlipsEveryRelevantFlag()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.All())
            .Build();

        Assert.True(figure.EnableZoomPan);
        Assert.True(figure.EnableLegendToggle);
        Assert.True(figure.ServerInteraction);
    }

    [Fact]
    public void WithServerInteraction_WithoutAnyEventOptIn_StillSetsServerInteractionAndChartId()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", _ => { })
            .Build();

        Assert.True(figure.ServerInteraction);
        Assert.Equal("c", figure.ChartId);
        Assert.False(figure.EnableZoomPan);
        Assert.False(figure.EnableLegendToggle);
    }

    [Fact]
    public void Figure_Default_ServerInteractionIsFalseAndChartIdIsNull()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        Assert.False(figure.ServerInteraction);
        Assert.Null(figure.ChartId);
    }

    [Fact]
    public void WithServerInteraction_ReturnsSameBuilder_ForChaining()
    {
        var builder = Plt.Create();
        var result = builder.WithServerInteraction("c", i => i.EnableZoom());
        Assert.Same(builder, result);
    }

    [Fact]
    public void ServerInteractionBuilder_FluentMethodsReturnSelf()
    {
        var b = new ServerInteractionBuilder();
        Assert.Same(b, b.EnableZoom());
        Assert.Same(b, b.EnablePan());
        Assert.Same(b, b.EnableReset());
        Assert.Same(b, b.EnableLegendToggle());
        Assert.Same(b, b.EnableBrushSelect());
        Assert.Same(b, b.EnableHover());
        Assert.Same(b, b.All());
    }

    // ── v1.2.2 additions ──────────────────────────────────────────────────

    [Fact]
    public void WithServerInteraction_EnableBrushSelect_FlipsEnableSelectionFlag()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnableBrushSelect())
            .Build();

        Assert.True(figure.EnableSelection);
    }

    [Fact]
    public void WithServerInteraction_EnableHover_FlipsEnableRichTooltipsFlag()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.EnableHover())
            .Build();

        Assert.True(figure.EnableRichTooltips);
    }

    [Fact]
    public void WithServerInteraction_All_IncludesBrushSelectAndHover()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .WithServerInteraction("c", i => i.All())
            .Build();

        Assert.True(figure.ServerInteraction);
        Assert.True(figure.EnableZoomPan);
        Assert.True(figure.EnableLegendToggle);
        Assert.True(figure.EnableSelection);
        Assert.True(figure.EnableRichTooltips);
    }
}
