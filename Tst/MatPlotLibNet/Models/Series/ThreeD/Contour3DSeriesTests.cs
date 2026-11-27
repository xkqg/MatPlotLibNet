// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Contour3DSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class Contour3DSeriesTests
{
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

        Assert.Equal(code is 5 or 10 ? 4 : 2, ctx.Count<DrawLineCall>());
    }

    /// <summary>A 3-D contour traces its levels by the one marching squares of a 2-D contour (<c>MarchingSquares</c>, contourpy's
    /// decisions): in a saddle cell whose bottom-left and top-right corners stand above the level, the line from the bottom crossing
    /// runs to the left crossing and the line from the top crossing to the right one. On the grid [[1, 0], [0, 1]] the lower level
    /// (0.25) then cuts the two long diagonals, the upper level (0.75) the two short corners; the 3-D contour paired them the other
    /// way round.</summary>
    [Fact]
    public void ASaddleCell_IsTracedAsTheTwoDimensionalContourTracesIt()
    {
        const double Width = 1.7;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Contour3D([0.0, 1.0], [0.0, 1.0], new double[,] { { 1, 0 }, { 0, 1 } }, s =>
        {
            s.Levels = 2;
            s.LineWidth = Width;
        })).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);

        double[] lengths = [.. ctx.Of<DrawLineCall>().Where(c => c.Thickness == Width)
            .Select(c => c.P1.DistanceTo(c.P2))];

        Assert.Equal(4, lengths.Length);
        Assert.True(lengths[..2].Min() > 2 * lengths[2..].Max(), string.Join(", ", lengths));
    }
}
