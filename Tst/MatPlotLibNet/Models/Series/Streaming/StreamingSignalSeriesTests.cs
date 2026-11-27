// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series.Streaming;

public sealed class StreamingSignalSeriesTests
{

    [Fact]
    public void Construction_CustomParameters()
    {
        var s = new StreamingSignalSeries(capacity: 1000, sampleRate: 44100.0, xStart: 5.0);
        Assert.Equal(1000, s.Capacity);
        Assert.Equal(44100.0, s.SampleRate);
        Assert.Equal(5.0, s.XStart);
    }

    [Fact]
    public void AppendSample_IncrementsCount()
    {
        var s = new StreamingSignalSeries();
        s.AppendSample(1.5);
        Assert.Equal(1, s.Count);
        Assert.Equal(1, s.Version);
    }

    [Fact]
    public void AppendSamples_Batch()
    {
        var s = new StreamingSignalSeries();
        s.AppendSamples([1.0, 2.0, 3.0]);
        Assert.Equal(3, s.Count);
    }

    [Fact]
    public void XAt_ComputesFromSampleRate()
    {
        var s = new StreamingSignalSeries(capacity: 100, sampleRate: 2.0, xStart: 10.0);
        s.AppendSamples([1.0, 2.0, 3.0]);
        Assert.Equal(10.0, s.XAt(0)); // 10 + 0/2
        Assert.Equal(10.5, s.XAt(1)); // 10 + 1/2
        Assert.Equal(11.0, s.XAt(2)); // 10 + 2/2
    }

    [Fact]
    public void XAt_AfterWrap_AccountsForTotalAppended()
    {
        var s = new StreamingSignalSeries(capacity: 3, sampleRate: 1.0, xStart: 0.0);
        s.AppendSamples([1.0, 2.0, 3.0, 4.0, 5.0]); // 5 appended, 3 in buffer
        Assert.Equal(2.0, s.XAt(0)); // first retained sample index = 2
        Assert.Equal(3.0, s.XAt(1));
        Assert.Equal(4.0, s.XAt(2));
    }

    [Fact]
    public void CreateSnapshot_ComputesXFromSampleRate()
    {
        var s = new StreamingSignalSeries(capacity: 100, sampleRate: 10.0, xStart: 0.0);
        s.AppendSamples([1.0, 2.0, 3.0]);
        var snap = s.CreateSnapshot();
        Assert.Equal([0.0, 0.1, 0.2], snap.XData);
        Assert.Equal([1.0, 2.0, 3.0], snap.YData);
    }

    [Fact]
    public void CreateSnapshot_AfterWrap_XStartsCorrectly()
    {
        var s = new StreamingSignalSeries(capacity: 2, sampleRate: 1.0, xStart: 0.0);
        s.AppendSamples([10.0, 20.0, 30.0]); // keeps [20, 30], indices 1 and 2
        var snap = s.CreateSnapshot();
        Assert.Equal([1.0, 2.0], snap.XData);
        Assert.Equal([20.0, 30.0], snap.YData);
    }

    [Fact]
    public void ComputeDataRange_UsesArithmeticX()
    {
        var s = new StreamingSignalSeries(capacity: 100, sampleRate: 1.0, xStart: 5.0);
        s.AppendSamples([10.0, 20.0, 30.0]);
        var range = s.ComputeDataRange(null!);
        Assert.Equal(5.0, range.XMin);
        Assert.Equal(7.0, range.XMax);
        Assert.Equal(10.0, range.YMin);
        Assert.Equal(30.0, range.YMax);
    }

    /// <summary>Once the ring has evicted, the X extent starts at the ordinal of the oldest sample still held, not at the first ever
    /// appended; and the range is asked of the ring by walking it, so asking copies nothing.</summary>
    [Fact]
    public void ComputeDataRange_AfterEviction_StartsAtTheOldestHeld_AndCopiesNothing()
    {
        var s = new StreamingSignalSeries(capacity: 2, sampleRate: 2.0, xStart: 1.0);
        s.AppendSamples([10.0, 40.0, 20.0]);
        _ = s.ComputeDataRange(null!);

        long before = GC.GetAllocatedBytesForCurrentThread();
        var range = s.ComputeDataRange(null!);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(1.5, range.XMin);
        Assert.Equal(2.0, range.XMax);
        Assert.Equal(20.0, range.YMin);
        Assert.Equal(40.0, range.YMax);
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void Clear_ResetsEverything()
    {
        var s = new StreamingSignalSeries();
        s.AppendSamples([1.0, 2.0]);
        s.Clear();
        Assert.Equal(0, s.Count);
    }

    [Fact]
    public void LargeCapacity_NoXArrayAllocation()
    {
        // StreamingSignalSeries doesn't store X data — only Y
        var s = new StreamingSignalSeries(capacity: 100_000);
        s.AppendSample(1.0);
        Assert.Equal(1, s.Count);
    }

    // ── the width of its line: given, else the theme's (a SignalSeries' rule), 1 px at rest under the library's own themes ──

    private static string DrawnWidth(Theme theme, Action<StreamingSignalSeries>? configure = null)
    {
        var svg = Plt.Create().WithTheme(theme).WithSize(400, 300).AddSubPlot(1, 1, 1, ax =>
        {
            var s = ax.StreamingSignal(capacity: 8);
            s.Color = Colors.Red;
            configure?.Invoke(s);
            s.AppendSamples([1.0, 3.0, 2.0]);
        }).ToSvg();
        return Assert.Single(svg.Xml().Descendants(), e => (string?)e.Attribute("stroke") == Colors.Red.ToHex())
            .Attribute("stroke-width")!.Value;
    }

    /// <summary>A stream given no width is drawn as a line given none (<c>plot</c>'s <c>Line2D</c>): the theme's <c>lines.linewidth</c>
    /// (matplotlib 3.11.2 prints <c>rcParams['lines.linewidth']</c> 1.5 under the default style and 1.0 under the classic one: 2.08 and
    /// 1.39 px), and the 1 px it has always rested at under the library's own themes.</summary>
    [Theory]
    [InlineData("MatplotlibV2", "2.08")]
    [InlineData("MatplotlibClassic", "1.39")]
    [InlineData("Default", "1")]
    public void AStreamGivenNoWidth_IsDrawnAtItsThemesWidth(string themeName, string expected)
    {
        var theme = themeName.AsTheme();

        Assert.Equal(expected, DrawnWidth(theme));
    }

    [Fact]
    public void AGivenWidth_WinsOverTheTheme()
    {
        Assert.Equal("3", DrawnWidth(Theme.MatplotlibV2, s => s.LineWidth = 3));
        Assert.Equal("1", DrawnWidth(Theme.MatplotlibV2, s => s.LineWidth = 1));
    }

    [Fact]
    public void TheWidth_ReadsItsRestingOne_UntilGiven_AndTheJsonWritesOnlyAGivenOne()
    {
        var resting = new StreamingSignalSeries(capacity: 4);
        var given = new StreamingSignalSeries(capacity: 4) { LineWidth = 1.0 };

        Assert.Equal(1.0, resting.LineWidth);
        Assert.Null(resting.ToSeriesDto().LineWidth);
        Assert.Equal(1.0, given.ToSeriesDto().LineWidth);
    }
}