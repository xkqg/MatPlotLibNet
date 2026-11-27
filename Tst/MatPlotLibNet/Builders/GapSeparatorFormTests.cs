// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Models.Series;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>A line that is drawn as several runs holds them as the points of one polyline with a missing point (NaN) between each two
/// runs. Everything the library writes in that shape writes ONE form of it: a single separator between two runs, none before the
/// first run, none after the last, never two in a row. And every line series READS any arrangement of missing points, so a document
/// written before the form was one (a separator after the last segment, in front of the first run, or doubled) draws the same.</summary>
public class GapSeparatorFormTests
{
    private const double NaN = double.NaN;

    /// <summary>The names of the places that write a line in runs: the three segment verbs, the Kagi lines, a class of rows of a
    /// parallel-coordinates chart, and a reshaped line that is joined across its gaps again.</summary>
    public static TheoryData<string> Writers { get; } =
        ["VLines", "HLines", "Triplot", "Kagi (thin)", "Kagi (thick)", "Parallel coordinates", "A reshaped line joined across its gaps"];

    private static LineSeries[] LinesOf(Action<AxesBuilder> verb) =>
        [.. Plt.Create().AddSubPlot(1, 1, 1, verb).Build().SubPlots[0].Series.OfType<LineSeries>()];

    private static (double[] X, double[] Y) Written(string writer)
    {
        switch (writer)
        {
            case "VLines": { var line = Assert.Single(LinesOf(ax => ax.VLines([1.0, 2.0, 3.0], [0.0], [5.0]))); return (line.XData, line.YData); }
            case "HLines": { var line = Assert.Single(LinesOf(ax => ax.HLines([1.0, 2.0, 3.0], [0.0], [5.0]))); return (line.XData, line.YData); }
            case "Triplot": { var line = Assert.Single(LinesOf(ax => ax.Triplot([0.0, 1.0, 0.0, 1.0], [0.0, 0.0, 1.0, 1.0]))); return (line.XData, line.YData); }
            case "Kagi (thin)": { var line = LinesOf(ax => ax.Kagi([10.0, 12, 9, 13, 8, 14, 7], 2))[0]; return (line.XData, line.YData); }
            case "Kagi (thick)": { var line = LinesOf(ax => ax.Kagi([10.0, 12, 9, 13, 8, 14, 7], 2))[1]; return (line.XData, line.YData); }
            case "Parallel coordinates": { var line = Assert.Single(LinesOf(ax => ax.ParallelCoordinates(["a", "b"], [new("k", [1, 5]), new("k", [3, 2]), new("k", [2, 4])]))); return (line.XData, line.YData); }
            default:
                double[] x = [1, 2, 3, NaN, 5, 6, NaN, 8, 9], y = [1, 2, 3, NaN, 5, 6, NaN, 8, 9];
                var joined = x.DrawableIndexRuns(y).ToList().JoinedAcrossGaps(x, y, run => run);
                return (joined.X, joined.Y);
        }
    }

    /// <summary>The one form: a separator is never first, never last, never next to another separator.</summary>
    private static void AssertTheOneForm(double[] x, double[] y)
    {
        Assert.Equal(x.Length, y.Length);
        Assert.Contains(x, double.IsNaN);   // the fixtures hold more than one run, so there is a separator to look at
        x.AssertGapsStandBetweenValues("x");
        for (int i = 0; i < x.Length; i++)
        {
            if (double.IsNaN(x[i]))
            {
                Assert.True(double.IsNaN(y[i]), $"The separator at {i} is a missing x with a y.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Writers))]
    public void EveryWriterOfALineInRuns_WritesTheOneForm(string writer)
    {
        var (x, y) = Written(writer);

        AssertTheOneForm(x, y);
    }

    // ── The reader keeps every arrangement ───────────────────────────────────

    /// <summary>The same two runs of a line, written in the one form and in the three arrangements earlier documents hold.</summary>
    public static TheoryData<string, double[], double[]> Arrangements { get; } = new()
    {
        { "in the one form", [1, 2, NaN, 4, 5], [1, 3, NaN, 2, 5] },
        { "with a separator after the last run", [1, 2, NaN, 4, 5, NaN], [1, 3, NaN, 2, 5, NaN] },
        { "with a separator in front of the first run", [NaN, 1, 2, NaN, 4, 5], [NaN, 1, 3, NaN, 2, 5] },
        { "with a doubled separator", [1, 2, NaN, NaN, 4, 5], [1, 3, NaN, NaN, 2, 5] },
    };

    private static List<Point[]> Polylines(double[] x, double[] y)
    {
        var figure = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax.Plot(x, y)).Build();
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return [.. recording.Polylines()];
    }

    [Theory]
    [MemberData(nameof(Arrangements))]
    public void ALineSeries_DrawsTheSamePolylines_WhateverArrangementOfSeparatorsItHolds(string arrangement, double[] x, double[] y)
    {
        var expected = Polylines([1, 2, NaN, 4, 5], [1, 3, NaN, 2, 5]);

        var drawn = Polylines(x, y);

        Assert.True(expected.Count == 2, "The one form draws two polylines.");
        Assert.True(drawn.Count == expected.Count && drawn.Zip(expected, (a, b) => a.SequenceEqual(b)).All(same => same),
            $"The line {arrangement} is not drawn as the line in the one form.");
    }
}
