// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="YAxisSlotBuilder"/>, the one builder every Y axis is configured through: a slot of the
/// left or the right stack, the primary Y axis (left slot 0) included. Each test builds the slot through the builder's
/// internal constructor so the verbs are held without the verbs that hand the builder out.</summary>
public class YAxisSlotBuilderTests
{
    private static readonly Color Mark = Color.FromHex("#E4007C");

    private static readonly double[] X = [1.0, 2.0, 3.0];

    private static readonly double[] Y = [10.0, 20.0, 30.0];

    /// <summary>The axes a test built, the slot it names and the builder over that slot.</summary>
    private readonly record struct SlotUnderTest(Axes Axes, YAxisSlotBuilder Builder, YAxisSlot Slot);

    /// <summary>An axes with the primary plus one more left slot and two right slots, and the slot a test names.</summary>
    private static SlotUnderTest SlotOf(string side, int index)
    {
        var axes = new Axes();
        axes.YAxes.Left.Add();
        axes.YAxes.Right.Add();
        axes.YAxes.Right.Add();
        var slot = (side == "Left" ? axes.YAxes.Left : axes.YAxes.Right)[index];
        return new SlotUnderTest(axes, new YAxisSlotBuilder(axes, slot), slot);
    }

    public static TheoryData<string, int> EverySlot { get; } = new()
    {
        { "Left", 0 }, { "Left", 1 }, { "Right", 0 }, { "Right", 1 },
    };

    // ---- label and limits ----------------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(EverySlot))]
    public void SetYLabel_NamesTheSlotsOwnAxis_AndNoOtherSlot(string side, int index)
    {
        var (axes, builder, slot) = SlotOf(side, index);

        builder.SetYLabel("Rate");

        Assert.Equal("Rate", slot.Axis.Label);
        Assert.Equal(1, axes.YAxes.IdentityOrder.Count(s => s.Axis.Label == "Rate"));
    }

    [Fact]
    public void SetYLabel_WithAStyle_PassesTheCurrentStyleToTheCallback_AndKeepsTheResult()
    {
        var (_, builder, slot) = SlotOf("Right", 0);
        TextStyle? seen = null;

        builder.SetYLabel("Rate", style => { seen = style; return style with { FontSize = 17 }; });

        Assert.NotNull(seen);
        Assert.Equal(17, slot.Axis.LabelStyle!.FontSize);
    }

    [Fact]
    public void SetYLabel_WithoutAStyle_LeavesTheLabelStyleAlone()
    {
        var (_, builder, slot) = SlotOf("Left", 1);

        builder.SetYLabel("Rate");

        Assert.Null(slot.Axis.LabelStyle);
    }

    [Theory]
    [MemberData(nameof(EverySlot))]
    public void SetYLim_FixesTheSlotsOwnRange(string side, int index)
    {
        var (_, builder, slot) = SlotOf(side, index);

        builder.SetYLim(-5, 50);

        Assert.Equal(-5, slot.Axis.Min);
        Assert.Equal(50, slot.Axis.Max);
    }

    // ---- colour --------------------------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(EverySlot))]
    public void SetYAxisColor_OnAnySlot_ColoursTicksTickLabelsAndTitle(string side, int index)
    {
        var (axes, builder, slot) = SlotOf(side, index);
        slot.Axis.MajorTicks = slot.Axis.MajorTicks with { Length = 9 };
        slot.Axis.LabelStyle = new TextStyle { FontSize = 17 };

        builder.SetYAxisColor(Mark);

        Assert.Equal(Mark, slot.Axis.MajorTicks.Color);
        Assert.Equal(Mark, slot.Axis.MajorTicks.LabelColor);
        Assert.Equal(Mark, slot.Axis.LabelStyle!.Color);
        Assert.Equal(9, slot.Axis.MajorTicks.Length);
        Assert.Equal(17, slot.Axis.LabelStyle!.FontSize);
        foreach (var other in axes.YAxes.IdentityOrder.Where(s => !ReferenceEquals(s, slot)))
        {
            Assert.Null(other.Axis.MajorTicks.Color);
            Assert.Null(other.Axis.MajorTicks.LabelColor);
            Assert.Null(other.Axis.LabelStyle?.Color);
        }
    }

