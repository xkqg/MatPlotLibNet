// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The marks a legend at <see cref="LegendPosition.Best"/> and a text that moves out of the way keep clear of, on an
/// axes that has every kind of seat: grouped or stacked bars and a line on the primary, a Y slot past it that is linked, one with
/// a series of its own, and a top axis with a series. The numbers are those the draw has always produced: the legend's rows and
/// the place the moving text ends up at, and the marks the legend is laid out around when it is asked for them outside a draw.
/// Computing the range and the marks fewer times in a draw must leave every one of them where it is.</summary>
public class RenderObstaclesTests
{
    private static readonly Rect Plot = new(80, 40, 560, 320);

    private static Axes RichAxes(BarMode mode)
    {
        var axes = new Axes { BarMode = mode };
        axes.Bar(["a", "b", "c", "d"], [3.0, 5, 2, 4]).Label = "first";
        axes.Bar(["a", "b", "c", "d"], [1.0, 2, 3, 1]).Label = "second";
        axes.Plot([0.0, 1, 2, 3], [2.0, 6, 1, 5]).Label = "line";
        axes.Legend = axes.Legend with { Position = LegendPosition.Best };
        axes.Annotate("Mover", 1, 5).AvoidOverlap = true;
        axes.YAxes.StackOn(AxisSide.Right).Add().Axis.Link = AxisTransform.Linear(1000);
        axes.YAxes.StackOn(AxisSide.Right).Add().AddSeries(new LineSeries([0.0, 1, 2, 3], [10.0, 40, 20, 30]) { Label = "slot" });
        axes.PlotXSecondary([0.0, 10, 20], [1.0, 3, 2]).Label = "top";
        return axes;
    }

    private static string Fingerprint(IEnumerable<double> values) =>
        string.Join(" ", values.Select(v => v.ToString("F3", CultureInfo.InvariantCulture)));

    /// <summary>What a draw places: the bounds of each legend row and the baseline the moving text is drawn at.</summary>
    private static string Drawn(BarMode mode)
    {
        var ctx = new RecordingRenderContext();
        var renderer = new CartesianAxesRenderer(RichAxes(mode), Plot, ctx, Theme.Default);
        renderer.Render();
        var rows = renderer.LegendBounds.SelectMany(b => new[] { b.SeriesIndex, b.Bounds.X, b.Bounds.Y, b.Bounds.Width, b.Bounds.Height });
        var mover = ctx.Of<DrawTextCall>().Single(a => a.Text == "Mover");
        Point at = mover.Position;
        return Fingerprint([.. rows, at.X, at.Y]);
    }

    /// <summary>The marks the legend keeps clear of when it is laid out outside a draw (the hit test of a native control).</summary>
    private static string Collected(BarMode mode)
    {
        var marks = new CartesianAxesRenderer(RichAxes(mode), Plot, new RecordingRenderContext(), Theme.Default).LegendObstacles();
        return Fingerprint([
            marks.Boxes.Count, marks.Paths.Count, marks.Points.Count,
            .. marks.Boxes.SelectMany(b => new[] { b.X, b.Y, b.Width, b.Height }),
            .. marks.Paths.SelectMany(p => p.SelectMany(v => new[] { v.X, v.Y })),
            .. marks.Points.SelectMany(p => new[] { p.X, p.Y }),
        ]);
    }

    [Theory]
    [InlineData(BarMode.Grouped, "0.000 538.800 58.000 83.200 15.600 1.000 538.800 73.600 83.200 15.600 2.000 538.800 89.200 83.200 15.600 3.000 538.800 104.800 83.200 15.600 4.000 538.800 120.400 83.200 15.600 261.991 80.032")]
    [InlineData(BarMode.Stacked, "0.000 538.800 58.000 83.200 15.600 1.000 538.800 73.600 83.200 15.600 2.000 538.800 89.200 83.200 15.600 3.000 538.800 104.800 83.200 15.600 4.000 538.800 120.400 83.200 15.600 209.991 168.313")]
    public void ADraw_PlacesTheLegendAndTheMovingTextWhereItAlwaysDid(BarMode mode, string expected) =>
        Assert.Equal(expected, Drawn(mode));

    [Theory]
    [InlineData(BarMode.Grouped, "9.000 3.000 0.000 125.035 207.619 45.688 152.381 255.571 106.032 45.688 253.968 386.107 258.413 45.688 101.587 516.643 156.825 45.688 203.175 170.723 309.206 45.688 50.794 301.259 258.413 45.688 101.587 431.795 207.619 45.688 152.381 562.331 309.206 45.688 50.794 235.991 93.032 39.000 13.000 105.455 258.413 235.991 55.238 366.527 309.206 497.063 106.032 105.455 345.455 235.991 54.545 366.527 248.485 497.063 151.515 105.455 309.206 360.000 207.619 614.545 258.413")]
    [InlineData(BarMode.Stacked, "9.000 3.000 0.000 118.508 229.388 104.429 130.612 249.044 142.313 104.429 217.687 379.580 272.925 104.429 87.075 510.117 185.850 104.429 174.150 118.508 185.850 104.429 43.537 249.044 55.238 104.429 87.075 379.580 142.313 104.429 130.612 510.117 142.313 104.429 43.537 235.991 129.313 39.000 13.000 105.455 272.925 235.991 98.776 366.527 316.463 497.063 142.313 105.455 345.455 235.991 54.545 366.527 248.485 497.063 151.515 105.455 316.463 360.000 229.388 614.545 272.925")]
    public void TheMarksALegendKeepsClearOf_AreTheOnesItAlwaysKeptClearOf(BarMode mode, string expected) =>
        Assert.Equal(expected, Collected(mode));
}
