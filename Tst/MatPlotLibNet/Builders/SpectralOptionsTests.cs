// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Verifies what only <see cref="SpectralOptions"/> has of the options of a spectrum, the settings of a Welch estimate:
/// <c>default</c> holds matplotlib's defaults for <c>psd</c>, <c>csd</c> and <c>cohere</c> (<c>detrend_none</c>, <c>NFFT=256</c>,
/// <c>noverlap=0</c>, <c>scale_by_freq=True</c>) and each keeps what it is given. The settings it shares with
/// <see cref="SpectrumOptions"/>, and the refusal of every setting by its own name, are held by
/// <see cref="SpectralOptionExtensionsTests"/>.</summary>
public class SpectralOptionsTests
{
    [Fact]
    public void Default_IsMatplotlibsDefaults()
    {
        SpectralOptions options = default;

        Assert.Equal(SpectralDetrend.None, options.Detrend);
        Assert.Equal(256, options.SegmentLength);
        Assert.Equal(0, options.Overlap);
        Assert.Equal(SpectralScaling.Density, options.Scaling);
    }

    [Fact]
    public void EveryProperty_KeepsWhatItIsGiven()
    {
        var options = new SpectralOptions
        {
            Detrend = SpectralDetrend.Linear,
            SegmentLength = 64,
            Overlap = 32,
            Scaling = SpectralScaling.Spectrum,
        };

        Assert.Equal(SpectralDetrend.Linear, options.Detrend);
        Assert.Equal(64, options.SegmentLength);
        Assert.Equal(32, options.Overlap);
        Assert.Equal(SpectralScaling.Spectrum, options.Scaling);
    }
}
