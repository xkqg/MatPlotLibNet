// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The Shewhart control chart of any kind in one call: the plotted statistic as a marked line, the centre line and the
/// two control limits across the plot, and the points flagged by the rules asked for (Nelson's eight tests) marked. The data says
/// which statistic it is and how its limits are estimated (<see cref="ControlChartData"/>); the rules are a set of flags
/// (<see cref="ControlRules"/>). <c>ControlChart</c> is this verb for the individuals chart with rule 1 only, and draws the same
/// picture it always did.</summary>
public class ShewhartChartTests
{
    private static readonly double[] Days = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

    /// <summary>Index 1 and 5 are beyond the upper limit of 3 (rule 1); indices 3 to 11 are nine above the centre (rule 2).</summary>
    private static readonly double[] Mixed = [0, 3.5, 0, 0.5, 0.5, 3.4, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5];

    private static readonly ControlLimits Given = new(0, -3, 3);

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static Axes Chart(ControlChartData data, ControlRules rules = ControlRules.Nelson1, ControlLimits? limits = null, Action<ControlChartParts>? configure = null) =>
        AxesOf(ax => ax.ShewhartChart(data, rules, limits, configure));

    private static ScatterSeries Violations(Axes axes) => Assert.IsType<ScatterSeries>(axes.Series[1]);

    // ---- the individuals chart, as ControlChart draws it ------------------------------------------------------------------

