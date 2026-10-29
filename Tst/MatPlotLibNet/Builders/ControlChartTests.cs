// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The control chart in one call: the individuals chart (the X chart of an I-MR pair). The centre is the mean of the
/// values, sigma is the average moving range over d2 = 1.128, the limits stand three sigma either side of the centre, and a
/// point strictly beyond a limit is marked (Western Electric rule 1 only). Limits that are given are drawn as given. The
/// values are one line series, the violations one scatter series that takes the next colour, the centre and the limits three
/// reference lines. The arithmetic is sums, differences and divisions, so the limits are asserted exactly, by the same
/// operations in the same order.</summary>
public class ControlChartTests
{
    private static readonly double[] Days = [1, 2, 3, 4, 5];

    /// <summary>Five values: the mean is 61 / 5 = 12.2, the moving ranges 2, 1, 4, 2 average 9 / 4 = 2.25, so sigma is
    /// 2.25 / 1.128 = 1.99468... and the limits are 12.2 -/+ 5.98404... = 6.21595... and 18.18404...</summary>
    private static readonly double[] Five = [10, 12, 11, 15, 13];

    private const double FiveCenter = 12.2;
    private const double FiveUpper = 12.2 + 3 * (2.25 / 1.128);
    private const double FiveLower = 12.2 - 3 * (2.25 / 1.128);

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static Axes Chart(double[] x, double[] values, ControlLimits? limits = null, Action<ControlChartParts>? configure = null) =>
        AxesOf(ax => ax.ControlChart(x, values, limits, configure));

    private static LineSeries Values(Axes axes) => Assert.IsType<LineSeries>(axes.Series[0]);

    private static ScatterSeries Violations(Axes axes) => Assert.IsType<ScatterSeries>(axes.Series[1]);

    private static ReferenceLine Line(Axes axes, string label) => Assert.Single(axes.ReferenceLines, r => r.Label == label);

    // ---- what the verb adds ---------------------------------------------------------------------------------------

    /// <summary>The chart is one line series of the values, one scatter series of the violations, after it, and three
    /// horizontal reference lines: the centre and the two limits. Nothing else: no second axis, trendline or annotation.</summary>
    [Fact]
    public void TheChart_IsALineAScatterAndThreeReferenceLines_AndNothingElse()
    {
        var axes = Chart(Days, Five);

        Assert.Equal(2, axes.Series.Count);
        Values(axes);
        Violations(axes);
        Assert.Equal(3, axes.ReferenceLines.Count);
        Assert.All(axes.ReferenceLines, r => Assert.Equal(Orientation.Horizontal, r.Orientation));
        Assert.Empty(axes.YAxes.Right);
        Assert.Empty(axes.Trendlines);
        Assert.Empty(axes.Annotations);
    }

    /// <summary>The values are drawn in the order given, at the x positions given, as a line with a circle on each value.</summary>
    [Fact]
    public void TheValues_AreAMarkedLine_AtTheGivenPositions()
    {
        var line = Values(Chart(Days, Five));

        Assert.Equal(Days, line.XData);
        Assert.Equal(Five, line.YData);
        Assert.Equal(MarkerStyle.Circle, line.Marker);
    }

    /// <summary>The chart holds its own copy of the data: changing the caller's arrays after the call changes neither the
    /// line nor the violations worked out from it.</summary>
    [Fact]
    public void TheChart_HoldsItsOwnCopyOfTheData()
    {
        double[] x = [.. Days], values = [.. Five];
        var axes = Chart(x, values);

        x[0] = 100;
        values[0] = 100;

        Assert.Equal(Days, Values(axes).XData);
        Assert.Equal(Five, Values(axes).YData);
    }

    // ---- the estimate ---------------------------------------------------------------------------------------------

    /// <summary>The individuals chart's estimate: the centre is the mean of the values (12.2), sigma the average moving range
    /// over 1.128 (2.25 / 1.128), the limits the centre -/+ three sigma (6.2159574468085... and 18.1840425531914...).</summary>
    [Fact]
    public void TheLimits_AreTheMean_PlusMinusThreeSigma_FromTheAverageMovingRange()
    {
        var axes = Chart(Days, Five);

        Assert.Equal(FiveCenter, Line(axes, "CL").Value);
        Assert.Equal(FiveUpper, Line(axes, "UCL").Value);
        Assert.Equal(FiveLower, Line(axes, "LCL").Value);
        Assert.Equal(18.184042553191489, Line(axes, "UCL").Value, 12);
        Assert.Equal(6.2159574468085111, Line(axes, "LCL").Value, 12);
    }

