// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The Kaplan-Meier chart in one call: one step line per group, the product-limit estimate of the group's survival
/// (worked out in <see cref="MatPlotLibNet.Tests.Models.Series.SurvivalGroupExtensionsTests"/>), drawn "steps-post" from
/// (0, 1); a Greenwood confidence band around it; and a censor mark at every time a subject left without an event. The
/// curve, its band and its marks share one colour group, so all three wear the same colour and only the curve has a legend
/// entry — the Y axis titled "Survival probability". No risk table and no log-rank test: a risk table is a table of
/// counts and a log-rank test a statistic, neither a mark of this curve, so <c>RiskTable</c> draws them on an
/// axes of its own under it. The arithmetic is fractions of whole numbers except
/// the band's standard error (one <see cref="Math.Sqrt"/>, which every platform computes to the same bit), so the levels
/// are asserted to 1e-15 and the band to 1e-12.</summary>
public class KaplanMeierTests
{
    /// <summary>The worked example: levels 1, 5/6, 1/2, 1/4 at 0, 1, 3, 5, extended to 6.</summary>
    private static readonly SurvivalGroup Worked = new("Worked", [3, 1, 3, 6, 3, 5], [false, true, true, false, true, true]);

    /// <summary>Four subjects: 4 at risk at 2 (3/4), 3 at risk at 4 with a censoring (3/4 x 2/3 = 1/2), the last event at 7 (0).</summary>
    private static readonly SurvivalGroup Second = new("Second", [2, 4, 4, 7], [true, true, false, true]);

    private static readonly SurvivalGroup[] Both = [Worked, Second];

    private const double Exact = 1e-15;

    private static Axes Chart(IReadOnlyList<SurvivalGroup> groups, Action<LineSeries>? configure = null) =>
        Plt.Create().OneAxes(ax => ax.KaplanMeier(groups, configure));

    private static LineSeries Curve(Axes axes, int group) => axes.Series.OfType<LineSeries>().ElementAt(group);

    private static void AssertLevels(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(Math.Abs(expected[i] - actual[i]) <= Exact, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
        }
    }

    // ---- what the verb adds ---------------------------------------------------------------------------------------

    /// <summary>The chart is, for each group in order: its curve (a line), its band (a fill) and its censor marks (a
    /// scatter) — both groups of <see cref="Both"/> have at least one censored subject, so neither skips its marks.
    /// No reference line, annotation or second axis; only the curve carries a label, so the band and marks never add a
    /// legend entry of their own.</summary>
    [Fact]
    public void TheChart_IsTheCurveItsBandAndItsCensorMarks_PerGroup()
    {
        var axes = Chart(Both);

        Assert.Equal(6, axes.Series.Count);
        Type[] expectedTypes = [typeof(LineSeries), typeof(AreaSeries), typeof(ScatterSeries),
            typeof(LineSeries), typeof(AreaSeries), typeof(ScatterSeries)];
        Assert.Equal(expectedTypes, axes.Series.Select(s => s.GetType()));
        Assert.Equal(["Worked", null, null, "Second", null, null], axes.Series.Select(s => s.Label));
        Assert.All(axes.Series.OfType<LineSeries>(), s => Assert.Null(s.Marker));
        Assert.All(axes.Series.OfType<ScatterSeries>(), s => Assert.Equal(MarkerStyle.Plus, s.Marker));
        Assert.Empty(axes.YAxes.Right);
        Assert.Empty(axes.ReferenceLines);
        Assert.Empty(axes.Annotations);
        Assert.Empty(axes.Trendlines);
    }

    /// <summary>A group with no censored subject has a curve and a band, but no (empty) marks series.</summary>
    [Fact]
    public void AGroupWithNoCensoring_HasNoMarksSeries()
    {
        var allEvents = new SurvivalGroup("AllEvents", [2, 4, 4, 7], [true, true, true, true]);

        var axes = Chart([allEvents]);

        Assert.Equal(2, axes.Series.Count);
        Assert.IsType<LineSeries>(axes.Series[0]);
        Assert.IsType<AreaSeries>(axes.Series[1]);
    }

