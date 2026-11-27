// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A setting a caller gave a series at the value the series rests at is still given: it is written to the JSON and reads
/// back given, so the drawing under a theme that would have chosen another value keeps what the caller said. A series keeps what
/// was given in an internal <c>Given…</c> slot beside the public setting; every such slot of every series under the drawn-property
/// contract is set to the resting value of its setting and followed through the document and back. A slot that collapses to
/// "never given" on the way is a defect, and fails here by name.</summary>
[Collection("ChartSerializerGlobalState")]
public class GivenSlotsAtRestContractTests
{
    private static readonly ChartSerializer Serializer = new();

    private readonly record struct Slot(Type Series, PropertyInfo Given, PropertyInfo Setting)
    {
        public string Name => $"{Series.Name}.{Setting.Name}";
    }

    private static readonly BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static IReadOnlyList<Slot> Slots { get; } =
    [
        .. from type in DrawnSeriesFixtures.DrawnTypes
           from given in type.GetProperties(Any)
           where given.Name.StartsWith("Given", StringComparison.Ordinal) && given.GetMethod is not null && given.GetIndexParameters().Length == 0
           let setting = type.GetProperty(given.Name["Given".Length..], BindingFlags.Instance | BindingFlags.Public)
           where setting is { CanWrite: true, SetMethod.IsPublic: true }
           select new Slot(type, given, setting),
    ];

    public static TheoryData<string> SlotNames { get; } = new(Slots.Select(slot => slot.Name));

    [Fact]
    public void TheSlotsOfTheSeries_AreFound()
    {
        Assert.True(Slots.Count >= 30, $"found {Slots.Count} slots");
    }

    [Theory]
    [MemberData(nameof(SlotNames))]
    public void ASettingGivenAtItsRestingValue_IsGiven_IsWritten_AndReadsBackGiven(string name)
    {
        var slot = Slots.Single(s => s.Name == name);
        var untouched = DrawnSeriesFixtures.Build(slot.Series.Name, everyPropertySet: false);
        var figure = DrawnSeriesFixtures.Build(slot.Series.Name, everyPropertySet: false);
        var series = DrawnSeriesFixtures.TheSeries(figure);
        Assert.Null(slot.Given.GetValue(series));
        var resting = slot.Setting.GetValue(series);

        slot.Setting.SetValue(series, resting);

        Assert.True(slot.Given.GetValue(series) is not null, $"{name} set to its resting value is not given");
        string given = Serializer.ToJson(figure);
        Assert.NotEqual(Serializer.ToJson(untouched), given);
        var readBack = DrawnSeriesFixtures.TheSeries(Serializer.FromJson(given));
        Assert.True(slot.Given.GetValue(readBack) is not null, $"{name} given at its resting value reads back not given");
    }

    /// <summary>The internal interfaces a kind states what it was given through (an opacity, a line width, a stroke): each is read the
    /// way the renderers read it, through the interface, so a kind that answers it from something other than its slot is seen too.</summary>
    private static readonly string[] SlotInterfaces = ["IGivenAlpha", "IWidthGiven", "IGivenStroked"];

    private readonly record struct InterfaceSlot(Type Series, Type Interface)
    {
        public string Name => $"{Series.Name}.{Interface.Name}";

        /// <summary>Whether <paramref name="series"/> says, through the interface, that a part of the slot was given.</summary>
        public bool IsGiven(object series) =>
            Interface.GetProperties()
                .Where(p => p.Name.StartsWith("Given", StringComparison.Ordinal) || p.Name == "Stroke")
                .Any(p => p.GetValue(series) is { } value && GivenPartOf(value));

        private static bool GivenPartOf(object value) =>
            value.GetType().IsPrimitive || value is Enum || value.GetType().GetProperties().Any(p => p.GetValue(value) is not null);
    }

    private static IReadOnlyList<InterfaceSlot> InterfaceSlots { get; } =
    [
        .. (from type in DrawnSeriesFixtures.DrawnTypes
           from name in SlotInterfaces
           let face = type.GetInterface(name)
           where face is not null
           from slotFace in new[] { face }.Concat(face.GetInterfaces().Where(i => SlotInterfaces.Contains(i.Name)))
           select new InterfaceSlot(type, slotFace)).Distinct(),
    ];

    public static TheoryData<string> AllSlotNames { get; } =
        new(Slots.Select(slot => slot.Name).Concat(InterfaceSlots.Select(slot => slot.Name)).Distinct());

    [Fact]
    public void TheWidthAndStrokeSlotsOfTheSeries_AreStatedThroughTheirInterfaces()
    {
        Assert.True(InterfaceSlots.Count(s => s.Interface.Name == "IWidthGiven") >= 18,
            $"found {InterfaceSlots.Count(s => s.Interface.Name == "IWidthGiven")} kinds that state a given line width through IWidthGiven");
        Assert.True(InterfaceSlots.Count(s => s.Interface.Name == "IGivenStroked") >= 12,
            $"found {InterfaceSlots.Count(s => s.Interface.Name == "IGivenStroked")} kinds that state a given stroke through IGivenStroked");
    }

    /// <summary>A slot nobody touched is written nowhere and reads back untouched: the JSON never names a resting value, and the reader never
    /// sets a setting the document does not name. The other half of the at-rest rule above.</summary>
    [Theory]
    [MemberData(nameof(AllSlotNames))]
    public void AGivenSlotLeftAlone_StaysUngiven_AfterJson(string name)
    {
        var figure = DrawnSeriesFixtures.Build(name.Split('.')[0], everyPropertySet: false);
        var series = DrawnSeriesFixtures.TheSeries(figure);
        Assert.False(IsGiven(name, series), $"{name} is given on a series nobody set it on");

        var readBack = DrawnSeriesFixtures.TheSeries(figure.RoundTripped());

        Assert.False(IsGiven(name, readBack), $"{name} left alone reads back given");
    }

    private static bool IsGiven(string name, object series) =>
        Slots.FirstOrDefault(s => s.Name == name) is { Given: not null } slot
            ? slot.Given.GetValue(series) is not null
            : InterfaceSlots.Single(s => s.Name == name).IsGiven(series);
}
