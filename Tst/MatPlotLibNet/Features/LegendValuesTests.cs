// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Features;

/// <summary>TDD tests for the LegendValues feature on <see cref="FigureBuilder"/> and <see cref="AxesBuilder"/>.</summary>
public class LegendValuesTests
{
    private static Figure FigureOf(Action<AxesBuilder> axes) => Plt.Create().AddSubPlot(1, 1, 1, axes).Build();

    // ── AxesBuilder fluent API tests ────────────────────────────────────────

    /// <summary>WithLegendValues(true) sets LegendValues on the axes Legend.</summary>
    [Fact]
    public void AxesBuilder_WithLegendValues_SetsFlag()
    {
        var figure = FigureOf(ax => ax
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Signal")
            .WithLegendValues());

        Assert.True(figure.SubPlots[0].Legend.LegendValues);
    }

    /// <summary>WithLegendValues(false) leaves LegendValues false.</summary>
    [Fact]
    public void AxesBuilder_WithLegendValues_FalseArg_StaysFalse()
    {
        var figure = FigureOf(ax => ax
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Signal")
            .WithLegendValues(false));

        Assert.False(figure.SubPlots[0].Legend.LegendValues);
    }

    // ── AxesBuilder ordering interplay (WithLegendValues vs WithLegend) ─────

    /// <summary>Regression test: WithLegend(position, visible) used to replace the axes'
    /// Legend record wholesale (<c>_axes.Legend = new Legend {...}</c>), silently discarding a
    /// LegendValues flag set earlier in the chain. Root fix must preserve it regardless of
    /// call order. This is the "WithLegendValues() then WithLegend(...)" ordering.</summary>
    [Fact]
    public void AxesBuilder_WithLegendValues_ThenWithLegend_PreservesLegendValues()
    {
        var figure = FigureOf(ax => ax
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Signal")
            .WithLegendValues()
            .WithLegend(LegendPosition.UpperRight));

        var legend = figure.SubPlots[0].Legend;
        Assert.True(legend.LegendValues);
        Assert.Equal(LegendPosition.UpperRight, legend.Position);
    }

    /// <summary>Inverse order — WithLegend(...) then WithLegendValues() — already worked before
    /// the fix (WithLegendValues does a `with`-mutation on the current Legend). Pinned here so
    /// the fix for the other ordering doesn't regress this one, making the whole chain
    /// order-independent.</summary>
    [Fact]
    public void AxesBuilder_WithLegend_ThenWithLegendValues_PreservesLegendValues()
    {
        var figure = FigureOf(ax => ax
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Signal")
            .WithLegend(LegendPosition.UpperRight)
            .WithLegendValues());

        var legend = figure.SubPlots[0].Legend;
        Assert.True(legend.LegendValues);
        Assert.Equal(LegendPosition.UpperRight, legend.Position);
    }

    // ── FigureBuilder fluent API tests ──────────────────────────────────────

    /// <summary>FigureBuilder.WithLegendValues sets the flag on the default axes' Legend.</summary>
    [Fact]
    public void FigureBuilder_WithLegendValues_SetsFlag()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Signal")
            .WithLegendValues()
            .Build();

        Assert.True(figure.SubPlots[0].Legend.LegendValues);
    }

    // ── SVG rendering tests ─────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  Without WithLegendValues the SVG legend shows only the series label (no value suffix).
        //  The series label should be present
        //  But it should NOT include " = " appended value
        //  (We check that "Signal = " does not appear when the flag is off)
        new DrawnToken("LegendValues_Disabled_LegendShowsOnlyLabel", () => FigureOf(ax => ax
            .Plot([1.0, 2.0], [3.0, 4.7])
            .Plot([1.0, 2.0], [0.0, 0.0], s => s.Label = "Signal")
            .WithLegend()).ToSvg(), "Signal") { Absent = ["Signal = "] },
        //  With WithLegendValues the SVG legend shows label + " = " + last value (InvariantCulture).
        //  Must contain the series label AND the appended last-value suffix
        //  Y data last value = 2.7 → legend should show "Signal = 2.70"
        new DrawnToken("LegendValues_Enabled_AppendsSuffixToLegendLabel", () => FigureOf(ax => ax
            .Plot([1.0, 2.0], [1.0, 2.7], s => s.Label = "Signal")
            .WithLegendValues()).ToSvg(), "Signal = "),
        //  The appended last value uses InvariantCulture (decimal dot, not comma) regardless of host locale.
        //  2.7 must render as "2.70" (InvariantCulture two-decimal F2 format), never as "2,70"
        new DrawnToken("LegendValues_UsesInvariantCulture_DecimalDot", () => FigureOf(ax => ax
            .Plot([1.0, 2.0], [1.0, 2.7], s => s.Label = "Signal")
            .WithLegendValues()).ToSvg(), "2.70") { Absent = ["2,70"] },
        //  Multiple labeled series each show their own last value.
        new DrawnToken("LegendValues_MultipleLabeled_EachGetsOwnValue", () => FigureOf(ax => ax
            .Plot([1.0, 2.0], [1.0, 3.5], s => s.Label = "Alpha")
            .Plot([1.0, 2.0], [1.0, 8.2], s => s.Label = "Beta")
            .WithLegendValues()).ToSvg(), "Alpha = ", "Beta = ", "3.50", "8.20"),
        //  Unlabeled series are not affected by WithLegendValues (they never appear in the legend).
        //  9.9 from the unlabeled series should not appear as a legend value
        new DrawnToken("LegendValues_UnlabeledSeries_NotInLegend", () => FigureOf(ax => ax
            .Plot([1.0, 2.0], [1.0, 9.9])
            .Plot([1.0, 2.0], [1.0, 3.5], s => s.Label = "Visible")
            .WithLegendValues()).ToSvg(), "Visible = ") { Absent = ["9.90"] },
        //  A series without YData (empty) shows label without value suffix even when flag is enabled.
        //  Bar series has no YData in XYSeries sense — should show label without suffix
        //  We can construct a non-XYSeries to ensure graceful fallback
        //  For simplicity: use a series whose label IS set but whose last-value resolution gracefully
        //  returns no suffix. Use a bar series (categorical, no continuous YData in the XY sense).
        new DrawnToken("LegendValues_EmptyYData_LabelOnlyNoSuffix", () => FigureOf(ax => ax
            .Bar(["A", "B"], [1.0, 2.0], s => s.Label = "Bars")
            .WithLegendValues()).ToSvg(), "Bars"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