    /// <summary>The curve, its band and its marks are one colour group: they wear the same colour and the group after
    /// them takes the next colour, not the fourth.</summary>
    [Fact]
    public void TheCurveItsBandAndItsMarks_ShareOneColourGroup()
    {
        var axes = Chart(Both);
        var worked = axes.Series.Take(3).ToList();
        var second = axes.Series.Skip(3).Take(3).ToList();

        Assert.All(worked, s => Assert.Equal(0, ((ChartSeries)s).ColorGroup));
        Assert.All(second, s => Assert.Equal(1, ((ChartSeries)s).ColorGroup));
    }

    /// <summary>Each band is a fill between Greenwood's lower and upper bound around its curve, over the same times:
    /// S(t) ± 1.959963984540054 x sqrt(S(t)^2 x sum(d_i / (n_i(n_i - d_i)))), clamped to [0, 1] — plain Greenwood, no
    /// log or log-log transform, so it costs one <see cref="Math.Sqrt"/> and stays bit-exact across platforms.</summary>
    [Fact]
    public void TheBand_IsGreenwoodsConfidenceInterval_AroundTheCurve()
    {
        var axes = Chart([Worked]);
        var band = Assert.IsType<AreaSeries>(axes.Series[1]);
        var curve = Curve(axes, 0);

        // worked example: at risk [6,6,5,2,1], events [0,1,2,1,0], survival [1, 5/6, 1/2, 1/4, 1/4].
        double[] cumulative = [0, 1.0 / (6 * 5), 1.0 / (6 * 5) + 2.0 / (5 * 3), 1.0 / (6 * 5) + 2.0 / (5 * 3) + 1.0 / (2 * 1), 1.0 / (6 * 5) + 2.0 / (5 * 3) + 1.0 / (2 * 1)];
        double[] expectedUpper = new double[5];
        double[] expectedLower = new double[5];
        for (int i = 0; i < 5; i++)
        {
            double se = Math.Sqrt(curve.YData[i] * curve.YData[i] * cumulative[i]);
            expectedUpper[i] = Math.Clamp(curve.YData[i] + 1.959963984540054 * se, 0, 1);
            expectedLower[i] = Math.Clamp(curve.YData[i] - 1.959963984540054 * se, 0, 1);
        }

        Assert.Equal(curve.XData, band.XData);
        for (int i = 0; i < 5; i++)
        {
            Assert.True(Math.Abs(expectedUpper[i] - band.YData[i]) <= 1e-12, $"upper[{i}]");
            Assert.True(Math.Abs(expectedLower[i] - band.YData2![i]) <= 1e-12, $"lower[{i}]");
        }
    }

    /// <summary>A censor mark stands at the curve's level at that moment, at every time a subject left without an
    /// event: the worked example censors at 3 (events are counted first, so the level there is already the one the
    /// two events at 3 dropped it to: 1/2) and at 6 (the held extension, 1/4).</summary>
    [Fact]
    public void ACensorMark_StandsAtTheCurvesLevel_AtTheTimeItLeft()
    {
        var axes = Chart([Worked]);
        var marks = Assert.IsType<ScatterSeries>(axes.Series[2]);

        Assert.Equal([3.0, 6], marks.XData);
        AssertLevels([1.0 / 2, 1.0 / 4], marks.YData);
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
        Assert.All(Chart(Both).Series.OfType<LineSeries>(), s => Assert.Equal(DrawStyle.StepsPost, s.DrawStyle));
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

        Assert.All(figure.SubPlots[0].Series.OfType<LineSeries>(), s => Assert.Null(s.Color));
        Assert.Equal(figure.Theme.CycleColors[0].ToHex(), svg.SeriesGroup(0).FirstColour());    // Worked's curve
        Assert.Equal(figure.Theme.CycleColors[1].ToHex(), svg.SeriesGroup(3).FirstColour());    // Second's curve
    }

