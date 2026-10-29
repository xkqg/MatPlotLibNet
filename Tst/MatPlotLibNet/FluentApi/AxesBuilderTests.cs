// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="AxesBuilder"/> fluent API methods.</summary>
public class AxesBuilderTests
{
    /// <summary>Verifies that WithTitle sets the axes title.</summary>
    [Fact]
    public void WithTitle_SetsTitle()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("My Plot"))
            .Build();
        Assert.Equal("My Plot", figure.SubPlots[0].Title);
    }

    /// <summary>Verifies that SetXLabel sets the X-axis label.</summary>
    [Fact]
    public void SetXLabel_SetsXAxisLabel()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXLabel("Time"))
            .Build();
        Assert.Equal("Time", figure.SubPlots[0].XAxis.Label);
    }

    /// <summary>Verifies that SetYLabel sets the Y-axis label.</summary>
    [Fact]
    public void SetYLabel_SetsYAxisLabel()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetYLabel("Value"))
            .Build();
        Assert.Equal("Value", figure.SubPlots[0].YAxis.Label);
    }

    /// <summary>Verifies that SetXLim sets the X-axis min and max limits.</summary>
    [Fact]
    public void SetXLim_SetsXAxisLimits()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXLim(0, 100))
            .Build();
        Assert.Equal(0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(100, figure.SubPlots[0].XAxis.Max);
    }

    /// <summary>Verifies that SetYLim sets the Y-axis min and max limits.</summary>
    [Fact]
    public void SetYLim_SetsYAxisLimits()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetYLim(-50, 50))
            .Build();
        Assert.Equal(-50, figure.SubPlots[0].YAxis.Min);
        Assert.Equal(50, figure.SubPlots[0].YAxis.Max);
    }

    /// <summary>Verifies that SetXScale sets the X-axis scale type.</summary>
    [Fact]
    public void SetXScale_SetsScale()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXScale(AxisScale.Log))
            .Build();
        Assert.Equal(AxisScale.Log, figure.SubPlots[0].XAxis.Scale);
    }

    /// <summary>Verifies that ShowGrid enables grid visibility.</summary>
    [Fact]
    public void ShowGrid_EnablesGrid()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.ShowGrid())
            .Build();
        Assert.True(figure.SubPlots[0].Grid.Visible);
    }

    /// <summary>Verifies that Plot and Scatter add series to the axes.</summary>
    [Fact]
    public void Plot_AddsSeriesToAxes()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [3.0, 4.0]);
                ax.Scatter([5.0], [6.0]);
            })
            .Build();

        Assert.Equal(2, figure.SubPlots[0].Series.Count);
    }

    /// <summary>Verifies that all fluent methods can be chained together.</summary>
    [Fact]
    public void MethodChaining_Works()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithTitle("Chart")
                  .SetXLabel("X")
                  .SetYLabel("Y")
                  .SetXLim(0, 10)
                  .SetYLim(0, 100)
                  .ShowGrid()
                  .Plot([1.0], [2.0]);
            })
            .Build();

        var axes = figure.SubPlots[0];
        Assert.Equal("Chart", axes.Title);
        Assert.Equal("X", axes.XAxis.Label);
        Assert.True(axes.Grid.Visible);
    }

    /// <summary>Verifies that Pie adds a <see cref="PieSeries"/> to the axes.</summary>
    [Fact]
    public void Pie_AddsPieSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pie([30.0, 70.0]))
            .Build();
        Assert.IsType<PieSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Heatmap adds a <see cref="HeatmapSeries"/> to the axes.</summary>
    [Fact]
    public void Heatmap_AddsHeatmapSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }))
            .Build();
        Assert.IsType<HeatmapSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that BoxPlot adds a <see cref="BoxSeries"/> to the axes.</summary>
    [Fact]
    public void BoxPlot_AddsBoxSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot([[1.0, 2.0, 3.0]]))
            .Build();
        Assert.IsType<BoxSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Violin adds a <see cref="ViolinSeries"/> to the axes.</summary>
    [Fact]
    public void Violin_AddsViolinSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Violin([[1.0, 2.0, 3.0]]))
            .Build();
        Assert.IsType<ViolinSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Contour adds a <see cref="ContourSeries"/> to the axes.</summary>
    [Fact]
    public void Contour_AddsContourSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contour([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } }))
            .Build();
        Assert.IsType<ContourSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Stem adds a <see cref="StemSeries"/> to the axes.</summary>
    [Fact]
    public void Stem_AddsStemSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stem([1.0, 2.0], [3.0, 4.0]))
            .Build();
        Assert.IsType<StemSeries>(figure.SubPlots[0].Series[0]);
    }

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

        Assert.Equal(3, axes.YAxes.Right.Count);
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

        Assert.Equal(3, axes.YAxes.Left.Count);
        Assert.Same(axes.YAxis, axes.YAxes.Left[0].Axis);
        Assert.Equal(["Primary", "Queue", "Depth"], axes.YAxes.Left.Select(slot => slot.Axis.Label));
        Assert.Single(axes.Series);
        Assert.Same(axes.Series, axes.YAxes.Left[0].Series);
        Assert.Single(axes.YAxes.Left[1].Series);
        Assert.Single(axes.YAxes.Left[2].Series);
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
        Assert.Equal(4, slots.Length);
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

    [Fact]
    public void TheAddVerbs_ReturnTheAxesBuilder_ForChaining()
    {
        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithAddLeftYAxis(left => left.Plot([1.0], [2.0]))
                .WithAddRightYAxis(right => right.Plot([1.0], [2.0]))
                .WithTitle("after the add verbs"))
            .Build().SubPlots[0];

        Assert.Equal("after the add verbs", axes.Title);
    }

    [Fact]
    public void TheAddVerbs_WithoutACallback_Throw_AndAddNoAxis()
    {
        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                Assert.Throws<ArgumentNullException>(() => ax.WithAddRightYAxis(null!));
                Assert.Throws<ArgumentNullException>(() => ax.WithAddLeftYAxis(null!));
            })
            .Build().SubPlots[0];

        Assert.Empty(axes.YAxes.Right);
        Assert.Single(axes.YAxes.Left);
    }

    /// <summary>Verifies that Annotate adds an annotation to the axes.</summary>
    [Fact]
    public void Annotate_AddsAnnotation()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .Annotate("peak", 2.0, 4.0))
            .Build();
        Assert.Single(figure.SubPlots[0].Annotations);
    }

    /// <summary>Verifies that AxHLine adds a horizontal reference line.</summary>
    [Fact]
    public void AxHLine_AddsReferenceLine()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AxHLine(3.5))
            .Build();
        Assert.Single(figure.SubPlots[0].ReferenceLines);
    }

    /// <summary>Verifies that AxVSpan adds a vertical span region.</summary>
    [Fact]
    public void AxVSpan_AddsSpanRegion()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AxVSpan(1.2, 1.8))
            .Build();
        Assert.Single(figure.SubPlots[0].Spans);
    }

    /// <summary>Verifies that Radar adds a <see cref="RadarSeries"/> to the axes.</summary>
    [Fact]
    public void Radar_AddsRadarSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(["A", "B", "C"], [1.0, 2.0, 3.0]))
            .Build();
        Assert.IsType<RadarSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that SetBarMode sets the bar stacking mode on the axes.</summary>
    [Fact]
    public void SetBarMode_SetsStackedMode()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .SetBarMode(BarMode.Stacked)
                .Bar(["A", "B"], [10.0, 20.0])
                .Bar(["A", "B"], [5.0, 10.0]))
            .Build();
        Assert.Equal(BarMode.Stacked, figure.SubPlots[0].BarMode);
    }

    /// <summary>Verifies that Quiver adds a <see cref="QuiverSeries"/> to the axes.</summary>
    [Fact]
    public void Quiver_AddsQuiverSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Quiver([1.0], [2.0], [0.5], [0.5]))
            .Build();
        Assert.IsType<QuiverSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Candlestick adds a <see cref="CandlestickSeries"/> to the axes.</summary>
    [Fact]
    public void Candlestick_AddsCandlestickSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Candlestick([10.0], [15.0], [8.0], [13.0]))
            .Build();
        Assert.IsType<CandlestickSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that ErrorBar adds an <see cref="ErrorBarSeries"/> to the axes.</summary>
    [Fact]
    public void ErrorBar_AddsErrorBarSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.ErrorBar([1.0, 2.0], [3.0, 4.0], [0.1, 0.1], [0.2, 0.2]))
            .Build();
        Assert.IsType<ErrorBarSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that Step adds a <see cref="StepSeries"/> to the axes.</summary>
    [Fact]
    public void Step_AddsStepSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Step([1.0, 2.0], [3.0, 4.0]))
            .Build();
        Assert.IsType<StepSeries>(figure.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that FillBetween adds an <see cref="AreaSeries"/> to the axes.</summary>
    [Fact]
    public void FillBetween_AddsAreaSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.FillBetween([1.0, 2.0], [3.0, 4.0]))
            .Build();
        Assert.IsType<AreaSeries>(figure.SubPlots[0].Series[0]);
    }

    // --- Spines builder tests ---

    /// <summary>Verifies that WithSpines configures spines on the built axes.</summary>
    [Fact]
    public void WithSpines_ConfiguresSpines()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithSpines(s => s with { Top = s.Top with { Visible = false } })
                .Plot([1.0], [2.0]))
            .Build();
        Assert.False(figure.SubPlots[0].Spines.Top.Visible);
        Assert.True(figure.SubPlots[0].Spines.Bottom.Visible);
    }

    /// <summary>Verifies that HideTopSpine hides only the top spine.</summary>
    [Fact]
    public void HideTopSpine_HidesTopOnly()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideTopSpine().Plot([1.0], [2.0]))
            .Build();
        Assert.False(figure.SubPlots[0].Spines.Top.Visible);
        Assert.True(figure.SubPlots[0].Spines.Bottom.Visible);
        Assert.True(figure.SubPlots[0].Spines.Left.Visible);
        Assert.True(figure.SubPlots[0].Spines.Right.Visible);
    }

    /// <summary>Verifies that HideRightSpine hides only the right spine.</summary>
    [Fact]
    public void HideRightSpine_HidesRightOnly()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.HideRightSpine().Plot([1.0], [2.0]))
            .Build();
        Assert.False(figure.SubPlots[0].Spines.Right.Visible);
        Assert.True(figure.SubPlots[0].Spines.Left.Visible);
    }

    // --- Inset axes builder tests ---

    /// <summary>Verifies that AddInset creates an inset on the built axes.</summary>
    [Fact]
    public void AddInset_Fluent_CreatesInset()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AddInset(0.6, 0.1, 0.35, 0.35, inset => inset
                    .Plot([1.5, 2.0], [3.5, 4.0])))
            .Build();

        Assert.Single(figure.SubPlots[0].Insets);
    }

    /// <summary>Verifies that the inset has series added via the builder.</summary>
    [Fact]
    public void AddInset_Fluent_ConfiguresInsetSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AddInset(0.6, 0.1, 0.35, 0.35, inset => inset
                    .Plot([1.5, 2.0], [3.5, 4.0])))
            .Build();

        var inset = figure.SubPlots[0].Insets[0];
        Assert.Single(inset.Series);
    }

    // --- Colormap builder tests ---

    /// <summary>Verifies that WithColorMap by name resolves from registry.</summary>
    [Fact]
    public void WithColorMap_String_ResolvesFromRegistry()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorMap("plasma"))
            .Build();

        var hs = (MatPlotLibNet.Models.Series.HeatmapSeries)figure.SubPlots[0].Series[0];
        Assert.NotNull(hs.ColorMap);
        Assert.Equal("plasma", hs.ColorMap!.Name);
    }

    /// <summary>Verifies that WithNormalizer sets the normalizer on the last heatmap series.</summary>
    [Fact]
    public void WithNormalizer_SetsOnHeatmapSeries()
    {
        var norm = new MatPlotLibNet.Styling.ColorMaps.LogNormalizer();
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithNormalizer(norm))
            .Build();

        var hs = (MatPlotLibNet.Models.Series.HeatmapSeries)figure.SubPlots[0].Series[0];
        Assert.Same(norm, hs.Normalizer);
    }

    /// <summary>Verifies that the inset has correct InsetBounds.</summary>
    [Fact]
    public void AddInset_Fluent_SetsInsetBounds()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AddInset(0.6, 0.1, 0.35, 0.35, inset => inset
                    .Plot([1.5, 2.0], [3.5, 4.0])))
            .Build();

        var inset = figure.SubPlots[0].Insets[0];
        Assert.NotNull(inset.InsetBounds);
        Assert.Equal(0.6, inset.InsetBounds.Value.X);
        Assert.Equal(0.35, inset.InsetBounds.Value.Width);
    }

    /// <summary>AddInset hand-copies a subset of the configured builder's state onto the real inset Axes
    /// (series, title, axis labels/min/max, spines, grid) — everything else configured on the inset builder
    /// is silently lost. Measured: an axis scale and a reference line configured inside the callback never
    /// reach the inset that is actually drawn.</summary>
    [Fact]
    public void AddInset_Fluent_CopiesAxisScale()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AddInset(0.6, 0.1, 0.35, 0.35, inset => inset
                    .Plot([1.5, 2.0], [3.5, 4.0])
                    .SetYScale(AxisScale.Log)))
            .Build();

        var inset = figure.SubPlots[0].Insets[0];
        Assert.Equal(AxisScale.Log, inset.YAxis.Scale);
    }

    /// <summary>See <see cref="AddInset_Fluent_CopiesAxisScale"/>.</summary>
    [Fact]
    public void AddInset_Fluent_CopiesReferenceLines()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AddInset(0.6, 0.1, 0.35, 0.35, inset => inset
                    .Plot([1.5, 2.0], [3.5, 4.0])
                    .AxHLine(2.5)))
            .Build();

        var inset = figure.SubPlots[0].Insets[0];
        Assert.Single(inset.ReferenceLines);
        Assert.Equal(2.5, inset.ReferenceLines[0].Value);
    }

    /// <summary>Verifies the canonical InsetBounds overload creates and configures the inset.</summary>
    [Fact]
    public void AddInset_InsetBounds_Fluent_ConfiguresInset()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .AddInset(new InsetBounds(0.6, 0.1, 0.35, 0.35), inset => inset
                    .Plot([1.5, 2.0], [3.5, 4.0])))
            .Build();

        var inset = figure.SubPlots[0].Insets[0];
        Assert.Single(inset.Series);
        Assert.Equal(new InsetBounds(0.6, 0.1, 0.35, 0.35), inset.InsetBounds!.Value);
    }

    // ── Sub-phase E: configure baseline accumulation ─────────────────────────

    [Fact]
    public void WithTitle_CalledTwice_AccumulatesStyle()
    {
        // Second call builds on top of the first rather than discarding it
        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("T", s => s with { FontSize = 14 })
                .WithTitle("T", s => s with { FontWeight = MatPlotLibNet.Styling.FontWeight.Bold }))
            .Build().SubPlots[0];

        Assert.NotNull(axes.TitleStyle);
        Assert.Equal(14, axes.TitleStyle!.FontSize);
        Assert.Equal(MatPlotLibNet.Styling.FontWeight.Bold, axes.TitleStyle!.FontWeight);
    }

    [Fact]
    public void WithColorBar_CalledTwice_AccumulatesSettings()
    {
        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1 }, { 2 } })
                .WithColorBar(cb => cb with { Label = "First" })
                .WithColorBar(cb => cb with { Visible = true }))
            .Build().SubPlots[0];

        Assert.NotNull(axes.ColorBar);
        Assert.True(axes.ColorBar!.Visible);
        Assert.Equal("First", axes.ColorBar!.Label);
    }

    /// <summary>Verifies that a coloured left Y axis keeps its spine, tick marks, tick labels and title colour through
    /// JSON — the verb works in-process and must not vanish in transit.</summary>
    [Fact]
    public void SetYAxisColor_SurvivesJsonRoundTrip()
    {
        var colour = Color.FromHex("#E4007C");
        var json = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetYLabel("Level").SetYAxisColor(colour))
            .ToJson();

        var axes = new ChartSerializer().FromJson(json).SubPlots[0];

        Assert.Equal(colour, axes.Spines.Left.Color);
        Assert.Equal(colour, axes.YAxis.MajorTicks.Color);
        Assert.Equal(colour, axes.YAxis.MajorTicks.LabelColor);
        Assert.Equal(colour, axes.YAxis.LabelStyle!.Color);
        Assert.Equal("Level", axes.YAxis.Label);
        Assert.Null(axes.Spines.Bottom.Color);
        Assert.Null(axes.XAxis.MajorTicks.Color);
    }

    /// <summary>Verifies that a coloured bottom X axis keeps its spine, tick marks, tick labels and title colour through JSON.</summary>
    [Fact]
    public void SetXAxisColor_SurvivesJsonRoundTrip()
    {
        var colour = Color.FromHex("#0A7E8C");
        var json = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXLabel("Time").SetXAxisColor(colour))
            .ToJson();

        var axes = new ChartSerializer().FromJson(json).SubPlots[0];

        Assert.Equal(colour, axes.Spines.Bottom.Color);
        Assert.Equal(colour, axes.XAxis.MajorTicks.Color);
        Assert.Equal(colour, axes.XAxis.MajorTicks.LabelColor);
        Assert.Equal(colour, axes.XAxis.LabelStyle!.Color);
        Assert.Equal("Time", axes.XAxis.Label);
        Assert.Null(axes.Spines.Left.Color);
        Assert.Null(axes.YAxis.MajorTicks.Color);
    }
}
