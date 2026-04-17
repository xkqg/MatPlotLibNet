// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Tests.Data;

public sealed class DoubleRingBufferTests
{
    [Fact]
    public void Construction_SetsCapacity()
    {
        var buf = new DoubleRingBuffer(100);
        Assert.Equal(100, buf.Capacity);
        Assert.Equal(0, buf.Count);
    }

    [Fact]
    public void Construction_ZeroCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DoubleRingBuffer(0));
    }

    [Fact]
    public void Append_IncrementsCount()
    {
        var buf = new DoubleRingBuffer(10);
        buf.Append(1.0);
        buf.Append(2.0);
        Assert.Equal(2, buf.Count);
    }

    [Fact]
    public void Append_BeyondCapacity_WrapsAndEvictsOldest()
    {
        var buf = new DoubleRingBuffer(3);
        buf.Append(1.0);
        buf.Append(2.0);
        buf.Append(3.0);
        buf.Append(4.0); // evicts 1.0

        Assert.Equal(3, buf.Count);
        var data = new double[3];
        buf.CopyTo(data);
        Assert.Equal([2.0, 3.0, 4.0], data);
    }

    [Fact]
    public void AppendRange_AddsAllValues()
    {
        var buf = new DoubleRingBuffer(10);
        buf.AppendRange([1.0, 2.0, 3.0, 4.0, 5.0]);
        Assert.Equal(5, buf.Count);
        var data = new double[5];
        buf.CopyTo(data);
        Assert.Equal([1.0, 2.0, 3.0, 4.0, 5.0], data);
    }

    [Fact]
    public void AppendRange_ExceedingCapacity_KeepsNewest()
    {
        var buf = new DoubleRingBuffer(3);
        buf.AppendRange([1.0, 2.0, 3.0, 4.0, 5.0]);
        Assert.Equal(3, buf.Count);
        var data = new double[3];
        buf.CopyTo(data);
        Assert.Equal([3.0, 4.0, 5.0], data);
    }

    [Fact]
    public void CopyTo_Unwrapped_ReturnsCorrectOrder()
    {
        var buf = new DoubleRingBuffer(5);
        buf.AppendRange([10.0, 20.0, 30.0]);
        var data = new double[3];
        buf.CopyTo(data);
        Assert.Equal([10.0, 20.0, 30.0], data);
    }

    [Fact]
    public void CopyTo_Wrapped_ReturnsCorrectOrder()
    {
        var buf = new DoubleRingBuffer(4);
        buf.AppendRange([1.0, 2.0, 3.0, 4.0]); // full
        buf.Append(5.0);                         // wraps: [5, 2, 3, 4] → logical [2, 3, 4, 5]
        buf.Append(6.0);                         // wraps: [5, 6, 3, 4] → logical [3, 4, 5, 6]

        var data = new double[4];
        buf.CopyTo(data);
        Assert.Equal([3.0, 4.0, 5.0, 6.0], data);
    }

    [Fact]
    public void CopyTo_Empty_ReturnsEmpty()
    {
        var buf = new DoubleRingBuffer(5);
        var data = new double[0];
        buf.CopyTo(data);
        Assert.Empty(data);
    }

    [Fact]
    public void Clear_ResetsCountToZero()
    {
        var buf = new DoubleRingBuffer(5);
        buf.AppendRange([1.0, 2.0, 3.0]);
        buf.Clear();
        Assert.Equal(0, buf.Count);
    }

    [Fact]
    public void Clear_AfterClear_AppendStartsFresh()
    {
        var buf = new DoubleRingBuffer(3);
        buf.AppendRange([1.0, 2.0, 3.0]);
        buf.Clear();
        buf.AppendRange([10.0, 20.0]);
        var data = new double[2];
        buf.CopyTo(data);
        Assert.Equal([10.0, 20.0], data);
    }

    [Fact]
    public void Min_ReturnsMinimumValue()
    {
        var buf = new DoubleRingBuffer(10);
        buf.AppendRange([5.0, 1.0, 3.0, 2.0, 4.0]);
        Assert.Equal(1.0, buf.Min);
    }

    [Fact]
    public void Max_ReturnsMaximumValue()
    {
        var buf = new DoubleRingBuffer(10);
        buf.AppendRange([5.0, 1.0, 3.0, 2.0, 4.0]);
        Assert.Equal(5.0, buf.Max);
    }

    [Fact]
    public void Min_Empty_ReturnsNaN()
    {
        var buf = new DoubleRingBuffer(5);
        Assert.True(double.IsNaN(buf.Min));
    }

    [Fact]
    public void Max_Empty_ReturnsNaN()
    {
        var buf = new DoubleRingBuffer(5);
        Assert.True(double.IsNaN(buf.Max));
    }

    [Fact]
    public void Min_AfterWrap_ScansAllValues()
    {
        var buf = new DoubleRingBuffer(3);
        buf.AppendRange([10.0, 20.0, 30.0]);
        buf.Append(5.0); // wraps, evicts 10 → [20, 30, 5]
        Assert.Equal(5.0, buf.Min);
    }

    [Fact]
    public void SingleElement_WrapBehavior()
    {
        var buf = new DoubleRingBuffer(1);
        buf.Append(1.0);
        Assert.Equal(1, buf.Count);
        buf.Append(2.0); // overwrites
        Assert.Equal(1, buf.Count);
        var data = new double[1];
        buf.CopyTo(data);
        Assert.Equal([2.0], data);
    }

    [Fact]
    public void ToArray_ReturnsCorrectCopy()
    {
        var buf = new DoubleRingBuffer(5);
        buf.AppendRange([1.0, 2.0, 3.0]);
        var arr = buf.ToArray();
        Assert.Equal([1.0, 2.0, 3.0], arr);
        Assert.Equal(3, arr.Length);
    }

    [Fact]
    public void ToArray_Empty_ReturnsEmptyArray()
    {
        var buf = new DoubleRingBuffer(5);
        Assert.Empty(buf.ToArray());
    }

    [Fact]
    public void ToArray_Wrapped_ReturnsCorrectOrder()
    {
        var buf = new DoubleRingBuffer(3);
        buf.AppendRange([1.0, 2.0, 3.0, 4.0, 5.0]);
        Assert.Equal([3.0, 4.0, 5.0], buf.ToArray());
    }

    [Fact]
    public void ConcurrentAppendAndSnapshot_DoesNotThrow()
    {
        var buf = new DoubleRingBuffer(1000);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var writer = Task.Run(() =>
        {
            double v = 0;
            while (!cts.Token.IsCancellationRequested)
                buf.Append(v++);
        });

        var reader = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var arr = buf.ToArray();
                // Verify order: each element should be <= the next
                for (int i = 1; i < arr.Length; i++)
                    Assert.True(arr[i] >= arr[i - 1], $"Out of order at index {i}: {arr[i - 1]} > {arr[i]}");
            }
        });

        Task.WhenAll(writer, reader).Wait();
    }

    [Fact]
    public void LargeCapacity_HandlesCorrectly()
    {
        var buf = new DoubleRingBuffer(100_000);
        for (int i = 0; i < 200_000; i++)
            buf.Append(i);
        Assert.Equal(100_000, buf.Count);
        Assert.Equal(100_000.0, buf.Min);
        Assert.Equal(199_999.0, buf.Max);
    }

    [Fact]
    public void Indexer_ReturnsLogicalOrder()
    {
        var buf = new DoubleRingBuffer(3);
        buf.AppendRange([1.0, 2.0, 3.0, 4.0]); // wraps: logical [2, 3, 4]
        Assert.Equal(2.0, buf[0]);
        Assert.Equal(3.0, buf[1]);
        Assert.Equal(4.0, buf[2]);
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var buf = new DoubleRingBuffer(5);
        buf.Append(1.0);
        Assert.Throws<ArgumentOutOfRangeException>(() => buf[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buf[-1]);
    }
}
