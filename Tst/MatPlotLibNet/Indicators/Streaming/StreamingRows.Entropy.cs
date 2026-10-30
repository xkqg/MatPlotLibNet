// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The row of the equality theory for the streaming transfer entropy: a source and a target, the body and the range of each
/// bar, over a window of 24 bars with 3 neighbours.</summary>
public static partial class StreamingRows
{
    private static readonly TransferEntropyOptions EntropyOptions = new(Neighbours: 3, Window: 24);

    private static IEnumerable<StreamingRow> Entropy()
    {
        yield return new("TransferEntropy", () => new StreamingTransferEntropy(EntropyOptions),
            b =>
            {
                double[] source = [.. Enumerable.Range(0, b.Count).Select(i => b.Close[i] - b.Open[i])];
                double[] target = [.. Enumerable.Range(0, b.Count).Select(i => b.High[i] - b.Low[i])];
                return [StreamingRow.Last(new TransferEntropy(source, target, EntropyOptions).Compute())];
            },
            Bars: 110,
            Feed: (form, bars, i) =>
                ((StreamingIndicator<TransferEntropySample>)form).Append(new TransferEntropySample(bars.Close[i] - bars.Open[i], bars.High[i] - bars.Low[i])));
    }
}
