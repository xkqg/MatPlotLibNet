// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The Pareto chart in one call: the counts sorted largest first as bars (ties keep the order given), the running
/// share of the total as a marked line on a second Y axis with its points on the bar centres, and the two Y axes one scale
/// (0 to 1.05 times the total on the left, 0 to 105 % on the right), each coloured and titled like its series. The last point
/// of the line is exactly 100, because the total IS the last running sum. A missing count (NaN) is a missing bar and a missing
/// point, sorted last; a count that is negative or infinite, and a total that is not above 0, are refused.</summary>
public class ParetoTests
{
    private static readonly string[] Defects = ["Cracks", "Warp", "Scratches", "Breaks", "Dents", "Missing", "Stains"];
    private static readonly double[] Counts = [67, 9, 142, 31, 98, 18, 45];

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static Axes Pareto(string[] categories, double[] counts, Action<ParetoParts>? configure = null) =>
        AxesOf(ax => ax.Pareto(categories, counts, configure));

    /// <summary>The picture with the legend toggle on: that is what gives every series group its <c>data-series-index</c>,
    /// which these tests read the parts by (the bars are group 0, the line on the right-hand axis group 1).</summary>
    private static string Svg(string[] categories, double[] counts)
    {
        var figure = Plt.Create().WithSize(900, 500).AddSubPlot(1, 1, 1, ax => ax.Pareto(categories, counts)).Build();
        figure.EnableLegendToggle = true;
        return figure.ToSvg();
    }

    // ---- what the verb adds ---------------------------------------------------------------------------------------

    /// <summary>One bar series on the left axis and one line on the right-hand axis, and nothing else.</summary>
    [Fact]
    public void TheChart_IsOneBarSeriesAndOneLineOnTheRightAxis()
    {
        var axes = Pareto(Defects, Counts);

        var bars = Assert.IsType<BarSeries>(Assert.Single(axes.Series));
        var line = Assert.IsType<LineSeries>(Assert.Single(axes.YAxes.Right[0].Series));
        Assert.Empty(axes.XSecondarySeries);
        Assert.Equal("Count", bars.Label);
        Assert.Equal("Cumulative %", line.Label);
        Assert.Equal(2, line.LineWidth);
        Assert.Equal(MarkerStyle.Circle, line.Marker);
        Assert.Equal(7, line.MarkerSize);
    }

    /// <summary>The bars stand largest first, each category with its own count (W17).</summary>
    [Fact]
    public void TheBars_AreSortedLargestFirst_EachCategoryWithItsCount()
    {
        var bars = (BarSeries)Pareto(Defects, Counts).Series[0];

        Assert.Equal(["Scratches", "Dents", "Cracks", "Stains", "Breaks", "Missing", "Warp"], bars.Categories);
        Assert.Equal([142.0, 98, 67, 45, 31, 18, 9], bars.Values);
    }

    /// <summary>Equal counts keep the order they were given in, so a tie never reshuffles between runs or platforms (W17).</summary>
    [Fact]
    public void Ties_KeepTheOrderTheyWereGivenIn()
    {
        var bars = (BarSeries)Pareto(["a", "b", "c", "d", "e"], [5, 9, 5, 9, 7]).Series[0];

        Assert.Equal(["b", "d", "e", "a", "c"], bars.Categories);
        Assert.Equal([9.0, 9, 7, 5, 5], bars.Values);
    }

    /// <summary>The line is the running sum of the sorted counts over their total, in percent: the share of the total the
    /// bars up to and including that one hold.</summary>
    [Theory]
    [InlineData(new double[] { 50, 30, 20 }, new double[] { 50, 80, 100 })]
    [InlineData(new double[] { 20, 50, 30 }, new double[] { 50, 80, 100 })]
    [InlineData(new double[] { 5, 0, 5 }, new double[] { 50, 100, 100 })]
    [InlineData(new double[] { 7 }, new double[] { 100 })]
    [InlineData(new double[] { 1, 1, 2 }, new double[] { 50, 75, 100 })]
    public void TheLine_IsTheRunningShareOfTheTotal_InPercent(double[] counts, double[] shares)
    {
        string[] categories = [.. counts.Select((_, i) => $"c{i}")];

        var line = (LineSeries)Pareto(categories, counts).YAxes.Right[0].Series[0];

        Assert.Equal(shares, line.YData);
    }