    [Fact]
    public void SetYAxisColor_OnSlotZero_AlsoColoursThatSidesSpine()
    {
        var (primaryAxes, primary, _) = SlotOf("Left", 0);
        var (rightAxes, right, _) = SlotOf("Right", 0);

        primary.SetYAxisColor(Mark);
        right.SetYAxisColor(Mark);

        Assert.Equal(Mark, primaryAxes.Spines.Left.Color);
        Assert.Null(primaryAxes.Spines.Right.Color);
        Assert.Equal(Mark, rightAxes.Spines.Right.Color);
        Assert.Null(rightAxes.Spines.Left.Color);
        Assert.Null(primaryAxes.Spines.Top.Color);
        Assert.Null(primaryAxes.Spines.Bottom.Color);
    }

    [Theory]
    [InlineData("Left", 1)]
    [InlineData("Right", 1)]
    public void SetYAxisColor_OnASlotPastZero_LeavesEverySpineUntouched(string side, int index)
    {
        var (axes, builder, _) = SlotOf(side, index);

        builder.SetYAxisColor(Mark);

        Assert.Equal(new SpinesConfig(), axes.Spines);
    }

    // ---- series --------------------------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(EverySlot))]
    public void Plot_AddsALineToTheSlotsOwnSeries_AndRunsTheCallback(string side, int index)
    {
        var (axes, builder, slot) = SlotOf(side, index);
        LineSeries? configured = null;

        builder.Plot(X, Y, line => configured = line);

        var line = Assert.IsType<LineSeries>(Assert.Single(slot.Series));
        Assert.Same(line, configured);
        Assert.Single(axes.AllSeries);
    }

    [Fact]
    public void Plot_WithoutACallback_StillAddsTheLine()
    {
        var (_, builder, slot) = SlotOf("Right", 1);

        builder.Plot(X, Y);

        Assert.IsType<LineSeries>(Assert.Single(slot.Series));
    }

    [Fact]
    public void Plot_WithMisalignedArrays_Throws_AndAddsNothing()
    {
        var (_, builder, slot) = SlotOf("Left", 1);

        Assert.Throws<ArgumentException>(() => builder.Plot(X, [1.0]));

        Assert.Empty(slot.Series);
    }

    [Fact]
    public void Scatter_AddsAScatterToTheSlotsOwnSeries_AndRunsTheCallback()
    {
        var (_, builder, slot) = SlotOf("Left", 1);
        ScatterSeries? configured = null;

        builder.Scatter(X, Y, points => configured = points);

        Assert.Same(configured, Assert.IsType<ScatterSeries>(Assert.Single(slot.Series)));
    }

    [Fact]
    public void Scatter_WithoutACallback_StillAddsTheScatter()
    {
        var (_, builder, slot) = SlotOf("Right", 0);

        builder.Scatter(X, Y);

        Assert.IsType<ScatterSeries>(Assert.Single(slot.Series));
    }

    [Fact]
    public void Scatter_WithMisalignedArrays_Throws_AndAddsNothing()
    {
        var (_, builder, slot) = SlotOf("Right", 0);

        Assert.Throws<ArgumentException>(() => builder.Scatter(X, [1.0]));

        Assert.Empty(slot.Series);
    }

    [Fact]
    public void StreamingPlot_AddsAStreamingLine_AndReturnsItAsTheHandle()
    {
        var (_, builder, slot) = SlotOf("Right", 1);

        var series = builder.StreamingPlot(capacity: 50);

        Assert.Same(series, Assert.IsType<StreamingLineSeries>(Assert.Single(slot.Series)));
    }

    [Fact]
    public void StreamingPlot_RunsTheCallbackOnTheNewSeries()
    {
        var (_, builder, _) = SlotOf("Left", 1);
        StreamingLineSeries? configured = null;

        var series = builder.StreamingPlot(configure: s => configured = s);

        Assert.Same(series, configured);
    }

