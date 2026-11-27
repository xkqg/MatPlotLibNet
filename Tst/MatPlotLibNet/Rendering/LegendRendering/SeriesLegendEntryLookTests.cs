// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>How a series row of a legend is told apart from another (<c>SeriesLegendEntry.LooksLike</c>, which a figure legend leaves a
/// row out by under the library's themes): its label, its handle, its colour slot and colour, and for an edged handle the edge the series
/// gives its patches, which the handle reads from the series and does not hold.</summary>
public class SeriesLegendEntryLookTests
{
    private static SeriesLegendEntry Row(ISeries series, LegendSwatch swatch, Color? partColor = null) =>
        new(series, 0, 0, "row", swatch, partColor);

    private static BarSeries Bar(Color? edge) => new(["a"], [1.0]) { Color = Colors.Blue, EdgeColor = edge };

    public static TheoryData<Color, Color, bool, bool> Edges => new()
    {
        { Colors.Red, Colors.Red, true, true },
        { Colors.Red, Colors.Green, true, false },
        { Colors.Red, Colors.Green, false, true },
    };

    /// <summary>Two bars look the same under an edged handle only when they were given the same edge; a plain handle is told apart by what
    /// it draws, and a bar's given edge does not reach a plain rectangle.</summary>
    [Theory]
    [MemberData(nameof(Edges))]
    public void TwoBars_LookTheSame_UnlessTheirHandleDrawsTheirOtherEdges(Color first, Color second, bool edgedHandle, bool same)
    {
        var swatch = edgedHandle ? LegendSwatch.EdgedPatch : LegendSwatch.Patch;

        Assert.Equal(same, Row(Bar(first), swatch).LooksLike(Row(Bar(second), swatch), Theme.Default));
    }

    /// <summary>An edged handle whose series is no patch has no edge to read: it looks as one with no edge.</summary>
    [Fact]
    public void AnEdgedHandleOfASeriesThatIsNoPatch_HasNoEdge()
    {
        var line = new LineSeries([1.0], [1.0]) { Color = Colors.Blue };

        Assert.True(Row(line, LegendSwatch.EdgedPatch).LooksLike(Row(new LineSeries([2.0], [2.0]) { Color = Colors.Blue }, LegendSwatch.EdgedPatch), Theme.Default));
    }

    /// <summary>A row's handle wears the slot of its COLOUR slot index and, in it, the colour of the part it stands for when its caller
    /// named one, else the colour the slot gives the series: the one rule the drawn handle and the row's look both read.</summary>
    [Fact]
    public void AHandle_WearsThePartsColour_ElseTheColourItsSlotGivesTheSeries()
    {
        var line = new LineSeries([1.0], [1.0]);
        var entry = new SeriesLegendEntry(line, 0, 3, "row", LegendSwatch.Patch);
        var slot = entry.SlotUnder(Theme.Default);

        Assert.Equal(Theme.Default.SeriesSlot(3), slot);
        Assert.Equal(slot.ColorOf(line), entry.HandleColorIn(slot));
        Assert.Equal(Colors.Red, (entry with { PartColor = Colors.Red }).HandleColorIn(slot));
    }

    /// <summary>A row standing for a part is told apart by the colour of the part.</summary>
    [Fact]
    public void ARowForAPart_IsToldApartByThePartsColour() =>
        Assert.False(Row(Bar(null), LegendSwatch.EdgedPatch, Colors.Red).LooksLike(Row(Bar(null), LegendSwatch.EdgedPatch, Colors.Green), Theme.Default));

    /// <summary>A row looks like another only with the same label, the same handle and the same colour slot; a patch row never.</summary>
    [Fact]
    public void ARow_IsToldApartByItsLabel_ItsHandle_ItsSlot_AndItsKind()
    {
        var line = new LineSeries([1.0], [1.0]) { Color = Colors.Blue };
        var row = Row(line, LegendSwatch.Patch);

        Assert.False(row.LooksLike(row with { Label = "other" }, Theme.Default));
        Assert.False(row.LooksLike(row with { Swatch = LegendSwatch.EdgedPatch }, Theme.Default));
        Assert.False(row.LooksLike(new SeriesLegendEntry(new LineSeries([1.0], [1.0]), 0, 1, "row", LegendSwatch.Patch), Theme.Default));
        Assert.False(row.LooksLike(new PatchLegendEntry(new MatPlotLibNet.Models.Patches.Rectangle(new(0, 0), 1, 1) { Label = "row" }, "row", 0), Theme.Default));
    }
}
