// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A line whose style is <see cref="LineStyle.None"/> draws no line ("No line is drawn", as matplotlib's
/// <c>linestyle='None'</c>): a series of markers alone, and the legend entry of one. Before this, the style was written into the
/// stroke and then ignored, so a line that named no line was drawn solid, and a markers-only series was a marked line.</summary>
public class InvisibleStrokeTests
{
    private static readonly Color SeriesColour = Theme.Default.CycleColors[0];

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    private static Figure LineOf(LineStyle style, bool legend = false) =>
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([1.0, 2.0, 3.0], [1.0, 3.0, 2.0], s => { s.LineStyle = style; s.Marker = MarkerStyle.Circle; if (legend) s.Label = "points"; });
            if (legend) ax.WithLegend();
        }).Build();

    /// <summary>The polylines in the series colour.</summary>
    private static int PolylinesOf(RecordingRenderContext drawn) =>
        drawn.OfKind("DrawLines").Count(call => (Color)((dynamic)call.Args!).color == SeriesColour);

    /// <summary>The straight lines in the series colour, which is what a legend swatch of a line is.</summary>
    private static int SwatchLinesOf(RecordingRenderContext drawn) =>
        drawn.OfKind("DrawLine").Count(call => (Color)((dynamic)call.Args!).color == SeriesColour);

    [Fact]
    public void ALineOfStyleNone_DrawsNoLine_ButItsMarkers()
    {
        var drawn = Drawn(LineOf(LineStyle.None));

        Assert.Equal(0, PolylinesOf(drawn));
        Assert.Equal(3, drawn.OfKind("DrawCircle").Count);
        Assert.DoesNotContain("<polyline", LineOf(LineStyle.None).ToSvg());
    }

    [Theory]
    [InlineData(LineStyle.Solid)]
    [InlineData(LineStyle.Dashed)]
    [InlineData(LineStyle.Dotted)]
    [InlineData(LineStyle.DashDot)]
    public void ALineOfAnyOtherStyle_IsStillDrawn(LineStyle style)
    {
        var drawn = Drawn(LineOf(style));

        Assert.Equal(1, PolylinesOf(drawn));
        Assert.Contains("<polyline", LineOf(style).ToSvg());
    }

    [Fact]
    public void AStepOfStyleNone_DrawsNoLine()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax =>
            ax.AddSeries(new StepSeries([1.0, 2.0, 3.0], [1.0, 3.0, 2.0]) { LineStyle = LineStyle.None, Marker = MarkerStyle.Square })).Build();

        Assert.Equal(0, PolylinesOf(Drawn(figure)));
    }

    [Fact]
    public void AnAreaWhoseEdgeIsStyleNone_IsFilledAndHasNoEdge()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax =>
            ax.AddSeries(new AreaSeries([1.0, 2.0, 3.0], [1.0, 3.0, 2.0]) { LineStyle = LineStyle.None })).Build();

        var drawn = Drawn(figure);

        Assert.NotEmpty(drawn.OfKind("DrawPolygon"));
        Assert.Equal(0, PolylinesOf(drawn));
    }

    [Fact]
    public void TheLegendEntry_OfALineOfStyleNone_IsItsMarkerAlone()
    {
        var none = Drawn(LineOf(LineStyle.None, legend: true));
        var solid = Drawn(LineOf(LineStyle.Solid, legend: true));

        Assert.Equal(0, SwatchLinesOf(none));
        Assert.Equal(1, SwatchLinesOf(solid));
        // The markers of the data (3) and the marker of the swatch (1).
        Assert.Equal(4, none.OfKind("DrawCircle").Count);
    }

    [Fact]
    public void TheLegendEntry_OfALineWithNoMarker_AndStyleNone_DrawsNothingButItsLabel()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2.0, 3.0], [1.0, 3.0, 2.0], s => { s.LineStyle = LineStyle.None; s.Label = "nothing"; })
            .WithLegend()).Build();

        var drawn = Drawn(figure);

        Assert.Equal(0, SwatchLinesOf(drawn));
        Assert.Contains(drawn.OfKind("DrawText"), call => (string)((dynamic)call.Args!).text == "nothing");
    }
}
