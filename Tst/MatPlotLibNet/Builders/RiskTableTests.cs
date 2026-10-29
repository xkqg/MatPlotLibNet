// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The risk table under a Kaplan-Meier chart, in one call: a row for each group, and in it, at each time, the number
/// of subjects still at risk (followed to that time or longer: the count the survival curve is estimated with, R's
/// <c>n.risk</c>); and the log-rank test of whether the groups' survival differs, as one line of text. The table is an axes
/// of its own that the caller stacks under the survival axes with <c>ShareX</c>, so a count stands directly under its time.
/// The test statistics are lifelines' (0.30.3, <c>multivariate_logrank_test</c>).</summary>
public class RiskTableTests
{
    private static readonly SurvivalGroup Treatment = new("Treatment", [6, 7, 10, 15, 19, 25, 32, 34], [true, true, false, true, false, true, false, false]);

    private static readonly SurvivalGroup Placebo = new("Placebo", [1, 2, 3, 4, 5, 8, 8, 11], [true, true, true, false, true, true, true, true]);

    private static readonly SurvivalGroup[] Arms = [Treatment, Placebo];

    /// <summary>Freireich's leukaemia trial, 6-MP against placebo (R's <c>survdiff</c>: chi-square 16.8 on 1 degree of freedom).</summary>
    private static readonly SurvivalGroup[] Freireich =
    [
        new("6-MP", [6.0, 6.0, 6.0, 6.0, 7.0, 9.0, 10.0, 10.0, 11.0, 13.0, 16.0, 17.0, 19.0, 20.0, 22.0, 23.0, 25.0, 32.0, 32.0, 34.0, 35.0], [true, true, true, false, true, false, true, false, false, true, true, false, false, false, true, true, false, false, false, false, false]),
        new("Placebo", [1.0, 1.0, 2.0, 2.0, 3.0, 4.0, 4.0, 5.0, 5.0, 8.0, 8.0, 8.0, 8.0, 11.0, 11.0, 12.0, 12.0, 15.0, 17.0, 22.0, 23.0], [true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true]),
    ];

    private static readonly SurvivalGroup[] Three =
    [
        new("G1", [5.0, 10.0, 7.0, 4.0, 4.0, 8.0, 8.0, 10.0], [true, true, true, true, true, true, false, true]),
        new("G2", [12.0, 8.0, 11.0, 7.0, 4.0, 6.0, 9.0, 9.0, 8.0], [true, true, false, true, true, false, true, true, false]),
        new("G3", [1.0, 2.0, 1.0, 1.0, 7.0, 8.0, 8.0], [true, false, false, true, true, true, true]),
    ];

