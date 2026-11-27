// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Tests.Data;

/// <summary>Arithmetic over a ring is arithmetic over its VALUES, so it sits on the numeric instantiation
/// rather than inside the buffer — the same rule that keeps this repo free of <c>*Helper</c> classes, and the
/// same rule the ring states for itself.</summary>
public sealed class RingBufferExtensionsTests
{
    [Fact]
    public void MinAndMax_OverIntegers()
    {
        var buffer = new RingBuffer<int>(5);
        buffer.AppendRange([4, 1, 9, 3]);

        Assert.Equal(1, buffer.Min());
        Assert.Equal(9, buffer.Max());
    }

    [Fact]
    public void MinAndMax_AfterAWrap_ScanOnlyWhatIsStillHeld()
    {
        var buffer = new RingBuffer<double>(3);
        buffer.AppendRange([100.0, 2.0, 3.0, 4.0]);

        Assert.Equal(2.0, buffer.Min());
        Assert.Equal(4.0, buffer.Max());
    }

    [Fact]
    public void MinAndMax_Empty_AreNull()
    {
        var buffer = new RingBuffer<int>(3);

        Assert.Null(buffer.Min());
        Assert.Null(buffer.Max());
    }

    [Fact]
    public void MinAndMax_OfAnEmptyDoubleRing_AreNaN_NotNull()
    {
        // A double ring is what a chart axis reads, and NaN is the value an axis already knows to skip.
        var buffer = new RingBuffer<double>(3);

        Assert.Equal(double.NaN, buffer.MinOrNaN());
        Assert.Equal(double.NaN, buffer.MaxOrNaN());
    }

    [Fact]
    public void MinOrNaN_AndMaxOrNaN_CarryTheValuesWhenThereAreAny()
    {
        var buffer = new RingBuffer<double>(4);
        buffer.AppendRange([2.5, -1.0, 8.0]);

        Assert.Equal(-1.0, buffer.MinOrNaN());
        Assert.Equal(8.0, buffer.MaxOrNaN());
    }

    /// <summary>Each of the four reductions refuses a buffer that is null.</summary>
    public static TheoryData<Action> NullBuffers => new()
    {
        () => ((RingBuffer<int>)null!).Min(),
        () => ((RingBuffer<int>)null!).Max(),
        () => ((RingBuffer<double>)null!).MinOrNaN(),
        () => ((RingBuffer<double>)null!).MaxOrNaN(),
    };

    [Theory]
    [MemberData(nameof(NullBuffers))]
    public void AReduction_OfANullBuffer_Throws(Action reduce) => Assert.Throws<ArgumentNullException>(reduce);
}
