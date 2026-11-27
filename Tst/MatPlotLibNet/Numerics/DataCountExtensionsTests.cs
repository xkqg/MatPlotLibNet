// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>How many items the data holds against what a parameter needs of it (a coherence needs two segments of samples, a dispersion
/// index two signals in a row, a turbulence index at most ten features): one check, one sentence, and the data's fault, so an
/// <see cref="ArgumentException"/> that is not an <see cref="ArgumentOutOfRangeException"/>, naming the parameter that asks.</summary>
public sealed class DataCountExtensionsTests
{
    private static readonly DataNeed Segment = new("The signal", "sample", "a segment of SegmentLength", "set a shorter SegmentLength");

    private static readonly DataNeed Signals = new("A row", "signal", "a dispersion index");

    [Fact]
    public void ThrowIfFewerThan_AcceptsTheCountNeededAndMore_AndRefusesFewer_InOneSentence()
    {
        Assert.Equal(4, 4.ThrowIfFewerThan(4, "options", Segment));
        Assert.Equal(9, 9.ThrowIfFewerThan(4, "options", Segment));

        var error = Assert.Throws<ArgumentException>(() => 3.ThrowIfFewerThan(4, "options", Segment));

        Assert.Equal("options", error.ParamName);
        Assert.Equal("The signal holds 3 samples; a segment of SegmentLength needs at least 4; set a shorter SegmentLength. (Parameter 'options')",
            error.Message);
    }

    /// <summary>One item is named in the singular, and a need with no remedy ends at the count.</summary>
    [Fact]
    public void OneItem_IsNamedInTheSingular_AndANeedWithoutARemedy_EndsAtTheCount() =>
        Assert.Equal("A row holds 1 signal; a dispersion index needs at least 2. (Parameter 'input')",
            Assert.Throws<ArgumentException>(() => 1.ThrowIfFewerThan(2, "input", Signals)).Message);

    [Fact]
    public void ThrowIfMoreThan_AcceptsTheMostAndFewer_AndRefusesMore()
    {
        var features = new DataNeed("A row", "feature", "a turbulence index", "use a shrinkage estimator for wider feature sets");
        Assert.Equal(10, 10.ThrowIfMoreThan(10, "features", features));
        Assert.Equal(0, 0.ThrowIfMoreThan(10, "features", features));

        var error = Assert.Throws<ArgumentException>(() => 11.ThrowIfMoreThan(10, "features", features));

        Assert.Equal("A row holds 11 features; a turbulence index takes at most 10; use a shrinkage estimator for wider feature sets. (Parameter 'features')",
            error.Message);
    }

    /// <summary>A need past the largest count (a correlation of int.MaxValue lags needs one sample more) is refused, not wrapped round.</summary>
    [Fact]
    public void ANeedPastTheLargestCount_IsRefused() =>
        Assert.Throws<ArgumentException>(() => int.MaxValue.ThrowIfFewerThan(int.MaxValue + 1L, "options", Segment));
}
