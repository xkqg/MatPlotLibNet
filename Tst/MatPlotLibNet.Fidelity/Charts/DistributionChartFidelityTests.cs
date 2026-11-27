// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Distribution-family fidelity tests — seaborn equivalents.</summary>
public class DistributionChartFidelityTests : FidelityTest
{
    [Theory]
    [FidelityRows]
    // Against seaborn's kdeplot: the curve alone on 200 points, smoothed by Scott's factor, standing on 0 with the 'Density' label (the
    // matplotlib themes draw it so); antialiasing and the text remain. Windows 9.83 / 0.9928 classic, 12.67 / 0.9934 v2; Linux 9.29 / 0.9925,
    // 11.72 / 0.9934.
    [FidelityTolerance(Rms = 11, Ssim = 0.98, DeltaE = 3, V2Rms = 14, V2Ssim = 0.98, V2DeltaE = 2)]
    public void Kde_NormalSamples_MatchesMatplotlib(string themeId)
    {
        var data = ReferenceData.Load("kde").Vector("data");
        AssertFidelity(themeId, "kde", "KDE — 500 normal samples", ax => ax
            .Kde(data));
    }

    [Theory]
    [FidelityRows]
    // Against seaborn's own rugplot: the ticks, their data, height, width and colour are the reference's, both pictures put a tick on the
    // pixel grid, and the axes end where the data and the margin end (ax.margins(tight=True), no tick rounding under classic). Windows
    // 6.21 / 0.9981 classic, 7.33 / 0.9980 v2; Linux 5.95 / 0.9979, 6.83 / 0.9980.
    [FidelityTolerance(Rms = 8, Ssim = 0.98, DeltaE = 2, V2Rms = 9, V2Ssim = 0.98, V2DeltaE = 3)]
    public void Rugplot_NormalSamples_MatchesMatplotlib(string themeId)
    {
        var data = ReferenceData.Load("rugplot").Vector("data");
        AssertFidelity(themeId, "rugplot", "Rugplot", ax => ax
            // sns.rugplot(data, height=0.3) then ax.set_ylim(0, 1): ticks 0.3 tall, drawn solid, one point wide (seaborn's
            // linewidth of 1), in a range of 0 to 1.
            .Rugplot(data, s => { s.Height = 0.3; s.Alpha = 1; s.LineWidth = 1.0 * RenderDpi / 72; })
            .SetYLim(0, 1));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 10, Ssim = 0.98, DeltaE = 3, V2Rms = 11)]   // dots on their categories (jitter off in both), the same size, colour and axes; antialiasing and the text remain
    public void Stripplot_ThreeGroups_MatchesMatplotlib(string themeId)
    {
        double[][] data = ReferenceData.Load("stripplot").Rows("groups");
        AssertFidelity(themeId, "stripplot", "Stripplot — 3 groups", ax => ax
            .Stripplot(data, s => s.Jitter = 0)
            .OnSeabornCategories(data.Length));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 10, Ssim = 0.98, DeltaE = 3, V2Rms = 11)]   // the dots stand where seaborn's Beeswarm puts them; antialiasing and the text remain
    public void Swarmplot_ThreeGroups_MatchesMatplotlib(string themeId)
    {
        double[][] data = ReferenceData.Load("swarmplot").Rows("groups");
        AssertFidelity(themeId, "swarmplot", "Swarmplot — 3 groups", ax => ax
            .Swarmplot(data)
            .OnSeabornCategories(data.Length));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 11, Ssim = 0.98, DeltaE = 3, V2Rms = 17, V2Ssim = 0.96, V2DeltaE = 3)]   // against seaborn's own pointplot: its line, lines, dots and axes are the reference's; the interval of a mean is Student's t where seaborn's is a seeded bootstrap (numpy's generator is not reproducible here), which moves the ends of the intervals, and with them the Y range and every label, by a pixel or two
    public void Pointplot_FourGroups_MatchesMatplotlib(string themeId)
    {
        double[][] data = ReferenceData.Load("pointplot").Rows("groups");
        AssertFidelity(themeId, "pointplot", "Pointplot — 4 groups", ax => ax
            .Pointplot(data)
            .OnSeabornCategories(data.Length));
    }

    [Theory]
    [FidelityRows]
    // Against seaborn's countplot: the 'count' label, the bars desaturated to 0.75 ((32, 32, 223) classic, (50, 116, 161) v2) standing on
    // 0; antialiasing and the text remain. Windows 7.44 / 0.9964 classic, 9.41 / 0.9966 v2; Linux 6.85 / 0.9964, 8.63 / 0.9965.
    [FidelityTolerance(Rms = 9, Ssim = 0.98, DeltaE = 3, V2Rms = 11, V2Ssim = 0.98, V2DeltaE = 2)]
    public void Countplot_FourCategories_MatchesMatplotlib(string themeId)
    {
        string[] values = ReferenceData.Load("countplot").Strings("categories");
        AssertFidelity(themeId, "countplot", "Countplot", ax => ax
            .Countplot(values));
    }
}
