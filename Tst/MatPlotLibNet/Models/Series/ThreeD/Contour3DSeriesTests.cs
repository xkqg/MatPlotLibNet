// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Contour3DSeries"/> default properties and construction.</summary>
public class Contour3DSeriesTests
{
    private static readonly double[] X = [1, 2, 3];
    private static readonly double[] Y = [4, 5];
    private static readonly double[,] Z = { { 1, 2, 3 }, { 4, 5, 6 } };
    private static readonly double[] SingleX = [1.0];
    private static readonly double[] SingleY = [2.0];
    private static readonly double[,] SingleZ = { { 1.0 } };

    /// <summary>Verifies that the constructor stores X, Y, and Z grid data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new Contour3DSeries(X, Y, Z);
        Assert.Equal(X, series.X);
        Assert.Equal(Y, series.Y);
        Assert.Equal(Z, series.Z);
    }

    /// <summary>Verifies that Levels defaults to 10.</summary>
    [Fact]
    public void DefaultLevels_Is10()
    {
        var series = new Contour3DSeries(SingleX, SingleY, SingleZ);
        Assert.Equal(10, series.Levels);
    }

    /// <summary>Verifies that LineWidth defaults to 1.0.</summary>
    [Fact]
    public void DefaultLineWidth_Is1Point0()
    {
        var series = new Contour3DSeries(SingleX, SingleY, SingleZ);
        Assert.Equal(1.0, series.LineWidth);
    }

    public static TheoryData<int> MarchingSquaresCases { get; } = new(Enumerable.Range(1, 14));

    /// <summary>Every marching-squares case of one cell draws its crossing: one segment per level, two for the saddles
    /// (5 and 10). The cell's corners are 0 or 1, one bit per corner (bottom-left 1, bottom-right 2, top-right 4,
    /// top-left 8), so both levels (0.25 and 0.75) cross the same edges.</summary>
    [Theory]
    [MemberData(nameof(MarchingSquaresCases))]
    public void EveryMarchingSquaresCase_DrawsItsCrossing(int code)
    {
        double[,] z = { { code & 1, (code >> 1) & 1 }, { (code >> 3) & 1, (code >> 2) & 1 } };
        var bounds = new Rect(0, 0, 400, 300);
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(new DataTransform(0, 1, 0, 1, bounds), ctx, Colors.Tab10Blue, new RenderArea(bounds, ctx))
        {
            Projection3D = new Projection3D(30, -60, bounds, 0, 1, 0, 1, 0, 1),
        };

        new Contour3DSeriesRenderer(context).Render(new Contour3DSeries([0, 1], [0, 1], z) { Levels = 2 });

        Assert.Equal(code is 5 or 10 ? 4 : 2, ctx.CountOf("DrawLine"));
    }

    /// <summary>Verifies that ToSeriesDto sets type to "contour3d".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsContour3D()
    {
        var series = new Contour3DSeries(SingleX, SingleY, SingleZ);
        var dto = series.ToSeriesDto();
        Assert.Equal("contour3d", dto.Type);
    }
}