    private static Axes Table(IReadOnlyList<SurvivalGroup> groups, double[]? times = null, Action<RiskTableParts>? configure = null) =>
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.RiskTable(groups, times, configure)).Build().SubPlots[0];

    private static string[] Counts(Axes axes) => [.. axes.Annotations.Where(a => a.Coordinates == AnnotationCoordinates.Data).Select(a => a.Text)];

    private static Annotation TestLine(Axes axes) => Assert.Single(axes.Annotations, a => a.Coordinates == AnnotationCoordinates.AxesFraction);

    /// <summary>The survival chart with its risk table stacked under it, sharing the X axis, as the documentation builds it.</summary>
    private static FigureBuilder Stacked(IReadOnlyList<SurvivalGroup> groups, double[]? times = null, bool tight = false)
    {
        var figure = Plt.Create().WithSize(800, 600).WithGridSpec(2, 1, heightRatios: [4, 1])
            .AddSubPlot(GridPosition.Single(0, 0), ax => ax.KaplanMeier(groups), key: "survival")
            .AddSubPlot(GridPosition.Single(1, 0), ax => ax.ShareX("survival").RiskTable(groups, times));
        return tight ? figure.TightLayout() : figure;
    }

    // ---- the counts -------------------------------------------------------------------------------------------------------

    /// <summary>At each time, each group's count is the number of subjects followed to that time or longer: worked out by hand
    /// from the durations. Treatment (6 7 10 15 19 25 32 34) has 8, 6, 3 and 2 at risk at 0, 10, 20 and 30; Placebo
    /// (1 2 3 4 5 8 8 11) has 8, 1, 0 and 0.</summary>
    [Fact]
    public void ARiskTable_CountsTheSubjectsAtRisk_AtEachTime()
    {
        var axes = Table(Arms, [0, 10, 20, 30]);

        Assert.Equal(["8", "6", "3", "2", "8", "1", "0", "0"], Counts(axes));
    }

    /// <summary>A subject whose follow-up ended exactly at the time is still at risk at it, whether the event happened or it
    /// was censored there (the count the curve itself is estimated with): Placebo's last subject, with the event at 11, is
    /// at risk at 11 and not at 12; Treatment's last subject, censored at 34, is at risk at 34.</summary>
    [Fact]
    public void ASubjectWhoseFollowUpEndsAtTheTime_IsStillAtRiskAtIt()
    {
        var axes = Table(Arms, [11, 12, 34, 35]);

        Assert.Equal(["5", "5", "1", "0", "1", "0", "0", "0"], Counts(axes));
    }

    /// <summary>A count stands where it is read: on the time along the X axis (the annotation's X is the time, in data
    /// units) and on the row of its group along the Y axis, the first group on the top row. The groups take the rows from
    /// the top, below which the test has the last.</summary>
    [Fact]
    public void ACount_StandsAtItsTime_OnTheRowOfItsGroup_TheFirstGroupOnTop()
    {
        var axes = Table(Arms, [0, 10]);

        var counts = axes.Annotations.Where(a => a.Coordinates == AnnotationCoordinates.Data).ToArray();
        Assert.Equal([0.0, 10.0, 0.0, 10.0], counts.Select(a => a.X));
        Assert.Equal([2.5, 2.5, 1.5, 1.5], counts.Select(a => a.Y));
        Assert.All(counts, a => Assert.Equal(TextVerticalAlignment.Center, a.VerticalAlignment));
        Assert.All(counts.Where(a => a.X > 0), a => Assert.Equal(TextAlignment.Center, a.Alignment));
    }

    /// <summary>A count at time 0 (or before) stands at the left edge of the plot, where half of a centred number would run into
    /// the names of the rows and read as part of one ("Treatment 8"): it stands just inside the plot, left-aligned. Every later
    /// count is centred on its time.</summary>
    [Fact]
    public void ACountAtTimeZero_StandsJustInsideThePlot_LeftAligned()
    {
        var axes = Table(Arms, [0, 10]);

        var counts = axes.Annotations.Where(a => a.Coordinates == AnnotationCoordinates.Data).ToArray();
        var atZero = counts.Where(a => a.X == 0).ToArray();
        Assert.Equal(2, atZero.Length);
        Assert.All(atZero, a =>
        {
            Assert.Equal(TextAlignment.Left, a.Alignment);
            Assert.Equal(3.0, a.OffsetX);
        });
        Assert.All(counts.Where(a => a.X == 10), a =>
        {
            Assert.Equal(TextAlignment.Center, a.Alignment);
            Assert.Equal(0.0, a.OffsetX);
        });
    }

    /// <summary>The names of the rows are the Y axis' ticks, one on each row of a group; the slot below the rows, which holds the
    /// test, has no tick (a mark with no name would read as a stray dash).</summary>
    [Fact]
    public void TheSlotOfTheTest_HasNoTick()
    {
        var axes = Table(Arms, [0]);

        Assert.Equal([2.5, 1.5], axes.YAxis.TickLocator!.Locate(0, 3));
        Assert.Equal("Treatment", axes.YAxis.TickFormatter!.Format(2.5));
        Assert.Equal("Placebo", axes.YAxis.TickFormatter!.Format(1.5));
    }

    /// <summary>Without times the table is read at the ticks the automatic locator gives on 0 to the longest follow-up: for
    /// 34 weeks every 5 (0, 5 ... 30), for the 35 of the Freireich trial every 5 up to 35.</summary>
    [Fact]
    public void WithoutTimes_TheTableIsReadAtTheTicksOfZeroToTheLongestFollowUp()
    {
        var arms = Table(Arms);
        var freireich = Table(Freireich);

        Assert.Equal([0.0, 5, 10, 15, 20, 25, 30], arms.Annotations.Where(a => a.Coordinates == AnnotationCoordinates.Data).Take(7).Select(a => a.X));
        Assert.Equal(14, Counts(arms).Length);
        Assert.Equal([0.0, 5, 10, 15, 20, 25, 30, 35], freireich.Annotations.Where(a => a.Coordinates == AnnotationCoordinates.Data).Take(8).Select(a => a.X));
    }

    /// <summary>When nobody was followed for any time there is one time to read: 0.</summary>
    [Fact]
    public void WhenNobodyWasFollowedForAnyTime_TheTableIsReadAtZero()
    {
        var axes = Table([new("Instant", [0, 0, 0], [true, false, true])]);

        Assert.Equal(["3"], Counts(axes));
    }

    // ---- the rows ---------------------------------------------------------------------------------------------------------

    /// <summary>The rows are named by the groups, the first at the top, with one slot more below them for the test; the Y axis
    /// spans the rows, and is titled "Number at risk".</summary>
    [Fact]
    public void TheRows_AreNamedByTheGroups_AndTheYAxisSpansThem()
    {
        var axes = Table(Arms, [0]);
        string svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.RiskTable(Arms, [0])).ToSvg();

        Assert.Equal(0.0, axes.YAxis.Min);
        Assert.Equal(3.0, axes.YAxis.Max);
        Assert.Equal("Number at risk", axes.YAxis.Label);
        Assert.Contains(">Treatment<", svg);
        Assert.Contains(">Placebo<", svg);
        Assert.True(svg.IndexOf(">Treatment<", StringComparison.Ordinal) > 0);
    }

    /// <summary>A title of your own set after the call replaces the Y axis title.</summary>
    [Fact]
    public void ATitleSetAfterTheCall_ReplacesTheYAxisTitle()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.RiskTable(Arms, [0]).SetYLabel("At risk")).Build().SubPlots[0];

        Assert.Equal("At risk", axes.YAxis.Label);
    }

    // ---- the test ---------------------------------------------------------------------------------------------------------

    /// <summary>The log-rank test is one line of text pinned to the panel (not to the data), at its right end in the slot below
    /// the last row, so it never stands on a count: Freireich's trial reads chi-square 16.79 on 1 degree of freedom, p below
    /// 0.0001 (lifelines: 16.792940989216547, 4.17e-05).</summary>
    [Fact]
    public void TheRiskTable_StatesTheLogRankTest()
    {
        var axes = Table(Freireich, [0, 10, 20, 30]);

        var test = TestLine(axes);
        Assert.Equal("Log-rank χ²(1) = 16.79, p < 0.0001", test.Text);
        Assert.Equal(AnnotationCoordinates.AxesFraction, test.Coordinates);
        Assert.Equal(1.0, test.X);
        Assert.Equal(0.5 / 3, test.Y, 12);
        Assert.Equal(TextAlignment.Right, test.Alignment);
        Assert.Equal(TextVerticalAlignment.Center, test.VerticalAlignment);
    }

    /// <summary>The toy trial of the documentation: chi-square 7.85 on 1 degree of freedom, p = 0.0051 (lifelines:
    /// 7.854898685977314, 0.005068319243038367), a p-value large enough to print in full to four decimals.</summary>
    [Fact]
    public void AP_ValueAboveOneInTenThousand_IsPrintedToFourDecimals()
    {
        Assert.Equal("Log-rank χ²(1) = 7.85, p = 0.0051", TestLine(Table(Arms, [0])).Text);
    }

    /// <summary>The degrees of freedom are the number of groups less one: three groups read chi-square 4.98 on 2 degrees
    /// (lifelines: 4.981659715785285, p 0.08284119162569575), and each has its row.</summary>
    [Fact]
    public void ThreeGroups_ReadTwoDegreesOfFreedom()
    {
        var axes = Table(Three, [0, 5]);

        Assert.Equal("Log-rank χ²(2) = 4.98, p = 0.0828", TestLine(axes).Text);
        Assert.Equal(4.0, axes.YAxis.Max);
        Assert.Equal([3.5, 3.5, 2.5, 2.5, 1.5, 1.5], axes.Annotations.Where(a => a.Coordinates == AnnotationCoordinates.Data).Select(a => a.Y));
    }

    /// <summary>With no event in any group there is nothing to compare: the line says so, and no number reaches it.</summary>
    [Fact]
    public void WithNoEventsInAnyGroup_TheLineSaysThereIsNothingToCompare()
    {
        var axes = Table([new("A", [3, 5, 8], [false, false, false]), new("B", [2, 4, 9], [false, false, false])], [0]);

        Assert.Equal("Log-rank test: no events to compare", TestLine(axes).Text);
        Assert.DoesNotContain("NaN", string.Concat(axes.Annotations.Select(a => a.Text)));
    }

    /// <summary>One group has nothing to be compared with: no test line, and no slot for one below the row.</summary>
    [Fact]
    public void OneGroup_HasNoTest_AndNoSlotForIt()
    {
        var axes = Table([Treatment], [0, 10], configure: parts => Assert.Null(parts.Test));

        Assert.DoesNotContain(axes.Annotations, a => a.Coordinates == AnnotationCoordinates.AxesFraction);
        Assert.Equal(1.0, axes.YAxis.Max);
        Assert.Equal([0.5, 0.5], axes.Annotations.Select(a => a.Y));
    }

    // ---- the parts --------------------------------------------------------------------------------------------------------

    /// <summary>The callback gets the very annotations on the axes (the counts in the order group by group, each group's times
    /// in order), the test line, and the result of the test, so what it sets wins.</summary>
    [Fact]
    public void TheCallback_RunsOnce_OnThePartsOnTheAxes_AfterTheVerbBuiltThem()
    {
        int calls = 0;
        RiskTableParts seen = default;

        var axes = Table(Arms, [0, 10], parts =>
        {
            calls++;
            seen = parts;
            parts.Test!.OffsetY = 3;
            parts.Counts[0].Color = MatPlotLibNet.Styling.Colors.Red;
        });

        Assert.Equal(1, calls);
        Assert.Equal(4, seen.Counts.Count);
        Assert.Same(axes.Annotations[0], seen.Counts[0]);
        Assert.Same(axes.Annotations[3], seen.Counts[3]);
        Assert.Same(TestLine(axes), seen.Test);
        Assert.Equal(1, seen.LogRank.DegreesOfFreedom);
        Assert.Equal(7.854898685977314, seen.LogRank.Statistic, 9);
        Assert.Equal(0.005068319243038367, seen.LogRank.PValue, 9);
        Assert.Equal(MatPlotLibNet.Styling.Colors.Red, axes.Annotations[0].Color);
        Assert.Equal(3, TestLine(axes).OffsetY);
    }

    /// <summary>The parts are a public readonly record struct a caller cannot make: no public constructor, no setters.</summary>
    [Fact]
    public void TheParts_AreAReadOnlyRecordStruct_WithNoPublicConstructor()
    {
        var type = typeof(RiskTableParts);

        Assert.True(type.IsValueType);
        Assert.True(type.IsPublic);
        Assert.True(type.IsDefined(typeof(IsReadOnlyAttribute)));
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.Equal(["Counts", "LogRank", "Test"], properties.Select(p => p.Name).Order());
        Assert.All(properties, p => Assert.Null(p.SetMethod));
    }

    // ---- refusals ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void MissingArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).RiskTable(Arms)).ParamName);
        Assert.Equal("groups", Assert.Throws<ArgumentNullException>(() => Table(null!)).ParamName);
    }

    [Fact]
    public void NoGroups_AreRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => Table([]));

        Assert.Equal("groups", error.ParamName);
        Assert.StartsWith("No groups were given; a risk table counts one row per group.", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A group that is not survival data is refused in the words the survival chart uses, naming the group.</summary>
    [Fact]
    public void AGroupThatIsNotSurvivalData_IsRefused_NamingIt()
    {
        var error = Assert.Throws<ArgumentException>(() => Table([Treatment, new("Broken", [4, double.NaN], [true, false])]));

        Assert.Equal("groups", error.ParamName);
        Assert.StartsWith("Group 'Broken' has a duration of NaN at index 1;", error.Message, StringComparison.Ordinal);
    }

    /// <summary>The times are one or more finite numbers.</summary>
    [Theory]
    [InlineData(new double[0])]
    [InlineData(new[] { 0.0, double.NaN })]
    [InlineData(new[] { double.PositiveInfinity })]
    public void TimesThatAreNotFiniteNumbers_AreRefused(double[] times)
    {
        var error = Assert.Throws<ArgumentException>(() => Table(Arms, times));

        Assert.Equal("times", error.ParamName);
    }

    /// <summary>A refused call has added nothing: no annotation, no title, no limit.</summary>
    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var builder = new AxesBuilder();

        Assert.Throws<ArgumentException>(() => builder.RiskTable(Arms, [double.NaN]));
        var axes = builder.Build(1, 1, 1);

        Assert.Empty(axes.Annotations);
        Assert.Null(axes.YAxis.Label);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
    }

    // ---- stacked under the survival chart ---------------------------------------------------------------------------------

    /// <summary>The table and the survival chart stand on the same width: the plot areas of the two axes have the same left
    /// and right edge, whatever the names of the rows and the numbers on the Y axis above them take to the left (the table's
    /// Y axis names the groups, the survival chart's is numbered), with the layout as it is by default and tightened.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheRiskTable_LinesUpWithTheSurvivalAxes(bool tight)
    {
        var figure = Stacked(Arms, tight: tight).Build();

        var layout = ChartRendererDefault.Current.ComputeLayout(figure, new SvgRenderContext());

        Assert.Equal(2, layout.PlotAreas.Count);
        Assert.Equal(layout.PlotAreas[0].X, layout.PlotAreas[1].X, 9);
        Assert.Equal(layout.PlotAreas[0].Width, layout.PlotAreas[1].Width, 9);
        Assert.True(layout.PlotAreas[1].Y > layout.PlotAreas[0].Bottom);
    }

    /// <summary>A count stands directly under its time on the survival chart: the X of the count at 15 weeks in the picture is
    /// the X of the vertex the survival curve has at 15 weeks (Treatment's step down to 3/5).</summary>
    [Fact]
    public void ACount_StandsDirectlyUnderItsTimeOnTheSurvivalCurve()
    {
        var figure = Stacked(Arms, [0, 15, 30]).Build();
        string svg = figure.ToSvg();
        var layout = ChartRendererDefault.Current.ComputeLayout(figure, new SvgRenderContext());
        var table = layout.PlotAreas[1];

        // Treatment has 5 at risk at 15 weeks; the count's text is the one inside the table's plot area.
        var count = XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName == "text" && e.Value == "5")
            .Single(e => Num((string)e.Attribute("y")!) > table.Y && Num((string)e.Attribute("y")!) < table.Bottom);
        double countX = Num((string)count.Attribute("x")!);

        // Treatment's step down at 15 weeks is a vertex of its curve (the Placebo curve, which ends at 11, has none there).
        var curveXs = XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName == "polyline")
            .SelectMany(e => ((string)e.Attribute("points")!).Split(' ').Select(p => Num(p.Split(',')[0])))
            .ToList();
        Assert.Contains(curveXs, x => Math.Abs(x - countX) < 1e-6);
    }

    // ---- JSON -------------------------------------------------------------------------------------------------------------

    /// <summary>The table is its parts in JSON: the counts, the test line and the row names read back draw the same picture.</summary>
    [Fact]
    public void TheTable_SurvivesJson()
    {
        var figure = Stacked(Arms, [0, 10, 20, 30]).Build();

        string drawn = figure.ToSvg();
        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
        Assert.Contains("Log-rank χ²(1) = 7.85, p = 0.0051", readBack);
    }

    // ---- the example in the documentation --------------------------------------------------------------------------------

    /// <summary>The example of the verb's documentation, as written there, compiles and draws.</summary>
    [Fact]
    public void TheDocumentationExample_Draws()
    {
        SurvivalGroup[] arms =
        [
            new("Treatment", [6, 7, 10, 15, 19, 25, 32, 34], [true, true, false, true, false, true, false, false]),
            new("Placebo", [1, 2, 3, 4, 5, 8, 8, 11], [true, true, true, false, true, true, true, true]),
        ];

        string svg = Plt.Create()
            .WithSize(800, 600)
            .WithGridSpec(2, 1, heightRatios: [4, 1])
            .AddSubPlot(GridPosition.Single(0, 0), ax => ax.KaplanMeier(arms).WithLegend(), key: "survival")
            .AddSubPlot(GridPosition.Single(1, 0), ax => ax.ShareX("survival").RiskTable(arms).SetXLabel("Weeks"))
            .ToSvg();

        Assert.Contains(">Number at risk<", svg);
        Assert.Contains(">Log-rank χ²(1) = 7.85, p = 0.0051<", svg);
        Assert.Contains(">Weeks<", svg);
    }

    // ---- the Kaplan-Meier chart is as it was ------------------------------------------------------------------------------

    /// <summary>The survival chart alone draws what it always drew: the table is a verb of its own and adds nothing to it (no
    /// annotation, no row of counts).</summary>
    [Fact]
    public void TheKaplanMeierChart_StillHasNoTableAndNoTest()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.KaplanMeier(Arms)).Build().SubPlots[0];

        Assert.Empty(axes.Annotations);
    }

    // ---- reading the picture ----------------------------------------------------------------------------------------------

    private static double Num(string text) => double.Parse(text, CultureInfo.InvariantCulture);
}
