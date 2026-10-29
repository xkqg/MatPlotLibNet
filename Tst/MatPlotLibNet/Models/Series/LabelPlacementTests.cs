// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>"A labelled line can ask for its name at the end of the line instead of in the legend." The setting
/// lives on the narrow capability <see cref="IHasLabelPlacement"/>, which a line series and a streaming line series
/// carry; <c>WithDirectLabel()</c> on the axes builder and on the secondary-axis builder marks the most recently
/// added series, the same "last capable series" shape as <c>WithColorMap</c>.</summary>
public class LabelPlacementTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [2.0, 3.0, 4.0];

    [Fact]
    public void ALineSeries_KeepsItsNameInTheLegend_ByDefault()
    {
        Assert.Equal(SeriesLabelPlacement.Legend, new LineSeries(X, Y).LabelPlacement);
        Assert.Equal(SeriesLabelPlacement.Legend, new StreamingLineSeries().LabelPlacement);
        Assert.Equal(0, (int)default(SeriesLabelPlacement));
    }

    [Fact]
    public void WithDirectLabel_MarksTheLastLine_AndNoEarlierOne()
    {
        AxesBuilder? returned = null;
        AxesBuilder? self = null;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            self = ax;
            returned = ax.Plot(X, Y, s => s.Label = "A")
                         .Plot(X, Y, s => s.Label = "B")
                         .WithDirectLabel();
        }).Build();

        Assert.Same(self, returned);
        var series = figure.SubPlots[0].Series;
        Assert.Equal(SeriesLabelPlacement.Legend, ((LineSeries)series[0]).LabelPlacement);
        Assert.Equal(SeriesLabelPlacement.LineEnd, ((LineSeries)series[1]).LabelPlacement);
    }

    [Fact]
    public void WithDirectLabel_MarksAStreamingLine_AddedLastOnThePrimaryAxis()
    {
        StreamingLineSeries? live = null;
        Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot(X, Y, s => s.Label = "history");
            live = ax.StreamingPlot(configure: s => s.Label = "live");
            ax.WithDirectLabel();
        }).Build();

        Assert.Equal(SeriesLabelPlacement.LineEnd, live!.LabelPlacement);
    }

    [Fact]
    public void WithDirectLabel_OnASeriesThatCannotCarryIt_MarksNothing_AndThrowsNothing()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "line")
            .Scatter(X, Y, s => s.Label = "dots")
            .WithDirectLabel()).Build();

        // The verb reads the LAST series only, like WithColorMap: the scatter cannot carry a placement, and the line
        // before it is not silently picked instead.
        Assert.Equal(SeriesLabelPlacement.Legend, ((LineSeries)figure.SubPlots[0].Series[0]).LabelPlacement);
    }

    [Fact]
    public void WithDirectLabel_OnAnEmptyAxes_ThrowsNothing()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithDirectLabel()).Build();
        Assert.Empty(figure.SubPlots[0].Series);
    }

    [Fact]
    public void RightWithDirectLabel_MarksTheLastRightLine_AndLeavesThePrimaryAlone()
    {
        YAxisSlotBuilder? returned = null;
        YAxisSlotBuilder? self = null;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "left")
            .WithAddRightYAxis(y2 =>
            {
                self = y2;
                returned = y2.Plot(X, [10.0, 20.0, 30.0], s => s.Label = "right 1")
                             .Plot(X, [30.0, 20.0, 10.0], s => s.Label = "right 2")
                             .WithDirectLabel();
            })).Build();

        Assert.Same(self, returned);
        var axes = figure.SubPlots[0];
        Assert.Equal(SeriesLabelPlacement.Legend, ((LineSeries)axes.Series[0]).LabelPlacement);
        Assert.Equal(SeriesLabelPlacement.Legend, ((LineSeries)axes.YAxes.Right[0].Series[0]).LabelPlacement);
        Assert.Equal(SeriesLabelPlacement.LineEnd, ((LineSeries)axes.YAxes.Right[0].Series[1]).LabelPlacement);
    }

    [Fact]
    public void RightWithDirectLabel_MarksARightStreamingLine()
    {
        StreamingLineSeries? live = null;
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y)
            .WithAddRightYAxis(y2 =>
            {
                live = y2.StreamingPlot(configure: s => s.Label = "live");
                y2.WithDirectLabel();
            })).Build();

        Assert.Equal(SeriesLabelPlacement.LineEnd, live!.LabelPlacement);
    }

    [Fact]
    public void RightWithDirectLabel_OnARightScatter_MarksNothing_AndThrowsNothing()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y)
            .WithAddRightYAxis(y2 => y2
                .Plot(X, Y, s => s.Label = "line")
                .Scatter(X, Y, s => s.Label = "dots")
                .WithDirectLabel())).Build();

        Assert.Equal(SeriesLabelPlacement.Legend, ((LineSeries)figure.SubPlots[0].YAxes.Right[0].Series[0]).LabelPlacement);
    }

    [Fact]
    public void RightWithDirectLabel_WithNoRightSeries_ThrowsNothing()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "left")
            .WithAddRightYAxis(y2 => y2.WithDirectLabel())).Build();

        // A right-axis verb never reaches across to the primary series.
        Assert.Equal(SeriesLabelPlacement.Legend, ((LineSeries)figure.SubPlots[0].Series[0]).LabelPlacement);
    }

    [Fact]
    public void ALineSeries_HandsTheLabelPartItsOwnPoints()
    {
        IHasLabelPlacement line = new LineSeries(X, Y);
        var points = line.LinePoints();
        Assert.Equal(X, points.XData);
        Assert.Equal(Y, points.YData);
    }

    [Fact]
    public void AStreamingLine_HandsTheLabelPartOneSnapshotOfItsBuffer()
    {
        var live = new StreamingLineSeries(capacity: 2);
        live.AppendPoint(1, 10);
        live.AppendPoint(2, 20);
        live.AppendPoint(3, 30);   // evicts the first point: the label part sees what the next render draws

        var points = ((IHasLabelPlacement)live).LinePoints();
        Assert.Equal([2.0, 3.0], points.XData);
        Assert.Equal([20.0, 30.0], points.YData);
    }

    [Fact]
    public void AnEmptyStreamingLine_HandsTheLabelPartNoPoints()
    {
        var points = ((IHasLabelPlacement)new StreamingLineSeries()).LinePoints();
        Assert.Empty(points.XData);
        Assert.Empty(points.YData);
    }
}
