// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Where the dots of a swarm plot stand. seaborn 0.13.2's <c>swarmplot</c> places them in pixels, lowest first, each at the position
/// nearest the group's midline that touches no dot below it within a diameter (<c>Beeswarm.beeswarm</c>, categorical.py), and keeps them
/// within 0.4 of a category of the midline (<c>add_gutters</c>): the matplotlib themes place the dots so, the library's own themes keep
/// their packing in data units.</summary>
public class SeabornSwarmTests
{
    private static readonly double[][] Groups =
    [
        [1.0, 1.0, 1.0, 1.04, 1.08, 1.1, 2.0, 2.0, 2.02, 2.5],
        [1.5, 1.52, 1.54, 3.0],
    ];

    private readonly record struct Dot(double X, double Y, double R);

    private static List<Dot> DotsUnder(Theme theme) =>
        [.. Plt.Create().WithTheme(theme).WithSize(600, 400)
                .AddSubPlot(1, 1, 1, ax => ax.Swarmplot(Groups)).ToSvg().Each("circle")
            .Select(e => new Dot(e.Number("cx"), e.Number("cy"), e.Number("r")))];

    // The SVG writes a coordinate to two decimals, and a dot is placed from the heights of the dots below it: a tenth of a pixel is the wiring, not the rounding.
    private static void AssertStandAt(double[] expected, List<Dot> dots)
    {
        Assert.Equal(expected.Length, dots.Count);
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], dots[i].X, 0.1);
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void UnderTheMatplotlibThemes_TheDotsOfAGroupStandWhereSeabornsBeeswarmPutsThem(string themeId)
    {
        var dots = DotsUnder(themeId.AsTheme());
        Assert.Equal(Groups[0].Length + Groups[1].Length, dots.Count);

        // The dots come group by group; the lowest dot of a group is the first one placed, so it stands on the midline.
        var first = dots[..Groups[0].Length];
        var second = dots[Groups[0].Length..];
        double midline0 = first.MinBy(d => -d.Y).X, midline1 = second.MinBy(d => -d.Y).X;
        double halfWidth = 0.4 * (midline1 - midline0);
        double radius = first[0].R;

        // matplotlib's pixels count upwards, the picture's downwards: seaborn's "along" is the negative of the picture's y.
        double[] expected = SwarmplotSeriesRenderer.OnPixels(midline0, [.. first.Select(d => -d.Y)], radius, halfWidth);

        AssertStandAt(expected, first);
        AssertStandAt(SwarmplotSeriesRenderer.OnPixels(midline1, [.. second.Select(d => -d.Y)], radius, halfWidth), second);
    }

    [Fact]
    public void UnderTheMatplotlibThemes_NoTwoDotsOverlapAndNoneLeavesItsGutter()
    {
        var dots = DotsUnder(Theme.MatplotlibV2);
        double midline = dots[..Groups[0].Length].MinBy(d => -d.Y).X;
        double width = dots[Groups[0].Length..].MinBy(d => -d.Y).X - midline;

        foreach (var dot in dots[..Groups[0].Length])
            Assert.InRange(Math.Abs(dot.X - midline), 0, 0.4 * width + 0.011);
        for (int i = 0; i < Groups[0].Length; i++)
        for (int j = i + 1; j < Groups[0].Length; j++)
            Assert.True(Math.Sqrt(Math.Pow(dots[i].X - dots[j].X, 2) + Math.Pow(dots[i].Y - dots[j].Y, 2)) >= 2 * dots[i].R - 0.05, $"dots {i} and {j} overlap");
    }

    [Fact]
    public void UnderTheLibrarysOwnThemes_TheDotsKeepTheirPackingInSteps_OfADiameter()
    {
        var dots = DotsUnder(Theme.Default);
        var first = dots[..Groups[0].Length];
        double midline = first.MinBy(d => -d.Y).X;
        double diameter = 2 * first[0].R;

        Assert.All(first, d =>
        {
            double steps = Math.Abs(d.X - midline) / diameter;
            Assert.InRange(Math.Abs(steps - Math.Round(steps)), 0, 0.01);
        });
    }
}
