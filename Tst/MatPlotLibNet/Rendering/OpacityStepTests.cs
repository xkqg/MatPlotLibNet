// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An opacity a colour is drawn at is the byte nearest to it wherever it is drawn: matplotlib keeps an alpha as a number, its
/// Agg backend turns it into a byte by rounding (<c>rgba8</c>: <c>uround(a * 255)</c>) and its SVG backend writes the number itself, so
/// the nearest byte is the one closest to both. An alpha of 0.85 is 216.75 of the full channel: 217, never the 216 that cutting the
/// fraction off gives (the rule of a colour map's bytes only, <c>(x * 255).astype(uint8)</c>). A span's fill, a legend frame and a
/// planar bar each cut it off.</summary>
public class OpacityStepTests
{
    private const double Given = 0.85;

    public static TheoryData<string> Drawers { get; } = new("span", "legend frame", "planar bar");

    [Theory]
    [MemberData(nameof(Drawers))]
    public void AnOpacity_IsDrawnAtTheNearestByte(string drawer)
    {
        var opacities = Opacities(Svg(drawer));

        Assert.Contains(opacities, o => Math.Abs(o - byte.MaxValue.Scaled(Given) / 255.0) < 1e-12);
        Assert.DoesNotContain(opacities, o => Math.Abs(o - byte.MaxValue.ScaledTruncating(Given) / 255.0) < 1e-12);
    }

    private static string Svg(string drawer) => drawer switch
    {
        "span" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 5], [1.0, 5]).AxVSpan(2, 3, s => s.Alpha = Given)).ToSvg(),
        "legend frame" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 5], [1.0, 5], s => s.Label = "a")
            .WithLegend(l => l with { FrameAlpha = Given })).ToSvg(),
        _ => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PlanarBar3D([1.0, 2.0], [1.0, 2.0], [3.0, 4.0], s => s.Alpha = Given)).ToSvg(),
    };

    private static List<double> Opacities(string svg) =>
        [.. svg.Xml().Descendants().SelectMany(e => e.Attributes())
            .Where(a => a.Name.LocalName is "fill-opacity" or "stroke-opacity" or "opacity")
            .Select(a => double.Parse(a.Value, CultureInfo.InvariantCulture))];
}
