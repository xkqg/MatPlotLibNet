// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The Q-Q plot in one call, by scipy's conventions (<c>scipy.stats.probplot</c>): the sample sorted up the Y axis
/// against the quantiles of the standard normal distribution at Filliben's plotting positions along the X axis, and the
/// least-squares line through those points as the reference, drawn in red from the first quantile to the last. The points are
/// one scatter series; the reference is a trendline, which takes no colour slot. Every expected number below was computed by
/// scipy 1.18.1 (<c>probplot(x, dist='norm')</c>) and is compared by tolerance, because the quantiles come from a logarithm
/// and a square root.</summary>
public class QQPlotTests
{
    /// <summary>A sample of five, unsorted.</summary>
    private static readonly double[] Five = [3.1, 1.2, 4.8, 2.0, 2.9];

    /// <summary>scipy's theoretical quantiles (<c>osm</c>) for five observations.</summary>
    private static readonly double[] FiveQuantiles =
        [-1.1289975352961017, -0.4856527065756922, 0.0, 0.48565270657569204, 1.1289975352961017];

    /// <summary>A sample of twelve, unsorted.</summary>
    private static readonly double[] Twelve = [12.5, 9.8, 11.2, 14.1, 10.4, 13.3, 8.7, 12.0, 10.9, 11.6, 15.2, 9.3];

    /// <summary>scipy's theoretical quantiles (<c>osm</c>) for twelve observations.</summary>
    private static readonly double[] TwelveQuantiles =
    [
        -1.5881546429662674, -1.0981497546858916, -0.7825592680591444, -0.5306911286167685, -0.30892352547711976,
        -0.10153400250628433, 0.1015340025062842, 0.30892352547711976, 0.5306911286167681, 0.7825592680591442,
        1.0981497546858916, 1.5881546429662674,
    ];

    private const double Tolerance = 1e-9;

    private static Axes QQ(double[] sample, Action<QQPlotParts>? configure = null) =>
        Plt.Create().OneAxes(ax => ax.QQPlot(sample, configure));

    private static ScatterSeries Points(Axes axes) => Assert.IsType<ScatterSeries>(Assert.Single(axes.Series));

    private static Trendline Reference(Axes axes) => Assert.Single(axes.Trendlines);

    // ---- what the verb adds ---------------------------------------------------------------------------------------

    /// <summary>The chart is one scatter series of the points and one trendline for the reference, and nothing else: no
    /// second axis, no reference line or annotation of its own.</summary>
    [Fact]
    public void TheChart_IsOneScatterSeriesAndOneTrendline_AndNothingElse()
    {
        var axes = QQ(Five);

        Points(axes);
        Reference(axes);
        Assert.Empty(axes.YAxes.Right);
        Assert.Empty(axes.XSecondarySeries);
        Assert.Empty(axes.ReferenceLines);
        Assert.Empty(axes.Annotations);
    }

    // ---- the points ---------------------------------------------------------------------------------------------------

    /// <summary>The sample is sorted up the Y axis (scipy's <c>osr</c>): the k-th point is the k-th smallest observation.</summary>
    [Fact]
    public void TheSample_IsSortedUpTheYAxis() =>
        Assert.Equal([1.2, 2.0, 2.9, 3.1, 4.8], Points(QQ(Five)).YData);

