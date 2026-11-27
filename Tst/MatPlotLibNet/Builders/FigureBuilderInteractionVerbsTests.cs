// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>What a viewer of the figure can do with it (<see cref="FigureBuilderInteractionVerbs"/>): a selection group needs a
/// name, and <c>WithServerInteraction</c> with its <see cref="ServerInteractionBuilder"/> sets <c>ChartId</c>, flips
/// <c>ServerInteraction</c>, and routes each opted-in event type to the matching existing <c>Enable*</c> flag on
/// <see cref="Figure"/>.</summary>
public class FigureBuilderInteractionVerbsTests
{
    [Fact]
    public void ASelectionGroupWithoutAName_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => Plt.Create().LinkSelection(" "));
    }

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

    /// <summary>The opt-ins of a <see cref="ServerInteractionBuilder"/>, by name.</summary>
    private static readonly Dictionary<string, Action<ServerInteractionBuilder>> OptIns = new()
    {
        ["zoom"] = i => i.EnableZoom(), ["pan"] = i => i.EnablePan(), ["reset"] = i => i.EnableReset(),
        ["legend toggle"] = i => i.EnableLegendToggle(), ["brush select"] = i => i.EnableBrushSelect(), ["hover"] = i => i.EnableHover(),
        ["data cursor"] = i => i.EnableDataCursor(), ["all"] = i => i.All(),
    };

    /// <summary>The flags of a figure an opt-in turns on, by name.</summary>
    private static readonly Dictionary<string, Func<Figure, bool>> Flags = new()
    {
        [nameof(Figure.EnableZoomPan)] = f => f.EnableZoomPan, [nameof(Figure.EnableLegendToggle)] = f => f.EnableLegendToggle,
        [nameof(Figure.EnableSelection)] = f => f.EnableSelection, [nameof(Figure.EnableRichTooltips)] = f => f.EnableRichTooltips,
        [nameof(Figure.EnableDataCursor)] = f => f.EnableDataCursor,
    };

    /// <summary>Each opt-in turns on the figure flag it routes to and no other: zoom, pan and reset the zoom-pan flag, brush select the
    /// selection flag, hover the rich tooltips, and <c>All</c> every one of them.</summary>
    [Theory]
    [InlineData("zoom", nameof(Figure.EnableZoomPan))]
    [InlineData("pan", nameof(Figure.EnableZoomPan))]
    [InlineData("reset", nameof(Figure.EnableZoomPan))]
    [InlineData("legend toggle", nameof(Figure.EnableLegendToggle))]
    [InlineData("brush select", nameof(Figure.EnableSelection))]
    [InlineData("hover", nameof(Figure.EnableRichTooltips))]
    [InlineData("data cursor", nameof(Figure.EnableDataCursor))]
    [InlineData("all", "EnableZoomPan EnableLegendToggle EnableSelection EnableRichTooltips EnableDataCursor")]
    public void AnOptIn_TurnsOnTheFlagsItRoutesTo_AndNoOther(string optIn, string flagsOn)
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).WithServerInteraction("c", OptIns[optIn]).Build();

        Assert.True(figure.ServerInteraction);
        Assert.Equal(flagsOn.Split(' ').Order(StringComparer.Ordinal), Flags.Where(flag => flag.Value(figure)).Select(flag => flag.Key).Order(StringComparer.Ordinal));
    }
}
