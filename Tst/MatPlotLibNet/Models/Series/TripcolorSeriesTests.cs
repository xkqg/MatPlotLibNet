// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Models.Series;

public class TripcolorSeriesTests
{
    private static readonly double[] X = [0.0, 1.0, 0.5, 1.0, 0.0];
    private static readonly double[] Y = [0.0, 0.0, 0.5, 1.0, 1.0];
    private static readonly double[] Z = [1.0, 2.0, 3.0, 2.0, 1.0];

    // ---- a list of triangles that cannot be drawn is refused when it is set, not when the figure is drawn ---------------------

    [Theory]
    [InlineData(new[] { 0, 1, 9 }, "A corner, triangles[2], must be from 0 to 4 (got 9).")]
    [InlineData(new[] { -1, 0, 1 }, "A corner, triangles[0], must be from 0 to 4 (got -1).")]
    [InlineData(new[] { 0, 1, 2, 3 }, "triangles holds 4 indices; a triangle has three corners, so give a multiple of three.")]
    public void ATriangleListThatNamesNoPointOrIsNotWhole_IsRefusedWhenItIsSet(int[] triangles, string message)
    {
        var s = new TripcolorSeries(X, Y, Z);

        var error = Assert.ThrowsAny<ArgumentException>(() => s.Triangles = triangles);

        Assert.Equal("triangles", error.ParamName);
        Assert.StartsWith(message, error.Message);
        Assert.Null(s.Triangles);
    }

    [Fact]
    public void ATriangleListThatNamesNoPoint_IsRefusedByTheInitializerToo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TripcolorSeries(X, Y, Z) { Triangles = [0, 1, 5] });
    }

    [Fact]
    public void WholeTrianglesOverThePoints_AreKept_AndNullGoesBackToTheTriangulation()
    {
        var s = new TripcolorSeries(X, Y, Z) { Triangles = [0, 1, 2, 2, 3, 4] };

        Assert.Equal([0, 1, 2, 2, 3, 4], s.Triangles!);
        s.Triangles = [];
        Assert.Empty(s.Triangles);
        s.Triangles = null;
        Assert.Null(s.Triangles);
    }

    [Fact]
    public void TheVerb_RefusesATriangleListThatNamesNoPoint_WhenTheFigureIsBuilt()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Tripcolor(X, Y, Z, s => s.Triangles = [0, 1, 9]));

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build());
    }

    [Fact]
    public void ADocumentWhoseTrianglesNameNoPoint_IsRefusedWhileItIsRead()
    {
        string json = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Tripcolor(X, Y, Z, s => s.Triangles = [0, 1, 2])).ToJson();
        string broken = Regex.Replace(json, @"""triangles""\s*:\s*\[[^\]]*\]", @"""triangles"": [0, 1, 9]", RegexOptions.IgnoreCase);
        Assert.NotEqual(json, broken);

        var error = Assert.ThrowsAny<Exception>(() => new ChartSerializer().FromJson(broken));

        Assert.Contains("A corner, triangles[2], must be from 0 to 4 (got 9)", error.ToString());
    }

    /// <summary>TripcolorSeries.GetColorBarRange — `Z.Length &gt; 0 ? (min,max) : (0,1)`
    /// both arms. The empty-Z fallback was previously 0%-covered.</summary>
    [Fact]
    public void TripcolorSeries_GetColorBarRange_EmptyZ_Returns_DefaultRange()
    {
        var s = new TripcolorSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0.0, min);
        Assert.Equal(1.0, max);
    }

    /// <summary>TripcolorSeries.GetColorBarRange — true arm.</summary>
    [Fact]
    public void TripcolorSeries_GetColorBarRange_NonEmptyZ_ReturnsZRange()
    {
        var s = new TripcolorSeries((Vec)new[] { 0.0, 1, 2 }, (Vec)new[] { 0.0, 1, 2 }, (Vec)new[] { 10.0, 20, 30 });
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(10.0, min);
        Assert.Equal(30.0, max);
    }

}
