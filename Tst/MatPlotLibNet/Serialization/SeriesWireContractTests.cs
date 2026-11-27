// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>How a series is spelled on the wire is written once per rule. The name of a kind (the <c>type</c> of the JSON) is a member
/// of the series class that the writer and the registry both read; an optional setting is read back as <c>is { } value</c>, a flag that
/// is false at rest is left out by <c>OrNullAtRest</c>, the name of an enum on the wire is <c>ToWireName</c>, and the colour scale of a
/// series (its map and its normaliser) is written and read as one. The rows of <c>OwnedRules.SerializationRules</c> hold the census of
/// each at zero; the facts below hold what a scan cannot see: the kinds of the registry are the kinds of the classes, and the bytes of
/// every enum name are the ones the files of earlier versions hold.</summary>
[Collection("ChartSerializerGlobalState")]
public class SeriesWireContractTests
{
    

    

    

    

    

    

    // ── The kinds ────────────────────────────────────────────────────────────

    private static string KindOf<TSeries>() where TSeries : ISeriesKind<TSeries> => TSeries.Kind;

    /// <summary>Every class of the library that is a kind of series, with the name it is written under.</summary>
    private static IReadOnlyList<(Type Series, string Kind)> KindsOfTheLibrary() =>
    [
        .. typeof(SeriesRegistry).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISeriesKind<>)))
            .Select(type => (type, (string)typeof(SeriesWireContractTests).GetMethod(nameof(KindOf), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(type).Invoke(null, null)!))
            .OrderBy(pair => pair.Item2, StringComparer.Ordinal),
    ];

    [Fact]
    public void EveryKind_IsTheKindOfExactlyOneClass()
    {
        var kinds = KindsOfTheLibrary();

        Assert.NotEmpty(kinds);
        Assert.Equal(kinds.Count, kinds.Select(k => k.Kind).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void TheRegistry_HoldsExactlyTheKindsOfTheClasses()
    {
        SeriesRegistry.ResetForTestsInternal();

        Assert.Equal(KindsOfTheLibrary().Select(k => k.Kind), SeriesRegistry.Discriminators);
    }

    [Fact]
    public void TheClassesWithAKind_AreTheClassesUnderTheDrawnPropertyContract()
    {
        var named = KindsOfTheLibrary().Select(k => k.Series.Name).Order(StringComparer.Ordinal);
        var drawn = DrawnSeriesFixtures.DrawnTypes.Select(t => t.Name).Order(StringComparer.Ordinal);

        Assert.Equal(drawn, named);
    }

    /// <summary>A series writes its own kind as its <c>type</c> and the registry reads that name back as the same class: the writer
    /// and the reader of a kind cannot drift apart, because the name is one member.</summary>
    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void ASeries_WritesItsKind_AndTheRegistryReadsItBackAsTheSameClass(string typeName)
    {
        var figure = DrawnSeriesFixtures.Build(typeName, everyPropertySet: true);
        var series = DrawnSeriesFixtures.TheSeries(figure);
        var kind = KindsOfTheLibrary().Single(k => k.Series == series.GetType()).Kind;

        var dto = series.ToSeriesDto();
        var read = SeriesRegistry.Create(kind, new Axes(), dto);

        Assert.Equal(kind, dto.Type);
        Assert.IsType(series.GetType(), read);
    }

    // ── The names of an enum ─────────────────────────────────────────────────

    /// <summary>The name each member of an enum written to a document is written under: the member's name in lower case, never a
    /// naming policy's (a <c>JsonNamingPolicy</c> would write <c>dash_dot</c> or <c>dashDot</c>, a reader of an earlier version would
    /// not know the name). A member added to one of these enums is added here with the name its first document writes.</summary>
    private static readonly Dictionary<(Type Enum, string Member), string> WireNames = new()
    {
        [(typeof(BarOrientation), "Vertical")] = "vertical",
        [(typeof(BarOrientation), "Horizontal")] = "horizontal",
        [(typeof(Orientation), "Horizontal")] = "horizontal",
        [(typeof(Orientation), "Vertical")] = "vertical",
        [(typeof(StepPosition), "Pre")] = "pre",
        [(typeof(StepPosition), "Post")] = "post",
        [(typeof(StepPosition), "Mid")] = "mid",
        [(typeof(ConnectionStyle), "Straight")] = "straight",
        [(typeof(ConnectionStyle), "Arc3")] = "arc3",
        [(typeof(ConnectionStyle), "Angle")] = "angle",
        [(typeof(ConnectionStyle), "Angle3")] = "angle3",
        [(typeof(BoxStyle), "None")] = "none",
        [(typeof(BoxStyle), "Square")] = "square",
        [(typeof(BoxStyle), "Round")] = "round",
        [(typeof(BoxStyle), "RoundTooth")] = "roundtooth",
        [(typeof(BoxStyle), "Sawtooth")] = "sawtooth",
        [(typeof(CoordinateSystem), "Cartesian")] = "cartesian",
        [(typeof(CoordinateSystem), "Polar")] = "polar",
        [(typeof(CoordinateSystem), "ThreeD")] = "threed",
        [(typeof(CoordinateSystem), "Ternary")] = "ternary",
        [(typeof(BreakStyle), "Zigzag")] = "zigzag",
        [(typeof(BreakStyle), "Straight")] = "straight",
        [(typeof(BreakStyle), "None")] = "none",
        [(typeof(AxisScale), "Linear")] = "linear",
        [(typeof(AxisScale), "Log")] = "log",
        [(typeof(AxisScale), "SymLog")] = "symlog",
        [(typeof(AxisScale), "Date")] = "date",
        [(typeof(AxisScale), "Logit")] = "logit",
        [(typeof(LineStyle), "Solid")] = "solid",
        [(typeof(LineStyle), "Dashed")] = "dashed",
        [(typeof(LineStyle), "Dotted")] = "dotted",
        [(typeof(LineStyle), "DashDot")] = "dashdot",
        [(typeof(LineStyle), "None")] = "none",
        [(typeof(SpinePosition), "Edge")] = "edge",
        [(typeof(SpinePosition), "Data")] = "data",
        [(typeof(SpinePosition), "Axes")] = "axes",
        [(typeof(SeriesLabelPlacement), "Legend")] = "legend",
        [(typeof(SeriesLabelPlacement), "LineEnd")] = "lineend",
        [(typeof(CycleCall), "Line")] = "line",
        [(typeof(CycleCall), "Scatter")] = "scatter",
        [(typeof(CycleCall), "FillBetween")] = "fillbetween",
        [(typeof(CycleCall), "Patch")] = "patch",
        [(typeof(CycleCall), "Violin")] = "violin",
        [(typeof(CycleCall), "Neither")] = "neither",
        [(typeof(BarMode), "Grouped")] = "grouped",
        [(typeof(BarMode), "Stacked")] = "stacked",
    };

    public static TheoryData<Type> WireEnums { get; } = new(WireNames.Keys.Select(key => key.Enum).Distinct());

    [Theory]
    [MemberData(nameof(WireEnums))]
    public void EveryMemberOfAnEnumWrittenInLowerCase_IsPinnedByName_AndWritesThatName(Type enumType)
    {
        var members = Enum.GetNames(enumType);
        var pinned = WireNames.Where(pair => pair.Key.Enum == enumType).Select(pair => pair.Key.Member).ToArray();

        Assert.Equal(members.Order(StringComparer.Ordinal), pinned.Order(StringComparer.Ordinal));
        foreach (var member in members)
        {
            var value = Enum.Parse(enumType, member);
            Assert.Equal(WireNames[(enumType, member)], (string)typeof(SeriesWireContractTests).GetMethod(nameof(WireNameOf), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(enumType).Invoke(null, [value])!);
        }
    }

    private static string WireNameOf<T>(T value) where T : struct, Enum => value.ToWireName();
}
