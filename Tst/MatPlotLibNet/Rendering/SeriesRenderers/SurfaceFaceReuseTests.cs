// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Turning a 3-D surface changes where its faces land and in which order they are painted, and nothing else: a
/// face's colour after the light, and the vertex data the browser turns it with, depend on the data, the colour map, the
/// light and the data box, not on the camera. A surface drawn again keeps those, and every fact here holds the picture to
/// the one a surface built from scratch draws: after a turn, after data changed in place, after the map, the light, the
/// strides or the data box changed.</summary>
public sealed class SurfaceFaceReuseTests
{
    private static readonly double[] Grid = [-2.0, -1.5, -0.5, 0.0, 0.5, 1.5, 2.0];

    private static double[,] Heights(double lift = 0)
    {
        var z = new double[Grid.Length, Grid.Length];
        for (int i = 0; i < Grid.Length; i++)
            for (int j = 0; j < Grid.Length; j++)
                z[i, j] = (Grid[i] * Grid[i]) - (0.5 * Grid[j] * Grid[j]) + (0.25 * Grid[i] * Grid[j]) + lift;
        return z;
    }

    private static Figure Build(double[,] z) =>
        Plt.Create().With3DRotation()
            .WithSize(400, 300)
            .Surface(Grid, Grid, z)
            .WithLighting(0.5, 0.5, 1.0)
            .Build();

    private static SurfaceSeries SurfaceOf(Figure figure) => (SurfaceSeries)figure.SubPlots[0].Series[0];

    /// <summary>The picture a figure built from scratch, with <paramref name="change"/> applied, draws at that camera.</summary>
    private static string Fresh(double[,] z, double azimuth, Action<Figure>? change = null)
    {
        var figure = Build((double[,])z.Clone());
        change?.Invoke(figure);
        figure.SubPlots[0].Azimuth = azimuth;
        return figure.ToSvg();
    }

    [Fact]
    public void TurningTheCamera_DrawsWhatAFreshSurfaceDrawsAtEachAngle()
    {
        var z = Heights();
        var figure = Build((double[,])z.Clone());

        foreach (double azimuth in new[] { -60.0, -45.0, 10.0, 135.0 })
        {
            figure.SubPlots[0].Azimuth = azimuth;
            Assert.Equal(Fresh(z, azimuth), figure.ToSvg());
        }
    }

    [Fact]
    public void TurningTheCamera_KeepsTheFacesOfTheFirstDraw()
    {
        var figure = Build(Heights());
        _ = figure.ToSvg();
        var kept = SurfaceOf(figure).DrawnFaces;

        figure.SubPlots[0].Azimuth += 15;
        _ = figure.ToSvg();

        Assert.NotNull(kept);
        Assert.Same(kept, SurfaceOf(figure).DrawnFaces);
    }

    [Fact]
    public void AHeightChangedInPlace_IsDrawn()
    {
        var z = Heights();
        var figure = Build((double[,])z.Clone());
        _ = figure.ToSvg();
        var kept = SurfaceOf(figure).DrawnFaces;

        SurfaceOf(figure).Z[3, 3] += 0.75;
        figure.SubPlots[0].Azimuth = -40;
        z[3, 3] += 0.75;

        Assert.Equal(Fresh(z, -40), figure.ToSvg());
        Assert.NotSame(kept, SurfaceOf(figure).DrawnFaces);
    }

    [Fact]
    public void AGridValueChangedInPlace_IsDrawn()
    {
        var figure = Plt.Create().With3DRotation().WithSize(400, 300)
            .Surface((double[])Grid.Clone(), (double[])Grid.Clone(), Heights()).WithLighting(0.5, 0.5, 1.0).Build();
        _ = figure.ToSvg();

        SurfaceOf(figure).X[1] = -1.25;
        SurfaceOf(figure).Y[5] = 1.25;
        figure.SubPlots[0].Azimuth = -40;
        double[] x = (double[])Grid.Clone(), y = (double[])Grid.Clone();
        x[1] = -1.25;
        y[5] = 1.25;
        var fresh = Plt.Create().With3DRotation().WithSize(400, 300)
            .Surface(x, y, Heights()).WithLighting(0.5, 0.5, 1.0).Build();
        fresh.SubPlots[0].Azimuth = -40;

        Assert.Equal(fresh.ToSvg(), figure.ToSvg());
    }

