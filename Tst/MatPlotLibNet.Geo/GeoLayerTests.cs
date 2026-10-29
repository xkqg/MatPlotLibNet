// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A map is drawn on one projection that the axes holds: <c>WithProjection</c> records it and every layer reads it, a
/// layer given a projection of its own that is not the axes' is refused, and the frame of the map is the frame of the axes:
/// a mark placed in projected coordinates lands on the pixel the polygon of the same coordinates is drawn at.</summary>
public class GeoLayerTests
{
    private static readonly Regex PathData = new("<path d=\"(?<d>[^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex PathPoint = new(@"[ML] (?<x>[-0-9.eE]+) (?<y>[-0-9.eE]+)", RegexOptions.Compiled);
    private static readonly Regex CircleCentres = new("<circle cx=\"(?<x>[-0-9.eE]+)\" cy=\"(?<y>[-0-9.eE]+)\"", RegexOptions.Compiled);

    private static (double X, double Y)[] FirstPolygon(string svg) =>
        [.. PathPoint.Matches(PathData.Match(svg).Groups["d"].Value)
            .Select(m => (double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture), double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture)))];

    private static (double X, double Y)[] Circles(string svg) =>
        [.. CircleCentres.Matches(svg).Select(m => (double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture),
            double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture)))];

    // ---- the frame is the axes' frame -------------------------------------------------------------------------------------

    [Fact]
    public void AMarkAtTheCornersOfThePlateCarreeFrame_LandsOnTheCornersOfTheOceanPolygon()
    {
        string svg = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.PlateCarree)
            .Ocean(Colors.Blue)
            .Scatter([-180.0, 180.0], [-90.0, 90.0])).ToSvg();

        var corners = FirstPolygon(svg);
        var marks = Circles(svg);

        Assert.Equal(2, marks.Length);
        (double X, double Y) bottomLeft = (corners.Min(p => p.X), corners.Max(p => p.Y));
        (double X, double Y) topRight = (corners.Max(p => p.X), corners.Min(p => p.Y));
        Assert.Equal(bottomLeft.X, marks[0].X, 2);
        Assert.Equal(bottomLeft.Y, marks[0].Y, 2);
        Assert.Equal(topRight.X, marks[1].X, 2);
        Assert.Equal(topRight.Y, marks[1].Y, 2);
    }

    [Fact]
    public void TheMapSeries_FillsTheRangeOfItsAxesExactly_NoMarginAroundTheProjectionsBounds()
    {
        var series = new MatPlotLibNet.Geo.Series.GeoPolygonSeries([], GeoProjection.Robinson);
        var bounds = GeoProjection.Robinson.Bounds;

        var range = series.ComputeDataRange(null!);

        Assert.Equal(bounds.XMin, range.StickyXMin);
        Assert.Equal(bounds.XMax, range.StickyXMax);
        Assert.Equal(bounds.YMin, range.StickyYMin);
        Assert.Equal(bounds.YMax, range.StickyYMax);
    }

    // ---- one projection for the axes ---------------------------------------------------------------------------------------

    [Fact]
    public void ALayerWithNoProjection_ReadsTheOneTheAxesWasGiven()
    {
        string given = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.Robinson).Land(Colors.Green)).ToSvg();
        string explicitOne = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.Robinson).Land(GeoProjection.Robinson, Colors.Green)).ToSvg();

        Assert.Equal(explicitOne, given);
    }

    [Theory]
    [InlineData("land")]
    [InlineData("ocean")]
    [InlineData("borders")]
    [InlineData("coastlines")]
    public void ALayerOnAxesThatWereGivenNoProjection_IsRefused_AndSaysWhatToCall(string layer)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax =>
            {
                _ = layer switch
                {
                    "land" => ax.Land(Colors.Green),
                    "ocean" => ax.Ocean(Colors.Blue),
                    "borders" => ax.Borders(),
                    _ => ax.Coastlines(),
                };
            }).Build());

        Assert.Contains("WithProjection", ex.Message);
    }

    [Fact]
    public void AProjectionThatIsNotTheAxes_IsRefused()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithProjection(GeoProjection.Robinson).Land(GeoProjection.Mercator, Colors.Green)).Build());

        Assert.Contains("Robinson", ex.Message);
        Assert.Contains("Mercator", ex.Message);
    }

    [Fact]
    public void ADifferentInstanceOfTheSameProjection_IsTheAxesProjection()
    {
        // GeoProjection.Robinson makes a new instance on every read; the same projection is the same by what it is.
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithProjection(GeoProjection.Robinson).Land(GeoProjection.Robinson, Colors.Green)).Build();

        Assert.Single(figure.SubPlots[0].Series);
    }

    [Fact]
    public void TheSameKindWithOtherParameters_IsNotTheAxesProjection()
    {
        Assert.Throws<ArgumentException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(GeoProjection.OrthographicAt(10, 20)).Land(GeoProjection.OrthographicAt(10, 30), Colors.Green)).Build());
    }

    [Fact]
    public void TheProjectionOfOneAxes_IsNotTheProjectionOfAnother()
    {
        Assert.Throws<InvalidOperationException>(() => Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithProjection(GeoProjection.Robinson).Land(Colors.Green))
            .AddSubPlot(1, 2, 2, ax => ax.Land(Colors.Green)).Build());
    }
}
