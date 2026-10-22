// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>What a stem plot draws under the automatic range: every head is a whole circle inside the plot area. A verb that
/// draws stems (the lollipop chart) sets no axis limits, because a limit clips every other series on the axes and overwrites
/// the caller's own, so the heads at the two ends of the data have to keep their room from the automatic range alone.</summary>
public class StemSeriesRendererTests
{
    /// <summary>The radius the renderer gives a stem's head.</summary>
    private const double HeadRadius = 4;

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>Where a stem's head is drawn: the centre of its circle, in pixels.</summary>
    /// <param name="Cx">Horizontal centre.</param>
    /// <param name="Cy">Vertical centre.</param>
    internal readonly record struct Head(double Cx, double Cy);

    /// <summary>The heads of a stem plot: the circles of the stem renderer, in the order they were drawn.</summary>
    internal static Head[] Heads(string svg) =>
        [.. Regex.Matches(svg, """<circle cx="([-0-9.eE+]+)" cy="([-0-9.eE+]+)" r="4" fill="#[0-9A-Fa-f]{6}" />""")
            .Select(m => new Head(Number(m.Groups[1].Value), Number(m.Groups[2].Value)))];

    /// <summary>Under the automatic range the end heads keep the axis margin: each head is a whole circle inside the plot
    /// area on both axes. Measured before the fix in this 800 x 450 figure of six stems: the first head stood at the plot's
    /// left edge (cx 100, the circle reaching 96) and the last at its right edge (cx 720, reaching 724).</summary>
    [Fact]
    public void EveryHead_IsAWholeCircleInsideThePlotArea_UnderTheAutomaticRange()
    {
        string svg = Plt.Create().WithSize(800, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Stem([0.5, 1.5, 2.5, 3.5, 4.5, 5.5], [82.5, 67.3, 91.1, 54.8, 76.4, 88.2]))
            .ToSvg();

        var plot = BarSeriesRendererTests.PlotArea(svg);
        var heads = Heads(svg);

        Assert.Equal(6, heads.Length);
        Assert.All(heads, h =>
        {
            Assert.True(h.Cx - HeadRadius >= plot.X, $"head at {h.Cx} reaches past the left edge {plot.X}");
            Assert.True(h.Cx + HeadRadius <= plot.X + plot.W, $"head at {h.Cx} reaches past the right edge {plot.X + plot.W}");
            Assert.True(h.Cy - HeadRadius >= plot.Y, $"head at {h.Cy} reaches past the top edge {plot.Y}");
            Assert.True(h.Cy + HeadRadius <= plot.Y + plot.H, $"head at {h.Cy} reaches past the bottom edge {plot.Y + plot.H}");
        });
    }
}
