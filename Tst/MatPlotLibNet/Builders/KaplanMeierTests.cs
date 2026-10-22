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
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The Kaplan-Meier chart in one call, the curve only: one step line per group, the product-limit estimate of the
/// group's survival (worked out in <see cref="MatPlotLibNet.Tests.Models.Series.SurvivalGroupExtensionsTests"/>), drawn
/// "steps-post" from (0, 1), each group in a colour of its own and named in the legend, the Y axis titled "Survival
/// probability". No risk table, no log-rank test, no confidence band and no censor marks: a band and censor marks are
/// several series in one colour, which the library has no way to draw yet. The arithmetic is fractions of whole numbers, so
/// the levels are asserted to 1e-15.</summary>
public class KaplanMeierTests
{
    /// <summary>The worked example: levels 1, 5/6, 1/2, 1/4 at 0, 1, 3, 5, extended to 6.</summary>
    private static readonly SurvivalGroup Worked = new("Worked", [3, 1, 3, 6, 3, 5], [false, true, true, false, true, true]);

    /// <summary>Four subjects: 4 at risk at 2 (3/4), 3 at risk at 4 with a censoring (3/4 x 2/3 = 1/2), the last event at 7 (0).</summary>
    private static readonly SurvivalGroup Second = new("Second", [2, 4, 4, 7], [true, true, false, true]);

    private static readonly SurvivalGroup[] Both = [Worked, Second];

    private const double Exact = 1e-15;

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static Axes Chart(IReadOnlyList<SurvivalGroup> groups, Action<LineSeries>? configure = null) =>
        AxesOf(ax => ax.KaplanMeier(groups, configure));

    private static LineSeries Curve(Axes axes, int group) => Assert.IsType<LineSeries>(axes.Series[group]);

    private static void AssertLevels(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(Math.Abs(expected[i] - actual[i]) <= Exact, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
        }
    }

    // ---- what the verb adds ---------------------------------------------------------------------------------------

    /// <summary>The chart is one line series per group, in the order of the groups, and nothing else: no band (a fill), no
    /// censor marks (a scatter, or markers on the line), no reference line, annotation or second axis.</summary>
    [Fact]
    public void TheChart_IsOneLineSeriesPerGroup_AndNothingElse()
    {
        var axes = Chart(Both);

        Assert.Equal(2, axes.Series.Count);
        Assert.All(axes.Series, s => Assert.IsType<LineSeries>(s));
        Assert.Equal(["Worked", "Second"], axes.Series.Select(s => s.Label));
        Assert.All(axes.Series, s => Assert.Null(((LineSeries)s).Marker));
        Assert.Empty(axes.SecondarySeries);
        Assert.Empty(axes.ReferenceLines);
        Assert.Empty(axes.Annotations);
        Assert.Empty(axes.Trendlines);
    }

    /// <summary>Each line is its group's product-limit estimate: from (0, 1), a point at every event time at the level the
    /// curve drops to, and the last follow-up when it is a censoring.</summary>
    [Fact]
    public void EachLine_IsItsGroupsProductLimitEstimate_FromZeroAtOne()
    {
        var axes = Chart(Both);

        Assert.Equal([0.0, 1, 3, 5, 6], Curve(axes, 0).XData);
        AssertLevels([1, 5.0 / 6, 1.0 / 2, 1.0 / 4, 1.0 / 4], Curve(axes, 0).YData);
        Assert.Equal([0.0, 2, 4, 7], Curve(axes, 1).XData);
        AssertLevels([1, 3.0 / 4, 1.0 / 2, 0], Curve(axes, 1).YData);
    }

    /// <summary>Each line steps "post": the level holds from a point to the next, where it drops.</summary>
    [Fact]
    public void EachLine_StepsAfterItsPoints()
    {
        Assert.All(Chart(Both).Series, s => Assert.Equal(DrawStyle.StepsPost, ((LineSeries)s).DrawStyle));
    }

