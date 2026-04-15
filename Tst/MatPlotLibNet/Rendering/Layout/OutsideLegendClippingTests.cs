// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>Regression test for the outside-legend-clipping bug: the legend_outside sample
/// in the console runner places the legend at x=748 width=164 in a 900-wide figure — the
/// rect extends to 913, clipping 13 px past the right edge despite ConstrainedLayoutEngine
/// supposedly reserving margin via LegendMeasurer. v1.1.4 claimed this was fixed; it wasn't.</summary>
public class OutsideLegendClippingTests
{
    [Fact]
    public void OutsideRight_LegendRect_MustFitInsideFigureWidth()
    {
        double[] xo = Enumerable.Range(0, 100).Select(i => i * 10.0 / 99).ToArray();
        var svg = Plt.Create()
            .WithTitle("Outside legend — constrained layout reserves right margin")
            .WithSize(900, 500)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(xo, xo.Select(v => Math.Sin(v)).ToArray(), s => s.Label = "sin(x)");
                ax.Plot(xo, xo.Select(v => Math.Cos(v)).ToArray(), s => s.Label = "cos(x)");
                ax.Plot(xo, xo.Select(v => 0.5 * Math.Sin(2 * v)).ToArray(),
                    s => s.Label = "½ sin(2x)");
                ax.Plot(xo, xo.Select(v => Math.Exp(-v / 5) * Math.Cos(v)).ToArray(),
                    s => s.Label = "exp(-x/5)·cos(x)");
                ax.SetXLabel("x");
                ax.SetYLabel("f(x)");
                ax.WithLegend(l => l with { Position = LegendPosition.OutsideRight, Title = "Series" });
            })
            .ToSvg();

        // Parse the frame rect under <g class="legend"> — the CCCCCC-stroked one.
        var m = Regex.Match(svg,
            @"<rect x=""(?<x>[0-9.]+)"" y=""[0-9.]+"" width=""(?<w>[0-9.]+)"" height=""[0-9.]+""[^>]*stroke=""#CCCCCC""");
        Assert.True(m.Success, "legend frame rect not found in SVG output");

        double rectX     = double.Parse(m.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
        double rectWidth = double.Parse(m.Groups["w"].Value, System.Globalization.CultureInfo.InvariantCulture);
        double rectRight = rectX + rectWidth;
        const double FigureWidth = 900;

        Assert.True(rectRight <= FigureWidth,
            $"Legend rect right edge {rectRight:F2} exceeds figure width {FigureWidth}. " +
            $"rectX={rectX:F2} rectWidth={rectWidth:F2}. " +
            $"ConstrainedLayoutEngine must reserve enough MarginRight to host the full legend box.");
    }

    [Fact]
    public void OutsideLeft_LegendRect_MustFitInsideFigureFromLeftEdge()
    {
        double[] xo = Enumerable.Range(0, 50).Select(i => i * 1.0).ToArray();
        var svg = Plt.Create()
            .WithSize(900, 500)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(xo, xo, s => s.Label = "a-very-long-label-that-pads-the-legend-box");
                ax.Plot(xo, xo.Select(v => v * 2).ToArray(), s => s.Label = "another-long-label-entry");
                ax.WithLegend(l => l with { Position = LegendPosition.OutsideLeft, Title = "Series" });
            })
            .ToSvg();

        var m = Regex.Match(svg,
            @"<rect x=""(?<x>[0-9.]+)"" y=""[0-9.]+"" width=""(?<w>[0-9.]+)"" height=""[0-9.]+""[^>]*stroke=""#CCCCCC""");
        Assert.True(m.Success);
        double rectX = double.Parse(m.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(rectX >= 0,
            $"Legend rect left edge {rectX:F2} is off-figure (must be ≥ 0). " +
            $"ConstrainedLayoutEngine under-reserved MarginLeft.");
    }
}
