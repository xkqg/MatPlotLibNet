// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>An indicator written outside the library keeps a window through the two public constructors of
/// <see cref="WindowedStreamingIndicator{TInput}"/>: it names the width and its lines, warms up in as many inputs as the width, and
/// sees the last inputs oldest first, fewer while it warms up.</summary>
public sealed class WindowedStreamingIndicatorTests
{
    /// <summary>The sum of the last three prices, and on a second line how many it summed.</summary>
    private sealed class LastThree : WindowedStreamingIndicator<double>
    {
        public LastThree() : base(3, [new StreamingLineSeries(8) { Label = "Sum" }, new StreamingLineSeries(8) { Label = "Count" }]) { }

        protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs)
        {
            double sum = 0;
            foreach (double value in window) sum += value;
            outputs[0] = sum;
            outputs[1] = window.Length;
        }
    }

    /// <summary>The oldest of the last two prices.</summary>
    private sealed class OldestOfTwo() : WindowedStreamingIndicator<double>(2, 8)
    {
        protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs) => outputs[0] = window[0];
    }

    [Fact]
    public void AFormWithSeveralLines_SeesTheLastInputs_FewerWhileItWarmsUp()
    {
        var form = new LastThree();
        foreach (double price in new double[] { 1, 2, 3, 4 }) form.Append(price);

        Assert.Equal(3, form.Width);
        Assert.Equal(3, form.WarmupPeriod);
        Assert.Equal(["Sum", "Count"], form.OutputSeries.Select(s => s.Label));
        Assert.Equal([1.0, 3, 6, 9], form.OutputSeries[0].CreateSnapshot().YData);
        Assert.Equal([1.0, 2, 3, 3], form.OutputSeries[1].CreateSnapshot().YData);
    }

    [Fact]
    public void AFormWithOneLine_SeesItsWindowOldestFirst()
    {
        var form = new OldestOfTwo();
        foreach (double price in new double[] { 5, 6, 7 }) form.Append(price);

        Assert.Equal(2, form.WarmupPeriod);
        Assert.Equal([5.0, 5, 6], form.OutputSeries[0].CreateSnapshot().YData);
    }
}