    /// <summary>Filliben's plotting positions (Filliben 1975; scipy's <c>_calc_uniform_order_statistic_medians</c>): the
    /// median of the k-th smallest of n uniform observations, the last 0.5^(1/n), the first one minus that, and the middle
    /// ones (k - 0.3175) / (n + 0.365). For n = 5 the first three, worked out by hand, are 0.12944943670387588,
    /// 0.3136067101584343 and exactly 0.5. The points stand at the standard normal quantiles of those positions.</summary>
    [Fact]
    public void ThePoints_StandAtTheNormalQuantiles_OfFillibensPositions_TheFirstThreeByHand()
    {
        double[] positions = [1 - Math.Pow(0.5, 1.0 / 5), (2 - 0.3175) / (5 + 0.365), (3 - 0.3175) / (5 + 0.365)];
        Assert.Equal(0.12944943670387588, positions[0], 1e-15);
        Assert.Equal(0.3136067101584343, positions[1], 1e-15);
        Assert.Equal(0.5, positions[2]);

        double[] x = Points(QQ(Five)).XData;

        // scipy.stats.norm.ppf of the three positions.
        (new double[] { -1.1289975352961017, -0.4856527065756922, 0.0 }).AssertCloseTo(x[..3], Tolerance);
        for (int k = 0; k < 3; k++) Assert.Equal(StandardNormal.Quantile(positions[k]), x[k], 1e-15);
    }

    /// <summary>All the theoretical quantiles equal scipy's <c>osm</c>, for five and for twelve observations.</summary>
    [Fact]
    public void TheTheoreticalQuantiles_AreScipys()
    {
        FiveQuantiles.AssertCloseTo(Points(QQ(Five)).XData, Tolerance);
        TwelveQuantiles.AssertCloseTo(Points(QQ(Twelve)).XData, Tolerance);
    }

    /// <summary>The positions are symmetric around the median, so the quantiles are too: the k-th from the left is minus the
    /// k-th from the right.</summary>
    [Fact]
    public void TheTheoreticalQuantiles_AreSymmetricAroundZero()
    {
        double[] x = Points(QQ(Twelve)).XData;

        for (int k = 0; k < x.Length; k++) Assert.Equal(-x[^(k + 1)], x[k], 1e-12);
    }

    // ---- the reference line ---------------------------------------------------------------------------------------------

    /// <summary>The reference is the least-squares line through the points (scipy's <c>linregress(osm, osr)</c>), drawn from
    /// the first theoretical quantile to the last, as probplot draws it: for five observations slope 1.5222202625729224 and
    /// intercept 2.8, so the line runs from 1.0814170753773857 to 4.5185829246226135.</summary>
    [Fact]
    public void TheReference_IsTheLeastSquaresLine_FromTheFirstQuantileToTheLast()
    {
        var axes = QQ(Five);
        var line = Reference(axes);
        double[] x = Points(axes).XData;

        Assert.Equal(x[0], line.X1);
        Assert.Equal(x[^1], line.X2);
        (new double[] { 1.0814170753773857, 4.5185829246226135 }).AssertCloseTo([line.Y1, line.Y2], Tolerance);
        Assert.False(line.IsExtended);
    }

    /// <summary>The same for twelve observations: scipy's slope 2.096890989025076 and intercept 11.583333333333334, the line
    /// from 8.253146173319031 to 14.913520493347637.</summary>
    [Fact]
    public void TheReference_OfTwelveObservations_IsScipys()
    {
        var line = Reference(QQ(Twelve));

        (new double[] { -1.5881546429662674, 8.253146173319031, 1.5881546429662674, 14.913520493347637 }).AssertCloseTo([line.X1, line.Y1, line.X2, line.Y2], Tolerance);
    }

    /// <summary>The line is the least-squares one: the residuals of the points against it sum to 0 and are uncorrelated with
    /// the quantiles (the two normal equations).</summary>
    [Fact]
    public void TheReference_SolvesTheNormalEquations()
    {
        var axes = QQ(Twelve);
        var line = Reference(axes);
        var points = Points(axes);
        double slope = (line.Y2 - line.Y1) / (line.X2 - line.X1);
        double[] residuals = [.. points.XData.Select((x, k) => points.YData[k] - (line.Y1 + slope * (x - line.X1)))];

        Assert.Equal(0, residuals.Sum(), 1e-9);
        Assert.Equal(0, residuals.Select((r, k) => r * points.XData[k]).Sum(), 1e-9);
    }

