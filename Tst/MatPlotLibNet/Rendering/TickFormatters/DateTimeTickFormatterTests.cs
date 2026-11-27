// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>v1.12.0 — Verifies <see cref="DateTimeTickFormatter"/> for both
/// index-based (<see cref="DateTimeTickFormatter.FromArray"/>) and
/// epoch-milliseconds (<see cref="DateTimeTickFormatter.FromEpochMs"/>) modes.</summary>
public class DateTimeTickFormatterTests
{
    private static readonly DateTime[] Timestamps =
    [
        new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
    ];

    // ── FromArray — happy path ────────────────────────────────────────────────

    [Theory]
    [InlineData(0, "2024-01-01")]
    [InlineData(2, "2024-12-31")]
    [InlineData(0.4, "2024-01-01")] // rounds to 0
    [InlineData(0.6, "2024-06-15")] // rounds to 1
    public void FromArray_Format_ReturnsTheFormattedDateOfTheNearestIndex(double index, string expected)
    {
        var f = DateTimeTickFormatter.FromArray(Timestamps);
        Assert.Equal(expected, f.Format(index));
    }

    [Fact]
    public void FromArray_Format_CustomFormat()
    {
        var f = DateTimeTickFormatter.FromArray(Timestamps, "MMM yyyy");
        Assert.Equal("Jan 2024", f.Format(0));
        Assert.Equal("Jun 2024", f.Format(1));
    }

    // ── FromArray — bounds ────────────────────────────────────────────────────

    public static TheoryData<int> IndicesOutsideTheTimestamps => new() { -1, Timestamps.Length };

    [Theory]
    [MemberData(nameof(IndicesOutsideTheTimestamps))]
    public void FromArray_Format_IndexOutsideTheTimestamps_ReturnsEmpty(int index)
    {
        var f = DateTimeTickFormatter.FromArray(Timestamps);
        Assert.Equal(string.Empty, f.Format(index));
    }

    // ── FromArray — rounding ──────────────────────────────────────────────────

    // ── FromArray — constructor validation ───────────────────────────────────

    [Fact]
    public void FromArray_NullTimestamps_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DateTimeTickFormatter.FromArray(null!));
    }

    [Fact]
    public void FromArray_EmptyArray_OutOfRange_ReturnsEmpty()
    {
        var f = DateTimeTickFormatter.FromArray([]);
        Assert.Equal(string.Empty, f.Format(0));
    }

    // ── FromEpochMs — happy path ──────────────────────────────────────────────

    [Fact]
    public void FromEpochMs_KnownEpoch_ReturnsCorrectDate()
    {
        var f    = DateTimeTickFormatter.FromEpochMs();
        long ms  = new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        Assert.Equal("2024-03-15", f.Format(ms));
    }

    [Fact]
    public void FromEpochMs_CustomFormat()
    {
        var f    = DateTimeTickFormatter.FromEpochMs("MMM yyyy");
        long ms  = new DateTimeOffset(2024, 7, 4, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        Assert.Equal("Jul 2024", f.Format(ms));
    }

    [Fact]
    public void FromEpochMs_Zero_Returns1970()
    {
        var f = DateTimeTickFormatter.FromEpochMs("yyyy-MM-dd");
        Assert.Equal("1970-01-01", f.Format(0));
    }

    // ── Interface contract ────────────────────────────────────────────────────

    [Fact]
    public void Implements_ITickFormatter_FromArray()
    {
        Assert.IsAssignableFrom<ITickFormatter>(DateTimeTickFormatter.FromArray(Timestamps));
    }

    [Fact]
    public void Implements_ITickFormatter_FromEpochMs()
    {
        Assert.IsAssignableFrom<ITickFormatter>(DateTimeTickFormatter.FromEpochMs());
    }
}