    /// <summary>Two values whose moving range is d2 itself give a sigma of exactly 1: the centre is their mean, 0.564, and the
    /// limits are 0.564 -/+ 3.</summary>
    [Fact]
    public void AMovingRangeOfD2_IsASigmaOfOne()
    {
        var axes = Chart([1, 2], [0, 1.128]);

        Assert.Equal(0.564, Line(axes, "CL").Value);
        Assert.Equal(0.564 + 3, Line(axes, "UCL").Value);
        Assert.Equal(0.564 - 3, Line(axes, "LCL").Value);
    }

    /// <summary>A missing value (NaN) and an infinity are left out of the estimate: the centre is the mean of the others, and
    /// the moving range is taken between the values on either side of the gap, so the limits are those of the same values
    /// without the gaps.</summary>
    [Fact]
    public void AMissingOrInfiniteValue_IsLeftOutOfTheEstimate_TheMovingRangeSpansTheGap()
    {
        var axes = Chart([1, 2, 3, 4, 5, 6, 7], [10, double.NaN, 12, 11, double.PositiveInfinity, 15, 13]);

        Assert.Equal(FiveCenter, Line(axes, "CL").Value);
        Assert.Equal(FiveUpper, Line(axes, "UCL").Value);
        Assert.Equal(FiveLower, Line(axes, "LCL").Value);
    }

    /// <summary>Limits that are given are drawn as given: nothing is estimated (the centre is not the mean of these values).</summary>
    [Fact]
    public void GivenLimits_AreDrawnAsGiven_NothingIsEstimated()
    {
        var axes = Chart(Days, Five, new ControlLimits(Center: 11, Lower: 9.5, Upper: 14));

        Assert.Equal(11, Line(axes, "CL").Value);
        Assert.Equal(9.5, Line(axes, "LCL").Value);
        Assert.Equal(14, Line(axes, "UCL").Value);
    }

    /// <summary>With limits given, the values need no spread to estimate from: one value, or none, is a chart.</summary>
    [Fact]
    public void GivenLimits_DrawFewerThanTwoValues()
    {
        var limits = new ControlLimits(10, 5, 15);

        Assert.Single(Values(Chart([1], [7], limits)).YData);
        Assert.Empty(Values(Chart([], [], limits)).YData);
        Assert.Empty(Violations(Chart([], [], limits)).YData);
    }

    // ---- the violations ---------------------------------------------------------------------------------------------

    /// <summary>Rule 1: a value strictly beyond a limit is a violation, and the violations are those points, in order. A value
    /// ON a limit is not; a missing value and an infinity are never a violation.</summary>
    [Fact]
    public void AValueStrictlyBeyondALimit_IsAViolation_OneOnALimitIsNot()
    {
        var axes = Chart([1, 2, 3, 4, 5, 6, 7, 8], [5, 4.9, 15, 15.1, 10, double.NaN, double.PositiveInfinity, double.NegativeInfinity],
            new ControlLimits(10, 5, 15));

        var violations = Violations(axes);
        Assert.Equal([2.0, 4.0], violations.XData);
        Assert.Equal([4.9, 15.1], violations.YData);
    }

    /// <summary>Values all between their limits have no violation: the scatter series is there and empty.</summary>
    [Fact]
    public void ValuesBetweenTheLimits_HaveNoViolation()
    {
        var violations = Violations(Chart(Days, Five));

        Assert.Empty(violations.XData);
        Assert.Empty(violations.YData);
    }

    /// <summary>A spike in a steady series is beyond the estimated limits: the moving range barely notices one spike, which is
    /// why the individuals chart takes sigma from it and not from the standard deviation.</summary>
    [Fact]
    public void ASpikeInASteadySeries_IsBeyondTheEstimatedLimits()
    {
        double[] values = [10, 10.5, 10, 10.5, 10, 14, 10, 10.5, 10, 10.5];
        double[] x = [.. Enumerable.Range(1, values.Length).Select(i => (double)i)];

        var violations = Violations(Chart(x, values));

        Assert.Equal([6.0], violations.XData);
        Assert.Equal([14.0], violations.YData);
    }

    // ---- how it is drawn ----------------------------------------------------------------------------------------------

    /// <summary>The centre is a solid reference line, the limits dashed ones, each named at its right end, and none has a colour
    /// of its own: they take the colour a reference line has when none is set.</summary>
    [Fact]
    public void TheCentreIsSolid_TheLimitsDashed_NamedAndWithoutAColourOfTheirOwn()
    {
        var axes = Chart(Days, Five);

        Assert.Equal(LineStyle.Solid, Line(axes, "CL").LineStyle);
        Assert.Equal(LineStyle.Dashed, Line(axes, "UCL").LineStyle);
        Assert.Equal(LineStyle.Dashed, Line(axes, "LCL").LineStyle);
        Assert.All(axes.ReferenceLines, r => Assert.Null(r.Color));
    }

