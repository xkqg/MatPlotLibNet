// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>
/// A scatter of ten thousand points is a black blob: the markers overlap, and where they overlap most is
/// exactly what the reader wants to know and cannot see. Colouring each marker by how crowded its
/// neighbourhood is puts that back. Everything needed was already here — a scatter takes a value per point and
/// a colour map — and what was missing is the step that works the values out.
/// </summary>
public class DensityScatterTests
{
    /// <summary>Four points crammed together and one out on its own.</summary>
    private static readonly double[] X = [0.01, 0.02, 0.03, 0.04, 10.0];
    private static readonly double[] Y = [0.01, 0.02, 0.03, 0.04, 10.0];

    private static ScatterSeries Built(Action<ScatterSeries>? configure = null) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.DensityScatter(X, Y, configure))
            .Build()
            .SubPlots[0].Series.OfType<ScatterSeries>().Single();

    [Fact]
    public void EveryPoint_GetsItsOwnColourValue()
    {
        var scatter = Built();

        Assert.NotNull(scatter.C);
        Assert.Equal(X.Length, scatter.C!.Length);
        Assert.Equal(X, scatter.XData);
        Assert.Equal(Y, scatter.YData);
    }

    [Fact]
    public void TheCrowdedPoints_ScoreHigherThanTheLonelyOne()
    {
        var c = Built().C!;

        Assert.True(c[0] > c[4], $"crowded {c[0]} should beat lonely {c[4]}");
    }

    [Fact]
    public void ItArrivesWithAColourMap_BecauseAValuePerPointWithoutOneShowsNothing()
    {
        Assert.NotNull(Built().ColorMap);
    }

    [Fact]
    public void TheCaller_CanChangeTheColourMap()
    {
        var scatter = Built(s => s.ColorMap = ColorMaps.Plasma);

        Assert.Same(ColorMaps.Plasma, scatter.ColorMap);
    }

    [Fact]
    public void TheCaller_CanReplaceTheDensityWithTheirOwnValues()
    {
        // configure runs last, so a caller who has a better number than crowding - an age, a score, an error -
        // keeps the marker colouring and supplies the meaning.
        var scatter = Built(s => s.C = [5.0, 4.0, 3.0, 2.0, 1.0]);

        Assert.Equal([5.0, 4.0, 3.0, 2.0, 1.0], scatter.C!);
    }

    [Fact]
    public void ItDrawsAMarkerPerPoint_InMoreThanOneColour()
    {
        string svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.DensityScatter(X, Y))
            .ToSvg();

        var fills = System.Text.RegularExpressions.Regex.Matches(svg, @"<circle[^>]*fill=""(#[0-9A-Fa-f]{6})""")
            .Select(m => m.Groups[1].Value)
            .ToArray();

        Assert.Equal(X.Length, fills.Length);
        Assert.True(fills.Distinct().Count() > 1, "a density scatter that draws one colour has said nothing");
    }

    [Fact]
    public void TwoAxesOfDifferentLengths_AreRefused()
    {
        Assert.Throws<ArgumentException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.DensityScatter([1.0, 2.0], [1.0])).Build());
    }

    [Fact]
    public void NoPointsAtAll_DrawsNothingAndDoesNotThrow()
    {
        var scatter = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.DensityScatter([], []))
            .Build()
            .SubPlots[0].Series.OfType<ScatterSeries>().Single();

        Assert.Empty(scatter.C!);
    }

    [Fact]
    public void AGridSizeOfYourOwn_IsUsedInsteadOfTheChosenOne()
    {
        // One cell holds the whole cloud, so every point scores five; two cells separate the crowded corner
        // from the lonely one, so they score four and one. The answers differ, which is what proves the
        // argument arrives rather than the chosen size being used regardless.
        var oneCell = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.DensityScatter(X, Y, bins: 1))
            .Build().SubPlots[0].Series.OfType<ScatterSeries>().Single().C!;

        var twoCells = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.DensityScatter(X, Y, bins: 2))
            .Build().SubPlots[0].Series.OfType<ScatterSeries>().Single().C!;

        Assert.All(oneCell, d => Assert.Equal(5.0, d));
        Assert.Equal(4.0, twoCells[0]);
        Assert.Equal(1.0, twoCells[4]);
    }

    [Fact]
    public void NoXAtAll_IsRefusedByName()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.DensityScatter(null!, Y)).Build());
    }

    [Fact]
    public void NoYAtAll_IsRefusedByName()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.DensityScatter(X, null!)).Build());
    }
}
