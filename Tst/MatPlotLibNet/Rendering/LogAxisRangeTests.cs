// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
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
        var lines = svg.Elements("polyline");
        Assert.True(lines.Count > 0, "the line is drawn");
        return lines[0]["points"]!;
    }

    [Fact]
    public void AnAutoRangedLogAxis_DrawsEveryPoint()
    {
        string svg = Render([2, 5, 20, 80, 250]);

        Points(svg).AssertNoNonFiniteNumber();
    }

    /// <summary>The decades are what the log scale brings: the drawing shows them, labelled 10¹ and 10², and the axis is left
    /// with no locator or formatter of its own, as the caller built it.</summary>
    [Fact]
    public void AnAutoRangedLogAxis_DrawsDecadeTicks_AndKeepsNoLocatorOfItsOwn()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 1.0, 2.0], [2.0, 20.0, 250.0]);
            ax.SetYScale(AxisScale.Log);
        }).Build();

        string svg = figure.ToSvg();

        Assert.Contains(">10¹<", svg);
        Assert.Contains(">10²<", svg);
        Assert.Null(figure.SubPlots[0].YAxis.TickLocator);
        Assert.Null(figure.SubPlots[0].YAxis.TickFormatter);
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

    /// <summary>A window that priced 0 µs is a point the axis cannot place: it is dropped from the range and never allowed to
    /// blank the panel. matplotlib's log scale puts it a thousand decades below the axis (<c>nonpositive='clip'</c>), so the
    /// line dives from 2 to the bottom edge and climbs from it to 20: two lines, 2 and its dive, then the climb, 20, 80, 250.</summary>
    [Fact]
    public void AZeroValue_IsLeftOutOfTheRange_AndTheLineDivesToTheEdgeThere()
    {
        string svg = Render([2, 0, 20, 80, 250]);

        var lines = svg.Elements("polyline").Select(polyline => polyline["points"]!).ToList();
        Assert.Equal(2, lines.Count);
        lines.AssertNoNonFiniteNumber("a polyline");
        Assert.Equal(2, lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Equal(4, lines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void AZeroValue_UnderPinnedLimits_IsMaskedToo()
    {
        string svg = Render([2, 0, 20, 80, 250], ax => ax.SetYLim(1, 1000));

        Points(svg).AssertNoNonFiniteNumber();
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

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AnAllNonPositiveSeries_RendersAnEmptyLine_NotAnError", () => Render([0, 0, 0]), "<svg") { Finite = true },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
