// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A bubble's size is an area in points squared, as the property says and as a scatter's size is: a bubble of size s is
/// sqrt(s) points across, and a point is 100 / 72 of a pixel. matplotlib 3.11.2: <c>scatter(x, y, s=100)</c> draws a marker 10 points
/// across (<c>PathCollection</c> scales the unit marker by <c>sqrt(s)</c> in points). The bubble read its size as pixels squared,
/// so a size of 100 was a bubble 10 pixels across, a scatter's size of 100 one 13.9 pixels across.</summary>
public class BubbleSizeTests
{
    private const double Pt = 100.0 / 72.0;
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];

    private static List<double> Radii(Theme theme, Color color, Action<AxesBuilder> axes) =>
        [.. Plt.Create().WithSize(400, 300).WithTheme(theme).AddSubPlot(1, 1, 1, axes).Build().ToSvg().Each("circle").Where(e => ((string?)e.Attribute("fill") == color.ToHex() || (string?)e.Attribute("stroke") == color.ToHex()))
            .Select(e => e.Number("r"))];

    [Theory]
    [MemberData(nameof(ThemeCases.MatplotlibAndDefault), MemberType = typeof(ThemeCases))]
    public void ASizeOfOneHundred_IsTenPointsAcross_UnderEveryTheme(string id)
    {
        var radii = Radii(id.AsTheme(), Colors.Red, ax => ax.Bubble(X, Y, [100, 100, 100], s => s.Color = Colors.Red));

        Assert.Equal(3, radii.Count);
        Assert.All(radii, r => Assert.Equal(10 * Pt / 2, r, 2));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.MatplotlibAndDefault), MemberType = typeof(ThemeCases))]
    public void ABubbleAndAScatterOfTheSameSize_AreTheSameSize(string id)
    {
        var bubble = Radii(id.AsTheme(), Colors.Red, ax => ax.Bubble(X, Y, [50, 200, 450], s => { s.Color = Colors.Red; s.Alpha = 1; }));
        var scatter = Radii(id.AsTheme(), Colors.Red, ax => ax.AddSeries(
            new ScatterSeries(X, Y) { Sizes = [50, 200, 450], Color = Colors.Red }));

        Assert.Equal(scatter, bubble);
    }

    [Fact]
    public void TheAreaIsInProportionToTheSize_TheRadiusToItsRoot()
    {
        var radii = Radii(Theme.Default, Colors.Red, ax => ax.Bubble(X, Y, [100, 400, 900], s => s.Color = Colors.Red));

        Assert.Equal(1.0, radii[1] / radii[0] / 2, 2);
        Assert.Equal(1.0, radii[2] / radii[0] / 3, 2);
    }
}
