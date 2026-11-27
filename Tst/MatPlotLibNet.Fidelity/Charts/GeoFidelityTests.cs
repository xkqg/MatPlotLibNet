// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>A world map on the Robinson projection, compared with cartopy's drawing of the same layers. The references are
/// drawn by <c>tools/mpl_reference/generate.py --chart geo_robinson</c> with cartopy installed (its wheels carry GEOS and PROJ;
/// <c>requirements.txt</c> pins it). Both draw Natural Earth at 110m (the countries, the coastlines and the land boundaries), the
/// map in its projection's shape inside the projection's outline, framed by it under both styles. The limits are the worse of
/// Windows and the ubuntu noble container per style (classic 8.65 / 8.58 RMS, 0.9801 / 0.97997 SSIM, 3.45 dE; v2 10.43 / 10.32,
/// 0.9799, 3.71), rounded out: the noble classic SSIM lies under 0.98, so its limit is 0.97.</summary>
public class GeoFidelityTests : FidelityTest
{
    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 9, Ssim = 0.97, DeltaE = 4, V2Rms = 11, V2Ssim = 0.97, V2DeltaE = 4)]
    public void Robinson_WorldMap_MatchesCartopy(string themeId)
    {
        var theme = themeId.AsTheme();
        var proj = GeoProjection.Robinson;
        var figure = FidelityFigure(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("World Map — Robinson")
                .WithProjection(proj)
                .Ocean(proj, Color.FromHex("#1a3a5c"))
                .Land(proj, Color.FromHex("#2d5a27"))
                .Coastlines(proj, Colors.White, lineWidth: 0.5)
                .Borders(proj, Color.FromHex("#888888"), lineWidth: 0.2)
                // The reference draws no legend: matplotlib draws one only when asked, and cartopy's features are no entries
                // of one. The library's layers carry labels, so the check hides the legend, as the samples' maps do.
                .WithLegend(visible: false))
            .Build();

        AssertFidelity(figure, "geo_robinson");
    }
}