    /// <summary>The line takes the theme's first colour and the violations, which have no colour of their own, the second, so a
    /// violation is a larger dot of the next colour over the line's marker.</summary>
    [Fact]
    public void TheLineTakesTheFirstColour_TheViolationsTheSecond()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ControlChart(Days, Five, new ControlLimits(12, 11, 13))).Build();
        figure.EnableLegendToggle = true;
        string svg = figure.ToSvg();

        var axes = figure.SubPlots[0];
        Assert.Null(Values(axes).Color);
        Assert.Null(Violations(axes).Color);
        Assert.Equal(figure.Theme.CycleColors[0].ToHex(), FirstColour(Group(svg, 0)));
        Assert.Equal(figure.Theme.CycleColors[1].ToHex(), FirstColour(Group(svg, 1)));
    }

    /// <summary>The limits are drawn inside the plot when every value lies between them: the automatic range holds them. The
    /// verb sets no axis limit.</summary>
    [Fact]
    public void TheLimits_AreDrawnInsideThePlot_WhenEveryValueLiesBetweenThem()
    {
        string svg = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.ControlChart(Days, Five)).ToSvg();

        var plot = PlotArea(svg);
        var grey = GreyLines(svg);
        Assert.Equal(3, grey.Count);
        foreach (var line in grey)
        {
            Assert.InRange(line.Y1, plot.Y + 1, plot.Bottom - 1);
        }
    }

    /// <summary>On a dark theme the reference lines are visible: they are drawn in the default grey, which stands at a contrast
    /// of 3.49 : 1 against the dark theme's plot background (WCAG asks 3 : 1 of a graphic).</summary>
    [Fact]
    public void OnADarkTheme_TheReferenceLines_AreVisible()
    {
        string svg = Plt.Create().WithTheme(Theme.Dark).WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.ControlChart(Days, Five)).ToSvg();

        var grey = GreyLines(svg);
        Assert.Equal(3, grey.Count);
        Assert.True(Colors.Gray.ContrastRatioWith(Theme.Dark.AxesBackground) >= 3.0,
            $"contrast {Colors.Gray.ContrastRatioWith(Theme.Dark.AxesBackground):F2} against {Theme.Dark.AxesBackground.ToHex()}");
    }

    /// <summary>The verb sets no axis limit and keeps one the caller set (W9).</summary>
    [Fact]
    public void TheVerb_SetsNoAxisLimits_AndKeepsTheCallersOwn()
    {
        var axes = Chart(Days, Five);
        var kept = AxesOf(ax => ax.SetYLim(0, 30).ControlChart(Days, Five));

        Assert.False(axes.HasExplicitLimit);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Equal(0.0, kept.YAxis.Min);
        Assert.Equal(30.0, kept.YAxis.Max);
    }

    /// <summary>A missing value breaks the line and reaches neither the picture nor the violations.</summary>
    [Fact]
    public void AMissingValue_BreaksTheLine_AndNeverReachesThePicture()
    {
        string svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ControlChart([1, 2, 3, 4, 5, 6, 7], [10, 12, double.NaN, 11, 15, double.PositiveInfinity, 13])).ToSvg();

        Assert.DoesNotContain("NaN", svg);
        Assert.DoesNotContain("Infinity", svg);
    }

    // ---- refusals -----------------------------------------------------------------------------------------------------

    /// <summary>A missing argument is refused by name.</summary>
    [Fact]
    public void MissingArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).ControlChart(Days, Five)).ParamName);
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => Chart(null!, Five)).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => Chart(Days, null!)).ParamName);
    }

    /// <summary>The values pair with the x positions one to one; arrays of different lengths are refused, naming both counts.</summary>
    [Fact]
    public void ValuesThatDoNotPairWithThePositions_AreRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => Chart([1, 2, 3, 4], [10, 12, 11]));

        Assert.Equal("values", error.ParamName);
        Assert.Equal("3 values were given for 4 x positions; there must be one value per x position. (Parameter 'values')", error.Message);
    }

    /// <summary>Without limits, the estimate needs two values that can be drawn (a moving range is the distance between two);
    /// fewer are refused, naming the parameter.</summary>
    [Theory]
    [InlineData(new double[0], 0)]
    [InlineData(new[] { 4.2 }, 1)]
    [InlineData(new[] { double.NaN, 4.2 }, 1)]
    [InlineData(new[] { double.PositiveInfinity, double.NaN }, 0)]
    public void FewerThanTwoValues_WithoutLimits_AreRefused(double[] values, int drawable)
    {
        double[] x = [.. Enumerable.Range(1, values.Length).Select(i => (double)i)];

        var error = Assert.Throws<ArgumentException>(() => Chart(x, values));

        Assert.Equal("values", error.ParamName);
        string count = drawable == 1 ? "1 observation" : $"{drawable} observations";
        Assert.Equal($"The sample has {count} that can be drawn; the limits of a control chart are estimated from 2 or more (NaN and the infinities are left out). (Parameter 'values')", error.Message);
    }

    /// <summary>Limits are three finite numbers with the lower at or below the centre and the centre at or below the upper;
    /// anything else is refused, naming the parameter.</summary>
    [Theory]
    [InlineData(double.NaN, 5, 15)]
    [InlineData(10, double.NegativeInfinity, 15)]
    [InlineData(10, 5, double.PositiveInfinity)]
    [InlineData(10, 11, 15)]
    [InlineData(10, 5, 9)]
    [InlineData(10, 15, 5)]
    public void LimitsThatAreNotOrdered_OrNotFinite_AreRefused(double center, double lower, double upper)
    {
        var error = Assert.Throws<ArgumentException>(() => Chart(Days, Five, new ControlLimits(center, lower, upper)));

        Assert.Equal("limits", error.ParamName);
        Assert.StartsWith("Control limits are three finite numbers, Lower <= Center <= Upper;", error.Message);
    }

    /// <summary>Limits that coincide are ordered: a process held to one value.</summary>
    [Fact]
    public void LimitsThatCoincide_AreAccepted()
    {
        var axes = Chart(Days, Five, new ControlLimits(12, 12, 12));

        Assert.Equal(4, Violations(axes).YData.Length);
    }

    /// <summary>A refused call has added nothing: no series, no reference line.</summary>
    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var builder = new AxesBuilder();

        Assert.Throws<ArgumentException>(() => builder.ControlChart([1], [1.0]));
        Assert.Throws<ArgumentException>(() => builder.ControlChart(Days, Five, new ControlLimits(10, 15, 5)));
        var axes = builder.Build(1, 1, 1);

        Assert.Empty(axes.Series);
        Assert.Empty(axes.ReferenceLines);
    }

    // ---- the callback ---------------------------------------------------------------------------------------------------

    /// <summary>The callback runs once, after the verb has built and styled the parts, and hands over the very series and
    /// reference lines on the axes, so what it sets wins.</summary>
    [Fact]
    public void TheCallback_RunsOnce_OnThePartsOnTheAxes_AfterTheVerbStyledThem()
    {
        int calls = 0;
        ControlChartParts seen = default;
        LineStyle? centreStyleWhenCalled = null;
        var red = Color.FromHex("#D62728");

        var axes = Chart(Days, Five, configure: parts =>
        {
            calls++;
            seen = parts;
            centreStyleWhenCalled = parts.Center.LineStyle;
            parts.Values.Label = "fill weight";
            parts.Violations.Color = red;
            parts.Upper.Color = red;
            parts.Lower.Label = null;
        });

        Assert.Equal(1, calls);
        Assert.Equal(LineStyle.Solid, centreStyleWhenCalled);
        Assert.Same(axes.Series[0], seen.Values);
        Assert.Same(axes.Series[1], seen.Violations);
        Assert.Same(axes.ReferenceLines.Single(r => r.Label == "CL"), seen.Center);
        Assert.Same(axes.ReferenceLines.Single(r => r.Label == "UCL"), seen.Upper);
        Assert.Contains(seen.Lower, axes.ReferenceLines);
        Assert.Equal(FiveLower, seen.Lower.Value);
        Assert.Equal("fill weight", Values(axes).Label);
        Assert.Equal(red, Violations(axes).Color);
        Assert.Equal(red, seen.Upper.Color);
        Assert.Null(seen.Lower.Label);
    }

    /// <summary>The parts are a public readonly record struct a caller cannot make: no public constructor, no setters.</summary>
    [Fact]
    public void TheParts_AreAReadOnlyRecordStruct_WithNoPublicConstructor()
    {
        var type = typeof(ControlChartParts);

        Assert.True(type.IsValueType);
        Assert.True(type.IsPublic);
        Assert.True(type.IsDefined(typeof(IsReadOnlyAttribute)));
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.NotEmpty(type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance));
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.Equal(["Center", "Lower", "Upper", "Values", "Violations"], properties.Select(p => p.Name).Order());
        Assert.All(properties, p => Assert.Null(p.SetMethod));
    }

    /// <summary>The limits a caller gives are a public readonly record struct of three named numbers, spelt as the public API
    /// spells a centre.</summary>
    [Fact]
    public void TheLimits_AreAReadOnlyRecordStruct_OfCenterLowerAndUpper()
    {
        var limits = new ControlLimits(Center: 50, Lower: 44, Upper: 56);
        var (center, lower, upper) = limits;

        Assert.True(typeof(ControlLimits).IsDefined(typeof(IsReadOnlyAttribute)));
        Assert.Equal((50.0, 44.0, 56.0), (center, lower, upper));
        Assert.Equal(limits, new ControlLimits(50, 44, 56));
    }

    // ---- the hand recipe and JSON ---------------------------------------------------------------------------------------

    /// <summary>The verb draws exactly the picture of the hand recipe made of the public building blocks, with and without a
    /// missing value.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsWhatTheHandRecipeDraws(bool missing)
    {
        string byVerb = ComposedChartShapes.Named("control-chart").Build(missing).ToSvg();
        string byHand = ComposedChartShapes.ControlChartByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    /// <summary>A control chart is its parts in JSON: read back it draws the same picture, with and without a missing value.
    /// It is not estimated again.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_SurvivesJson(bool missing)
    {
        var figure = ComposedChartShapes.Named("control-chart").Build(missing).Build();

        string drawn = figure.ToSvg();
        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
    }

    // ---- the example in the documentation --------------------------------------------------------------------------------

    /// <summary>The example of the verb's documentation, as written there, compiles and draws: the seventh day (53.9 g) is
    /// beyond its upper limit (50.44 + 3 x 1.1222 / 1.128 = 53.42 g).</summary>
    [Fact]
    public void TheDocumentationExample_Draws()
    {
        double[] day = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] fill = [50.2, 49.8, 50.1, 50.4, 49.9, 50.0, 53.9, 50.3, 49.7, 50.1];

        ControlChartParts chart = default;
        string svg = Plt.Create()
            .WithTitle("Fill weight")
            .AddSubPlot(1, 1, 1, ax => ax
                .ControlChart(day, fill, configure: parts => { parts.Violations.Label = "beyond a limit"; chart = parts; })
                .SetXLabel("Day")
                .SetYLabel("Weight (g)"))
            .ToSvg();

        Assert.Equal([7.0], chart.Violations.XData);
        Assert.Equal(53.42, chart.Upper.Value, 2);
        Assert.Contains(">UCL<", svg);
        Assert.Contains(">Weight (g)<", svg);
    }

    // ---- reading the picture --------------------------------------------------------------------------------------------

    private readonly record struct SvgLine(double X1, double Y1, double X2, double Y2);

    private static double Num(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>The lines drawn in the default reference-line grey.</summary>
    private static List<SvgLine> GreyLines(string svg) =>
        [.. XDocument.Parse(svg).Descendants()
            .Where(e => e.Name.LocalName == "line" && (string?)e.Attribute("stroke") == Colors.Gray.ToHex())
            .Select(e => new SvgLine(Num((string)e.Attribute("x1")!), Num((string)e.Attribute("y1")!),
                                     Num((string)e.Attribute("x2")!), Num((string)e.Attribute("y2")!)))];

    /// <summary>The plot area: the second rectangle drawn into the figure, after the figure's own background.</summary>
    private static Rect PlotArea(string svg)
    {
        var rect = XDocument.Parse(svg).Root!.Elements().Where(e => e.Name.LocalName == "rect").Skip(1).First();
        return new(Num((string)rect.Attribute("x")!), Num((string)rect.Attribute("y")!),
                   Num((string)rect.Attribute("width")!), Num((string)rect.Attribute("height")!));
    }

    /// <summary>The markup of the series group with index <paramref name="seriesIndex"/>: up to the next series group or the
    /// end of the picture.</summary>
    private static string Group(string svg, int seriesIndex)
    {
        int start = svg.IndexOf($"""<g class="series" data-series-index="{seriesIndex}" """.TrimEnd(), StringComparison.Ordinal);
        Assert.True(start >= 0, $"series group {seriesIndex} is not in the picture");
        int next = svg.IndexOf("""<g class="series" """.TrimEnd(), start + 1, StringComparison.Ordinal);
        return svg[start..(next > start ? next : svg.Length)];
    }

    /// <summary>The first fill or stroke colour in a piece of markup.</summary>
    private static string FirstColour(string markup) =>
        Regex.Match(markup, """(?:fill|stroke)="(#[0-9A-F]{6})" """.TrimEnd()).Groups[1].Value;
}
