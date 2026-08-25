// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>A dual-axis chart has two units and therefore two stories, and both of them must be tellable:
/// the legend has to NAME the right-hand trace, and a live chart has to be able to APPEND to it.
///
/// <para>Neither held before this suite. <c>AxesRenderer.RenderLegend</c> walked <c>Axes.Series</c> only, so a
/// secondary trace was silently absent from the legend — a one-entry legend beside two lines reads as "the
/// other line has no name". And <c>StreamingPlot</c> existed only on the primary axes, so a live chart could
/// stream one unit and had to redraw the whole figure to move the other.</para></summary>
public class SecondaryAxisLegendAndStreamingTests
{
    [Fact]
    public void TheLegendNAMESTheSecondaryTrace_notOnlyThePrimaryOne()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5), l => l.Label = "left-line")
                .WithSecondaryYAxis(s => s
                    .Plot(EdgeCaseData.Ramp(5), new[] { 10.0, 20, 30, 40, 50 }, l => l.Label = "right-line"))
                .WithLegend())
            .ToSvg();

        Assert.Contains("left-line", svg);
        Assert.Contains("right-line", svg);
    }

    [Fact]
    public void TheSecondaryLegendEntryWearsTheCOLOURTheSeriesIsDrawnIn()
    {
        // The secondary renderer offsets its cycle index by the primary series count
        // (CartesianSecondaryYAxisPart.Render), so a legend that started its own count at 0 would hand the
        // right-hand line the LEFT one's colour — a key that points at the wrong trace.
        var explicitColour = Color.FromHex("#123456");

        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5), l => l.Label = "left-line")
                .WithSecondaryYAxis(s => s
                    .Plot(EdgeCaseData.Ramp(5), new[] { 10.0, 20, 30, 40, 50 }, l =>
                    {
                        l.Label = "right-line";
                        l.Color = explicitColour;
                    }))
                .WithLegend())
            .ToSvg();

        Assert.Contains("#123456", svg);
    }

    [Fact]
    public void AnUnlabelledSecondarySeriesAddsNoLegendEntry()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5), l => l.Label = "left-line")
                .WithSecondaryYAxis(s => s.Plot(EdgeCaseData.Ramp(5), new[] { 10.0, 20, 30, 40, 50 }))
                .WithLegend())
            .ToSvg();

        Assert.Contains("left-line", svg);
        Assert.DoesNotContain("Series 2", svg);
    }

    [Fact]
    public void AStreamingSeriesCanLiveOnTheSecondaryAxis_soALiveChartCanCarryTwoUnits()
    {
        StreamingLineSeries? right = null;

        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5), l => l.Label = "left-line")
                .WithSecondaryYAxis(s =>
                    right = s.StreamingPlot(capacity: 100, configure: l => l.Label = "right-live")))
            .Build();

        Assert.NotNull(right);
        Assert.Same(right, (object)Assert.Single(fig.SubPlots[0].SecondarySeries));
    }

    [Fact]
    public void ASecondaryStreamingSeriesCONTRIBUTESItsRangeToTheRightAxis()
    {
        // The range walk used to gate on `is IHasDataRange`, a marker a streaming series does not carry, so
        // its data never reached the right-hand scale. (DRAWING a streaming series in SVG is a separate,
        // still-open gap: ISeriesVisitor.Visit(StreamingLineSeries) is an empty default body and
        // SvgSeriesRenderer does not override it, so no streaming series renders to SVG on EITHER axis.)
        StreamingLineSeries? right = null;

        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(new[] { 0.0, 1, 2 }, new[] { 0.0, 1, 2 }, l => l.Label = "left-line")
                .WithSecondaryYAxis(s =>
                {
                    right = s.StreamingPlot(capacity: 100, configure: l => l.Label = "right-live");
                    s.SetYLabel("right");
                }))
            .Build();

        right!.AppendPoint(0, 100);
        right.AppendPoint(1, 200);
        right.AppendPoint(2, 300);

        var svg = figure.ToSvg();

        // The right-hand ticks now span the streamed data instead of the 0..1 sentinel.
        Assert.Contains("300", svg);
    }

    [Fact]
    public void AStreamingSeriesIsDRAWNInSvg_onBothAxes()
    {
        // The visitor declares Visit(StreamingLineSeries) as an EMPTY default and SVG never overrode it, so a
        // streaming series rendered nothing at all — on either axis — while its range was folded in, leaving
        // axes scaled to data no reader could see. Measured 2026-08-22: 0 polylines for 3 appended points.
        StreamingLineSeries? left = null;
        StreamingLineSeries? right = null;

        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                left = ax.StreamingPlot(100, l => l.Label = "left-live");
                ax.WithSecondaryYAxis(s => right = s.StreamingPlot(100, l => l.Label = "right-live"));
            })
            .Build();

        for (var i = 0; i < 3; i++)
        {
            left!.AppendPoint(i, i + 1);
            right!.AppendPoint(i, (i + 1) * 100);
        }

        var svg = figure.ToSvg();

        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(svg, "<polyline").Count);
    }

    [Fact]
    public void AnEmptyStreamingSeriesDrawsNOTHING_notAFlatLineAtZero()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StreamingPlot(100, l => l.Label = "live"))
            .Build();

        var svg = figure.ToSvg();

        Assert.DoesNotContain("<polyline", svg);
    }

    [Fact]
    public void TheSecondaryAxisLabelClearsItsOwnTICKLABELS()
    {
        // The primary Y label has measured its clearance since the fixed-offset era (AxesRenderer: tick
        // length + pad + the widest MEASURED tick label + a gap, rotated 90). The secondary label kept the
        // constant it was extracted with — plot-right + 45, unrotated — so with three-digit ticks it printed
        // straight through them. Measured on a live ops wall, 2026-08-22: "kB / sec" over 225/200/175.
        var svg = Plt.Create()
            .WithSize(600, 300)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(new[] { 0.0, 1, 2 }, new[] { 0.0, 1, 2 }, l => l.Label = "left")
                .WithSecondaryYAxis(s => s
                    .Plot(new[] { 0.0, 1, 2 }, new[] { 0.0, 125.0, 225.0 }, l => l.Label = "right")
                    .SetYLabel("kB / sec")))
            .ToSvg();

        var labelX = double.Parse(
            System.Text.RegularExpressions.Regex.Match(svg, "<text[^>]*x=\"([0-9.]+)\"[^>]*>kB / sec</text>").Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture);
        var tickXs = System.Text.RegularExpressions.Regex.Matches(svg, "<text[^>]*x=\"([0-9.]+)\"[^>]*>(?:225|200|175|125)</text>")
            .Select(m => double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        Assert.NotEmpty(tickXs);
        Assert.True(labelX > tickXs.Max() + 20,
            $"the axis label sits at x={labelX} while its own tick labels start at x={tickXs.Max()}");
    }

    [Fact]
    public void TheSecondaryAxisLabelIsROTATED_likeThePrimaryOne()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(new[] { 0.0, 1, 2 }, new[] { 0.0, 1, 2 })
                .SetYLabel("left unit")
                .WithSecondaryYAxis(s => s
                    .Plot(new[] { 0.0, 1, 2 }, new[] { 0.0, 125.0, 225.0 })
                    .SetYLabel("right unit")))
            .ToSvg();

        var label = System.Text.RegularExpressions.Regex.Match(svg, "<text[^>]*>right unit</text>").Value;
        Assert.Contains("rotate(", label);
    }

    /// <summary>A formatter that hides the numbers but keeps the ticks — legitimate on a right-hand axis
    /// whose scale is carried by its label alone.</summary>
    private sealed class BlankTickFormatter : MatPlotLibNet.Rendering.TickFormatters.ITickFormatter
    {
        public string Format(double value) => string.Empty;
    }

    [Fact]
    public void ASecondaryAxisWithBLANKTickLabelsStillPlacesItsLabel()
    {
        // The measured clearance needs something to measure. With blank tick labels there is nothing to
        // clear, and the label falls back to the constant offset — the arm that carries the placement when
        // the measurement comes back zero.
        var figure = Plt.Create()
            .WithSize(600, 300)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(new[] { 0.0, 1, 2 }, new[] { 0.0, 1, 2 }, l => l.Label = "left")
                .WithSecondaryYAxis(s => s
                    .Plot(new[] { 0.0, 1, 2 }, new[] { 0.0, 125.0, 225.0 }, l => l.Label = "right")
                    .SetYLabel("kB / sec")))
            .Build();
        figure.SubPlots[0].SecondaryYAxis!.TickFormatter = new BlankTickFormatter();

        var svg = figure.ToSvg();

        Assert.Contains(">kB / sec</text>", svg);
        Assert.DoesNotContain(">225</text>", svg);
    }

    [Fact]
    public void AnEmptyStreamingSCATTERSeriesDrawsNOTHING()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StreamingScatter(100, s => s.Label = "live points"))
            .ToSvg();

        Assert.DoesNotContain("<circle", svg);
    }

    [Fact]
    public void AnEmptyStreamingSIGNALSeriesDrawsNOTHING()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StreamingSignal(100, 1.0, s => s.Label = "live signal"))
            .ToSvg();

        Assert.DoesNotContain("<polyline", svg);
    }
}
