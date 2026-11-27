// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The axes joined to a subplot: added Y axes stack, the title gap is the container's, and an inset is configured where it
/// stands.</summary>
public class AxesBuilderLinkedAxesVerbsTests
{
    /// <summary>Every call stacks one more right axis past the ones before it, as often as the caller wants it: three
    /// calls give three right axes, each with its own label and series, and the left side keeps only the primary.</summary>
    [Fact]
    public void WithAddRightYAxis_CalledThreeTimes_StacksThreeRightAxes()
    {
        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithAddRightYAxis(right => right.SetYLabel("Rate").Plot([1.0, 2.0], [10.0, 20.0]))
                .WithAddRightYAxis(right => right.SetYLabel("Load").Plot([1.0, 2.0], [0.1, 0.2]))
                .WithAddRightYAxis(right => right.SetYLabel("Temp").Plot([1.0, 2.0], [300.0, 310.0])))
            .Build().SubPlots[0];

        Assert.Equal(["Rate", "Load", "Temp"], axes.YAxes.Right.Select(slot => slot.Axis.Label));
        Assert.All(axes.YAxes.Right, slot => Assert.Single(slot.Series));
        Assert.Single(axes.YAxes.Left);
    }

    /// <summary>The container's title gap is 4 px (matplotlib's <c>axes.labelpad</c>) until the figure sets another, for
    /// the whole container, and the verb chains.</summary>
    [Fact]
    public void WithYAxisTitleGap_SetsTheContainersGap_AndChains()
    {
        var plain = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0])).Build().SubPlots[0];
        var set = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0]).WithYAxisTitleGap(7.5).WithTitle("chained"))
            .Build().SubPlots[0];

        Assert.Equal(4, plain.YAxes.TitleGap);
        Assert.Equal(7.5, set.YAxes.TitleGap);
        Assert.Equal("chained", set.Title);
    }

    /// <summary>A gap must be a finite number of pixels, 0 or more: a negative, NaN or infinite one is refused, naming the
    /// parameter, and the container keeps its gap.</summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void WithYAxisTitleGap_Negative_Throws(double pixels)
    {
        var refused = Assert.Throws<ArgumentOutOfRangeException>(() => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithYAxisTitleGap(pixels)).Build());
        var axes = new Axes();
        var refusedOnTheModel = Assert.Throws<ArgumentOutOfRangeException>(() => axes.YAxes.TitleGap = pixels);

        Assert.Equal("pixels", refused.ParamName);
        Assert.Equal("value", refusedOnTheModel.ParamName);
        Assert.Equal(4, axes.YAxes.TitleGap);
    }

    /// <summary>The primary Y axis is left slot 0 and stays there: the first axis added on the left is left slot 1, the
    /// next stacks past it, and the primary keeps its label and its series.</summary>
    [Fact]
    public void WithAddLeftYAxis_StacksPastThePrimary()
    {
        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .SetYLabel("Primary")
                .WithAddLeftYAxis(left => left.SetYLabel("Queue").Plot([1.0, 2.0], [5.0, 6.0]))
                .WithAddLeftYAxis(left => left.SetYLabel("Depth").Plot([1.0, 2.0], [7.0, 8.0])))
            .Build().SubPlots[0];

        Assert.Same(axes.YAxis, axes.YAxes.Left[0].Axis);
        Assert.Equal(["Primary", "Queue", "Depth"], axes.YAxes.Left.Select(slot => slot.Axis.Label));
        Assert.Same(axes.Series, axes.YAxes.Left[0].Series);
        Assert.All(axes.YAxes.Left, slot => Assert.Single(slot.Series));
        Assert.Empty(axes.YAxes.Right);
    }

    /// <summary>The primary, one more on the left and two on the right make four Y axes, each with its own label and
    /// series, numbered in the order the subplot's series have always been (primary, right axes, left axes past the
    /// primary) and each drawing its title.</summary>
    [Fact]
    public void WithAddRightYAxis_AndLeft_FourAxesTotal_EachWithItsOwnLabelAndSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], line => line.Label = "p line")
                .SetYLabel("Primary")
                .WithAddLeftYAxis(left => left.SetYLabel("Left 1").Plot([1.0, 2.0], [5.0, 6.0], line => line.Label = "l1 line"))
                .WithAddRightYAxis(right => right.SetYLabel("Right 0").Plot([1.0, 2.0], [50.0, 60.0], line => line.Label = "r0 line"))
                .WithAddRightYAxis(right => right.SetYLabel("Right 1").Plot([1.0, 2.0], [500.0, 600.0], line => line.Label = "r1 line")))
            .Build();
        var axes = figure.SubPlots[0];

        var slots = axes.YAxes.IdentityOrder.ToArray();
        Assert.Equal(["Primary", "Right 0", "Right 1", "Left 1"], slots.Select(slot => slot.Axis.Label));
        Assert.All(slots, slot => Assert.Single(slot.Series));
        Assert.Equal(["p line", "r0 line", "r1 line", "l1 line"], axes.AllSeries.Select(series => series.Label));
        string svg = figure.ToSvg();
        Assert.All(slots, slot => Assert.Contains($">{slot.Axis.Label}<", svg, StringComparison.Ordinal));
    }

    /// <summary>The first call on a side with no axis yet creates that side's slot 0 and colours its spine; a call after
    /// it stacks an axis that draws no spine, so colouring it leaves the right spine as the first axis set it.</summary>
    [Fact]
    public void WithAddRightYAxis_FirstCallIsRightSlotZero_AndOnlyItsColourReachesTheRightSpine()
    {
        var first = Color.FromHex("#1F77B4");
        var second = Color.FromHex("#FF7F0E");

        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithAddRightYAxis(right => right.Plot([1.0], [2.0]).SetYAxisColor(first))
                .WithAddRightYAxis(right => right.Plot([1.0], [3.0]).SetYAxisColor(second)))
            .Build().SubPlots[0];

        Assert.Equal(2, axes.YAxes.Right.Count);
        Assert.Equal(first, axes.Spines.Right.Color);
        Assert.Equal(first, axes.YAxes.Right[0].Axis.MajorTicks.Color);
        Assert.Equal(second, axes.YAxes.Right[1].Axis.MajorTicks.Color);
        Assert.Equal(second, axes.YAxes.Right[1].Axis.LabelStyle!.Color);
    }

    /// <summary>The inset the callback fills, past one line on the subplot.</summary>
    private static Axes InsetOf(Func<AxesBuilder, AxesBuilder> configure) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AddInset(0.6, 0.1, 0.35, 0.35, inset => configure(inset.Plot([1.5, 2.0], [3.5, 4.0]))))
            .Build().SubPlots[0].Insets[0];

    [Fact]
    public void AddInset_PlacesTheInsetAtItsBounds_EitherWayTheyAreGiven()
    {
        var byNumbers = InsetOf(b => b);
        var byBounds = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddInset(new InsetBounds(0.6, 0.1, 0.35, 0.35), inset => inset.Plot([1.5, 2.0], [3.5, 4.0])))
            .Build().SubPlots[0].Insets[0];

        Assert.Equal(new InsetBounds(0.6, 0.1, 0.35, 0.35), byNumbers.InsetBounds);
        Assert.Equal(byNumbers.InsetBounds, byBounds.InsetBounds);
        Assert.Single(byBounds.Series);
    }

    /// <summary>The inset is configured where it stands, so nothing configured inside the callback has to be carried across
    /// to it: an axis scale, an overlay, a user tick formatter or locator, the legend and a colour bar stay as set.</summary>
    [Fact]
    public void AddInset_KeepsWhatItsCallbackSets()
    {
        var formatter = new TestTickFormatter(value => $"<{value}>");

        var inset = InsetOf(b => b
            .SetYScale(AxisScale.Log)
            .AxHLine(2.5)
            .Annotate("peak", 2.0, 4.0)
            .AxVSpan(1.6, 1.8)
            .AddTrendline(1.5, 3.5, 2.0, 4.0)
            .AddLevel(3.8)
            .AddFibonacci(4.0, 3.5)
            .SetXTickFormatter(formatter)
            .SetYTickFormatter(formatter)
            .SetXTickLocator(new FixedLocator([1.6, 1.8]))
            .WithLegend(LegendPosition.LowerLeft)
            .WithColorBar()
            .SignalXY([1.0, 2.0], [3.0, 4.0])
            .PlanarBar3D([1.0, 2.0], [1.0, 2.0], [3.0, 4.0]));

        Assert.Equal(AxisScale.Log, inset.YAxis.Scale);
        Assert.Equal(2.5, Assert.Single(inset.ReferenceLines).Value);
        Assert.Equal("peak", Assert.Single(inset.Annotations).Text);
        Assert.Single(inset.Spans);
        Assert.Single(inset.Trendlines);
        Assert.Equal(3.8, Assert.Single(inset.HorizontalLevels).Value);
        Assert.Single(inset.FibonacciRetracements);
        Assert.Same(formatter, inset.XAxis.TickFormatter);
        Assert.Same(formatter, inset.YAxis.TickFormatter);
        Assert.IsType<FixedLocator>(inset.XAxis.TickLocator);
        Assert.Equal(LegendPosition.LowerLeft, inset.Legend.Position);
        Assert.True(inset.Legend.Visible);
        Assert.NotNull(inset.ColorBar);
        Assert.Contains(inset.Series, s => s is SignalXYSeries);
        Assert.Contains(inset.Series, s => s is PlanarBar3DSeries);
    }

    /// <summary>Under the classic theme, whose grid is off, an inset that changed one property of its grid draws no grid: the
    /// change is a change over the grid of the theme the figure is drawn under, never a constant fixed at build time.</summary>
    [Fact]
    public void AddInset_GridFollowsTheFiguresTheme()
    {
        var theme = Theme.MatplotlibV2;
        Assert.False(theme.DefaultGrid.Visible);
        var figure = Plt.Create().WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AddInset(0.6, 0.1, 0.35, 0.35, inset => inset
                    .Plot([1.5, 2.0], [3.5, 4.0])
                    .WithGrid(g => g with { Alpha = 0.5 })))
            .Build();

        var drawn = figure.SubPlots[0].Insets[0].GridUnder(theme);
        Assert.False(drawn.Visible);
        Assert.Equal(0.5, drawn.Alpha);
        Assert.True(figure.SubPlots[0].Insets[0].GridUnder(Theme.Default).Visible);
    }

    /// <summary>A share key set inside the callback is not consumed: the inset is not a subplot of the figure, so it joins
    /// no share group.</summary>
    [Fact]
    public void AddInset_DoesNotConsumeAShareKey()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .ShareX("k")
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AddInset(0.6, 0.1, 0.35, 0.35, inset => inset.Plot([1.5, 2.0], [3.5, 4.0]).ShareX("k")))
            .Build();

        Assert.Null(figure.SubPlots[0].ShareXWith);
        Assert.Null(figure.SubPlots[0].Insets[0].ShareXWith);
    }

    /// <summary>The callback of an inset may itself add an inset: the second one is configured in place too.</summary>
    [Fact]
    public void AddInset_NestsInPlace()
    {
        var formatter = new TestTickFormatter(value => $"<{value}>");

        var inner = InsetOf(b => b.AddInset(0.1, 0.1, 0.5, 0.5, i => i.Plot([1.5, 1.6], [3.5, 3.6]).SetXTickFormatter(formatter))).Insets[0];

        Assert.Same(formatter, inner.XAxis.TickFormatter);
    }
}
