// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The seat shape is read off the wire records, so a seat added to the wire format is a seat of the reader's
/// checks on the next build. These tests hold that claim from the outside: the seats the shape finds against a list kept
/// by hand, and every member of the records that holds an axis or a series against the shape.</summary>
public class AxesSeatShapeTests
{
    private static Type Dto(string name) =>
        typeof(SeriesDto).Assembly.GetType($"MatPlotLibNet.Serialization.{name}")
        ?? throw new InvalidOperationException($"The serializer no longer declares {name}.");

    [Fact]
    public void TheShape_NamesEverySeatOfASubplot()
    {
        var names = AxesSeatShape.OfSubplot.Names;

        Assert.Equal(
            new[]
            {
                "xAxis", "yAxis", "zAxis", "secondaryYAxis", "secondaryXAxis",
                "extraRightYAxes[].axis", "rightYAxesOverflow[].axis", "leftYAxes[].axis", "leftYAxesOverflow[].axis",
            }.Order(StringComparer.Ordinal),
            names.Where(name => name.Kind == SeatKind.Axis).Select(name => name.Path).Order(StringComparer.Ordinal));
        Assert.Equal(
            new[]
            {
                "series[]", "secondarySeries[]", "xSecondarySeries[]",
                "extraRightYAxes[].series[]", "rightYAxesOverflow[].series[]", "leftYAxes[].series[]", "leftYAxesOverflow[].series[]",
            }.Order(StringComparer.Ordinal),
            names.Where(name => name.Kind == SeatKind.Series).Select(name => name.Path).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TheListsOfYAxisSlots_AreTheFourTheWireCarries()
    {
        Assert.Equal(
            new[] { "extraRightYAxes", "rightYAxesOverflow", "leftYAxes", "leftYAxesOverflow" }.Order(StringComparer.Ordinal),
            AxesSeatShape.OfSubplot.SlotLists.Order(StringComparer.Ordinal));
    }

    /// <summary>The claim the whole step rests on: nothing on the wire holds an axis or a series that the shape does not
    /// walk. A member of the subplot record, or of a Y axis slot record, whose type names an axis record, a series record
    /// or a slot record (alone or in a list) must be a seat; an inset list holds subplots, which the reader walks in turn.</summary>
    [Theory]
    [InlineData("AxesDto")]
    [InlineData("SecondaryAxisSlotDto")]
    public void EveryMemberOfTheWireRecords_ThatHoldsAnAxisOrASeries_IsASeat(string record)
    {
        var holders = new[] { Dto("AxisDto"), typeof(SeriesDto), Dto("SecondaryAxisSlotDto") };
        var covered = AxesSeatShape.OfSubplot.Names
            .SelectMany(name => name.Path.Split("[]."))
            .Select(segment => segment.TrimEnd('[', ']'))
            .ToHashSet(StringComparer.Ordinal);

        var unseated = Dto(record)
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(property => Holds(property.PropertyType, holders))
            .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name))
            .Where(name => !covered.Contains(name))
            .ToList();

        Assert.Empty(unseated);
    }

    /// <summary>Whether <paramref name="type"/> is one of <paramref name="holders"/>, or a list or an array of one.</summary>
    private static bool Holds(Type type, Type[] holders)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (holders.Contains(type))
        {
            return true;
        }

        return type.IsArray
            ? Holds(type.GetElementType()!, holders)
            : type.IsGenericType && type.GetGenericArguments().Any(argument => Holds(argument, holders));
    }

    private const string EverySeat = """
        {
          "xAxis": {}, "yAxis": {}, "zAxis": {}, "secondaryYAxis": {}, "secondaryXAxis": {},
          "series": [{}, {}],
          "secondarySeries": [{}],
          "xSecondarySeries": [{}],
          "extraRightYAxes": [{"axis": {}, "series": [{}]}, {"axis": {}, "series": [{}, {}]}],
          "rightYAxesOverflow": [{"axis": {}, "series": [{}]}],
          "leftYAxes": [{"axis": {}, "series": [{}]}],
          "leftYAxesOverflow": [{"axis": {}}, {"series": [{}]}],
          "annotations": [{}], "insets": [{"series": [{}]}]
        }
        """;

    [Fact]
    public void TheSeatsOfASubplotWithEverySeat_AreEachOneWithItsIndexedPath()
    {
        var subplot = JsonNode.Parse(EverySeat)!.AsObject();

        var seats = subplot.Seats("$.subPlots[0]").Select(seat => $"{seat.Kind}:{seat.Path}").Order(StringComparer.Ordinal).ToList();

        string[] expected =
        [
            "Axis:$.subPlots[0].xAxis", "Axis:$.subPlots[0].yAxis", "Axis:$.subPlots[0].zAxis",
            "Axis:$.subPlots[0].secondaryYAxis", "Axis:$.subPlots[0].secondaryXAxis",
            "Axis:$.subPlots[0].extraRightYAxes[0].axis", "Axis:$.subPlots[0].extraRightYAxes[1].axis",
            "Axis:$.subPlots[0].rightYAxesOverflow[0].axis",
            "Axis:$.subPlots[0].leftYAxes[0].axis",
            "Axis:$.subPlots[0].leftYAxesOverflow[0].axis",
            "Series:$.subPlots[0].series[0]", "Series:$.subPlots[0].series[1]",
            "Series:$.subPlots[0].secondarySeries[0]",
            "Series:$.subPlots[0].xSecondarySeries[0]",
            "Series:$.subPlots[0].extraRightYAxes[0].series[0]",
            "Series:$.subPlots[0].extraRightYAxes[1].series[0]", "Series:$.subPlots[0].extraRightYAxes[1].series[1]",
            "Series:$.subPlots[0].rightYAxesOverflow[0].series[0]",
            "Series:$.subPlots[0].leftYAxes[0].series[0]",
            "Series:$.subPlots[0].leftYAxesOverflow[1].series[0]",
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), seats);
    }

    [Fact]
    public void AnInsetIsNotASeat_ItsOwnSeatsAreWalkedWithTheSameShapeInTurn()
    {
        var subplot = JsonNode.Parse(EverySeat)!.AsObject();
        var inset = subplot["insets"]!.AsArray()[0]!.AsObject();

        Assert.DoesNotContain(subplot.Seats("$"), seat => seat.Path.Contains("insets", StringComparison.Ordinal));
        Assert.Equal("$.insets[0].series[0]", Assert.Single(inset.Seats("$.insets[0]")).Path);
    }

    [Fact]
    public void AnEntryThatIsNotAnObject_IsLeftForTheStrictPass_ButTakesItsIndex()
    {
        var subplot = JsonNode.Parse("""{"series":[5,{"type":"line"},"x"]}""")!.AsObject();

        var seat = Assert.Single(subplot.Seats("$"));

        Assert.Equal("$.series[1]", seat.Path);
    }

    [Fact]
    public void AMemberOfAnotherShapeThanItsSeat_HoldsNoSeat()
    {
        var subplot = JsonNode.Parse("""{"xAxis":5,"series":"x","leftYAxes":{},"secondaryYAxis":null,"extraRightYAxes":[1]}""")!.AsObject();

        Assert.Empty(subplot.Seats("$"));
    }

    [Fact]
    public void ANullEntryOfASeatList_IsRefusedWithItsPath()
    {
        var subplot = JsonNode.Parse("""{"leftYAxes":[{"series":[{}, null]}]}""")!.AsObject();

        var refusal = Assert.Throws<ToolRefusalException>(() => subplot.Seats("$").ToList());

        Assert.Contains("$.leftYAxes[0].series[1]", refusal.Message);
    }
}
