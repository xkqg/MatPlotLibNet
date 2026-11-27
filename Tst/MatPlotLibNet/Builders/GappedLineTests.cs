// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The one place a line made of several runs gets its separators: <see cref="GappedLine"/> writes a missing point (NaN)
/// between two runs and nowhere else, however often it is asked to part them.</summary>
public class GappedLineTests
{
    private const double Gap = double.NaN;

    [Fact]
    public void ALineWithNoPoint_IsEmpty()
    {
        var data = new GappedLine().ToData();

        Assert.Empty(data.X);
        Assert.Empty(data.Y);
    }

    [Fact]
    public void OneRun_IsItsPoints_AndHasNoSeparator()
    {
        var line = new GappedLine();
        line.Add(1, 10);
        line.Add(2, 20);

        var data = line.ToData();

        Assert.Equal([1.0, 2.0], data.X);
        Assert.Equal([10.0, 20.0], data.Y);
    }

    [Fact]
    public void TwoRuns_AreParted_ByOneSeparator()
    {
        var line = new GappedLine();
        line.Add(1, 10);
        line.Gap();
        line.Add(2, 20);
        line.Add(3, 30);

        var data = line.ToData();

        Assert.Equal([1.0, Gap, 2.0, 3.0], data.X);
        Assert.Equal([10.0, Gap, 20.0, 30.0], data.Y);
    }

    [Fact]
    public void AGapBeforeTheFirstPoint_WritesNothing()
    {
        var line = new GappedLine();
        line.Gap();
        line.Add(1, 10);

        Assert.Equal([1.0], line.ToData().X);
    }

    [Fact]
    public void AGapAskedForTwice_IsOneSeparator()
    {
        var line = new GappedLine();
        line.Add(1, 10);
        line.Gap();
        line.Gap();
        line.Add(2, 20);

        Assert.Equal([1.0, Gap, 2.0], line.ToData().X);
    }

    [Fact]
    public void AGapAfterTheLastPoint_WritesNothing()
    {
        var line = new GappedLine();
        line.Add(1, 10);
        line.Gap();

        var data = line.ToData();

        Assert.Equal([1.0], data.X);
        Assert.Equal([10.0], data.Y);
    }

    [Fact]
    public void ARunAddedAtOnce_IsTheSameAsItsPointsAddedOneByOne()
    {
        var line = new GappedLine();
        line.AddRange([1, 2], [10, 20]);
        line.Gap();
        line.AddRange([3], [30]);

        var data = line.ToData();

        Assert.Equal([1.0, 2.0, Gap, 3.0], data.X);
        Assert.Equal([10.0, 20.0, Gap, 30.0], data.Y);
    }

    /// <summary>Segments by index, the ones in <c>leftOut</c> refused.</summary>
    private readonly struct Segments(double[] starts, HashSet<int>? leftOut = null) : ISegmentSource
    {
        public bool TryGet(int index, out double x0, out double y0, out double x1, out double y1)
        {
            x0 = starts[index];
            y0 = starts[index] * 10;
            x1 = starts[index] + 1;
            y1 = starts[index] * 10 + 1;
            return leftOut is null || !leftOut.Contains(index);
        }
    }

    [Fact]
    public void Segments_AreTwoPointsEach_PartedByOneSeparator_InArraysOfTheExactLength()
    {
        var data = GappedLine.Segments(3, new Segments([1, 3, 5]));

        Assert.Equal([1.0, 2.0, Gap, 3.0, 4.0, Gap, 5.0, 6.0], data.X);
        Assert.Equal([10.0, 11.0, Gap, 30.0, 31.0, Gap, 50.0, 51.0], data.Y);
    }

    [Fact]
    public void ASegmentTheSourceRefuses_LeavesNoPointAndNoSeparator()
    {
        var data = GappedLine.Segments(4, new Segments([1, 3, 5, 7], [0, 2]));

        Assert.Equal([3.0, 4.0, Gap, 7.0, 8.0], data.X);
        Assert.Equal([30.0, 31.0, Gap, 70.0, 71.0], data.Y);
    }

    [Fact]
    public void SegmentsOfASourceHoldingNone_AreAnEmptyLine()
    {
        var data = GappedLine.Segments(0, new Segments([]));

        Assert.Empty(data.X);
        Assert.Empty(data.Y);
    }

    [Fact]
    public void ARunWhoseArraysDifferInLength_IsRefused_AndAnEmptyRunAddsNothing()
    {
        var line = new GappedLine();
        line.Add(1, 10);
        line.Gap();

        Assert.Throws<ArgumentException>(() => line.AddRange([1, 2], [1]));
        line.AddRange([], []);
        line.AddRange([2], [20]);

        // The empty run left the gap pending, so the next run is still parted from the first.
        Assert.Equal([1.0, Gap, 2.0], line.ToData().X);
    }

    [Fact]
    public void AMissingPointTheCallerAdds_IsData_NotASeparator()
    {
        var line = new GappedLine();
        line.Add(1, 10);
        line.Add(Gap, Gap);
        line.Gap();
        line.Add(2, 20);

        // The caller's own missing point stays; the line's separator comes after it, since a separator is written only when a run begins.
        Assert.Equal([1.0, Gap, Gap, 2.0], line.ToData().X);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(1000)]
    public void ALineHoldsMorePointsThanTheCapacityItWasGiven_AtAnyCapacity(int capacity)
    {
        var line = new GappedLine(capacity);
        for (int run = 0; run < 40; run++)
        {
            line.Gap();
            line.Add(run, run);
            line.Add(run + 0.5, run);
        }

        var data = line.ToData();

        Assert.Equal(40 * 2 + 39, data.X.Length);
        Assert.Equal(data.X.Length, data.Y.Length);
        Assert.Equal(39.5, data.X[^1]);
    }

    [Fact]
    public void AnExactCapacity_HandsBackTheArraysWithoutCopyingThem()
    {
        var line = new GappedLine(3);
        line.Add(1, 10);
        line.Gap();
        line.Add(2, 20);

        var first = line.ToData();
        var second = line.ToData();

        Assert.Equal(3, first.X.Length);
        Assert.Same(first.X, second.X);
    }

    [Fact]
    public void TheCountIsThePointsWritten_SeparatorsIncluded()
    {
        var line = new GappedLine();
        line.Add(1, 10);
        line.Gap();
        Assert.Equal(1, line.Count);

        line.Add(2, 20);

        Assert.Equal(3, line.Count);
    }
}