    public static TheoryData<Action<SurfaceSeries>> ASingleChange => new()
    {
        surface => surface.ColorMap = ColorMaps.Plasma,
        surface => surface.ColStride = 2,
    };

    [Theory]
    [MemberData(nameof(ASingleChange))]
    public void AnotherColourMap_OrOnlyTheColumnStride_Changed_IsDrawn(Action<SurfaceSeries> change)
    {
        var z = Heights();
        var figure = Build((double[,])z.Clone());
        _ = figure.ToSvg();
        change(SurfaceOf(figure));
        Assert.Equal(Fresh(z, -60, f => change(SurfaceOf(f))), figure.ToSvg());
    }

    [Fact]
    public void AnotherNormalizer_IsDrawn()
    {
        var z = Heights(lift: 3);
        var figure = Build((double[,])z.Clone());
        _ = figure.ToSvg();
        var log = new LogNormalizer();

        SurfaceOf(figure).Normalizer = log;

        Assert.Equal(Fresh(z, -60, f => SurfaceOf(f).Normalizer = log), figure.ToSvg());
    }

    [Fact]
    public void AnotherLight_IsDrawn()
    {
        var z = Heights();
        var figure = Build((double[,])z.Clone());
        _ = figure.ToSvg();
        var light = new DirectionalLight(-1, 0.25, 0.5);

        figure.SubPlots[0].LightSource = light;

        Assert.Equal(Fresh(z, -60, f => f.SubPlots[0].LightSource = light), figure.ToSvg());
    }

    [Fact]
    public void NoLightAnyMore_IsDrawn()
    {
        var z = Heights();
        var figure = Build((double[,])z.Clone());
        _ = figure.ToSvg();

        figure.SubPlots[0].LightSource = null;

        Assert.Equal(Fresh(z, -60, f => f.SubPlots[0].LightSource = null), figure.ToSvg());
    }

    [Fact]
    public void OtherStrides_AreDrawn()
    {
        var z = Heights();
        var figure = Build((double[,])z.Clone());
        _ = figure.ToSvg();

        SurfaceOf(figure).RowStride = 2;
        SurfaceOf(figure).ColStride = 3;

        Assert.Equal(Fresh(z, -60, f => { SurfaceOf(f).RowStride = 2; SurfaceOf(f).ColStride = 3; }), figure.ToSvg());
    }

    [Fact]
    public void OnlyARowCoordinate_ChangedInPlace_IsDrawn()
    {
        var figure = Plt.Create().With3DRotation().WithSize(400, 300)
            .Surface((double[])Grid.Clone(), (double[])Grid.Clone(), Heights()).WithLighting(0.5, 0.5, 1.0).Build();
        _ = figure.ToSvg();
        var kept = SurfaceOf(figure).DrawnFaces;

        SurfaceOf(figure).Y[2] = -0.75;

        double[] y = (double[])Grid.Clone();
        y[2] = -0.75;
        var fresh = Plt.Create().With3DRotation().WithSize(400, 300)
            .Surface((double[])Grid.Clone(), y, Heights()).WithLighting(0.5, 0.5, 1.0).Build();
        Assert.Equal(fresh.ToSvg(), figure.ToSvg());
        Assert.NotSame(kept, SurfaceOf(figure).DrawnFaces);
    }

    [Fact]
    public void AnotherDataBox_IsDrawn()
    {
        // The vertex data the browser turns the surface with is the data placed in the data box, so a new z limit
        // moves every face's vertex data while the heights stay as they were.
        var z = Heights();
        var figure = Build((double[,])z.Clone());
        _ = figure.ToSvg();

        figure.SubPlots[0].ZAxis.Max = 9;

        Assert.Equal(Fresh(z, -60, f => f.SubPlots[0].ZAxis.Max = 9), figure.ToSvg());
    }

