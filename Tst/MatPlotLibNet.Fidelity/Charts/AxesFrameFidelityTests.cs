// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>The plots matplotlib has that this library draws through the frame of the axes: <c>fill_betweenx</c> (an area that
/// lies down), <c>matshow</c> (a matrix as equal cells, the first row on top) and <c>Spine.set_bounds</c> (a line that spans only
/// part of its axis). The references are made by <c>tools/mpl_reference/generate.py</c>.</summary>
public class AxesFrameFidelityTests : FidelityTest
{
    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 13, Ssim = 0.98, DeltaE = 3, V2Rms = 10, V2DeltaE = 2)]
    public void FillBetweenX_AreaThatLiesDown_MatchesMatplotlib(string themeId)
    {
        double[] y = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] x = [2, 4, 3, 6, 5, 8, 6, 9, 7, 10, 8];
        AssertFidelity(themeId, "fill_betweenx", "fill_betweenx", ax => ax
            .FillBetweenX(y, x, null, s => s.Alpha = 1));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 9, Ssim = 0.98, DeltaE = 3, V2Rms = 11)]   // the reference's colour bar, which takes its room from the axes as fig.colorbar's does
    public void MatShow_MatrixAsEqualCells_MatchesMatplotlib(string themeId)
    {
        var data = new double[6, 9];
        for (int row = 0; row < 6; row++)
            for (int column = 0; column < 9; column++)
                data[row, column] = (row * 9 + column) % 7;
        var theme = themeId.AsTheme();
        var figure = FidelityFigure(theme)
            .AddSubPlot(1, 1, 1, ax => ax.MatShow(data, s => s.ColorMap = ColorMaps.Viridis)   // the reference names cmap="viridis"
                .WithColorBar())
            .Build();
        AssertFidelity(figure, "matshow");
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 8, Ssim = 0.98, DeltaE = 3, V2Rms = 10, V2DeltaE = 2)]
    public void SpineBounds_LinesThatSpanPartOfTheirAxis_MatchMatplotlib(string themeId)
    {
        double[] x = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] y = [2, 3, 5, 4, 6, 8, 7, 9, 8, 10, 9];
        AssertFidelity(themeId, "set_bounds", "set_bounds", ax => ax
            .Plot(x, y)
            .WithSpines(spines => spines with
            {
                Bottom = spines.Bottom with { BoundsFrom = 2, BoundsTo = 8 },
                Left = spines.Left with { BoundsFrom = 3, BoundsTo = 9 },
                Top = spines.Top with { Visible = false },
                Right = spines.Right with { Visible = false },
            }));
    }
}
