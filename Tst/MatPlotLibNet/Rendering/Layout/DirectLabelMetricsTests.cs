// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary><see cref="DirectLabelMetrics"/> is the one owner of what an end-of-line label says, the font it is drawn
/// in and the gap before it. The layout engine reserves room with it and the label drawing draws with it, so the two
/// cannot measure different text.</summary>
public class DirectLabelMetricsTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];

    private static DirectLabelMetrics Metrics(Action<AxesBuilder> configure, IRenderContext? ctx = null)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, configure).Build();
        return new DirectLabelMetrics(figure.SubPlots[0], figure.Theme, ctx ?? new RecordingRenderContext());
    }

    private static List<string> Texts(DirectLabelMetrics m) => m.Labels.Select(l => l.Text).ToList();

    [Fact]
    public void TheTextIsTheSeriesLabel()
    {
        var m = Metrics(ax => ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = "Revenue").WithDirectLabel());

        Assert.Equal(["Revenue"], Texts(m));
    }

    [Fact]
    public void WithLegendValues_TheTextCarriesTheLastValue_InTheLegendsFormat()
    {
        var m = Metrics(ax => ax.Plot(X, [1.0, 2.0, 3.25], s => s.Label = "Revenue").WithDirectLabel().WithLegendValues());

        Assert.Equal(["Revenue = 3.25"], Texts(m));
    }

    /// <summary>The label is placed at the last FINITE point and the suffix names that point's value: one rule for both,
    /// so the text never says "NaN" and never names a value the label is not standing next to.</summary>
    [Fact]
    public void WithLegendValues_ANonFiniteLastValue_TakesThePreviousFinitePoint()
    {
        var m = Metrics(ax => ax
            .Plot(X, [1.0, 2.0, double.NaN], s => s.Label = "nan").WithDirectLabel()
            .Plot(X, [1.0, 2.0, double.PositiveInfinity], s => s.Label = "inf").WithDirectLabel()
            .WithLegendValues());

        Assert.Equal(["nan = 2.00", "inf = 2.00"], Texts(m));
        Assert.All(m.Labels, l =>
        {
            Assert.Equal(1.0, l.EndX);
            Assert.Equal(2.0, l.EndY);
        });
    }

    [Fact]
    public void ALineWithoutAFinitePoint_HasNoLabel()
    {
        var m = Metrics(ax => ax
            .Plot(Array.Empty<double>(), Array.Empty<double>(), s => s.Label = "empty").WithDirectLabel()
            .Plot(X, [double.NaN, double.NaN, double.NaN], s => s.Label = "all NaN").WithDirectLabel()
            .Plot([double.NaN], [1.0], s => s.Label = "NaN x").WithDirectLabel()
            .WithLegendValues());

        Assert.Empty(m.Labels);
        Assert.Equal(0, m.WidestLabel());
    }

    [Fact]
    public void TheVisibleXRange_DecidesWhichPointIsTheEnd()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.25], s => s.Label = "Revenue").WithDirectLabel().WithLegendValues()).Build();

        var m = new DirectLabelMetrics(figure.SubPlots[0], figure.Theme, new RecordingRenderContext(),
            new MatPlotLibNet.Numerics.MinMaxRange(-1, 1.5));

        Assert.Equal(["Revenue = 2.00"], Texts(m));
        Assert.Equal(1.0, m.Labels[0].EndX);
    }

    [Fact]
    public void AnInvertedVisibleRange_IsReadAsTheSameInterval()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.25], s => s.Label = "Revenue").WithDirectLabel().WithLegendValues()).Build();

        var m = new DirectLabelMetrics(figure.SubPlots[0], figure.Theme, new RecordingRenderContext(),
            new MatPlotLibNet.Numerics.MinMaxRange(1.5, -1));

        Assert.Equal(["Revenue = 2.00"], Texts(m));
    }

    [Fact]
    public void WithoutAVisibleRange_TheAxisLimitsBoundTheEnd()
    {
        var m = Metrics(ax => ax
            .Plot(X, [1.0, 2.0, 3.25], s => s.Label = "lower").WithDirectLabel()
            .SetXLim(0.5, 1.5).WithLegendValues());

        Assert.Equal(["lower = 2.00"], Texts(m));
    }

    [Fact]
    public void ALineWhollyOutsideTheXLimits_HasNoLabel()
    {
        var m = Metrics(ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "left of the range").WithDirectLabel()
            .Plot([30.0, 31.0], [1.0, 2.0], s => s.Label = "right of the range").WithDirectLabel()
            .SetXLim(10, 20));

        Assert.Empty(m.Labels);
    }

    [Fact]
    public void WithoutAnyXLimit_EveryPointIsInRange()
    {
        var m = Metrics(ax => ax.Plot([-1e300, 1e300], [1.0, 4.0], s => s.Label = "wide").WithDirectLabel().WithLegendValues());

        Assert.Equal(["wide = 4.00"], Texts(m));
    }

    [Fact]
    public void AStreamingLine_IsLabelled_WithItsLastValue()
    {
        var m = Metrics(ax =>
        {
            var live = ax.StreamingPlot(configure: s =>
            {
                s.Label = "live";
                s.LabelPlacement = SeriesLabelPlacement.LineEnd;
            });
            live.AppendPoint(1, 3);
            live.AppendPoint(2, 4.5);
            ax.WithLegendValues();
        });

        Assert.Equal(["live = 4.50"], Texts(m));
    }

    [Fact]
    public void OnlyVisibleLabelledLineEndSeries_AreListed_PrimaryThenSecondary_EachWithTheSlotItIsDrawnAgainst()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Scatter(X, [1.0, 2.0, 3.0], s => s.Label = "a scatter has no line end")
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "legend only")
            .Plot(X, [1.0, 2.0, 3.0], s => s.LabelPlacement = SeriesLabelPlacement.LineEnd)
            .Plot(X, [1.0, 2.0, 3.0], s => { s.Label = "hidden"; s.Visible = false; }).WithDirectLabel()
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "primary").WithDirectLabel()
            .WithAddRightYAxis(y2 => y2.Plot(X, [5.0, 6.0, 7.0], s => s.Label = "secondary").WithDirectLabel())).Build();
        var axes = figure.SubPlots[0];
        var m = new DirectLabelMetrics(axes, figure.Theme, new RecordingRenderContext());

        Assert.Equal(
            [new DirectLabel(m.Labels[0].Series, SeriesAxisPair.Primary, 4, axes.YAxes.Left[0], "primary", 2.0, 3.0),
             new DirectLabel(m.Labels[1].Series, SeriesAxisPair.SecondaryY, 0, axes.YAxes.Right[0], "secondary", 2.0, 7.0)],
            m.Labels);
        Assert.Equal("primary", m.Labels[0].Series.Label);
        Assert.Equal("secondary", m.Labels[1].Series.Label);
    }

    [Fact]
    public void OnPolarAxes_NoLabelIsListed_BecauseNoneIsDrawn()
    {
        var m = Metrics(ax => ax
            .PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0])
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "flagged").WithDirectLabel());

        Assert.Empty(m.Labels);
        Assert.Equal(0, m.WidestLabel());
    }

    [Fact]
    public void TheFontIsTheLegendFont_IncludingItsSizeOverride()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0])).Build();
        var plain = new DirectLabelMetrics(figure.SubPlots[0], figure.Theme, new RecordingRenderContext());
        var sized = Metrics(ax => ax.Plot(X, [1.0, 2.0, 3.0]).WithLegend(l => l with { FontSize = 17 }));

        Assert.Equal(ThemedFontProvider.TickFont(figure.Theme), plain.Font);
        Assert.Equal(17, sized.Font.Size);
    }

    [Fact]
    public void WidestLabel_IsTheWidestMeasuredText()
    {
        var ctx = new RecordingRenderContext();
        var m = Metrics(ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "ab").WithDirectLabel()
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "abcdef").WithDirectLabel()
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "abc").WithDirectLabel(), ctx);

        Assert.Equal(ctx.MeasureText("abcdef", m.Font).Width, m.WidestLabel(), 9);
    }

    [Fact]
    public void AMathLabel_IsMeasuredAsTheRichTextItRendersAs()
    {
        var ctx = new RecordingRenderContext();
        const string label = @"$\alpha_{max}$";
        var m = Metrics(ax => ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = label).WithDirectLabel(), ctx);

        Assert.Equal(((IRenderContext)ctx).MeasureRichText(MathTextParser.Parse(label), m.Font).Width, m.Measure(label).Width, 9);
        Assert.NotEqual(ctx.MeasureText(label, m.Font).Width, m.Measure(label).Width);
    }
}