    /// <summary>Two observations are the least a line can be fitted through, and it passes through both.</summary>
    [Fact]
    public void TwoObservations_AreJoinedByTheReference()
    {
        var axes = QQ([7.0, 4.0]);
        var line = Reference(axes);

        (new double[] { -0.5449521356173604, 0.5449521356173604 }).AssertCloseTo(Points(axes).XData, Tolerance);
        Assert.Equal([4.0, 7.0], Points(axes).YData);
        (new double[] { 4.0, 7.0 }).AssertCloseTo([line.Y1, line.Y2], Tolerance);
    }

    /// <summary>A sample without a spread lies on a level line at its value.</summary>
    [Fact]
    public void ASampleWithoutSpread_HasALevelReference()
    {
        var line = Reference(QQ([5.0, 5.0, 5.0]));

        Assert.Equal(5.0, line.Y1, 1e-12);
        Assert.Equal(5.0, line.Y2, 1e-12);
    }

    /// <summary>A sample far from 0 with a small spread (prices around 45 000 moving by a few units) gets the line scipy fits,
    /// to 1e-9 (measured: 7e-12 off, two units in the last place).</summary>
    [Fact]
    public void ASampleFarFromZero_GetsScipysLine()
    {
        double[] prices = [.. Twelve.Select(v => 45000 + v)];

        var line = Reference(QQ(prices));

        Assert.Equal(45000 + 8.253146173319031, line.Y1, Tolerance);
        Assert.Equal(45000 + 14.913520493347637, line.Y2, Tolerance);
    }

    /// <summary>The reference is drawn as probplot draws it (<c>'r-'</c>): a solid red line, 1.5 points wide (matplotlib's
    /// line width), not extended, without a label.</summary>
    [Fact]
    public void TheReference_IsASolidRedLine_LikeProbplots()
    {
        var line = Reference(QQ(Five));

        Assert.Equal(Colors.Red, line.Color);
        Assert.Equal(1.5, line.LineWidth);
        Assert.Equal(LineStyle.Solid, line.LineStyle);
        Assert.Null(line.Label);
    }

    /// <summary>The points are the chart's one series and wear the theme's first colour; the reference is a trendline, which
    /// takes no colour slot, so a series added after the Q-Q plot wears the theme's SECOND colour under the library's one cycle
    /// (under the matplotlib themes the points ask the patch cycle, so a line after them takes the lines cycle's first).</summary>
    [Fact]
    public void TheReference_TakesNoColourSlot()
    {
        var figure = Plt.Create().WithTheme(Theme.Default).AddSubPlot(1, 1, 1, ax => ax.QQPlot(Five).Plot([0.0, 1.0], [2.0, 3.0])).Build();
        figure.EnableLegendToggle = true;
        string svg = figure.ToSvg();

        Assert.Equal(figure.Theme.CycleColors[0].ToHex(), svg.SeriesGroup(0).FirstColour());
        Assert.Equal(figure.Theme.CycleColors[1].ToHex(), svg.SeriesGroup(1).FirstColour());
        Assert.Single(svg.Elements("line"), line => line["stroke"] == "#FF0000");
    }

    // ---- axes ----------------------------------------------------------------------------------------------------------

    /// <summary>The axes are titled with what they measure, and a title set after the call wins.</summary>
    [Fact]
    public void TheAxes_AreTitledWithTheQuantiles()
    {
        var axes = QQ(Five);

        Assert.Equal("Theoretical quantiles", axes.XAxis.Label);
        Assert.Equal("Sample quantiles", axes.YAxis.Label);
    }

