// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>An indicator's lines are as thick as its <see cref="Indicator.LineWidth"/> says (the documented thickness of the indicator's
/// lines, in pixels, 1.5 at rest), so the lines of one indicator share a width; a width that was given is drawn as given (1.5 included),
/// and an indicator given no width draws its lines at the width of its theme (matplotlib's <c>lines.linewidth</c> under the matplotlib themes: 1.5 pt, 2.08 px, in the default style). The
/// +DI and -DI lines of an ADX and the conversion and base lines of an Ichimoku cloud were fixed at 1 pixel whatever the indicator's width
/// and the theme said, beside a main line that followed both. pandas-ta, which the fidelity reference is drawn with, draws every one of
/// them with the style's line width.</summary>
public sealed class IndicatorLineWidthTests
{
    private const double Pt = 100.0 / 72.0;

    private static readonly double[] High = [30.20, 30.28, 30.45, 29.35, 29.35, 29.29, 28.83, 28.73, 28.67, 28.85, 28.64, 27.68, 27.21, 26.87, 27.41, 26.94, 26.52, 26.52, 27.09, 27.69];
    private static readonly double[] Low = [29.41, 29.32, 29.96, 28.74, 28.56, 28.41, 28.08, 27.43, 27.66, 27.83, 27.40, 27.09, 26.18, 26.13, 26.63, 26.13, 25.43, 25.35, 26.18, 26.58];
    private static readonly double[] Close = [29.87, 30.24, 30.10, 28.90, 28.92, 28.48, 28.56, 27.56, 28.47, 28.28, 27.49, 27.23, 26.35, 26.33, 27.03, 26.22, 26.01, 25.46, 27.03, 27.45];

    private static readonly double[] LongHigh = [.. Enumerable.Range(1, 60).Select(i => (double)(50 + i))];
    private static readonly double[] LongLow = [.. Enumerable.Range(1, 60).Select(i => (double)(40 + i))];
    private static readonly double[] LongClose = [.. Enumerable.Range(1, 60).Select(i => (double)(45 + i))];

    private static Adx NewAdx(double? width = null)
    {
        var adx = new Adx(OhlcvBars.Hlc(High, Low, Close), 5) { PlusDiColor = Colors.Tab10Orange, MinusDiColor = Colors.Tab10Green };
        if (width is { } w) adx.LineWidth = w;
        return adx;
    }

    private static Ichimoku NewIchimoku(double? width = null)
    {
        var cloud = new Ichimoku(OhlcvBars.Hlc(LongHigh, LongLow, LongClose));
        if (width is { } w) cloud.LineWidth = w;
        return cloud;
    }

    private static XDocument Svg(Theme theme, Action<AxesBuilder> axes) =>
        Plt.Create().WithSize(500, 400).WithTheme(theme).AddSubPlot(1, 1, 1, axes).Build().ToSvg().Xml();

    private static double WidthOfLine(XDocument svg, Color stroke) =>
        double.Parse((string)svg.Descendants().First(e => (string?)e.Attribute("stroke") == stroke.ToHex() && (string?)e.Attribute("fill") == "none")
            .Attribute("stroke-width")!, CultureInfo.InvariantCulture);

    // ── the model: one width for the lines of an indicator ──────────────────────────────────────────────────────

    [Fact]
    public void AnAdx_DrawsItsThreeLinesAtTheIndicatorsWidth()
    {
        var axes = new Axes();
        NewAdx().Apply(axes);

        Assert.Equal(3, axes.Series.Count);
        Assert.All(axes.Series.Cast<LineSeries>(), line => Assert.Equal(1.5, line.LineWidth));
    }

    [Fact]
    public void AnAdxWithAWidth_DrawsItsThreeLinesAtIt()
    {
        var axes = new Axes();
        NewAdx(3).Apply(axes);

        Assert.All(axes.Series.Cast<LineSeries>(), line => Assert.Equal(3, line.LineWidth));
    }

    [Fact]
    public void AnIchimoku_DrawsItsFiveLinesAtTheIndicatorsWidth()
    {
        var axes = new Axes();
        NewIchimoku().Apply(axes);

        var lines = axes.Series.OfType<LineSeries>().ToArray();
        Assert.Equal(5, lines.Length);
        Assert.All(lines, line => Assert.Equal(1.5, line.LineWidth));
    }

    [Fact]
    public void AnIchimokuWithAWidth_DrawsItsFiveLinesAtIt()
    {
        var axes = new Axes();
        NewIchimoku(3).Apply(axes);

        Assert.All(axes.Series.OfType<LineSeries>(), line => Assert.Equal(3, line.LineWidth));
    }

    // ── the picture: the theme's width for a line left at rest ──────────────────────────────────────────────────

    /// <summary>The default style draws the DI lines as wide as the main line, 1.5 points; the classic style draws them at its own line
    /// width, 1 point.</summary>
    [Theory]
    [InlineData("v2", 1.5)]
    [InlineData("classic", 1.0)]
    public void TheDiLinesOfAnAdx_AreTheStylesLineWidth(string theme, double points)
    {
        var svg = Svg(theme.AsTheme(), ax => ax.Indicator(NewAdx()));

        Assert.Equal(points * Pt, WidthOfLine(svg, Colors.Tab10Orange), 2);
        Assert.Equal(points * Pt, WidthOfLine(svg, Colors.Tab10Green), 2);
    }

    [Fact]
    public void TheLibrarysOwnTheme_DrawsTheDiLinesOfAnAdxAtTheIndicatorsWidth()
    {
        var svg = Svg(Theme.Default, ax => ax.Indicator(NewAdx()));

        Assert.Equal(1.5, WidthOfLine(svg, Colors.Tab10Orange), 2);
        Assert.Equal(1.5, WidthOfLine(svg, Colors.Tab10Green), 2);
    }

    [Fact]
    public void AWidthTheAuthorNames_IsDrawnAsItIs_UnderTheMatplotlibThemesToo()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Indicator(NewAdx(3)));

        Assert.Equal(3, WidthOfLine(svg, Colors.Tab10Orange), 2);
        Assert.Equal(3, WidthOfLine(svg, Colors.Tab10Green), 2);
    }
}