    /// <summary>The last point is EXACTLY 100, whatever the counts: the total is the last running sum, so the last point is
    /// that sum over itself. A total summed in another order (the shipped recipe summed the counts as given and accumulated
    /// them sorted) differs in its last bit in about four trials of ten, and the line then ends a hair off 100 (W17: 8571 of
    /// 20000 in the council's measurement).</summary>
    [Fact]
    public void TheLastPoint_IsExactly100_ForEveryDrawOfCounts()
    {
        var random = new Random(20261022);
        int missed = 0;
        for (int trial = 0; trial < 20000; trial++)
        {
            int n = random.Next(1, 13);
            double[] counts = [.. Enumerable.Range(0, n).Select(_ => random.NextDouble() * 1000)];
            string[] categories = [.. Enumerable.Range(0, n).Select(i => $"c{i}")];
            double last = double.NaN;

            new AxesBuilder().Pareto(categories, counts, parts => last = parts.Cumulative.YData[^1]);

            if (last != 100) missed++;
        }

        Assert.Equal(0, missed);
    }

    // ---- a missing count ------------------------------------------------------------------------------------------

    /// <summary>A missing count (NaN) cannot be ranked: its category goes last, keeps its name on the axis, and has no bar and
    /// no point. It adds nothing to the total, so the line still ends at exactly 100 on the last count that is there.</summary>
    [Fact]
    public void AMissingCount_IsLast_WithNoBarAndNoPoint_AndAddsNothingToTheTotal()
    {
        var axes = Pareto(["a", "b", "c", "d"], [10, double.NaN, 30, 60]);
        var bars = (BarSeries)axes.Series[0];
        var line = (LineSeries)axes.YAxes.Right[0].Series[0];

        Assert.Equal(["d", "c", "a", "b"], bars.Categories);
        Assert.Equal([60.0, 30, 10, double.NaN], bars.Values);
        Assert.Equal([60.0, 90, 100, double.NaN], line.YData);
        Assert.Equal(100 * 1.05, axes.YAxis.Max);
    }