    /// <summary>The groups are named in the legend, one entry each, with no call beyond the verb.</summary>
    [Fact]
    public void TheGroups_AreNamedInTheLegend()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.KaplanMeier(Both)).Build();
        figure.EnableLegendToggle = true;
        string svg = figure.ToSvg();

        Assert.Equal(2, svg.CountOf("data-legend-index="));
        Assert.Contains(">Worked<", svg);
        Assert.Contains(">Second<", svg);
    }

    /// <summary>The Y axis is titled "Survival probability"; the X axis is the caller's (its unit is the durations'). A title
    /// set after the call replaces the verb's.</summary>
    [Fact]
    public void TheYAxis_IsTitledSurvivalProbability_ATitleSetAfterTheCallWins()
    {
        var axes = Chart(Both);
        var retitled = Plt.Create().OneAxes(ax => ax.KaplanMeier(Both).SetYLabel("Share without relapse"));

        Assert.Equal("Survival probability", axes.YAxis.Label);
        Assert.Null(axes.XAxis.Label);
        Assert.Equal("Share without relapse", retitled.YAxis.Label);
    }

    /// <summary>A curve holds its own arrays: editing one line's data moves no other line.</summary>
    [Fact]
    public void EveryLine_HoldsItsOwnArrays()
    {
        var axes = Chart([Worked, Worked]);

        Assert.NotSame(Curve(axes, 0).XData, Curve(axes, 1).XData);
        Assert.NotSame(Curve(axes, 0).YData, Curve(axes, 1).YData);
    }

    // ---- refusals -------------------------------------------------------------------------------------------------

    /// <summary>A group that is not survival data is refused through the estimator's one check, naming the group and the
    /// verb's parameter: a duration that is missing (NaN), negative or infinite, flags that do not pair with the durations, no
    /// subjects.</summary>
    [Theory]
    [InlineData(new[] { 2.0, double.NaN }, 2, "The duration at index 1 of group 'South' must be finite (got NaN).")]
    [InlineData(new[] { 2.0, -3.0 }, 2, "The duration at index 1 of group 'South' must be >= 0 (got -3).")]
    [InlineData(new double[0], 0, "Group 'South' has no subjects;")]
    public void AGroupThatIsNotSurvivalData_IsRefused_NamingTheGroup(double[] durations, int flags, string opening)
    {
        SurvivalGroup[] groups = [Worked, new("South", durations, new bool[flags])];

        var error = Assert.ThrowsAny<ArgumentException>(() => Chart(groups));

        Assert.Equal("groups", error.ParamName);
        Assert.StartsWith(opening, error.Message);
    }

    /// <summary>Flags that do not pair with the durations are refused in the one sentence of every length that differs, naming the group.</summary>
    [Fact]
    public void AGroupWhoseFlagsDoNotPairWithItsDurations_IsRefusedInTheLengthSentence()
    {
        SurvivalGroup[] groups = [Worked, new("South", [2.0, 5.0], new bool[3])];

        var error = Assert.ThrowsAny<ArgumentException>(() => Chart(groups));

        Assert.Equal("groups", error.ParamName);
        Assert.StartsWith(3.RefusalAgainst(2, of: "group 'South'"), error.Message);
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

    /// <summary>The points of the longest polyline of the picture: the curve (a legend swatch is two points).</summary>
    private static List<SvgPoint> Polyline(string svg) =>
        svg.Each("polyline")
            .Select(e => ((string)e.Attribute("points")!).Split(' ').Select(p => p.Split(',')).Select(xy => new SvgPoint(xy[0].Number(), xy[1].Number())).ToList())
            .OrderByDescending(p => p.Count).First();}
