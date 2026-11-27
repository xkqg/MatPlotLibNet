// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="SpectrogramSeries"/> rendering.</summary>
public class SpectrogramSeriesRenderTests
{
    // 512 samples at 1000 Hz — enough for STFT with default window 256
    private static readonly double[] Signal =
        Enumerable.Range(0, 512).Select(i => Math.Sin(2 * Math.PI * 50 * i / 1000.0)).ToArray();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Spectrogram_SvgContainsRectangles", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Spectrogram(Signal, 1000, s => { s.WindowSize = 64; s.Overlap = 32; }))
            .ToSvg(), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // Branch coverage — exercise both halves of the (nBins==0 || nFrames==0) guard.
    // Fft.Stft returns an empty (0,0) matrix when WindowSize<=0, hitting nBins==0 first.

}
