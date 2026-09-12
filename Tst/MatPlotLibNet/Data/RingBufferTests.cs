// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Tests.Data;

/// <summary>The ring holds a SEQUENCE of anything — a sample record, a state, a timestamp — not only doubles.
/// The index arithmetic is the same for every element type, so it is written once; what is NOT the same is
/// arithmetic on the values, which is why Min and Max live in extensions on the numeric instantiation instead
/// of inside a buffer that would then only work for numbers.</summary>
public sealed class RingBufferTests
{
    private readonly record struct Sample(DateTime At, double Rate);

    [Fact]
    public void Construction_SetsCapacity() => Assert.Equal(100, new RingBuffer<Sample>(100).Capacity);

    [Fact]
    public void Construction_ZeroCapacity_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<string>(0));

    [Fact]
    public void AReferenceType_IsHeldAndReadBackInOrder()
    {
        var buffer = new RingBuffer<string>(3);

        buffer.Append("a");
        buffer.Append("b");

        Assert.Equal(2, buffer.Count);
        Assert.Equal(["a", "b"], buffer.ToArray());
    }

    [Fact]
    public void AStruct_BeyondCapacity_WrapsAndEvictsTheOldest()
    {
        var buffer = new RingBuffer<Sample>(2);
        var t0 = new DateTime(2026, 9, 12, 8, 0, 0, DateTimeKind.Utc);

        buffer.Append(new Sample(t0, 1));
        buffer.Append(new Sample(t0.AddSeconds(1), 2));
        buffer.Append(new Sample(t0.AddSeconds(2), 3));

        Assert.Equal(2, buffer.Count);
        Assert.Equal([2.0, 3.0], buffer.ToArray().Select(s => s.Rate));
        Assert.Equal(t0.AddSeconds(1), buffer[0].At);
    }

    [Fact]
    public void AppendRange_LongerThanCapacity_KeepsTheNewest()
    {
        var buffer = new RingBuffer<int>(3);

        buffer.AppendRange([1, 2, 3, 4, 5]);

        Assert.Equal([3, 4, 5], buffer.ToArray());
    }

    [Fact]
    public void CopyTo_Wrapped_HandsBackLogicalOrder()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.AppendRange([1, 2, 3, 4]);
        var destination = new int[3];

        buffer.CopyTo(destination);

        Assert.Equal([2, 3, 4], destination);
    }

    [Fact]
    public void Clear_EmptiesIt_AndAppendStartsFresh()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.AppendRange([1, 2, 3]);

        buffer.Clear();
        buffer.Append(9);

        Assert.Equal([9], buffer.ToArray());
    }

    /// <summary>A reference the ring has evicted no longer answers for it. The slot is overwritten on append
    /// and blanked on <c>Clear</c>, so nothing the ring has forgotten is still reachable through it — asserted
    /// on what the ring SAYS rather than on when a collector runs, because a GC-timing test is a test that
    /// fails on a busy CI runner for reasons that have nothing to do with the code.</summary>
    [Fact]
    public void AnEvictedReference_IsGoneFromEveryAnswer()
    {
        var buffer = new RingBuffer<string>(2);
        buffer.AppendRange(["gone", "kept", "newest"]);

        Assert.Equal(["kept", "newest"], buffer.ToArray());
        Assert.Equal("kept", buffer[0]);
        Assert.DoesNotContain("gone", buffer.ToArray());
    }

    [Fact]
    public void Clear_LeavesNothingToRead()
    {
        var buffer = new RingBuffer<string>(3);
        buffer.AppendRange(["a", "b", "c"]);

        buffer.Clear();

        Assert.Equal(0, buffer.Count);
        Assert.Empty(buffer.ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[0]);
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var buffer = new RingBuffer<int>(3);
        buffer.Append(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[-1]);
    }

    [Fact]
    public void ToArray_Empty_IsEmpty() => Assert.Empty(new RingBuffer<int>(4).ToArray());

    [Fact]
    public void CopyTo_Empty_TouchesNothing()
    {
        var destination = new[] { 7, 7 };

        new RingBuffer<int>(2).CopyTo(destination);

        Assert.Equal([7, 7], destination);
    }
}

/// <summary>Arithmetic over a ring is arithmetic over its VALUES, so it sits on the numeric instantiation
/// rather than inside the buffer — the same rule that keeps this repo free of <c>*Helper</c> classes, and the
/// same rule Ait.Core's own ring states for itself.</summary>
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

    [Fact]
    public void Min_NullBuffer_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ((RingBuffer<int>)null!).Min());

    [Fact]
    public void Max_NullBuffer_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ((RingBuffer<int>)null!).Max());

    [Fact]
    public void MinOrNaN_NullBuffer_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ((RingBuffer<double>)null!).MinOrNaN());

    [Fact]
    public void MaxOrNaN_NullBuffer_Throws() =>
        Assert.Throws<ArgumentNullException>(() => ((RingBuffer<double>)null!).MaxOrNaN());
}
