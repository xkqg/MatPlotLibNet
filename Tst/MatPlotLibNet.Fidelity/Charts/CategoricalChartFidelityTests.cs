// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Categorical-family fidelity tests (broken_bar, eventplot, gantt, waterfall).</summary>
public class CategoricalChartFidelityTests : FidelityTest
{
    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 12, Ssim = 0.97, DeltaE = 3, V2Rms = 13, V2DeltaE = 2)]   // the bars, their edges and the colours are the reference's; antialiasing and the text's blend remain
    public void BrokenBar_TwoRows_MatchesMatplotlib(string themeId)
    {
        BarRange[][] ranges =
        [
            [new(0, 3), new(5, 2), new(8, 4)],
            [new(1, 2), new(6, 3)],
        ];
        var figure = FidelityFigure(themeId)
            // The reference's rows stand from 10 to 15 and from 20 to 25 in a Y range of 5 to 30 (generate.py, fig_broken_bar);
            // the rows of BrokenBarH stand at 0 and 1, so the same picture is half a row high in a range of -0.75 to 1.75.
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Broken bar — broken_barh")
                .BrokenBarH(ranges, s =>
                {
                    s.BarHeight = 0.5;
                    s.Labels = ["task A", "task B"];
                })
                .SetXLim(0, 15)
                .SetYLim(-0.75, 1.75)
                .SetXLabel("time"))
            .Build();
        AssertFidelity(figure, "broken_bar");
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 9, Ssim = 0.98, DeltaE = 2, V2Rms = 10, V2DeltaE = 4)]   // event ticks are thin; mostly background → low SSIM
    public void Eventplot_FourRows_MatchesMatplotlib(string themeId)
    {
        double[][] positions = ReferenceData.Load("eventplot").Rows("rows");
        AssertFidelity(themeId, "eventplot", "Eventplot — 4 rows", ax => ax
            // matplotlib draws every row in lines.color, the first colour of each style's cycle ('b' classic, 'C0' v2), which
            // the library's eventplot draws too.
            .Eventplot(positions));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 14, Ssim = 0.98, DeltaE = 2, V2Rms = 11)]   // the bars, names and colours are the reference's; antialiasing and the text remain
    public void Gantt_FourTasks_MatchesMatplotlib(string themeId)
    {
        string[] tasks = ["Design", "Build", "Test", "Ship"];
        double[] starts = [0, 3, 8, 11];
        double[] ends   = [3, 8, 11, 13];
        // The reference is barh(i, duration, left=start, height=0.6) for each task, each bar in the next colour of the cycle, with the task
        // names for tick labels and invert_yaxis(): the first task on top. The library's Gantt chart draws that: its rows named by the
        // tasks, the first on top, a colour for each task; under the classic style its named rows still round out (-0.5 .. 3.5). Under classic
        // the first bar's black edge reaches one pixel past the left spine (classic RMS 12.24): matplotlib clips every artist to the axes,
        // the library clips a series only where an axis has a limit of its own (SeriesGroupExtensions.ClippedForSeries).
        AssertFidelity(themeId, "gantt", "Gantt chart", ax => ax
            .SetXLabel("day")
            .Gantt(tasks, starts, ends));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 11, Ssim = 0.98, DeltaE = 3, V2Rms = 34, V2Ssim = 0.90, V2DeltaE = 2)]   // the bars and their range are the reference's; what remains under v2 is the top of the Y range (the library pads past 150, matplotlib stops at the foot of the last bar) and the antialiased text
    public void Waterfall_Cumulative_MatchesMatplotlib(string themeId)
    {
        // An opening total of 100, four steps and a closing total of 140, in the colours the generator writes out (a reference
        // taken from the style's cycle would be red and cyan in the classic style): six bars as wide as matplotlib's bar() draws them.
        string[] labels = ["Start", "A", "B", "C", "D", "End"];
        double[] values = [100, 30, -20, 40, -10, 140];
        var figure = FidelityFigure(themeId)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithTitle("Waterfall chart")
                    .Waterfall(labels, values, s =>
                    {
                        s.TotalIndices = [0, 5];
                        s.IncreaseColor = Color.FromHex("#2CA02C");
                        s.DecreaseColor = Color.FromHex("#D62728");
                        s.TotalColor = Color.FromHex("#1F77B4");
                        s.BarWidth = 0.8;
                    });
                // The reference's bars stand on the numbers 0 to 5 (ax.bar(i, ...)): matplotlib's range is the bars' edges (-0.4 to 5.4) padded by the
                // style, rounded out under classic while the ticks are still the default ones, before set_xticks names them (get_xlim() is -1 to 6
                // classic, -0.69 to 5.69 v2). The library's category k stands in the slot k to k + 1, centred 0.5 past k, and its waterfall ranges
                // over the slots as they are (0 to 6, no margin), so the check states the reference's range in slot units, 0.5 further on.
                ax.SetXLim(themeId == "classic" ? -0.5 : -0.19, themeId == "classic" ? 6.5 : 6.19);
            })
            .Build();
        AssertFidelity(figure, "waterfall");
    }
}
