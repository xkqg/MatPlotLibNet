// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A log axis must DRAW. Measured 2026-08-30 (council M16): an auto-ranged log y over 2–250 padded
/// its 5 % margin in RAW space, drove the floor negative, mapped it to NaN and every point with it — a blank
/// panel titled "Bus latency", with linear ticks. One 0-valued point did the same even under pinned limits.
/// matplotlib pads in scaled space and MASKS non-positive values (<c>nonpositive='mask'</c>); so does this.</summary>
public class LogAxisRangeTests
{
    private static readonly Regex Polyline = new("<polyline points=\"([^\"]*)\"", RegexOptions.Compiled);

    private static string Render(double[] y, Action<AxesBuilder>? configure = null)
    {
        double[] x = [.. Enumerable.Range(0, y.Length).Select(i => (double)i)];
        return Plt.Create().WithSize(600, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(x, y);
                ax.SetYScale(AxisScale.Log);
                configure?.Invoke(ax);
            })
            .ToSvg();
    }

    private static string Points(string svg)
    {
        var m = Polyline.Match(svg);
        Assert.True(m.Success, "the line is drawn");
        return m.Groups[1].Value;
    }

    [Fact]
    public void AnAutoRangedLogAxis_DrawsEveryPoint()
    {
        string svg = Render([2, 5, 20, 80, 250]);

        Assert.DoesNotContain("NaN", Points(svg));
    }

    [Fact]
    public void AnAutoRangedLogAxis_GetsDecadeTicks()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 1.0, 2.0], [2.0, 20.0, 250.0]);
            ax.SetYScale(AxisScale.Log);
        }).Build();

        _ = figure.ToSvg();

        Assert.IsType<LogLocator>(figure.SubPlots[0].YAxis.TickLocator);
        Assert.IsType<LogTickFormatter>(figure.SubPlots[0].YAxis.TickFormatter);
    }

    [Fact]
    public void AnExplicitLocator_IsLeftAlone()
    {
        var locator = new LogLocator();
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 1.0], [2.0, 250.0]);
            ax.SetYScale(AxisScale.Log);
            ax.SetYTickLocator(locator);
        }).Build();

        _ = figure.ToSvg();

        Assert.Same(locator, figure.SubPlots[0].YAxis.TickLocator);
    }

    /// <summary>A window that priced 0 µs is a point the axis cannot place; it is MASKED — dropped from the
    /// range and from the line — never allowed to blank the panel.</summary>
    [Fact]
    public void AZeroValue_IsMasked_NotFatal()
    {
        string svg = Render([2, 0, 20, 80, 250]);

        string points = Points(svg);
        Assert.DoesNotContain("NaN", points);
        Assert.Equal(4, points.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void AZeroValue_UnderPinnedLimits_IsMaskedToo()
    {
        string svg = Render([2, 0, 20, 80, 250], ax => ax.SetYLim(1, 1000));

        Assert.DoesNotContain("NaN", Points(svg));
    }

    /// <summary>The floor of a log axis is the smallest POSITIVE contribution, padded in log space — never a
    /// linear margin below it.</summary>
    [Fact]
    public void TheFloor_IsPaddedInLogSpace()
    {
        var figure = Plt.Create().WithSize(600, 300).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 1.0], [2.0, 250.0]);
            ax.SetYScale(AxisScale.Log);
        }).Build();

        string svg = figure.ToSvg();
        // The line's lowest pixel must sit INSIDE the plot area, i.e. the range floor is below 2 yet positive.
        double lowest = Points(svg).Split(' ').Select(p => double.Parse(p.Split(',')[1], System.Globalization.CultureInfo.InvariantCulture)).Max();
        Assert.True(lowest < 300, $"the y=2 point is drawn above the figure's bottom edge, was {lowest}");
    }

    [Fact]
    public void AnAllNonPositiveSeries_RendersAnEmptyLine_NotAnError()
    {
        string svg = Render([0, 0, 0]);

        Assert.Contains("<svg", svg);
        Assert.DoesNotContain("NaN", svg);
    }
}
