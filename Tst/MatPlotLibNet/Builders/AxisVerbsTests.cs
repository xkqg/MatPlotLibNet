// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The verbs of one axis (<see cref="AxisVerbs"/>) and of its ticks (<see cref="TickVerbs"/>), the one set every axis builder
/// composes: each factory picks the axis, the spine its colour reaches and the series a link refuses; each verb sets one thing.</summary>
public class AxisVerbsTests
{
    [Fact]
    public void ThePrimaryXAxis_StandsOnTheBottomSpine_AndHoldsNothingALinkRefuses()
    {
        var axes = new Axes();
        var verbs = AxisVerbs.BottomOf(axes);

        Assert.Same(axes.XAxis, verbs.Axis);
        Assert.Equal(SpineEdge.Bottom, verbs.Spine);
        Assert.Empty(verbs.Held);
    }

    [Fact]
    public void TheZAxis_StandsOnNoSpine()
    {
        var axes = new Axes();
        var verbs = AxisVerbs.DepthOf(axes);

        Assert.Same(axes.ZAxis, verbs.Axis);
        Assert.Null(verbs.Spine);
    }

    [Fact]
    public void TheTopAxis_IsMadeWhenThereIsNone_StandsOnTheTopSpine_AndHoldsTheTopSeries()
    {
        var axes = new Axes();
        var verbs = AxisVerbs.TopOf(axes);

        Assert.Same(axes.SecondaryXAxis, verbs.Axis);
        Assert.Equal(SpineEdge.Top, verbs.Spine);
        Assert.Same(axes.XSecondarySeries, verbs.Held);
    }

    [Fact]
    public void SlotZeroOfASide_StandsOnThatSidesSpine_ASlotStackedPastIt_AndALinkedXAxisBelow_OnNone()
    {
        var axes = new Axes();
        var right = axes.YAxes.Right.Add();
        var past = axes.YAxes.Right.Add();
        var below = axes.YAxes.Bottom.Add();

        Assert.Equal(SpineEdge.Left, AxisVerbs.Of(axes, axes.YAxes.Left[0]).Spine);
        Assert.Equal(SpineEdge.Right, AxisVerbs.Of(axes, right).Spine);
        Assert.Null(AxisVerbs.Of(axes, past).Spine);
        Assert.Null(AxisVerbs.Of(axes, below).Spine);
        Assert.Same(right.Series, AxisVerbs.Of(axes, right).Held);
    }

    [Fact]
    public void ALink_IsRefused_WhileTheAxisHoldsASeries_AndSetOtherwise()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Right.Add();
        var verbs = AxisVerbs.Of(axes, slot);
        var transform = AxisTransform.Linear(2, 0);

        verbs.Link(transform);
        Assert.Same(transform, slot.Axis.Link);

