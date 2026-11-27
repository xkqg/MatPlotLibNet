// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A surface draws its grid as matplotlib's <c>plot_surface</c> does (matplotlib 3.11, mpl_toolkits/mplot3d/axes3d.py):
/// <code>
/// rstride = kwargs.pop('rstride', 10); cstride = kwargs.pop('cstride', 10)
/// rcount = kwargs.pop('rcount', 50);   ccount = kwargs.pop('ccount', 50)
/// if mpl.rcParams['_internal.classic_mode']: compute_strides = has_count
/// else:                                      compute_strides = not has_stride
/// if compute_strides: rstride = int(max(np.ceil(rows / rcount), 1)); cstride = ...
/// row_inds = list(range(0, rows-1, rstride)) + [rows-1]       # the same for the columns
/// for rs, rs_next in pairwise(row_inds): for cs, cs_next in pairwise(col_inds):
///     ps = [_array_perimeter(a[rs:rs_next+1, cs:cs_next+1]) for a in (X, Y, Z)]   # one patch per block
/// avg_z = np.array([ps[:, 2].mean() for ps in polys])          # the patch's colour value
/// </code>
/// A stride draws one patch over the whole block between two drawn rows, never one cell of it with holes beside.</summary>
public sealed class SurfaceStrideTests
{
    private static double[] Axis(int n) => Enumerable.Range(0, n).Select(i => -3.0 + (6.0 * i / (n - 1))).ToArray();

    private static double[,] Bowl(int rows, int cols)
    {
        var z = new double[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                z[r, c] = (r * 0.25) - (c * 0.125) + ((r * c) % 7 * 0.01);
        return z;
    }

    private static SurfaceSeries Drawn(int rows, int cols, Theme theme, Action<SurfaceSeries>? configure = null)
    {
        var figure = Plt.Create().WithTheme(theme).WithSize(400, 300)
            .Surface(Axis(cols), Axis(rows), Bowl(rows, cols), s => configure?.Invoke(s)).Build();
        _ = figure.ToSvg();
        return (SurfaceSeries)figure.SubPlots[0].Series[0];
    }

    private static SurfaceFaces.Face[] FacesOf(SurfaceSeries series) => series.DrawnFaces!.Faces;

    /// <summary>The first row (or column) of every drawn block, as matplotlib's <c>row_inds</c> lists them.</summary>
    private static int[] Starts(SurfaceFaces.Face[] faces, Func<SurfaceFaces.Face, int> pick) =>
        faces.Select(pick).Distinct().Order().ToArray();

    [Fact]
    public void AStride_DrawsEveryBlockAsOnePatch_SoNoCellIsLeftOut()
    {
        var faces = FacesOf(Drawn(21, 21, Theme.MatplotlibV2, s => { s.RowStride = 10; s.ColStride = 10; }));

        Assert.Equal(4, faces.Length);
        Assert.Equal([0, 10], Starts(faces, f => f.Row));
        Assert.All(faces, f => Assert.Equal(f.Row + 10, f.RowEnd));
        Assert.All(faces, f => Assert.Equal(f.Col + 10, f.ColEnd));
        // The four patches tile the 20 x 20 cells of the grid.
        Assert.Equal(400, faces.Sum(f => (f.RowEnd - f.Row) * (f.ColEnd - f.Col)));
    }

    [Fact]
    public void AStrideThatDoesNotDivideTheGrid_EndsOnTheLastRow()
    {
        var faces = FacesOf(Drawn(40, 40, Theme.MatplotlibV2, s => { s.RowStride = 10; s.ColStride = 10; }));

        Assert.Equal(16, faces.Length);
        Assert.Equal([0, 10, 20, 30], Starts(faces, f => f.Row));
        Assert.Equal(39, faces.Max(f => f.RowEnd));
        Assert.Equal(39, faces.Max(f => f.ColEnd));
    }

    [Fact]
    public void APatch_IsDrawnAlongItsWholePerimeter()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(400, 300)
            .Surface(Axis(21), Axis(21), Bowl(21, 21), s => { s.RowStride = 10; s.ColStride = 10; }).Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        // A 10 x 10 block has 2 * (10 + 10) points on its edge, as _array_perimeter walks it.
        Assert.Equal(4, ctx.Of<DrawPolygonCall>().Count(c => c.Points.Length == 40));
    }