    [Fact]
    public void RotationInTheBrowserSwitchedOff_DropsTheVertexData()
    {
        var z = Heights();
        var figure = Build((double[,])z.Clone());
        _ = figure.ToSvg();

        // The vertex data follows the figure as it is at each draw: switched off on the figure, it is gone from the next one.
        static void SwitchOff(Figure f) => f.Enable3DRotation = false;
        SwitchOff(figure);

        string drawn = figure.ToSvg();
        Assert.Equal(Fresh(z, -60, SwitchOff), drawn);
        Assert.DoesNotContain("data-v3d", drawn);
    }

    [Fact]
    public void AnSvgDrawAndANativeDraw_KeepTheSameFaces()
    {
        // The SVG of a figure turned in the browser writes the vertex data of every face; a native draw writes none. The
        // vertex data is made beside the faces, so the two draws in turn never make the faces again.
        var figure = Build(Heights());
        _ = figure.ToSvg();
        var kept = SurfaceOf(figure).DrawnFaces;

        new MatPlotLibNet.Rendering.ChartRenderer().Render(figure, new RecordingRenderContext());
        Assert.Same(kept, SurfaceOf(figure).DrawnFaces);

        string again = figure.ToSvg();
        Assert.Same(kept, SurfaceOf(figure).DrawnFaces);
        Assert.Contains("data-v3d", again);
    }

    [Fact]
    public void AMissingHeight_LeavesItsFacesOut_AsAFreshSurfaceDoes()
    {
        var z = Heights();
        z[2, 4] = double.NaN;
        z[5, 1] = double.PositiveInfinity;
        var figure = Build((double[,])z.Clone());
        _ = figure.ToSvg();

        figure.SubPlots[0].Azimuth = 30;

        Assert.Equal(Fresh(z, 30), figure.ToSvg());
    }

    [Fact]
    public void AMissingGridCoordinate_LeavesItsFacesOut_AsAFreshSurfaceDoes()
    {
        double[] x = (double[])Grid.Clone(), y = (double[])Grid.Clone();
        x[3] = double.NaN;
        y[4] = double.NegativeInfinity;
        Figure Make() => Plt.Create().With3DRotation().WithSize(400, 300)
            .Surface((double[])x.Clone(), (double[])y.Clone(), Heights()).WithLighting(0.5, 0.5, 1.0).Build();
        var figure = Make();
        _ = figure.ToSvg();

        figure.SubPlots[0].Azimuth = 30;
        var fresh = Make();
        fresh.SubPlots[0].Azimuth = 30;

        Assert.Equal(fresh.ToSvg(), figure.ToSvg());
    }

    [Fact]
    public void ASurfaceOfOneRowOrOneColumn_HasNoFaceToDraw_AndKeepsNone()
    {
        var oneRow = Plt.Create().With3DRotation().Surface([1.0, 2, 3], [1.0], new double[,] { { 1, 2, 3 } }).Build();
        var oneColumn = Plt.Create().With3DRotation().Surface([1.0], [1.0, 2, 3], new double[,] { { 1 }, { 2 }, { 3 } }).Build();

        string row = oneRow.ToSvg(), column = oneColumn.ToSvg();

        Assert.Null(SurfaceOf(oneRow).DrawnFaces);
        Assert.Null(SurfaceOf(oneColumn).DrawnFaces);
        Assert.Contains("<svg", row);
        Assert.Contains("<svg", column);
    }

    [Fact]
    public void AWireframeAndAlphaChange_AreDrawn_WithoutMakingTheFacesAgain()
    {
        var z = Heights();
        var figure = Build((double[,])z.Clone());
        _ = figure.ToSvg();
        var kept = SurfaceOf(figure).DrawnFaces;

        SurfaceOf(figure).ShowWireframe = false;
        SurfaceOf(figure).Alpha = 0.5;

        Assert.Equal(Fresh(z, -60, f => { SurfaceOf(f).ShowWireframe = false; SurfaceOf(f).Alpha = 0.5; }), figure.ToSvg());
        Assert.Same(kept, SurfaceOf(figure).DrawnFaces);
    }
}