    /// <summary>With the default rule (rule 1) and the individuals data, the verb draws the very picture <c>ControlChart</c>
    /// does, byte for byte, estimated or with limits given, with and without a missing value.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void WithTheDefaultRule_ItDrawsTheControlChart_ByteForByte(bool given, bool missing)
    {
        double[] values = [10, 12, 11, 15, 13, 12, 30, 11, 12, 10, 13, 11];
        if (missing) values[4] = double.NaN;
        ControlLimits? limits = given ? new ControlLimits(12, 5, 19) : null;

        string chart = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ControlChart(Days, values, limits)).ToSvg();
        string shewhart = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ShewhartChart(ControlChartData.Individuals(Days, values), limits: limits)).ToSvg();

        Assert.Equal(chart, shewhart);
    }

    // ---- the rules --------------------------------------------------------------------------------------------------------

    /// <summary>The violations are ONE scatter series holding the union of the points the rules flag, at their x positions in
    /// the order of the values.</summary>
    [Fact]
    public void SeveralRules_MarkTheUnionOfTheirPoints_InOneScatter()
    {
        var axes = Chart(ControlChartData.Individuals(Days, Mixed), ControlRules.Nelson1 | ControlRules.Nelson2, Given);

        Assert.Equal(2, axes.Series.Count);
        Assert.Equal([2.0, 6.0, 12.0], Violations(axes).XData);
        Assert.Equal([3.5, 3.4, 0.5], Violations(axes).YData);
    }

    [Fact]
    public void EachRuleAlone_MarksItsOwnPoints()
    {
        Assert.Equal([2.0, 6.0], Violations(Chart(ControlChartData.Individuals(Days, Mixed), ControlRules.Nelson1, Given)).XData);
        Assert.Equal([12.0], Violations(Chart(ControlChartData.Individuals(Days, Mixed), ControlRules.Nelson2, Given)).XData);
    }

    /// <summary>No rule asked for marks nothing, and the scatter is still the chart's second series.</summary>
    [Fact]
    public void NoRules_MarkNothing()
    {
        var axes = Chart(ControlChartData.Individuals(Days, Mixed), ControlRules.None, Given);

        Assert.Empty(Violations(axes).XData);
        Assert.Equal(3, axes.ReferenceLines.Count);
    }

    /// <summary>The zones of rules 5 to 8 stand on the limits the chart is drawn with, given or estimated: a sigma is a third of
    /// the way from the centre to the upper limit. Nine values above the given centre are rule 2 whatever the data would have
    /// estimated.</summary>
    [Fact]
    public void TheRules_JudgeByTheLimitsTheChartIsDrawnWith()
    {
        double[] values = [2.5, 0.1, 2.5, 0.1, 0.1];

        Assert.Equal([3.0], Violations(Chart(ControlChartData.Individuals([1, 2, 3, 4, 5], values), ControlRules.Nelson5, new ControlLimits(0, -3, 3))).XData);
        Assert.Empty(Violations(Chart(ControlChartData.Individuals([1, 2, 3, 4, 5], values), ControlRules.Nelson5, new ControlLimits(0, -6, 6))).XData);
    }

    // ---- the rules are a numbered vocabulary -------------------------------------------------------------------------------

    /// <summary>The rules are flags numbered as Nelson numbers them: one bit each for 1 to 8, nothing for none, all of them for
    /// all.</summary>
    [Fact]
    public void TheRules_AreFlagsNumberedLikeNelsons()
    {
        Assert.True(typeof(ControlRules).IsDefined(typeof(FlagsAttribute)));
        ControlRules[] each = [ControlRules.Nelson1, ControlRules.Nelson2, ControlRules.Nelson3, ControlRules.Nelson4,
                               ControlRules.Nelson5, ControlRules.Nelson6, ControlRules.Nelson7, ControlRules.Nelson8];
        Assert.Equal([1, 2, 4, 8, 16, 32, 64, 128], each.Select(r => (int)r));
        Assert.Equal(0, (int)ControlRules.None);
        Assert.Equal(255, (int)ControlRules.All);
        Assert.Equal(ControlRules.All, each.Aggregate((a, b) => a | b));
    }

    // ---- refusals ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void MissingArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).ShewhartChart(ControlChartData.Individuals(Days, Mixed))).ParamName);
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => ControlChartData.Individuals(null!, Mixed)).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => ControlChartData.Individuals(Days, null!)).ParamName);
    }

    /// <summary>Data nobody made with a factory (<c>default</c>) holds nothing to draw.</summary>
    [Fact]
    public void DataFromNoFactory_IsRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => Chart(default));

        Assert.Equal("data", error.ParamName);
        Assert.StartsWith("The control chart data holds nothing to draw; make it with one of the ControlChartData factories", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A bit that names no rule is refused; so is a value that does not pair with the positions.</summary>
    [Fact]
    public void ARuleThatIsNotInTheVocabulary_IsRefused()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => Chart(ControlChartData.Individuals(Days, Mixed), (ControlRules)256));

        Assert.Equal("rules", error.ParamName);
    }

    [Fact]
    public void ValuesThatDoNotPairWithThePositions_AreRefused_ByTheFactory()
    {
        var error = Assert.Throws<ArgumentException>(() => ControlChartData.Individuals([1, 2, 3], [1, 2]));

        Assert.Equal("values", error.ParamName);
    }

    [Fact]
    public void LimitsThatAreNotOrdered_AreRefused_ByTheVerb()
    {
        var error = Assert.Throws<ArgumentException>(() => Chart(ControlChartData.Individuals(Days, Mixed), limits: new ControlLimits(0, 3, -3)));

        Assert.Equal("limits", error.ParamName);
    }

    /// <summary>A refused call has added nothing: no series, no reference line.</summary>
    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var builder = new AxesBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.ShewhartChart(ControlChartData.Individuals(Days, Mixed), (ControlRules)256));
        Assert.Throws<ArgumentException>(() => builder.ShewhartChart(default));
        var axes = builder.Build(1, 1, 1);

        Assert.Empty(axes.Series);
        Assert.Empty(axes.ReferenceLines);
    }

    // ---- the callback -----------------------------------------------------------------------------------------------------

    /// <summary>The callback gets the five parts of a control chart, the very series and reference lines on the axes, once, after
    /// the verb has built and styled them.</summary>
    [Fact]
    public void TheCallback_RunsOnce_OnThePartsOnTheAxes()
    {
        int calls = 0;
        ControlChartParts seen = default;

        var axes = Chart(ControlChartData.Individuals(Days, Mixed), ControlRules.Nelson1, Given, parts => { calls++; seen = parts; parts.Violations.Label = "beyond"; });

        Assert.Equal(1, calls);
        Assert.Same(axes.Series[0], seen.Values);
        Assert.Same(axes.Series[1], seen.Violations);
        Assert.Equal(0.0, seen.Center.Value);
        Assert.Equal(-3.0, seen.Lower.Value);
        Assert.Equal(3.0, seen.Upper.Value);
        Assert.Equal("beyond", Violations(axes).Label);
    }

    /// <summary>The data is a public readonly record struct a caller cannot make by hand: the factories make it.</summary>
    [Fact]
    public void TheData_IsAReadOnlyRecordStruct_MadeByItsFactories()
    {
        var type = typeof(ControlChartData);

        Assert.True(type.IsValueType);
        Assert.True(type.IsPublic);
        Assert.True(type.IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute)));
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.DoesNotContain(type.GetProperties(BindingFlags.Public | BindingFlags.Instance), p => p.CanWrite);
    }

    // ---- the example in the documentation ---------------------------------------------------------------------------------

    /// <summary>The example of the verb's documentation, as written there, compiles and draws: the process is held to the
    /// limits of an earlier period (50 g, 49 to 51), no day is beyond them, but ten days in a row stand above the centre, and
    /// rule 2 flags the ninth of them and the one after it.</summary>
    [Fact]
    public void TheDocumentationExample_Draws()
    {
        double[] day = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        double[] fill = [49.8, 49.9, 50.1, 50.2, 50.1, 50.3, 50.2, 50.4, 50.1, 50.3, 50.2, 50.4];

        ControlChartParts chart = default;
        string svg = Plt.Create()
            .WithTitle("Fill weight")
            .AddSubPlot(1, 1, 1, ax => ax
                .ShewhartChart(ControlChartData.Individuals(day, fill), ControlRules.Nelson1 | ControlRules.Nelson2,
                    new ControlLimits(Center: 50, Lower: 49, Upper: 51),
                    parts => { parts.Violations.Label = "out of control"; chart = parts; })
                .SetXLabel("Day")
                .SetYLabel("Weight (g)"))
            .ToSvg();

        Assert.Equal([11.0, 12.0], chart.Violations.XData);
        Assert.Equal([50.2, 50.4], chart.Violations.YData);
        Assert.Contains(">out of control<", svg);
        Assert.Contains(">UCL<", svg);
    }
}