    [Fact]
    public void ThePatchOfABlock_HasThePointsOfItsPerimeterAsVertexData()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).With3DRotation().WithSize(400, 300)
            .Surface(Axis(21), Axis(21), Bowl(21, 21), s => { s.RowStride = 10; s.ColStride = 10; }).Build();

        string svg = figure.ToSvg();

        var v3d = svg.AttributeValues("data-v3d");
        Assert.Equal(4, v3d.Count(v => v.Trim().Split(' ').Length == 40));
    }

    [Fact]
    public void ABlockWithMorePointsOnItsEdgeThanTheStackBufferHolds_IsWalkedWhole()
    {
        // A 20 x 20 block has 80 points on its edge, more than the 64 a walk holds without a heap buffer.
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).With3DRotation().WithSize(400, 300)
            .Surface(Axis(41), Axis(41), Bowl(41, 41), s => { s.RowStride = 20; s.ColStride = 20; }).Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);
        string svg = figure.ToSvg();

        Assert.True(80 > SurfaceFaces.MostOnTheStack);
        Assert.Equal(4, ctx.Of<DrawPolygonCall>().Count(c => c.Points.Length == 80));
        var v3d = svg.AttributeValues("data-v3d");
        Assert.Equal(4, v3d.Count(v => v.Trim().Split(' ').Length == 80));
    }

    [Fact]
    public void APatch_IsColouredByTheMeanHeightOfItsPerimeter()
    {
        // A 3 x 7 grid in three blocks of 2 x 2 cells: each perimeter is the eight outer points of its block, the centre
        // is not on it. Means: 1/8 (z[0,0] = 1), 0 and 8/8 (z[0,6] = 8); the centres 9, -9 and 0 count for nothing.
        var z = new double[3, 7];
        z[0, 0] = 1;
        z[1, 1] = 9;
        z[1, 3] = -9;
        z[0, 6] = 8;
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2)
            .Surface([0.0, 1, 2, 3, 4, 5, 6], [0.0, 1, 2], z, s => { s.RowStride = 2; s.ColStride = 2; s.ColorMap = ColorMaps.Viridis; }).Build();
        _ = figure.ToSvg();

        var faces = FacesOf((SurfaceSeries)figure.SubPlots[0].Series[0]);

        Assert.Equal(3, faces.Length);
        Assert.Equal(ColorMaps.Viridis.ColorOf(1.0 / 8, LinearNormalizer.Instance, 0, 1), faces[0].Fill);
    }

    [Fact]
    public void ThePatchColours_SpanTheMapFromTheLowestToTheHighestPatchMean()
    {
        // polyc.set_array(avg_z) with no vmin / vmax: the norm is autoscaled over the patch means, not over the data, so
        // the lowest patch takes the map's first colour and the highest its last. Two cells: means 0 and 1, data 0 to 4.
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2)
            .Surface([0.0, 1, 2], [0.0, 1], new double[,] { { 0, 0, 0 }, { 0, 0, 4 } }, s => s.ColorMap = ColorMaps.Viridis).Build();
        _ = figure.ToSvg();

        var faces = FacesOf((SurfaceSeries)figure.SubPlots[0].Series[0]);

        Assert.Equal(ColorMaps.Viridis.GetColor(0), faces[0].Fill);
        Assert.Equal(ColorMaps.Viridis.GetColor(1), faces[1].Fill);
    }

    [Fact]
    public void APatchThatCannotBeDrawn_DoesNotWidenTheColourRange()
    {
        // matplotlib leaves a patch with a missing height out of the collection before it colours the rest.
        var z = new double[,] { { 0, 0, 0, 0 }, { 0, 0, 4, double.NaN } };
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2)
            .Surface([0.0, 1, 2, 3], [0.0, 1], z, s => s.ColorMap = ColorMaps.Viridis).Build();
        _ = figure.ToSvg();

        var faces = FacesOf((SurfaceSeries)figure.SubPlots[0].Series[0]);

        Assert.False(faces[2].Finite);
        Assert.Equal(ColorMaps.Viridis.GetColor(0), faces[0].Fill);
        Assert.Equal(ColorMaps.Viridis.GetColor(1), faces[1].Fill);
    }

    [Fact]
    public void NoStrideSet_UnderTheClassicTheme_DrawsEveryTenthRowAndColumn()
    {
        var faces = FacesOf(Drawn(40, 40, Theme.MatplotlibClassic));

        Assert.Equal(16, faces.Length);
        Assert.Equal([0, 10, 20, 30], Starts(faces, f => f.Col));
    }

    [Fact]
    public void NoStrideSet_UnderAnotherTheme_DrawsAtMostFiftyPatchesASide()
    {
        Assert.Equal(39 * 39, FacesOf(Drawn(40, 40, Theme.MatplotlibV2)).Length);

        // rows 120: stride ceil(120 / 50) = 3, row_inds 0, 3, ..., 117, 119 -> 40 blocks.
        var faces = FacesOf(Drawn(120, 60, Theme.MatplotlibV2));
        Assert.Equal(40 * 30, faces.Length);
        Assert.Equal(119, faces.Max(f => f.RowEnd));
        Assert.Equal(59, faces.Max(f => f.ColEnd));
    }

    [Fact]
    public void OneStrideSet_LeavesTheOtherAtMatplotlibsTen()
    {
        // has_stride holds when either is given; the one not given keeps its default of 10, in every style.
        var faces = FacesOf(Drawn(40, 40, Theme.MatplotlibV2, s => s.RowStride = 2));

        Assert.Equal(20 * 4, faces.Length);
        Assert.Equal([0, 10, 20, 30], Starts(faces, f => f.Col));
    }

    [Fact]
    public void AStrideSetToOne_DrawsEveryCellUnderTheClassicTheme()
    {
        Assert.Equal(39 * 39, FacesOf(Drawn(40, 40, Theme.MatplotlibClassic, s => { s.RowStride = 1; s.ColStride = 1; })).Length);
    }

    [Fact]
    public void TheClassicSurfaceStride_SurvivesJsonAndADerivedTheme()
    {
        Assert.Equal(10, Theme.MatplotlibClassic.Looks.Cube.SurfaceStride);
        Assert.Null(Theme.MatplotlibV2.Looks.Cube.SurfaceStride);
        Assert.Equal(10, Theme.MatplotlibClassic.ThroughDto().Looks.Cube.SurfaceStride);
        Assert.Equal(10, Theme.MatplotlibClassic.Cloned().Looks.Cube.SurfaceStride);
        Assert.Null(ThemeDto.From(Theme.MatplotlibV2).SurfaceStride);
    }

    [Fact]
    public void AnotherTheme_MakesTheFacesAgain()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).Surface(Axis(40), Axis(40), Bowl(40, 40)).Build();
        _ = figure.ToSvg();

        figure.Theme = Theme.MatplotlibClassic;
        _ = figure.ToSvg();

        Assert.Equal(16, FacesOf((SurfaceSeries)figure.SubPlots[0].Series[0]).Length);
    }
}
