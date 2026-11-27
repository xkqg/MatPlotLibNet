// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The bytes of a figure whose series has every drawn property GIVEN, pinned in a checked-in file per series type (and per
/// secondary seat of the series types that sit on one). <see cref="ChartSerializerGoldenCorpusTests"/> pins a series at rest, which
/// writes none of its settings; this pins the other half of the wire, the key and the value of every setting a caller can give, in
/// the order the writer puts them. A refactor of how a series writes, reads or defaults a setting that moves one byte of that wire
/// fails here and names the series type, so a change that must keep the JSON identical is proved by this corpus passing without a
/// file regenerated.
/// <para>The values are the ones the drawn-property contract gives (<see cref="DrawnSeriesFixtures.Build"/> with every property set,
/// a different value for each). A series type that gains a settable property gains its key here, and the properties after it in
/// the alphabet take the next value of the generator: such a change regenerates that one type's file, and the commit says so.</para>
/// <para>The files live under <c>Serialization/Golden/GivenSeries</c>; <c>MPL_REGEN_GOLDEN=1</c> writes them, as it writes the
/// others.</para></summary>
[Collection("ChartSerializerGlobalState")]
public class GivenSeriesCorpusTests
{
    private static readonly ChartSerializer Serializer = new();

    private const string Folder = "GivenSeries";

    /// <summary>A figure with one series of a type at a seat, and the name of the file that pins its bytes.</summary>
    private readonly record struct Pin(string Type, SeriesSeat Seat)
    {
        public string Name => Seat == SeriesSeat.Primary ? Type : $"{Type}.{Seat}";
    }

    private static readonly Pin[] Pins =
    [
        .. DrawnSeriesFixtures.DrawnTypes.Select(type => new Pin(type.Name, SeriesSeat.Primary)),
        .. from type in DrawnSeriesFixtures.SecondaryTypes
           from seat in new[] { SeriesSeat.SecondaryY, SeriesSeat.SecondaryX }
           select new Pin(type.Name, seat),
    ];

    /// <summary>One row per series type under the contract, and one per secondary seat of the types that sit there.</summary>
    public static TheoryData<string> Rows { get; } = new(Pins.Select(pin => pin.Name));

    private static string JsonOf(Pin pin) => Serializer.ToJson(DrawnSeriesFixtures.Build(pin.Type, everyPropertySet: true, pin.Seat));

    [Theory]
    [MemberData(nameof(Rows))]
    public void AFigureWhoseSeriesHasEveryPropertyGiven_WritesTheBytesPinned(string name)
    {
        var pin = Pins.Single(p => p.Name == name);
        string json = JsonOf(pin);

        string pinned = BytePins.Pinned(Folder, name, json);

        Assert.Equal(pinned, json);
    }

    /// <summary>The bytes pinned, read back and written again, are the bytes pinned, byte for byte: nothing is added, dropped or
    /// reordered by a trip through the reader, a kind drawn in a coordinate system of its own included (<c>Axes.AddSeries</c> puts the
    /// axes in it, as its verb and the reader do).</summary>
    [Theory]
    [MemberData(nameof(Rows))]
    public void TheBytesPinned_ReadBackAndWrittenAgain_AreTheSameBytes(string name)
    {
        string pinned = BytePins.Pinned(Folder, name, JsonOf(Pins.Single(p => p.Name == name)));
        string once = Serializer.ToJson(Serializer.FromJson(pinned));

        Assert.Equal(pinned, once);
        Assert.Equal(once, Serializer.ToJson(Serializer.FromJson(once)));
    }

    [Fact]
    public void ThePinnedFiles_AreExactlyTheRows()
    {
        Assert.Equal(Pins.Select(pin => pin.Name).Order(StringComparer.Ordinal), BytePins.Names(Folder));
    }

    [Fact]
    public void AGivenSeries_WritesMoreThanTheSameSeriesAtRest()
    {
        // Liveness: the corpus pins settings, so a series with every property given must write keys the same series at rest does not.
        string atRest = Serializer.ToJson(DrawnSeriesFixtures.Build(nameof(BarSeries), everyPropertySet: false));
        string given = Serializer.ToJson(DrawnSeriesFixtures.Build(nameof(BarSeries), everyPropertySet: true));

        Assert.True(given.Length > atRest.Length * 2, $"at rest {atRest.Length} chars, given {given.Length} chars");
    }
}
