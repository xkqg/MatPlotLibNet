// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="PolarHeatmapSeries"/> rendering.</summary>
public class PolarHeatmapRenderTests
{
    private static double[,] MakeData(int thetaBins = 8, int rBins = 4)
    {
        var d = new double[thetaBins, rBins];
        for (int t = 0; t < thetaBins; t++)
            for (int r = 0; r < rBins; r++)
                d[t, r] = t + r * 0.5;
        return d;
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("PolarHeatmap_SvgContainsPolygonElements", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(MakeData(), 8, 4))
            .ToSvg(), "<polygon"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
