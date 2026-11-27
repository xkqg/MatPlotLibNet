// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>The length of an error bar's caps where its author names none. matplotlib 3.11.2, printed under each style:
/// <c>rcParams['errorbar.capsize']</c> is 0.0 in the default style and 3.0 in the classic one, a length in points, and
/// <c>Axes.errorbar</c> draws a cap as a marker <c>2 * capsize</c> points across, so a cap reaches <c>capsize</c> points either side of
/// its bar: nothing under v2, 3 pt = 4.17 px under classic. The library's cap reaches <c>CapSize</c> pixels either side, 5 under its own
/// themes. A cap that is named is drawn as named, 5 included; the legend's handle follows the plot's cap.</summary>
public class ErrorBarCapSizeTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [2.0, 3.0, 2.5];
    private static readonly double[] E = [0.5, 0.5, 0.5];

    private static readonly double ClassicCap = 3.0.PointsInPixels;

    private static XDocument Doc(Theme theme, Action<ErrorBarSeries>? configure = null, bool legend = false) =>
        Plt.Create().WithTheme(theme).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => { ax.ErrorBar(X, Y, E, E, s => { s.Label = "e"; configure?.Invoke(s); }); if (legend) ax.WithLegend(); }).ToSvg().Xml();

    private static double Length(XElement line)
    {
        return Math.Max(Math.Abs(line.Number("x2") - line.Number("x1")), Math.Abs(line.Number("y2") - line.Number("y1")));
    }

    private static bool Horizontal(XElement line) => (string?)line.Attribute("y1") == (string?)line.Attribute("y2");

    private static bool Vertical(XElement line) => (string?)line.Attribute("x1") == (string?)line.Attribute("x2");

    private static IEnumerable<XElement> SeriesLines(XDocument doc, Theme theme, bool inLegend) =>
        doc.Each("line").Where(e => (string?)e.Attribute("stroke") == theme.CycleColors[0].ToHex()
            && e.Ancestors().Any(a => (string?)a.Attribute("class") == "legend") == inLegend);

    /// <summary>The caps of the plot: the horizontal strokes in the first cycle colour, outside the legend.</summary>
    private static List<XElement> Caps(Theme theme, Action<ErrorBarSeries>? configure = null) =>
        [.. SeriesLines(Doc(theme, configure), theme, inLegend: false).Where(Horizontal)];

    [Fact]
    public void UnderV2_ABarGivenNoCapSize_HasNoCaps_AsMatplotlibsCapsizeOfZero()
    {
        Assert.Empty(Caps(Theme.MatplotlibV2));
        Assert.Equal(3, SeriesLines(Doc(Theme.MatplotlibV2), Theme.MatplotlibV2, inLegend: false).Count(Vertical));   // the bars stay
    }

    [Fact]
    public void UnderClassic_ABarGivenNoCapSize_HasCapsOfThreePointsEitherSide()
    {
        var caps = Caps(Theme.MatplotlibClassic);

        Assert.Equal(6, caps.Count);   // a top and a bottom for each of the three bars
        Assert.All(caps, cap => Assert.Equal(2 * ClassicCap, Length(cap), 1));
    }

    [Fact]
    public void UnderTheLibrarysThemes_ABarGivenNoCapSize_KeepsItsFivePixels()
    {
        var caps = Caps(Theme.Default);

        Assert.Equal(6, caps.Count);
        Assert.All(caps, cap => Assert.Equal(10.0, Length(cap), 1));
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    [InlineData("library")]
    public void ACapSizeThatIsNamed_IsDrawnAsNamed_EvenWhenItIsTheOneANewSeriesHolds(string theme)
    {
        var chosen = theme.AsTheme();

        var five = Caps(chosen, s => s.CapSize = 5);
        var zero = Caps(chosen, s => s.CapSize = 0);

        Assert.Equal(6, five.Count);
        Assert.All(five, cap => Assert.Equal(10.0, Length(cap), 1));
        Assert.Empty(zero);   // a cap of no length is no cap, not six strokes of no length
    }

    [Fact]
    public void ACapSizeThatIsNamed_SurvivesTheJson_AndOneThatIsNot_StaysUnnamed()
    {
        string unnamed = Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.ErrorBar(X, Y, E, E)).Build().ToJson();
        string named = Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.ErrorBar(X, Y, E, E, s => s.CapSize = 5)).Build().ToJson();

        Assert.DoesNotContain("capSize", unnamed);
        Assert.Contains("\"capSize\":5", named);
        Assert.Equal(0, SeriesLines(ChartSerializerDefault.Current.FromJson(unnamed).ToSvg().Xml(), Theme.MatplotlibV2, false).Count(Horizontal));
        Assert.Equal(6, SeriesLines(ChartSerializerDefault.Current.FromJson(named).ToSvg().Xml(), Theme.MatplotlibV2, false).Count(Horizontal));
    }

    // ── the legend's handle follows the plot's cap ─────────────────────────────────────────────────────────────

    private static List<XElement> HandleCaps(Theme theme) =>
        [.. SeriesLines(Doc(theme, legend: true), theme, inLegend: true).Where(Vertical)];

    [Fact]
    public void TheLegendHandle_HasNoCapsUnderV2_AndCapsOfThePlotsLengthUnderClassic_AndItsOwnThreeUnderTheLibrarysThemes()
    {
        Assert.Empty(HandleCaps(Theme.MatplotlibV2));

        var classic = HandleCaps(Theme.MatplotlibClassic);
        Assert.Equal(2, classic.Count);
        Assert.All(classic, cap => Assert.Equal(2 * ClassicCap, Length(cap), 1));

        var library = HandleCaps(Theme.Default);
        Assert.Equal(2, library.Count);
        Assert.All(library, cap => Assert.Equal(6.0, Length(cap), 1));
    }
}