    [Fact]
    public void WithDirectLabel_MarksTheSeriesAddedLast_ToShowItsNameAtTheEndOfItsLine()
    {
        var (_, builder, slot) = SlotOf("Right", 0);

        builder.Plot(X, Y, line => line.Label = "first").Plot(X, Y, line => line.Label = "last").WithDirectLabel();

        var series = slot.Series.Cast<LineSeries>().ToArray();
        Assert.Equal(SeriesLabelPlacement.Legend, series[0].LabelPlacement);
        Assert.Equal(SeriesLabelPlacement.LineEnd, series[1].LabelPlacement);
    }

    [Fact]
    public void WithDirectLabel_WhenTheLastSeriesCannotCarryAPlacement_ChangesNothing()
    {
        var (_, builder, slot) = SlotOf("Right", 0);

        builder.Scatter(X, Y).WithDirectLabel();

        Assert.IsType<ScatterSeries>(Assert.Single(slot.Series));
    }

    [Fact]
    public void WithDirectLabel_OnASlotWithoutSeries_ChangesNothing()
    {
        var (_, builder, slot) = SlotOf("Left", 1);

        builder.WithDirectLabel();

        Assert.Empty(slot.Series);
    }

    [Fact]
    public void EveryVerbButTheStreamingOne_ReturnsTheBuilder_ForChaining()
    {
        var (_, builder, _) = SlotOf("Right", 0);

        Assert.Same(builder, builder.SetYLabel("a"));
        Assert.Same(builder, builder.SetYLabel("a", style => style));
        Assert.Same(builder, builder.SetYLim(0, 1));
        Assert.Same(builder, builder.SetYAxisColor(Mark));
        Assert.Same(builder, builder.Plot(X, Y));
        Assert.Same(builder, builder.Scatter(X, Y));
        Assert.Same(builder, builder.WithDirectLabel());
    }

    // ---- the primary Y axis is left slot 0 ---------------------------------------------------------------------------------

    /// <summary>The builder's own Y verbs act on the primary Y axis, which is left slot 0 of the container: they are the
    /// slot builder's verbs, not a second copy, so a chain of them leaves the model exactly as the same chain on a slot
    /// builder over left slot 0 does.</summary>
    [Fact]
    public void TheAxesBuildersYVerbs_ActOnLeftSlotZero_AsTheSlotBuilderDoes()
    {
        Axes ThroughTheAxesBuilder() => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetYLabel("Level", style => style with { FontSize = 15 }).SetYLim(-1, 4).SetYAxisColor(Mark))
            .Build().SubPlots[0];

        Axes ThroughTheSlotBuilder()
        {
            var axes = new Axes();
            new YAxisSlotBuilder(axes, axes.YAxes.Left[0])
                .SetYLabel("Level", style => style with { FontSize = 15 }).SetYLim(-1, 4).SetYAxisColor(Mark);
            return axes;
        }

        var viaAxesBuilder = ThroughTheAxesBuilder();
        var viaSlotBuilder = ThroughTheSlotBuilder();

        Assert.Equal("Level", viaAxesBuilder.YAxes.Left[0].Axis.Label);
        Assert.Equal(viaSlotBuilder.YAxis.Label, viaAxesBuilder.YAxis.Label);
        Assert.Equal(viaSlotBuilder.YAxis.Min, viaAxesBuilder.YAxis.Min);
        Assert.Equal(viaSlotBuilder.YAxis.Max, viaAxesBuilder.YAxis.Max);
        Assert.Equal(viaSlotBuilder.YAxis.MajorTicks, viaAxesBuilder.YAxis.MajorTicks);
        Assert.Equal(viaSlotBuilder.YAxis.LabelStyle, viaAxesBuilder.YAxis.LabelStyle);
        Assert.Equal(viaSlotBuilder.Spines, viaAxesBuilder.Spines);
        Assert.Equal(Mark, viaAxesBuilder.Spines.Left.Color);
    }

    [Fact]
    public void TheAxesBuildersSetYLabel_WithoutAStyle_NamesThePrimary_AndLeavesItsLabelStyleAlone()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetYLabel("Level")).Build().SubPlots[0];

        Assert.Equal("Level", axes.YAxes.Left[0].Axis.Label);
        Assert.Null(axes.YAxis.LabelStyle);
    }
}