    /// <summary>Drawn: the missing count writes no NaN, no bar and no point; the other three are drawn.</summary>
    [Fact]
    public void AMissingCount_DrawsNothingOfItsOwn()
    {
        string[] categories = ["a", "b", "c", "d"];
        double[] counts = [10, double.NaN, 30, 60];

        Assert.DoesNotContain("NaN", Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Pareto(categories, counts)).ToSvg());
        string svg = Svg(categories, counts);
        Assert.Equal(3, Bars(svg).Length);
        Assert.Equal(3, Points(svg).Length);
    }

    // ---- where things stand: one scale on two axes ----------------------------------------------------------------

    /// <summary>Each point of the line stands on the centre of its bar, read from the one rule both obey (W18).</summary>
    [Fact]
    public void ThePoints_StandOnTheCentresOfTheirBars()
    {
        var axes = Pareto(Defects, Counts);
        var line = (LineSeries)axes.YAxes.Right[0].Series[0];
        Assert.Equal(Enumerable.Range(0, Defects.Length).Select(k => CategorySlot.Of(k).Centre), line.XData);

        string svg = Svg(Defects, Counts);
        var bars = Bars(svg);
        var points = Points(svg);
        Assert.Equal(Defects.Length, bars.Length);
        Assert.Equal(Defects.Length, points.Length);
        for (int k = 0; k < bars.Length; k++)
        {
            Assert.Equal(bars[k].X + bars[k].Width / 2, points[k].X, 9);
        }
    }

    /// <summary>The left axis runs from 0 to 1.05 times the total and the right one from 0 to 105 %, so the two are one scale:
    /// a count c on the left reads c / total on the right, and the top of the first bar IS the first point of the line (M5).
    /// Measured: with the automatic left axis the shipped recipe drew them 239.7 px apart in a 900 x 500 figure.</summary>
    [Fact]
    public void TheTwoYAxes_AreOneScale_SoTheFirstBarsTopIsTheFirstPoint()
    {
        var axes = Pareto(Defects, Counts);
        double total = Counts.Sum();

        Assert.Equal(0, axes.YAxis.Min);
        Assert.Equal(total * 1.05, axes.YAxis.Max);
        Assert.Equal(0, axes.YAxes.Right[0].Axis.Min);
        Assert.Equal(105, axes.YAxes.Right[0].Axis.Max);

        string svg = Svg(Defects, Counts);
        Assert.Equal(Bars(svg)[0].Y, Points(svg)[0].Y, 6);
    }

    /// <summary>The 100 % point is drawn whole: its marker ends below the top of the plot, where a set limit clips.</summary>
    [Fact]
    public void The100PercentMarker_IsWhole_InsideTheClippedPlot()
    {
        string svg = Svg(Defects, Counts);

        var top = Regex.Match(svg, """<clipPath id="[^"]+"><rect x="[-0-9.eE+]+" y="([-0-9.eE+]+)" """.TrimEnd());
        Assert.True(top.Success, "the plot with set limits is clipped");
        var last = Points(svg)[^1];
        Assert.True(last.Y - last.Radius > Number(top.Groups[1].Value), $"the 100 % marker reaches {last.Y - last.Radius} px, above the plot top {top.Groups[1].Value} px");
    }

    /// <summary>The verb sets the two Y axes' limits and leaves the X axis to the bars.</summary>
    [Fact]
    public void TheVerb_LeavesTheXAxisToTheBars()
    {
        var axes = Pareto(Defects, Counts);

        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
    }

    /// <summary>A limit of the caller's own, set after the call, wins over the verb's.</summary>
    [Fact]
    public void ALimitSetAfterTheCall_Wins()
    {
        var axes = AxesOf(ax => ax.Pareto(Defects, Counts).SetYLim(0, 1000));

        Assert.Equal(1000, axes.YAxis.Max);
    }

    // ---- colours, names, legend -----------------------------------------------------------------------------------

    /// <summary>A colour-coded dual axis: the bars wear blue and the left axis with them, the line orange and the right-hand
    /// axis with it, each axis titled with its series' name. The X axis belongs to both and takes neither colour. Blue and
    /// orange are the default theme's first two colours, a pair that stays apart under the common forms of colour-vision
    /// deficiency.</summary>
    [Fact]
    public void EachYAxis_WearsItsSeriesColour_AndIsTitledWithItsName()
    {
        var axes = Pareto(Defects, Counts);
        var bars = (BarSeries)axes.Series[0];
        var line = (LineSeries)axes.YAxes.Right[0].Series[0];

        Assert.Equal(Colors.Tab10Blue, bars.Color);
        Assert.Equal(Colors.Tab10Orange, line.Color);
        Assert.Equal(Colors.Tab10Blue, axes.Spines.Left.Color);
        Assert.Equal(Colors.Tab10Blue, axes.YAxis.MajorTicks.Color);
        Assert.Equal(Colors.Tab10Blue, axes.YAxis.LabelStyle!.Color);
        Assert.Equal(Colors.Tab10Orange, axes.Spines.Right.Color);
        Assert.Equal(Colors.Tab10Orange, axes.YAxes.Right[0].Axis.MajorTicks.Color);
        Assert.Equal(Colors.Tab10Orange, axes.YAxes.Right[0].Axis.LabelStyle!.Color);
        Assert.Equal("Count", axes.YAxis.Label);
        Assert.Equal("Cumulative %", axes.YAxes.Right[0].Axis.Label);
        Assert.Null(axes.Spines.Bottom.Color);
        Assert.Null(axes.XAxis.MajorTicks.Color);
    }

    /// <summary>The chart holds exactly ONE right-hand axis, whatever the verb does inside to colour and title it: no further
    /// slot on that side and none on the left, the line is the only series on it, and the slot wears the line's colour and
    /// carries its name. Two right axes (the second one empty) is what the chart would grow if its two entries into the
    /// right-hand axis stacked instead of reaching the same one.</summary>
    [Fact]
    public void Pareto_HasExactlyOneRightAxis_WearingTheLineColour()
    {
        var axes = Pareto(Defects, Counts);
        var line = (LineSeries)Assert.Single(axes.YAxes.Right[0].Series);

        var right = Assert.Single(axes.YAxes.Right);
        Assert.Single(axes.YAxes.Left);
        Assert.Same(axes.YAxes.Right[0].Axis, right.Axis);
        Assert.Same(line, Assert.Single(right.Series));
        Assert.Equal(Colors.Tab10Orange, line.Color);
        Assert.Equal(line.Color, right.Axis.MajorTicks.Color);
        Assert.Equal(line.Color, right.Axis.MajorTicks.LabelColor);
        Assert.Equal(line.Color, right.Axis.LabelStyle!.Color);
        Assert.Equal(line.Color, axes.Spines.Right.Color);
        Assert.Equal(line.Label, right.Axis.Label);
    }

    /// <summary>The legend stands right of centre: between the short tail bars and the line, which at that height runs near
    /// 100 %. Measured on the picture: "best" is the upper right corner, where it covers the line's last two points.</summary>
    [Fact]
    public void TheLegend_StandsRightOfCentre()
    {
        var legend = Pareto(Defects, Counts).Legend;

        Assert.True(legend.Visible);
        Assert.Equal(LegendPosition.Right, legend.Position);
    }

    /// <summary>One drawn unit, one colour: two series groups, blue and orange.</summary>
    [Fact]
    public void TheBarsAndTheLine_AreDrawnInTheirOwnColours()
    {
        string svg = Svg(Defects, Counts);

        Assert.Equal("#1F77B4", FirstColour(Group(svg, 0)));
        Assert.Equal("#FF7F0E", FirstColour(Group(svg, 1)));
    }

    // ---- the callback ---------------------------------------------------------------------------------------------

    /// <summary>The callback runs once, after the verb has styled both series, and hands over the very series on the axes;
    /// each Y axis then takes the colour and the name its series has when the callback is done, so recolouring or renaming a
    /// part recolours and retitles its axis.</summary>
    [Fact]
    public void TheCallback_ReceivesThePartsOnce_AndEachAxisFollowsItsSeries()
    {
        int calls = 0;
        ParetoParts seen = default;
        string? barLabelWhenCalled = null;
        Color? lineColourWhenCalled = null;
        var plum = Color.FromHex("#7B3294");
        var green = Color.FromHex("#008837");

        var axes = Pareto(Defects, Counts, parts =>
        {
            calls++;
            seen = parts;
            barLabelWhenCalled = parts.Bars.Label;
            lineColourWhenCalled = parts.Cumulative.Color;
            parts.Bars.Color = plum;
            parts.Bars.Label = "Defects";
            parts.Cumulative.Color = green;
            parts.Cumulative.Label = "Share of all defects (%)";
        });

        Assert.Equal(1, calls);
        Assert.Equal("Count", barLabelWhenCalled);
        Assert.Equal(Colors.Tab10Orange, lineColourWhenCalled);
        Assert.Same(axes.Series[0], seen.Bars);
        Assert.Same(axes.YAxes.Right[0].Series[0], seen.Cumulative);
        Assert.Equal(plum, axes.Spines.Left.Color);
        Assert.Equal(plum, axes.YAxis.LabelStyle!.Color);
        Assert.Equal("Defects", axes.YAxis.Label);
        Assert.Equal(green, axes.Spines.Right.Color);
        Assert.Equal(green, axes.YAxes.Right[0].Axis.LabelStyle!.Color);
        Assert.Equal("Share of all defects (%)", axes.YAxes.Right[0].Axis.Label);
    }

    /// <summary>A part left without a colour of its own (it then takes the theme's) leaves its axis uncoloured, and a part
    /// left without a name leaves its axis untitled: an axis never claims a colour or a name its series does not show.</summary>
    [Fact]
    public void APartWithoutAColourOrAName_LeavesItsAxisUncolouredOrUntitled()
    {
        var axes = Pareto(Defects, Counts, parts =>
        {
            parts.Bars.Color = null;
            parts.Cumulative.Label = null;
        });

        Assert.Null(axes.Spines.Left.Color);
        Assert.Null(axes.YAxis.MajorTicks.Color);
        Assert.Equal("Count", axes.YAxis.Label);
        Assert.Equal(Colors.Tab10Orange, axes.Spines.Right.Color);
        Assert.Null(axes.YAxes.Right[0].Axis.Label);
    }

    /// <summary>The parts are a public readonly record struct a caller cannot make: no public constructor, no setters.</summary>
    [Fact]
    public void TheParts_AreAReadOnlyRecordStruct_WithNoPublicConstructor()
    {
        var type = typeof(ParetoParts);

        Assert.True(type.IsValueType);
        Assert.True(type.IsPublic);
        Assert.True(type.IsDefined(typeof(IsReadOnlyAttribute)));
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.NotEmpty(type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance));
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.Equal(["Bars", "Cumulative"], properties.Select(p => p.Name).Order());
        Assert.All(properties, p => Assert.Null(p.SetMethod));
    }

    // ---- against the hand recipe, JSON ----------------------------------------------------------------------------------

    /// <summary>The verb draws exactly what the hand-written chain on hand-sorted data draws, with and without a missing count.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDrew(bool missing)
    {
        string byVerb = ComposedChartShapes.Named("pareto").Build(missing).ToSvg();
        string byHand = ComposedChartShapes.ParetoByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    /// <summary>A Pareto chart is its parts in JSON: read back it draws the same picture, with and without a missing count. It
    /// does not sort or accumulate again; the bars and the line come back as they were drawn.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_SurvivesJson(bool missing)
    {
        var figure = ComposedChartShapes.Named("pareto").Build(missing).Build();

        string drawn = figure.ToSvg();
        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
    }

    // ---- refusals -------------------------------------------------------------------------------------------------

    /// <summary>A missing argument, or counts that do not pair up with the categories, are refused by name.</summary>
    [Fact]
    public void MissingOrMisalignedArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).Pareto(Defects, Counts)).ParamName);
        Assert.Equal("categories", Assert.Throws<ArgumentNullException>(() => Pareto(null!, Counts)).ParamName);
        Assert.Equal("counts", Assert.Throws<ArgumentNullException>(() => Pareto(Defects, null!)).ParamName);

        var misaligned = Assert.Throws<ArgumentException>(() => Pareto(Defects, [1, 2, 3]));
        Assert.Equal("counts", misaligned.ParamName);
        Assert.Equal("3 values were given for 7 categories; there must be one value per category. (Parameter 'counts')", misaligned.Message);
    }

    /// <summary>A count is a number of occurrences: 0 or more and finite. A negative count has no share of a total, and an
    /// infinite one makes every other share 0; both are refused by name, naming the category.</summary>
    [Theory]
    [InlineData(-1.0, "-1")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    [InlineData(double.PositiveInfinity, "Infinity")]
    public void ACountThatIsNegativeOrInfinite_IsRefused(double bad, string written)
    {
        var error = Assert.Throws<ArgumentException>(() => Pareto(["a", "b", "c"], [5, bad, 3]));

        Assert.Equal("counts", error.ParamName);
        Assert.Equal($"The count of 'b' is {written}; a count is a finite number of occurrences, 0 or more (NaN marks a missing count). (Parameter 'counts')", error.Message);
    }

    /// <summary>The shares are of the total, so the total must be a finite number above 0: no counts, only zeros, only missing
    /// counts, or counts whose sum overflows a double are refused.</summary>
    [Theory]
    [InlineData(new double[0], "0")]
    [InlineData(new double[] { 0, 0 }, "0")]
    [InlineData(new double[] { double.NaN, double.NaN }, "0")]
    [InlineData(new double[] { double.MaxValue, double.MaxValue }, "Infinity")]
    public void ATotalThatIsNotAFiniteNumberAbove0_IsRefused(double[] counts, string written)
    {
        string[] categories = [.. counts.Select((_, i) => $"c{i}")];

        var error = Assert.Throws<ArgumentException>(() => Pareto(categories, counts));

        Assert.Equal("counts", error.ParamName);
        Assert.Equal($"The counts add up to {written}; a Pareto chart draws each count as a share of the total, which must be a finite number above 0. (Parameter 'counts')", error.Message);
    }

    /// <summary>A refused call has added nothing: no series, no second axis, no limits, no legend.</summary>
    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var builder = new AxesBuilder();

        Assert.Throws<ArgumentException>(() => builder.Pareto(["a", "b"], [1, -1]));
        Assert.Throws<ArgumentException>(() => builder.Pareto(["a", "b"], [0, 0]));
        var axes = builder.Build(1, 1, 1);

        Assert.Empty(axes.Series);
        Assert.Empty(axes.YAxes.Right);
        Assert.Null(axes.YAxis.Max);
        Assert.Null(axes.YAxis.Label);
        Assert.Equal(new Legend(), axes.Legend);
    }

    // ---- reading the picture ----------------------------------------------------------------------------------------

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>The markup of the series group with index <paramref name="seriesIndex"/>: up to the next series group or the
    /// legend, whichever comes first (the legend's swatch of the line draws a marker too).</summary>
    private static string Group(string svg, int seriesIndex)
    {
        int start = svg.IndexOf($"""<g class="series" data-series-index="{seriesIndex}" """.TrimEnd(), StringComparison.Ordinal);
        Assert.True(start >= 0, $"series group {seriesIndex} is not in the picture");
        int[] ends =
        [
            svg.IndexOf("""<g class="series" """.TrimEnd(), start + 1, StringComparison.Ordinal),
            svg.IndexOf("""<g class="legend" """.TrimEnd(), start + 1, StringComparison.Ordinal),
        ];
        int end = ends.Where(e => e > start).DefaultIfEmpty(svg.Length).Min();
        return svg[start..end];
    }

    /// <summary>A drawn bar: its left edge, its top and its width, in pixels.</summary>
    /// <param name="X">The bar's left edge.</param>
    /// <param name="Y">The bar's top.</param>
    /// <param name="Width">The bar's width.</param>
    private readonly record struct DrawnBar(double X, double Y, double Width);

    /// <summary>The bars of group 0, in the order drawn: its filled rectangles (the clip path's rectangle has no fill).</summary>
    private static DrawnBar[] Bars(string svg) =>
        [.. Regex.Matches(Group(svg, 0), """<rect x="([-0-9.eE+]+)" y="([-0-9.eE+]+)" width="([-0-9.eE+]+)" height="[-0-9.eE+]+" fill="#""")
            .Select(m => new DrawnBar(Number(m.Groups[1].Value), Number(m.Groups[2].Value), Number(m.Groups[3].Value)))];

    /// <summary>A drawn point of the line: its centre and its radius, in pixels.</summary>
    /// <param name="X">The marker's centre along the X axis.</param>
    /// <param name="Y">The marker's centre along the Y axis.</param>
    /// <param name="Radius">The marker's radius.</param>
    private readonly record struct DrawnPoint(double X, double Y, double Radius);

    /// <summary>The markers of the line (group 1), in the order drawn.</summary>
    private static DrawnPoint[] Points(string svg) =>
        [.. Regex.Matches(Group(svg, 1), """<circle cx="([-0-9.eE+]+)" cy="([-0-9.eE+]+)" r="([-0-9.eE+]+)" """.TrimEnd())
            .Select(m => new DrawnPoint(Number(m.Groups[1].Value), Number(m.Groups[2].Value), Number(m.Groups[3].Value)))];

    /// <summary>The first fill or stroke colour inside a group, upper case.</summary>
    private static string FirstColour(string group) =>
        Regex.Match(group, """(?:fill|stroke)="(#[0-9A-Fa-f]{6})" """.TrimEnd()).Groups[1].Value.ToUpperInvariant();
}
