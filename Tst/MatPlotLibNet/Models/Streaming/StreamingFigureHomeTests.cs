// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.Tests.Models.Streaming;

/// <summary>A streaming figure sets the limits of its axes from the data before every frame. A reader who zooms into
/// such a chart is overruled by the very next frame unless the scale mode leaves the axes alone for as long as the reader
/// is away from the start; Home gives the axes back to the scale mode, which follows the data again.</summary>
public sealed class StreamingFigureHomeTests
{
    private static (StreamingFigure Stream, StreamingLineSeries Series) Following()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var series = axes.AddSeries(new StreamingLineSeries(100));
        figure.ChartId = "c";
        var stream = new StreamingFigure(figure)
        {
            DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.AutoScale(), new AxisScaleMode.AutoScale()),
        };
        for (int i = 0; i < 10; i++) series.AppendPoint(i, i * 2.0);
        stream.ApplyAxisScaling();
        return (stream, series);
    }

    [Fact]
    public void AZoomedAxes_IsLeftWhereTheReaderPutIt_WhileDataKeepsArriving()
    {
        var (stream, series) = Following();
        new ZoomEvent("c", 0, 2, 4, 4, 8).ApplyTo(stream.Figure);

        for (int i = 10; i < 20; i++) series.AppendPoint(i, i * 2.0);
        stream.ApplyAxisScaling();

        Assert.Equal(2.0, stream.Figure.SubPlots[0].XAxis.Min);
        Assert.Equal(4.0, stream.Figure.SubPlots[0].XAxis.Max);
        stream.Dispose();
    }

    [Fact]
    public void Home_HandsTheAxesBackToTheScaleMode_WhichFollowsTheDataAgain()
    {
        var (stream, series) = Following();
        new ZoomEvent("c", 0, 2, 4, 4, 8).ApplyTo(stream.Figure);
        new ResetEvent("c", 0, 0, 0, 0, 0).ApplyTo(stream.Figure);

        for (int i = 10; i < 20; i++) series.AppendPoint(i, i * 2.0);
        stream.ApplyAxisScaling();

        Assert.Equal(0.0, stream.Figure.SubPlots[0].XAxis.Min);
        Assert.Equal(19.0, stream.Figure.SubPlots[0].XAxis.Max);
        stream.Dispose();
    }

    [Fact]
    public void AnAxesNobodyZoomed_StillFollowsTheData()
    {
        var (stream, series) = Following();

        for (int i = 10; i < 20; i++) series.AppendPoint(i, i * 2.0);
        stream.ApplyAxisScaling();

        Assert.Equal(19.0, stream.Figure.SubPlots[0].XAxis.Max);
        stream.Dispose();
    }
}