    /// <summary>In the picture every stretch of a curve is level or a drop straight down: no stretch slopes, and the curve
    /// holds each level up to the next event time. The worked example's 5 points are 9 polyline points; the last stretch, to
    /// the point the level is held to, has no length (steps-post runs level to a point and then to the point itself).</summary>
    [Fact]
    public void ThePicture_IsSteps_LevelOrStraightDown()
    {
        string svg = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.KaplanMeier([Worked])).ToSvg();

        // The transform takes four points at a time with a fused multiply-add and the rest one by one, so one data value can
        // land a last bit apart in pixels: "the same" is to a billionth of a pixel.
        static bool Same(double a, double b) => Math.Abs(a - b) < 1e-9;
        var points = Polyline(svg);
        Assert.Equal(9, points.Count);
        Assert.Equal(3, Enumerable.Range(1, points.Count - 1).Count(i => points[i].Y > points[i - 1].Y));   // the drops at 1, 3, 5
        for (int i = 1; i < points.Count; i++)
        {
            bool level = Same(points[i].Y, points[i - 1].Y) && points[i].X >= points[i - 1].X;
            bool drop = Same(points[i].X, points[i - 1].X) && points[i].Y > points[i - 1].Y;
            Assert.True(level || drop, $"stretch {i} runs from ({points[i - 1].X}, {points[i - 1].Y}) to ({points[i].X}, {points[i].Y})");
        }
    }

    /// <summary>Every group wears a colour of its own from the theme's cycle (none is set by the verb), the first group the
    /// first colour.</summary>
    [Fact]
    public void EveryGroup_WearsItsOwnColour_FromTheTheme()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.KaplanMeier(Both)).Build();
        figure.EnableLegendToggle = true;
        string svg = figure.ToSvg();

        Assert.All(figure.SubPlots[0].Series, s => Assert.Null(((LineSeries)s).Color));
        Assert.Equal(figure.Theme.CycleColors[0].ToHex(), FirstColour(Group(svg, 0)));
        Assert.Equal(figure.Theme.CycleColors[1].ToHex(), FirstColour(Group(svg, 1)));
    }

    /// <summary>The groups are named in the legend, one entry each, with no call beyond the verb.</summary>
    [Fact]
    public void TheGroups_AreNamedInTheLegend()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.KaplanMeier(Both)).Build();
        figure.EnableLegendToggle = true;
        string svg = figure.ToSvg();

        Assert.Equal(2, Regex.Matches(svg, "data-legend-index=").Count);
        Assert.Contains(">Worked<", svg);
        Assert.Contains(">Second<", svg);
    }

    /// <summary>The Y axis is titled "Survival probability"; the X axis is the caller's (its unit is the durations'). A title
    /// set after the call replaces the verb's.</summary>
    [Fact]
    public void TheYAxis_IsTitledSurvivalProbability_ATitleSetAfterTheCallWins()
    {
        var axes = Chart(Both);
        var retitled = AxesOf(ax => ax.KaplanMeier(Both).SetYLabel("Share without relapse"));

        Assert.Equal("Survival probability", axes.YAxis.Label);
        Assert.Null(axes.XAxis.Label);
        Assert.Equal("Share without relapse", retitled.YAxis.Label);
    }

    /// <summary>The verb sets no axis limit (a limit clips every series and overwrites the caller's own) and keeps one the
    /// caller set.</summary>
    [Fact]
    public void TheVerb_SetsNoAxisLimits_AndKeepsTheCallersOwn()
    {
        var axes = Chart(Both);
        var kept = AxesOf(ax => ax.SetYLim(0, 1.05).KaplanMeier(Both));

        Assert.False(axes.HasExplicitLimit);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Equal(0.0, kept.YAxis.Min);
        Assert.Equal(1.05, kept.YAxis.Max);
    }

    /// <summary>A curve holds its own arrays: editing one line's data moves no other line.</summary>
    [Fact]
    public void EveryLine_HoldsItsOwnArrays()
    {
        var axes = Chart([Worked, Worked]);

        Assert.NotSame(Curve(axes, 0).XData, Curve(axes, 1).XData);
        Assert.NotSame(Curve(axes, 0).YData, Curve(axes, 1).YData);
    }

    // ---- the callback ---------------------------------------------------------------------------------------------

    /// <summary>The callback runs once per curve, in the order of the groups, after the verb has built the curve, and hands
    /// over the very series on the axes, so what it sets wins.</summary>
    [Fact]
    public void TheCallback_RunsOncePerCurve_InGroupOrder_OnTheSeriesOnTheAxes_AfterTheVerbBuiltIt()
    {
        var seen = new List<LineSeries>();
        var stylesWhenCalled = new List<DrawStyle?>();
        var labelsWhenCalled = new List<string?>();

        var axes = Chart(Both, line =>
        {
            seen.Add(line);
            stylesWhenCalled.Add(line.DrawStyle);
            labelsWhenCalled.Add(line.Label);
            line.LineWidth = 2.5;
        });

        Assert.Equal(2, seen.Count);
        Assert.Same(axes.Series[0], seen[0]);
        Assert.Same(axes.Series[1], seen[1]);
        Assert.Equal([DrawStyle.StepsPost, DrawStyle.StepsPost], stylesWhenCalled);
        Assert.Equal(["Worked", "Second"], labelsWhenCalled);
        Assert.All(axes.Series, s => Assert.Equal(2.5, ((LineSeries)s).LineWidth));
    }

    // ---- refusals -------------------------------------------------------------------------------------------------

    /// <summary>A missing argument is refused by name.</summary>
    [Fact]
    public void MissingArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).KaplanMeier(Both)).ParamName);
        Assert.Equal("groups", Assert.Throws<ArgumentNullException>(() => Chart(null!)).ParamName);
    }

    /// <summary>A chart of no groups has nothing to draw.</summary>
    [Fact]
    public void NoGroups_AreRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => Chart([]));

        Assert.Equal("groups", error.ParamName);
        Assert.Equal("No groups were given; a Kaplan-Meier chart draws one curve per group. (Parameter 'groups')", error.Message);
    }

    /// <summary>A group that is not survival data is refused through the estimator's one check, naming the group and the
    /// verb's parameter: a duration that is missing (NaN), negative or infinite, flags that do not pair with the durations, no
    /// subjects.</summary>
    [Theory]
    [InlineData(new[] { 2.0, double.NaN }, 2, "Group 'South' has a duration of NaN at index 1;")]
    [InlineData(new[] { 2.0, -3.0 }, 2, "Group 'South' has a duration of -3 at index 1;")]
    [InlineData(new[] { 2.0, 5.0 }, 3, "Group 'South' has 2 durations and 3 Observed flags;")]
    [InlineData(new double[0], 0, "Group 'South' has no subjects;")]
    public void AGroupThatIsNotSurvivalData_IsRefused_NamingTheGroup(double[] durations, int flags, string opening)
    {
        SurvivalGroup[] groups = [Worked, new("South", durations, new bool[flags])];

        var error = Assert.Throws<ArgumentException>(() => Chart(groups));

        Assert.Equal("groups", error.ParamName);
        Assert.StartsWith(opening, error.Message);
    }

    /// <summary>A refused call has added nothing: no curve, no axis title. Every group is checked before the first curve is
    /// added, so a bad second group does not leave the first one drawn.</summary>
    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var builder = new AxesBuilder();

        Assert.Throws<ArgumentException>(() => builder.KaplanMeier([Worked, new SurvivalGroup("South", [double.NaN], [true])]));
        var axes = builder.Build(1, 1, 1);

        Assert.Empty(axes.Series);
        Assert.Null(axes.YAxis.Label);
    }

    // ---- the public type ------------------------------------------------------------------------------------------

    /// <summary>A survival group is a public readonly record struct of a name, the durations and the flags, made like a
    /// <see cref="NamedSample"/>.</summary>
    [Fact]
    public void SurvivalGroup_IsAPublicReadonlyRecordStruct_OfALabelDurationsAndFlags()
    {
        var type = typeof(SurvivalGroup);

        Assert.True(type.IsValueType);
        Assert.True(type.IsPublic);
        Assert.True(type.IsDefined(typeof(IsReadOnlyAttribute)));
        var group = new SurvivalGroup("a", [1.0, 2.0], [true, false]);
        var (label, durations, observed) = group;
        Assert.Equal("a", label);
        Assert.Equal([1.0, 2.0], durations);
        Assert.Equal([true, false], observed);
        Assert.Equal(["Durations", "Label", "Observed"], type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).Order());
    }

    // ---- the hand recipe and JSON ---------------------------------------------------------------------------------------

    /// <summary>The verb draws exactly the picture of the hand recipe made of the public building blocks (the step points
    /// worked out by hand), with and without a subject lost to follow-up.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsWhatTheHandRecipeDraws(bool missing)
    {
        string byVerb = ComposedChartShapes.Named("kaplan-meier").Build(missing).ToSvg();
        string byHand = ComposedChartShapes.KaplanMeierByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    /// <summary>A Kaplan-Meier chart is its parts in JSON: read back it draws the same picture. It is not estimated again.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_SurvivesJson(bool missing)
    {
        var figure = ComposedChartShapes.Named("kaplan-meier").Build(missing).Build();

        string drawn = figure.ToSvg();
        string readBack = ChartServices.Serializer.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
    }

    // ---- the example in the documentation --------------------------------------------------------------------------------

    /// <summary>The example of the verb's documentation, as written there, compiles and draws: the placebo arm drops to 0 at
    /// week 11, its last subject's relapse; the treatment arm ends at week 34 on the level its last relapse (week 25) left.</summary>
    [Fact]
    public void TheDocumentationExample_Draws()
    {
        SurvivalGroup[] arms =
        [
            new("Treatment", [6, 7, 10, 15, 19, 25, 32, 34], [true, true, false, true, false, true, false, false]),
            new("Placebo", [1, 2, 3, 4, 5, 8, 8, 11], [true, true, true, false, true, true, true, true]),
        ];

        string svg = Plt.Create()
            .WithTitle("Time to relapse")
            .AddSubPlot(1, 1, 1, ax => ax
                .KaplanMeier(arms)
                .SetXLabel("Weeks"))
            .ToSvg();

        var axes = Chart(arms);
        var treatment = Curve(axes, 0);
        var placebo = Curve(axes, 1);
        // Treatment, 8 at risk: 7/8 at 6, x 6/7 at 7 (3/4), x 4/5 at 15 (3/5), x 2/3 at 25 (2/5), held to 34.
        Assert.Equal([0.0, 6, 7, 15, 25, 34], treatment.XData);
        AssertLevels([1, 7.0 / 8, 3.0 / 4, 3.0 / 5, 2.0 / 5, 2.0 / 5], treatment.YData);
        Assert.Equal(11.0, placebo.XData[^1]);
        Assert.Equal(0.0, placebo.YData[^1]);
        Assert.Contains(">Survival probability<", svg);
        Assert.Contains(">Weeks<", svg);
        Assert.Contains(">Placebo<", svg);
    }

    // ---- reading the picture --------------------------------------------------------------------------------------------

    private readonly record struct SvgPoint(double X, double Y);

    private static double Num(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>The points of the longest polyline of the picture: the curve (a legend swatch is two points).</summary>
    private static List<SvgPoint> Polyline(string svg) =>
        XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName == "polyline")
            .Select(e => ((string)e.Attribute("points")!).Split(' ').Select(p => p.Split(',')).Select(xy => new SvgPoint(Num(xy[0]), Num(xy[1]))).ToList())
            .OrderByDescending(p => p.Count).First();

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
