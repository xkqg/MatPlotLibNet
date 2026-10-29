// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="KdeSeries"/> rendering.</summary>
public class KdeSeriesRenderTests
{
    private static readonly double[] Data = [1.0, 2.0, 2.5, 3.0, 3.5, 4.0, 5.0];

    /// <summary>KDE renders to SVG without throwing.</summary>
    [Fact]
    public void Kde_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Data))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>SVG contains a polyline element (the density curve).</summary>
    [Fact]
    public void Kde_SvgContainsPolyline()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Data))
            .ToSvg();
        Assert.Contains("<polyline", svg);
    }

    /// <summary>Fill=true produces a polygon element for the shaded area.</summary>
    [Fact]
    public void Kde_Fill_True_ContainsPolygon()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Data, s => s.Fill = true))
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Fill=false omits the polygon element.</summary>
    [Fact]
    public void Kde_Fill_False_NoPolygon()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Data, s => s.Fill = false))
            .ToSvg();
        Assert.DoesNotContain("<polygon", svg);
    }

    /// <summary>Empty data renders without throwing.</summary>
    [Fact]
    public void Kde_EmptyData_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde([]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Explicit bandwidth is accepted without error.</summary>
    [Fact]
    public void Kde_ExplicitBandwidth_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Data, s => s.Bandwidth = 0.5))
            .ToSvg();
        Assert.Contains("<polyline", svg);
    }

    /// <summary>FigureBuilder shortcut Kde() produces valid SVG.</summary>
    [Fact]
    public void Kde_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Kde(Data)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>The curve lies inside the plot: for 1 to 6 and 30 in a figure of 500 by 350 pixels it used to run 82.66 px
    /// past the plot on each side, into the margins and past the edge of the figure, because the range of the axes and the
    /// grid the curve is drawn on were two rules for one extent.</summary>
    [Fact]
    public void TheCurve_LiesInsideThePlot_WhateverTheSample()
    {
        var figure = Plt.Create().WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax.Kde([1.0, 2, 3, 4, 5, 6, 30]))
            .Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        var curve = ctx.OfKind("DrawLines").Select(c => (Point[])((dynamic)c.Args!).points).OrderByDescending(p => p.Length).First();
        var plot = ctx.OfKind("DrawRectangle").Select(c => (Rect)((dynamic)c.Args!).rect).OrderByDescending(r => r.Width * r.Height).Skip(1).First();
        Assert.All(curve, p =>
        {
            Assert.True(p.X >= plot.X - 1e-6 && p.X <= plot.X + plot.Width + 1e-6, $"x {p.X} lies outside the plot {plot.X}..{plot.X + plot.Width}");
        });
    }
}
