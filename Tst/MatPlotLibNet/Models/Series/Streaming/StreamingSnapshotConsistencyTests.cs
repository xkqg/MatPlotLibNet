// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Models.Series.Streaming;

/// <summary>A streaming series is a sequence of POINTS and of BARS — the library names both
/// (<see cref="StreamingPoint"/>, <see cref="OhlcBar"/>) — and a reader must never see one whose fields come
/// from different samples. Storing a point as two buffers and a bar as four makes each buffer individually
/// thread-safe and leaves the only invariant that matters guarded by nothing: an append is then two or four
/// separate acts, and a snapshot two or four separate reads.
///
/// <para>Each test appends values in a RELATION (y = 2x, high = open + 1) and checks the relation in every
/// snapshot taken while appending. A length check alone would miss the worse half — fields that pair up but
/// come from different ticks. A failure here is proof; a pass is not, which is why the relation is checked on
/// every element of every snapshot rather than on a sample of them.</para></summary>
public sealed class StreamingSnapshotConsistencyTests
{
    private const int Appends = 200_000;

    [Fact]
    public void APoint_NeverCrossesSamples_WhileTheSeriesIsWrittenTo()
    {
        var series = new StreamingLineSeries(1_024);
        var broken = Race(
            i => series.AppendPoint(i, i * 2),
            () =>
            {
                var snapshot = series.CreateSnapshot();
                if (snapshot.XData.Length != snapshot.YData.Length)
                {
                    return $"lengths differ: x={snapshot.XData.Length}, y={snapshot.YData.Length}";
                }

                for (int i = 0; i < snapshot.XData.Length; i++)
                {
                    if (snapshot.YData[i] != snapshot.XData[i] * 2)
                    {
                        return $"point {i} crosses samples: x={snapshot.XData[i]}, y={snapshot.YData[i]}";
                    }
                }

                return null;
            });

        Assert.Null(broken);
    }

    [Fact]
    public void ABar_NeverCrossesTicks_WhileTheSeriesIsWrittenTo()
    {
        var series = new StreamingCandlestickSeries(1_024);
        var broken = Race(
            i => series.AppendBar(new OhlcBar(i, i + 1, i - 1, i)),
            () =>
            {
                var snapshot = series.CreateOhlcSnapshot();
                int length = snapshot.Open.Length;
                if (snapshot.High.Length != length || snapshot.Low.Length != length || snapshot.Close.Length != length)
                {
                    return $"lengths differ: o={length}, h={snapshot.High.Length}, l={snapshot.Low.Length}, c={snapshot.Close.Length}";
                }

                for (int i = 0; i < length; i++)
                {
                    double open = snapshot.Open[i];
                    if (snapshot.High[i] != open + 1 || snapshot.Low[i] != open - 1 || snapshot.Close[i] != open)
                    {
                        return $"bar {i} crosses ticks: o={open}, h={snapshot.High[i]}, l={snapshot.Low[i]}, c={snapshot.Close[i]}";
                    }
                }

                return null;
            });

        Assert.Null(broken);
    }

    /// <summary>A range is one question about one state. Reading a minimum and a maximum as two separate acts
    /// lets them come from either side of an append, so a window can report a maximum the minimum never saw.</summary>
    [Fact]
    public void ARange_IsOneReadOfOneState()
    {
        var series = new StreamingLineSeries(1_024);
        var broken = Race(
            i => series.AppendPoint(i, i * 2),
            () =>
            {
                var range = series.ComputeDataRange(null!);
                if (range.XMin is not { } xMin || range.XMax is not { } xMax ||
                    range.YMin is not { } yMin || range.YMax is not { } yMax)
                {
                    return null; // empty series: nothing to compare yet
                }

                // Every point satisfies y = 2x, so the y range must be exactly twice the x range — unless the
                // two were read from different states.
                return yMin == xMin * 2 && yMax == xMax * 2
                    ? null
                    : $"range crosses states: x=[{xMin}..{xMax}], y=[{yMin}..{yMax}]";
            });

        Assert.Null(broken);
    }

    // One writer, one reader, for a bounded number of appends. The reader reports the FIRST broken invariant it
    // sees; the writer stops when it is done and the reader stops with it.
    private static string? Race(Action<int> append, Func<string?> check)
    {
        string? broken = null;
        using var done = new ManualResetEventSlim(false);

        var reader = new Thread(() =>
        {
            while (!done.IsSet && broken is null)
            {
                broken = check();
            }
        })
        { IsBackground = true };

        reader.Start();
        for (int i = 1; i <= Appends && broken is null; i++)
        {
            append(i);
        }

        done.Set();
        reader.Join(TimeSpan.FromSeconds(10));
        return broken;
    }
}