        var held = axes.YAxes.Right.Add();
        held.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        Assert.Throws<ArgumentException>(() => AxisVerbs.Of(axes, held).Link(transform));
    }

    [Fact]
    public void AColour_ReachesTheSpineOfAnAxisThatStandsOnOne_AndNoSpineOtherwise()
    {
        var axes = new Axes();
        AxisVerbs.BottomOf(axes).Color(Colors.Red);
        AxisVerbs.DepthOf(axes).Color(Colors.Blue);

        Assert.Equal(Colors.Red, axes.Spines.Bottom.Color);
        Assert.Equal(Colors.Red, axes.XAxis.MajorTicks.LabelColor);
        Assert.Equal(Colors.Blue, axes.ZAxis.MajorTicks.Color);
        Assert.NotEqual(Colors.Blue, axes.Spines.Left.Color);
    }

    [Fact]
    public void TheAxisVerbs_SetTheirOneSettingEach()
    {
        var axes = new Axes();
        var x = AxisVerbs.BottomOf(axes);

        x.Label("time", style => style with { FontSize = 9 });
        x.Limits(1, 2);
        x.Margin(0.1);
        x.Invert();

        Assert.Equal("time", axes.XAxis.Label);
        Assert.Equal(9, axes.XAxis.LabelStyle!.FontSize);
        Assert.Equal((1.0, 2.0), (axes.XAxis.Min, axes.XAxis.Max));
        Assert.Equal(0.1, axes.XAxis.Margin);
        Assert.True(axes.XAxis.Inverted);
        x.Invert();
        Assert.False(axes.XAxis.Inverted);
    }

    [Fact]
    public void TheScaleVerbs_PutTheAxisOnTheirScale()
    {
        var axes = new Axes();
        var x = AxisVerbs.BottomOf(axes);

        x.Scale(AxisScale.Log);
        Assert.Equal(AxisScale.Log, axes.XAxis.Scale);
        x.Symlog(3);
        Assert.Equal(AxisScale.SymLog, axes.XAxis.Scale);
        x.AutoDates();
        Assert.Equal(AxisScale.Date, axes.XAxis.Scale);
        x.DateFormat("yyyy");
        Assert.Equal("yyyy", Assert.IsType<DateTickFormatter>(axes.XAxis.TickFormatter).DateFormat);
    }

    [Fact]
    public void TheTickVerbs_SetTheTicksOfTheirAxis()
    {
        var axes = new Axes();
        var ticks = AxisVerbs.Of(axes, axes.YAxes.Left[0]).Ticks;
        var formatter = new PercentFormatter(1);
        var locator = new FixedLocator([1.0]);

        ticks.Formatter(formatter);
        ticks.Locator(locator);
        ticks.LabelRotation(45);
        ticks.Mirror();
        Assert.Same(formatter, axes.YAxis.TickFormatter);
        Assert.Same(locator, axes.YAxis.TickLocator);
        Assert.Equal(45, axes.YAxis.MajorTicks.LabelRotation);
        Assert.True(axes.YAxis.MajorTicks.Mirror);
        Assert.True(axes.YAxis.MajorTicks.LabelsOnOwnSide);

        ticks.LabelsOnTheOtherSide();
        Assert.False(axes.YAxis.MajorTicks.LabelsOnOwnSide);

        ticks.Minor(true);
        Assert.True(axes.YAxis.MinorTicks.Visible);
        ticks.Hide();
        Assert.False(axes.YAxis.MajorTicks.Visible);
        Assert.False(axes.YAxis.MinorTicks.Visible);
    }

    [Fact]
    public void ADirectLabel_NamesTheLastSeriesOfASlot_WhenItCanStandAtTheEnd_AndNothingOtherwise()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Left[0];
        slot.LabelLastAtLineEnd();

        var line = slot.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        slot.AddSeries(new BarSeries(["a"], [1.0]));
        slot.LabelLastAtLineEnd();
        Assert.NotEqual(SeriesLabelPlacement.LineEnd, line.LabelPlacement);

        var last = slot.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));
        slot.LabelLastAtLineEnd();
        Assert.Equal(SeriesLabelPlacement.LineEnd, last.LabelPlacement);
    }

    // ---- the colour an axis wears (ApplyAxisColor) ----
    // Verifies the one rule that colours an axis and the spine standing on its edge: the four colour verbs (<c>SetYAxisColor</c>
    // and <c>SetXAxisColor</c> on the builder, the right Y and the top X on their builders) all call it, so what it does to an
    // edge is the same for each of them.

    private static readonly Color Mark = Color.FromHex("#E4007C");

    private static readonly string[] Edges = ["Top", "Bottom", "Left", "Right"];

    public static TheoryData<string> EdgeNames { get; } = new(Edges);

    /// <summary>The colour each of the four spines carries, keyed by the edge it stands on.</summary>
    private static Dictionary<string, Color?> SpineColours(Axes axes) => new()
    {
        ["Top"] = axes.Spines.Top.Color,
        ["Bottom"] = axes.Spines.Bottom.Color,
        ["Left"] = axes.Spines.Left.Color,
        ["Right"] = axes.Spines.Right.Color,
    };

    [Theory]
    [MemberData(nameof(EdgeNames))]
    public void ApplyAxisColor_WithAnEdge_ColoursTheAxisAndExactlyThatSpine(string edge)
    {
        var axes = new Axes();
        var axis = new Axis();

        AxisVerbs.ApplyAxisColor(axes, axis, Enum.Parse<SpineEdge>(edge), Mark);

        Assert.Equal(Mark, axis.MajorTicks.Color);
        Assert.Equal(Mark, axis.MajorTicks.LabelColor);
        Assert.Equal(Mark, axis.LabelStyle!.Color);
        foreach (var (name, colour) in SpineColours(axes))
        {
            Assert.Equal(name == edge ? (Color?)Mark : null, colour);
        }
    }

    [Fact]
    public void ApplyAxisColor_WithoutAnEdge_ColoursTheAxisAndLeavesEverySpineUntouched()
    {
        var axes = new Axes();
        var axis = new Axis();

        AxisVerbs.ApplyAxisColor(axes, axis, spine: null, Mark);

        Assert.Equal(Mark, axis.MajorTicks.Color);
        Assert.Equal(Mark, axis.MajorTicks.LabelColor);
        Assert.Equal(Mark, axis.LabelStyle!.Color);
        Assert.Equal(new SpinesConfig(), axes.Spines);
    }

    [Fact]
    public void ApplyAxisColor_KeepsEverythingElseTheSpineAndTheAxisAlreadyCarry()
    {
        var axes = new Axes { Spines = new SpinesConfig { Left = new SpineConfig { LineWidth = 3, Visible = false } } };
        var axis = new Axis
        {
            MajorTicks = new TickConfig { Length = 9, LabelRotation = 45 },
            LabelStyle = new TextStyle { FontSize = 17 },
        };

        AxisVerbs.ApplyAxisColor(axes, axis, SpineEdge.Left, Mark);

        Assert.Equal(9, axis.MajorTicks.Length);
        Assert.Equal(45, axis.MajorTicks.LabelRotation);
        Assert.Equal(17, axis.LabelStyle!.FontSize);
        Assert.Equal(3, axes.Spines.Left.LineWidth);
        Assert.False(axes.Spines.Left.Visible);
        Assert.Equal(Mark, axes.Spines.Left.Color);
    }

    // ---- the label an axis is given (SetLabel) ----
    // The one rule that gives an axis its title together with an optional edit of the title's style, which the label verb of the
    // bottom X axis and of every Y axis share: the style edit starts from the style the axis already has, or from a plain <see
    // cref="TextStyle"/> when it has none, and an absent edit leaves the style as it was.

    [Fact]
    public void SetLabel_NamesTheAxis_AndWithoutAnEditLeavesTheStyleAlone()
    {
        var axis = new Axis();

        AxisVerbs.SetLabel(axis, "time", null);

        Assert.Equal("time", axis.Label);
        Assert.Null(axis.LabelStyle);
    }

    [Fact]
    public void SetLabel_EditsAPlainStyle_WhenTheAxisHasNone()
    {
        var axis = new Axis();
        TextStyle? seen = null;

        AxisVerbs.SetLabel(axis, "time", style => { seen = style; return style with { FontSize = 14 }; });

        Assert.Equal(new TextStyle(), seen);
        Assert.Equal(14, axis.LabelStyle!.FontSize);
    }

    [Fact]
    public void SetLabel_EditsTheStyleTheAxisHas()
    {
        var kept = new TextStyle { FontSize = 9, Color = Color.FromHex("#D62728") };
        var axis = new Axis { LabelStyle = kept };
        TextStyle? seen = null;

        AxisVerbs.SetLabel(axis, "rate", style => { seen = style; return style with { FontSize = 12 }; });

        Assert.Same(kept, seen);
        Assert.Equal(12, axis.LabelStyle!.FontSize);
        Assert.Equal(Color.FromHex("#D62728"), axis.LabelStyle.Color);
    }

    // ── The verbs that put an axis on a scale with the ticks that scale brings (Symlog, AutoDates, DateFormat) ──

    /// <summary>The verbs of <paramref name="axis"/> on axes of its own, standing on no spine and holding no series.</summary>
    private static AxisVerbs VerbsOf(Axis axis) => new(new Axes(), axis, null, []);

    [Fact]
    public void ALinearAxis_BecomesADateAxisWithAutomaticTicks()
    {
        var axis = new Axis();

        VerbsOf(axis).AutoDates();

        Assert.Equal(AxisScale.Date, axis.Scale);
        Assert.IsType<AutoDateLocator>(axis.TickLocator);
        Assert.IsType<AutoDateFormatter>(axis.TickFormatter);
    }

    [Fact]
    public void TheFormatter_ReadsTheIntervalItsOwnLocatorChose()
    {
        var axis = new Axis();
        VerbsOf(axis).AutoDates();
        var start = new DateTime(2001, 1, 1).ToOADate();
        var end = new DateTime(2020, 1, 1).ToOADate();

        axis.TickLocator!.Locate(start, end);

        Assert.Equal("2001", axis.TickFormatter!.Format(start));
    }

    [Fact]
    public void ADateAxisWithAFixedFormat_TakesTheAutomaticTicksInstead()
    {
        var axis = new Axis { Scale = AxisScale.Date, TickFormatter = new DateTickFormatter { DateFormat = "yyyy" } };

        VerbsOf(axis).AutoDates();

        Assert.IsType<AutoDateFormatter>(axis.TickFormatter);
        Assert.IsType<AutoDateLocator>(axis.TickLocator);
    }

    [Fact]
    public void ALinearAxis_BecomesASymlogAxisWithItsLocator()
    {
        var axis = new Axis();

        VerbsOf(axis).Symlog(2.5);

        Assert.Equal(AxisScale.SymLog, axis.Scale);
        Assert.Equal(2.5, axis.SymLogLinThresh);
        var locator = Assert.IsType<SymlogLocator>(axis.TickLocator);
        Assert.Equal(new SymlogLocator(2.5).Locate(-100, 100), locator.Locate(-100, 100));
    }

    [Fact]
    public void ASymlogAxis_ReplacesALocatorItHad_AndKeepsItsFormatter()
    {
        var formatter = new LogTickFormatter();
        var axis = new Axis { TickLocator = new MultipleLocator(5), TickFormatter = formatter };

        VerbsOf(axis).Symlog(1);

        Assert.IsType<SymlogLocator>(axis.TickLocator);
        Assert.Same(formatter, axis.TickFormatter);
    }

    [Fact]
    public void ALinearAxis_BecomesADateAxisWithAFixedFormat_AndKeepsItsLocator()
    {
        var locator = new MultipleLocator(5);
        var axis = new Axis { TickLocator = locator };

        VerbsOf(axis).DateFormat("yyyy-MM");

        Assert.Equal(AxisScale.Date, axis.Scale);
        Assert.Equal("yyyy-MM", Assert.IsType<DateTickFormatter>(axis.TickFormatter).DateFormat);
        Assert.Same(locator, axis.TickLocator);
    }

    [Fact]
    public void TheDateFormatVerbs_WriteTheJsonTheyAlwaysWrote()
    {
        var verbs = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0])
            .SetXDateFormat("yyyy").SetYDateFormat("MM-dd")).ToJson();

        var byHand = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0])).Build();
        var axes = byHand.SubPlots[0];
        (axes.XAxis.Scale, axes.XAxis.TickFormatter) = (AxisScale.Date, new DateTickFormatter { DateFormat = "yyyy" });
        (axes.YAxis.Scale, axes.YAxis.TickFormatter) = (AxisScale.Date, new DateTickFormatter { DateFormat = "MM-dd" });

        Assert.Equal(byHand.ToJson(), verbs);
    }

    [Fact]
    public void TheSymlogVerbs_WriteTheJsonTheyAlwaysWrote()
    {
        var verbs = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [-5.0, 50.0])
            .WithSymlogYScale(2.5).WithSymlogXScale(0.5)).ToJson();

        var byHand = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [-5.0, 50.0])).Build();
        var axes = byHand.SubPlots[0];
        (axes.YAxis.Scale, axes.YAxis.SymLogLinThresh, axes.YAxis.TickLocator) = (AxisScale.SymLog, 2.5, new SymlogLocator(2.5));
        (axes.XAxis.Scale, axes.XAxis.SymLogLinThresh, axes.XAxis.TickLocator) = (AxisScale.SymLog, 0.5, new SymlogLocator(0.5));

        Assert.Equal(byHand.ToJson(), verbs);
    }
}
