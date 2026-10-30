// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The streaming transfer entropy is the rolling transfer entropy taken one sample at a time: after each sample it says what
/// the batch says over everything so far. The theory holds its default row; these hold the variants (a stride, a condition) and
/// what the constructor refuses.</summary>
public sealed class StreamingTransferEntropyTests
{
    /// <summary>The source and the target of a bar: its body and its range, and for the conditioned form its volume in hundreds.</summary>
    private static (double Source, double Target, double Condition) At(OracleBars bars, int i) =>
        (bars.Close[i] - bars.Open[i], bars.High[i] - bars.Low[i], bars.Volume[i] / 100);

    private static StreamingRow RowOf(string name, TransferEntropyOptions options, bool conditioned) => new(
        name,
        () => new StreamingTransferEntropy(options, conditioned),
        bars =>
        {
            var source = new double[bars.Count];
            var target = new double[bars.Count];
            var condition = new double[bars.Count];
            for (int i = 0; i < bars.Count; i++) (source[i], target[i], condition[i]) = At(bars, i);
            var settings = conditioned ? options with { Condition = condition } : options;
            return [StreamingRow.Last(new TransferEntropy(source, target, settings).Compute())];
        },
        Bars: 110,
        MinCompared: 25,
        Feed: (form, bars, i) =>
        {
            var (source, target, condition) = At(bars, i);
            ((StreamingIndicator<TransferEntropySample>)form).Append(new TransferEntropySample(source, target, condition));
        });

    [Fact]
    public void WithAStride_ItSaysWhatTheBatchSays_NaNBetweenTheWindows() =>
        StreamingEquality.AssertAgrees(RowOf("TE stride", new TransferEntropyOptions(Neighbours: 3, Window: 20, Stride: 3), conditioned: false));

    [Fact]
    public void WithALag_ItSaysWhatTheBatchSays() =>
        StreamingEquality.AssertAgrees(RowOf("TE lag", new TransferEntropyOptions(Neighbours: 3, Lag: 2, Window: 20), conditioned: false));

    [Fact]
    public void Conditioned_ItSaysWhatTheBatchSays() =>
        StreamingEquality.AssertAgrees(RowOf("TE condition", new TransferEntropyOptions(Neighbours: 3, Window: 20), conditioned: true));

    [Fact]
    public void TheWindow_IsTheWidthAndTheWarmUp()
    {
        var form = new StreamingTransferEntropy(new TransferEntropyOptions(Window: 30));

        Assert.Equal(30, form.Width);
        Assert.Equal(30, form.WarmupPeriod);
        Assert.Equal("TE(k=4,lag=1,W=30)", form.Label);
    }

    [Fact]
    public void ABarCannotBeAppended_BecauseTheFormReadsTwoSeries()
    {
        var form = new StreamingTransferEntropy(new TransferEntropyOptions(Window: 30));

        Assert.Throws<NotSupportedException>(() => form.AppendBar(new MatPlotLibNet.Data.OhlcvBar(1, 2, 0, 1, 5)));
    }

    // ── What is refused, by name ──

    private static string? ParamOf(TransferEntropyOptions options) =>
        Assert.ThrowsAny<ArgumentException>(() => new StreamingTransferEntropy(options)).ParamName;

    [Fact]
    public void WithoutAWindow_ThereIsNothingToKeep() =>
        Assert.Equal("Window", ParamOf(new TransferEntropyOptions()));

    [Fact]
    public void ACondition_IsGivenInEachSample_NotInTheOptions() =>
        Assert.Equal("Condition", ParamOf(new TransferEntropyOptions(Window: 30, Condition: new double[30])));

    [Fact]
    public void ABinnedEstimator_IsRefusedByName() =>
        Assert.Equal("Window", ParamOf(new TransferEntropyOptions(TransferEntropyEstimator.Binned, Window: 30)));

    [Fact]
    public void AWindowTooShortForTheNeighbours_IsRefusedByName() =>
        Assert.Equal("Window", ParamOf(new TransferEntropyOptions(Neighbours: 4, Window: 5)));

    [Fact]
    public void AStrideBelowOne_IsRefusedByName() =>
        Assert.Equal("Stride", ParamOf(new TransferEntropyOptions(Window: 30, Stride: 0)));
}
