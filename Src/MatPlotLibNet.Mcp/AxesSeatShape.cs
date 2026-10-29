// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp;

/// <summary>What a seat of a subplot holds.</summary>
internal enum SeatKind
{
    /// <summary>An axis: <c>xAxis</c>, <c>secondaryYAxis</c>, the <c>axis</c> of a Y axis slot.</summary>
    Axis,

    /// <summary>A series: an entry of <c>series</c>, <c>secondarySeries</c>, the <c>series</c> of a Y axis slot.</summary>
    Series,
}

/// <summary>Where one seat stands inside a subplot, without an index: <c>xAxis</c>, <c>series[]</c>,
/// <c>leftYAxes[].axis</c>.</summary>
/// <param name="Kind">What the seat holds.</param>
/// <param name="Path">The seat's field path, a list written as <c>name[]</c>.</param>
internal readonly record struct SeatName(SeatKind Kind, string Path);

/// <summary>Where a subplot's spec keeps its axes and its series, found once from the very records the serializer
/// reads: every member of <see cref="AxesDto"/> that holds an axis, a list of series or a list of Y axis slots (a slot
/// holds an axis and a list of series of its own) is a seat. A seat added to the wire format, an overflow list included,
/// is therefore a seat of the reader's checks on the next build, without a loop written for it: both the normalising pass
/// (over the JSON nodes) and the validating pass (over the DTO records) walk this one shape.</summary>
/// <remarks>The names are the serializer's own wire names (its naming policy and attributes, read from its options), so
/// there is no second copy of a field name here. An inset subplot is not a seat but a subplot of its own: the reader
/// walks it with this shape in turn.</remarks>
internal sealed class AxesSeatShape
{
    /// <summary>A copy of the serializer's read options that carries the default resolver: the serializer's own instance
    /// takes its resolver only when it first reads a document, and asking it for a record's members before that fails.
    /// The copy keeps its naming policy, so a member's name is the one the serializer reads it by.</summary>
    private static readonly JsonSerializerOptions Wire = new(ChartSerializer.ReadOptions) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

    /// <summary>The shape of a subplot, the <see cref="AxesDto"/> a figure's <c>subPlots</c> entry is.</summary>
    internal static readonly AxesSeatShape OfSubplot = Of(typeof(AxesDto));

    private readonly IReadOnlyList<SeatMember> _members;

    private AxesSeatShape(IReadOnlyList<SeatMember> members) => _members = members;

    /// <summary>The seat-bearing members of <paramref name="dto"/>, in the order the record declares them.</summary>
    private static AxesSeatShape Of(Type dto) =>
        new([.. Wire.GetTypeInfo(dto).Properties.Select(SeatMember.Of).OfType<SeatMember>()]);

    /// <summary>Every seat of this shape, by name and without indices.</summary>
    internal IReadOnlyList<SeatName> Names => [.. _members.SelectMany(member => member.Names())];

    /// <summary>The members that hold a list of Y axis slots, by wire name.</summary>
    internal IReadOnlyList<string> SlotLists => [.. _members.OfType<SlotListMember>().Select(member => member.Name)];

    /// <summary>Every seat that stands in <paramref name="owner"/>, in the order the shape lists them; a list is walked
    /// in order and each entry names its index.</summary>
    /// <param name="owner">The subplot (or Y axis slot) node.</param>
    /// <param name="path">Where <paramref name="owner"/> stands.</param>
    /// <param name="nodes">How this kind of node tree is read.</param>
    /// <exception cref="ToolRefusalException">An entry of a seat list is <c>null</c>.</exception>
    internal IEnumerable<SpecSeat<TNode>> Seats<TNode>(TNode owner, string path, ISeatNodes<TNode> nodes) where TNode : class =>
        _members.SelectMany(member => member.Walk(owner, path, nodes));

    /// <summary>One member of a record that holds seats; the kinds differ in what a seat is and how many a member holds.</summary>
    internal abstract class SeatMember(JsonPropertyInfo property)
    {
        /// <summary>The member, as the serializer reads it.</summary>
        internal JsonPropertyInfo Property { get; } = property;

        /// <summary>The member's wire name.</summary>
        internal string Name => Property.Name;

        /// <summary>The seat-bearing member the serializer's <paramref name="property"/> is, or <see langword="null"/> when
        /// it holds no axis and no series.</summary>
        internal static SeatMember? Of(JsonPropertyInfo property) => property.PropertyType switch
        {
            var type when type == typeof(AxisDto) => new AxisMember(property),
            var type when type == typeof(List<SeriesDto>) => new SeriesListMember(property),
            var type when type == typeof(List<SecondaryAxisSlotDto>) => new SlotListMember(property, AxesSeatShape.Of(typeof(SecondaryAxisSlotDto))),
            _ => null,
        };

        /// <summary>The seats this member holds in <paramref name="owner"/>.</summary>
        internal abstract IEnumerable<SpecSeat<TNode>> Walk<TNode>(TNode owner, string path, ISeatNodes<TNode> nodes) where TNode : class;

        /// <summary>The seats this member holds, by name.</summary>
        internal abstract IEnumerable<SeatName> Names();

        /// <summary>The entries of the list this member holds in <paramref name="owner"/>, each an object.</summary>
        /// <exception cref="ToolRefusalException">An entry is <c>null</c>: the strict structural pass accepts it, and the
        /// validators and the serializer would fail on it without saying where.</exception>
        protected IEnumerable<SeatEntry<TNode>> ObjectsIn<TNode>(TNode owner, string path, ISeatNodes<TNode> nodes) where TNode : class
        {
            foreach (var entry in nodes.EntriesAt(owner, this))
            {
                if (entry.Node is null)
                {
                    throw new ToolRefusalException($"The entry at {path}.{Name}[{entry.Index}] is null; it must be an object.");
                }

                yield return entry;
            }
        }
    }

    private sealed class AxisMember(JsonPropertyInfo property) : SeatMember(property)
    {
        internal override IEnumerable<SpecSeat<TNode>> Walk<TNode>(TNode owner, string path, ISeatNodes<TNode> nodes)
        {
            if (nodes.ObjectAt(owner, this) is { } axis)
            {
                yield return new(SeatKind.Axis, $"{path}.{Name}", axis);
            }
        }

        internal override IEnumerable<SeatName> Names() => [new(SeatKind.Axis, Name)];
    }

    private sealed class SeriesListMember(JsonPropertyInfo property) : SeatMember(property)
    {
        internal override IEnumerable<SpecSeat<TNode>> Walk<TNode>(TNode owner, string path, ISeatNodes<TNode> nodes) =>
            ObjectsIn(owner, path, nodes).Select(entry => new SpecSeat<TNode>(SeatKind.Series, $"{path}.{Name}[{entry.Index}]", entry.Node!));

        internal override IEnumerable<SeatName> Names() => [new(SeatKind.Series, $"{Name}[]")];
    }

    /// <summary>A list of Y axis slots: each entry is an object that holds seats of its own.</summary>
    private sealed class SlotListMember(JsonPropertyInfo property, AxesSeatShape slot) : SeatMember(property)
    {
        internal override IEnumerable<SpecSeat<TNode>> Walk<TNode>(TNode owner, string path, ISeatNodes<TNode> nodes) =>
            ObjectsIn(owner, path, nodes).SelectMany(entry => slot.Seats(entry.Node!, $"{path}.{Name}[{entry.Index}]", nodes));

        internal override IEnumerable<SeatName> Names() =>
            slot.Names.Select(inner => inner with { Path = $"{Name}[].{inner.Path}" });
    }
}
