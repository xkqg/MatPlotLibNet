// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The one rule every streaming form is held to, as a check that can be run on any row: after the bar at index <c>i</c>, each
/// output line holds what the batch indicator gives as its last value over the bars <c>0..i</c>, to <c>1e-9 × max(1, |batch|)</c>. The
/// comparison is made after every bar, on a batch run over that prefix each time; nothing is skipped: the lengths of the lines, the
/// number of values compared and the warm-up the form states are asserted as well.</summary>
public static class StreamingEquality
{
    /// <summary>Feeds the row's form the bars one at a time and compares it with the batch indicator over every prefix.</summary>
    /// <param name="row">The form, the batch indicator and the bars.</param>
    public static void AssertAgrees(StreamingRow row)
    {
        var bars = OracleBars.Walk(row.Bars);
        var form = row.Make();
        int compared = 0;
        int firstValue = -1;

        for (int i = 0; i < bars.Count; i++)
        {
            (row.Feed ?? ((f, all, at) => f.AppendBar(all.At(at))))(form, bars, i);
            double[] expected = row.BatchLast(bars.Prefix(i + 1));

            Assert.Equal(expected.Length, form.OutputSeries.Count);
            bool every = true;
            for (int k = 0; k < expected.Length; k++)
            {
                Assert.Equal(i + 1, form.OutputSeries[k].Count);
                double actual = form.OutputSeries[k].CreateSnapshot().YData[^1];
                if (double.IsNaN(expected[k]))
                {
                    Assert.True(double.IsNaN(actual), $"{row.Name} output {k} after bar {i}: the batch has no value yet, the form says {actual}");
                    every = false;
                    continue;
                }

                compared++;
                Assert.True(
                    Math.Abs(actual - expected[k]) <= 1e-9 * Math.Max(1, Math.Abs(expected[k])),
                    $"{row.Name} output {k} after bar {i}: the batch says {expected[k]:R}, the form says {actual:R}");
            }

            if (every && firstValue < 0) firstValue = i;
        }

        Assert.True(firstValue >= 0, $"{row.Name}: the batch never had a value on every output over {bars.Count} bars");
        int least = row.MinCompared ?? bars.Count / 2;
        Assert.True(compared >= least, $"{row.Name}: only {compared} values were compared over {bars.Count} bars, at least {least} are wanted");
        Assert.Equal(firstValue + 1, form.WarmupPeriod);
        Assert.Equal(bars.Count, form.ProcessedCount);
    }
}
