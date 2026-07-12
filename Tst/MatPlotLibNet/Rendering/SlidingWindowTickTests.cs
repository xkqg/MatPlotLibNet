// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that a rolling time window keeps its tick labels still.
/// <para>A strip chart slides continuously: the window is <c>[now - span, now]</c> and it moves every frame.
/// If the locator picks which labels to keep by counting from the first tick <i>inside the window</i>, then the
/// moment the window advances past a tick the entire label row re-shuffles — every label jumps to a new place
/// while the trace under it glides smoothly. It reads as a broken chart.</para>
/// <para>The cure is to anchor the labels on absolute time rather than on position in a list: a given clock
/// instant is either a labelled tick or it is not, no matter where the window happens to start. Then the
/// labels glide out of frame with the data they belong to.</para></summary>
public class SlidingWindowTickTests
{
    private const double OneSecond = 1.0 / (24 * 60 * 60);

    /// <summary>Advancing the window by one second does not re-shuffle the labels: every tick still visible in
    /// the new window was a tick in the old one.</summary>
    [Fact]
    public void AdvancingTheWindow_KeepsTheSameLabelsOnTheSameInstants()
    {
        var locator = new AutoDateLocator();
        var start = new DateTime(2026, 7, 12, 14, 30, 0, DateTimeKind.Utc).ToOADate();
        double span = 60 * OneSecond;

        double[] before = locator.Locate(start, start + span);
        double[] after = locator.Locate(start + OneSecond, start + span + OneSecond);

        // Everything still inside the new window must have been a label a second ago too.
        var carriedOver = after.Where(t => t >= start + OneSecond && t <= start + span).ToArray();

        Assert.NotEmpty(carriedOver);
        foreach (double tick in carriedOver)
        {
            Assert.Contains(before, b => Math.Abs(b - tick) < OneSecond / 100);
        }
    }

    /// <summary>The chosen labels sit on ROUND clock instants, so they read as times rather than as arbitrary
    /// offsets from wherever the window began.</summary>
    [Fact]
    public void TheLabels_SitOnRoundClockInstants()
    {
        var locator = new AutoDateLocator();
        // A deliberately unround start: 14:30:07. The labels must not inherit the :07.
        var start = new DateTime(2026, 7, 12, 14, 30, 7, DateTimeKind.Utc).ToOADate();

        double[] ticks = locator.Locate(start, start + 60 * OneSecond);

        Assert.NotEmpty(ticks);
        foreach (double tick in ticks)
        {
            var t = DateTime.FromOADate(tick);
            Assert.True(t.Millisecond < 2, $"tick {t:HH:mm:ss.fff} is not on a whole second");
        }
    }

    /// <summary>Across many consecutive frames the label row stays phase-stable: no frame invents a label on an
    /// instant that a neighbouring frame did not also consider a label.</summary>
    [Fact]
    public void AcrossManyFrames_TheLabelPhaseNeverFlips()
    {
        var locator = new AutoDateLocator();
        var start = new DateTime(2026, 7, 12, 14, 30, 0, DateTimeKind.Utc).ToOADate();
        double span = 60 * OneSecond;

        var seen = new List<double[]>();
        for (int frame = 0; frame < 12; frame++)
        {
            double from = start + frame * OneSecond;
            seen.Add(locator.Locate(from, from + span));
        }

        for (int i = 1; i < seen.Count; i++)
        {
            double lo = start + i * OneSecond;
            foreach (double tick in seen[i].Where(t => t <= start + span))
            {
                Assert.True(seen[i - 1].Any(p => Math.Abs(p - tick) < OneSecond / 100),
                    $"frame {i} labelled an instant frame {i - 1} did not: {DateTime.FromOADate(tick):HH:mm:ss}");
            }
        }
    }
}
