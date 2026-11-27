// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Tests.Models.Series.Streaming;

/// <summary>The one place a streaming series keeps its samples and counts their changes: an append, a batch and a clear each store
/// and count once, and the change is marked for the figure's redraw.</summary>
public sealed class StreamRingTests
{
    [Fact]
    public void ANewRing_HoldsNothing_AtVersionZero()
    {
        var ring = new StreamRing<int>(3);

        Assert.Equal(0, ring.Count);
        Assert.Equal(3, ring.Capacity);
        Assert.Equal(0, ring.Version);
        Assert.Empty(ring.Read().Samples);
    }

    [Fact]
    public void ACapacityBelowOne_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new StreamRing<int>(0));

    [Fact]
    public void AnAppend_StoresAndCountsOnce_AndEvictsTheOldestWhenFull()
    {
        var ring = new StreamRing<int>(2);

        ring.Append(1);
        ring.Append(2);
        ring.Append(3);

        Assert.Equal(3, ring.Version);
        Assert.Equal(2, ring.Count);
        Assert.Equal([2, 3], ring.Read().Samples);
        Assert.Equal(2, ring[0]);
    }

    [Fact]
    public void ABatch_CountsOnce()
    {
        var ring = new StreamRing<int>(4);

        ring.AppendRange([1, 2, 3]);

        Assert.Equal(1, ring.Version);
        Assert.Equal([1, 2, 3], ring.Read().Samples);
    }

    [Fact]
    public void AClear_EmptiesTheRing_AndCounts()
    {
        var ring = new StreamRing<int>(4);
        ring.Append(1);

        ring.Clear();

        Assert.Equal(0, ring.Count);
        Assert.Equal(2, ring.Version);
    }

    [Fact]
    public void ARead_CarriesTheVersionItWasTakenAt()
    {
        var ring = new StreamRing<int>(4);
        ring.AppendRange([5, 6]);
        ring.Append(7);

        var held = ring.Read();

        Assert.Equal(2, held.Version);
        Assert.Equal([5, 6, 7], held.Samples);
    }

    [Fact]
    public void AFold_WalksTheSamplesOldestFirst()
    {
        var ring = new StreamRing<int>(3);
        ring.AppendRange([1, 2, 3, 4]);

        string walked = ring.Aggregate("", static (s, x) => s + x);

        Assert.Equal("234", walked);
    }

    [Fact]
    public void EveryChange_MarksTheRingChanged_LaterThanTheMarkBefore()
    {
        var ring = new StreamRing<int>(3);
        long atRest = ring.LatestChange;

        ring.Append(1);
        long appended = ring.LatestChange;
        ring.AppendRange([2, 3]);
        long batched = ring.LatestChange;
        ring.Clear();
        long cleared = ring.LatestChange;

        Assert.Equal(0, atRest);
        Assert.True(appended > atRest);
        Assert.True(batched > appended);
        Assert.True(cleared > batched);
    }
}
