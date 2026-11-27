// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>How wide a 3-D wireframe is drawn. matplotlib 3.11.2, <c>plot_wireframe</c> builds an <c>art3d.Line3DCollection</c>
/// with no width of its own, so it takes <c>lines.linewidth</c>: measured 1.5 pt under the default style and 1.0 pt under the
/// classic one. A width given wins; the library's own themes keep 0.5 px.</summary>
public sealed class WireframeLineWidthTests
{
    private static readonly double[,] Z = { { 0, 1 }, { 1, 2 } };

    private static string Svg(Theme theme, Action<WireframeSeries>? configure = null) => Plt.Create().WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.Wireframe([0.0, 1], [0.0, 1], Z, configure))
        .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("TheMatplotlibThemes_DrawItAsWideAsTheirLines (#1)", () => Svg(Theme.MatplotlibV2), "stroke-width=\"2.08\""),
        new DrawnToken("TheMatplotlibThemes_DrawItAsWideAsTheirLines (#2)", () => Svg(Theme.MatplotlibClassic), "stroke-width=\"1.39\""),
        new DrawnToken("TheLibrarysThemes_KeepHalfAPixel_AndAGivenWidthWins (#1)", () => Svg(Theme.Default), "stroke-width=\"0.5\""),
        new DrawnToken("TheLibrarysThemes_KeepHalfAPixel_AndAGivenWidthWins (#2)", () => Svg(Theme.MatplotlibV2, s => s.LineWidth = 0.5), "stroke-width=\"0.5\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A given width, 0.5 included, survives the JSON; one not given is not written.</summary>
    [Fact]
    public void AGivenWidth_SurvivesTheJson()
    {
        var given = new WireframeSeries(new double[] { 0, 1 }, new double[] { 0, 1 }, Z) { LineWidth = 0.5 };
        var resting = new WireframeSeries(new double[] { 0, 1 }, new double[] { 0, 1 }, Z);

        Assert.Equal(0.5, given.ToSeriesDto().LineWidth);
        Assert.Null(resting.ToSeriesDto().LineWidth);
    }
}
