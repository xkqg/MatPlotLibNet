// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that symlog axis tick labels do not overlap. Bug found 2026-04-18:
/// rendering symlog with linthresh=100 and data ranging -125000..+125000 produced labels
/// "-1000", "1000", "-10000" stacked at nearly identical Y pixel positions (delta ~3px).</summary>
public class SymlogTickTests
{
    /// <summary>Minimum vertical distance between adjacent tick labels (pixels).
    /// Below this they visually overlap.</summary>
    private const double MinLabelSpacing = 12;

    [Fact]
    public void Symlog_TickPositions_DoNotOverlap()
    {
        // Cubic data spanning -125000..+125000 with symlog(linthresh=100).
        double[] x = Enumerable.Range(-50, 101).Select(i => (double)i).ToArray();
        double[] y = x.Select(v => v * v * v).ToArray();

        string svg = Plt.Create()
            .WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x, y)
                .WithSymlogYScale(linthresh: 100))
            .ToSvg();

        var tickYPositions = ExtractYAxisTickYPositions(svg);
        Assert.True(tickYPositions.Count >= 2,
            $"Expected at least 2 Y-axis ticks for visual sanity, got {tickYPositions.Count}");

        // Sort and check gaps
        tickYPositions.Sort();
        for (int i = 1; i < tickYPositions.Count; i++)
        {
            double gap = Math.Abs(tickYPositions[i] - tickYPositions[i - 1]);
            Assert.True(gap >= MinLabelSpacing,
                $"Y-tick labels at y={tickYPositions[i - 1]} and y={tickYPositions[i]} have gap={gap}px (< {MinLabelSpacing}px minimum) — labels overlap");
        }
    }

    private static List<double> ExtractYAxisTickYPositions(string svg)
    {
        // Y-axis tick marks are drawn as short horizontal lines on the LEFT spine,
        // typically at x ≈ 95..100 (just left of the plot area which starts ~x=100).
        // Pattern: <line x1="..." y1="<Y>" x2="..." y2="<Y>" ...>
        // Filter to those with x1 < 110 and x1 == x1 (horizontal short marks).
        var positions = new List<double>();
        var matches = Regex.Matches(svg,
            @"<line\s+x1=""(?<x1>[\d\.\-]+)""\s+y1=""(?<y1>[\d\.\-]+)""\s+x2=""(?<x2>[\d\.\-]+)""\s+y2=""(?<y2>[\d\.\-]+)""");
        foreach (Match m in matches)
        {
            double x1 = double.Parse(m.Groups["x1"].Value);
            double x2 = double.Parse(m.Groups["x2"].Value);
            double y1 = double.Parse(m.Groups["y1"].Value);
            double y2 = double.Parse(m.Groups["y2"].Value);
            // Y-axis tick: short horizontal line near the left edge
            if (x1 < 110 && Math.Abs(y1 - y2) < 0.5 && Math.Abs(x2 - x1) < 10)
                positions.Add(y1);
        }
        return positions;
    }
}
