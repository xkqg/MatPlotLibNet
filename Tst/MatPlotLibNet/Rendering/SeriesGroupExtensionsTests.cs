// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary><see cref="SeriesGroupExtensions.BeginSeriesGroup"/> — the one rule for the group a series is drawn in,
/// shared by the primary series passes and both secondary-axis parts.</summary>
public class SeriesGroupExtensionsTests
{
    private readonly record struct OneLine(Axes Axes, LineSeries Line);

    private static OneLine Line(string? label, bool interactive)
    {
        var axes = new Axes { EnableInteractiveAttributes = interactive };
        var line = axes.Plot([0.0, 1.0], [0.0, 1.0]);
        line.Label = label;
        return new OneLine(axes, line);
    }

    public static TheoryData<string?, bool, int, string> TheGroupOfASeries => new()
    {
        { "Revenue & cost", true, 7, "<g class=\"series\" data-series-index=\"7\" aria-label=\"Revenue &amp; cost\">" }, // the slot and the label
        { null, true, 3, "<g class=\"series\" data-series-index=\"3\">" },                                                  // an unlabelled series still gets its data group
        { "Revenue", false, 2, "<g class=\"series\" aria-label=\"Revenue\">" },                                            // without interactive attributes a labelled series gets an accessible group
    };

    [Theory]
    [MemberData(nameof(TheGroupOfASeries))]
    public void BeginSeriesGroup_OpensTheGroupOfASeries(string? label, bool interactive, int slot, string expected)
    {
        var (axes, line) = Line(label, interactive);
        var ctx = new SvgRenderContext();

        Assert.True(ctx.BeginSeriesGroup(axes, line, slot));

        Assert.Equal(expected, ctx.GetOutput().Trim());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithoutInteractiveAttributes_AnUnlabelledSeries_GetsNoGroup(string? label)
    {
        var (axes, line) = Line(label, interactive: false);
        var ctx = new SvgRenderContext();

        Assert.False(ctx.BeginSeriesGroup(axes, line, 0));

        Assert.Equal("", ctx.GetOutput());
    }

    [Fact]
    public void ANonSvgContext_OpensNoGroup()
    {
        var (axes, line) = Line("Revenue", interactive: true);
        var ctx = new RecordingRenderContext();

        Assert.False(ctx.BeginSeriesGroup(axes, line, 0));

        Assert.Empty(ctx.Calls);
    }

    /// <summary>A series of your own, which is no <see cref="ChartSeries"/>, is cut off by the rule for an artist of its own: the clip opens
    /// exactly when the axes cuts off such an artist's marks under the theme.</summary>
    [Fact]
    public void ASeriesOfYourOwn_IsCutOffAsAnArtistOfItsOwn()
    {
        var axes = new Axes();

        Assert.All(new[] { Theme.MatplotlibV2, Theme.Default }, theme =>
        {
            using var clip = new SvgRenderContext().ClippedForSeries(axes, new ForeignSeries(), new ClipRegion(new Rect(0, 0, 10, 10)), theme);
            Assert.Equal(axes.ClipsItsMarksUnder(theme, MatplotlibArtist.Own), clip.Context is not null);
        });
    }
}