    /// <summary>The reference line of a skewed sample leaves the range the points span: scipy draws it from -467 to 1494 for the
    /// squares 0 to 1521, with <c>ax.plot</c>, so the automatic Y axis holds it (scipy 1.18.1, matplotlib 3.11.2: ylim -566.64 to
    /// 1620.41, not the points' -76 to 1597). Its ends stand inside the plot, never over the margins, the tick labels or the
    /// title.</summary>
    [Fact]
    public void TheReference_OfASkewedSample_StaysInsideThePlotArea()
    {
        double[] squares = [.. Enumerable.Range(0, 40).Select(i => (double)(i * i))];

        string svg = Plt.Create().WithSize(600, 500).AddSubPlot(1, 1, 1, ax => ax.QQPlot(squares)).ToSvg();

        var plot = svg.PlotArea();
        var red = svg.Each("line").Single(e => (string?)e.Attribute("stroke") == "#FF0000");
        foreach (string end in new[] { "1", "2" })
        {
            double px = red.Number("x" + end), py = red.Number("y" + end);
            Assert.InRange(px, plot.X - 1e-9, plot.Right + 1e-9);
            Assert.InRange(py, plot.Y - 1e-9, plot.Bottom + 1e-9);
        }
        // The axis holds the line: its start stands above the bottom edge, where the range ends a margin below it.
        Assert.True(red.Number("y1") < plot.Bottom - 1);
        var range = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.QQPlot(squares)).Build().SubPlots[0].ComputeDataRanges(Theme.MatplotlibV2);
        Assert.Equal(-566.6402792019456, range.YMin, 6);
        Assert.Equal(1620.4114418667593, range.YMax, 6);
    }

    // ---- missing values -------------------------------------------------------------------------------------------------

    /// <summary>A missing observation (NaN) is not there, and an infinity cannot be placed: both are left out, so the plot is
    /// exactly the plot of the other observations (five here, at the positions for five), and no NaN reaches the picture.</summary>
    [Fact]
    public void AMissingOrInfiniteObservation_IsLeftOut()
    {
        double[] gappy = [double.NaN, 3.1, 1.2, double.PositiveInfinity, 4.8, 2.0, double.NegativeInfinity, 2.9];

        var clean = QQ(Five);
        var axes = QQ(gappy);

        Assert.Equal(Points(clean).XData, Points(axes).XData);
        Assert.Equal(Points(clean).YData, Points(axes).YData);
        Assert.Equal(Reference(clean).Y1, Reference(axes).Y1);
        Assert.Equal(Reference(clean).Y2, Reference(axes).Y2);
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.QQPlot(gappy)).ToSvg().AssertNoNonFiniteNumber();
    }

    // ---- refusals -----------------------------------------------------------------------------------------------------

    /// <summary>A line is fitted through two points or more: a sample with fewer that can be drawn (NaN and the infinities are
    /// left out) is refused, naming the parameter.</summary>
    [Theory]
    [InlineData(new double[0], "0 observations")]
    [InlineData(new[] { double.NaN, 4.2 }, "1 observation")]
    public void ASampleWithFewerThanTwoObservations_IsRefused(double[] sample, string held)
    {
        var error = Assert.Throws<ArgumentException>(() => QQ(sample));

        Assert.Equal("sample", error.ParamName);
        Assert.Equal($"The sample holds {held}; a Q-Q plot needs at least 2; NaN and the infinities are not counted. (Parameter 'sample')", error.Message);
    }

    // ---- the example in the documentation --------------------------------------------------------------------------------

    /// <summary>The example of the verb's documentation, as written there, compiles and draws.</summary>
    [Fact]
    public void TheDocumentationExample_Draws()
    {
        double[] returns = [0.8, -1.2, 0.3, 2.1, -0.4, 0.9, -2.6, 0.1, 1.4, -0.7, 0.5, -0.2];

        string svg = Plt.Create()
            .WithTitle("Daily returns against the normal distribution")
            .AddSubPlot(1, 1, 1, ax => ax
                .QQPlot(returns, parts => parts.Points.Alpha = 0.7)
                .SetYLabel("Daily return (%)"))
            .ToSvg();

        Assert.Contains(">Theoretical quantiles<", svg);
        Assert.Contains(">Daily return (%)<", svg);
        Assert.Contains("""stroke="#FF0000" """.TrimEnd(), svg);
    }
}
