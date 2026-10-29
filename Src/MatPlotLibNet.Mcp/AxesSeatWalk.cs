// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Json.Nodes;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp;

/// <summary>One place in a subplot's spec that holds an axis or a series: what it holds, the JSON path that names it
/// (<c>$.subPlots[0].leftYAxes[1].series[0]</c>) and the node that stands there.</summary>
/// <typeparam name="TNode">The kind of node the walk reads: a <see cref="JsonObject"/> or a DTO record.</typeparam>
/// <param name="Kind">What the seat holds.</param>
/// <param name="Path">The seat's JSON path, indices included.</param>
/// <param name="Node">The axis or the series.</param>
internal readonly record struct SpecSeat<TNode>(SeatKind Kind, string Path, TNode Node) where TNode : class;

/// <summary>One entry of a seat list, with its position in that list.</summary>
/// <typeparam name="TNode">The kind of node the walk reads.</typeparam>
/// <param name="Index">The entry's position, counted over every entry, objects or not.</param>
/// <param name="Node">The entry, or <see langword="null"/> when the document wrote <c>null</c> there.</param>
internal readonly record struct SeatEntry<TNode>(int Index, TNode? Node) where TNode : class;

/// <summary>How one kind of node tree is read by the seat walk. The shape of the seats is the same for the JSON nodes the
/// reader normalises and for the DTO records it validates; only the reading of a member differs.</summary>
/// <typeparam name="TNode">The kind of node the walk reads.</typeparam>
internal interface ISeatNodes<TNode> where TNode : class
{
    /// <summary>The object the single-valued <paramref name="member"/> holds in <paramref name="owner"/>, or
    /// <see langword="null"/> when it holds none (absent, or a value of another shape, which the strict pass names).</summary>
    TNode? ObjectAt(TNode owner, AxesSeatShape.SeatMember member);

    /// <summary>The entries of the list <paramref name="member"/> holds in <paramref name="owner"/>: each object, and each
    /// <c>null</c>; a value of another shape is left out (the strict pass names it) but still takes its index.</summary>
    IEnumerable<SeatEntry<TNode>> EntriesAt(TNode owner, AxesSeatShape.SeatMember member);
}

/// <summary>The seat walk over the document as parsed: the nodes the reader normalises in place.</summary>
internal sealed class JsonSeatNodes : ISeatNodes<JsonObject>
{
    /// <summary>The one reader; it holds no state.</summary>
    internal static readonly JsonSeatNodes Instance = new();

    /// <inheritdoc />
    public JsonObject? ObjectAt(JsonObject owner, AxesSeatShape.SeatMember member) => owner[member.Name] as JsonObject;

    /// <inheritdoc />
    public IEnumerable<SeatEntry<JsonObject>> EntriesAt(JsonObject owner, AxesSeatShape.SeatMember member)
    {
        if (owner[member.Name] is not JsonArray items)
        {
            yield break;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is null)
            {
                yield return new(i, null);
            }
            else if (items[i] is JsonObject entry)
            {
                yield return new(i, entry);
            }
        }
    }
}

/// <summary>The seat walk over the records the strict pass produced: the DTOs the reader validates.</summary>
internal sealed class DtoSeatNodes : ISeatNodes<object>
{
    /// <summary>The one reader; it holds no state.</summary>
    internal static readonly DtoSeatNodes Instance = new();

    /// <inheritdoc />
    public object? ObjectAt(object owner, AxesSeatShape.SeatMember member) => member.Property.Get!(owner);

    /// <inheritdoc />
    public IEnumerable<SeatEntry<object>> EntriesAt(object owner, AxesSeatShape.SeatMember member)
    {
        if (member.Property.Get!(owner) is not IList items)
        {
            yield break;
        }

        for (int i = 0; i < items.Count; i++)
        {
            yield return new(i, items[i]);
        }
    }
}

/// <summary>The seats of a subplot, for the two passes of <see cref="ChartSpecReader"/>.</summary>
internal static class AxesSeatExtensions
{
    extension(JsonObject subplot)
    {
        /// <summary>Every axis and every series of the subplot: the primary pair, the top and right axes, and every Y
        /// axis slot of either side, the overflow lists included. The caller normalises each in place.</summary>
        /// <param name="path">Where the subplot stands, <c>$.subPlots[0]</c>.</param>
        /// <exception cref="ToolRefusalException">An entry of a seat list is <c>null</c>.</exception>
        internal IEnumerable<SpecSeat<JsonObject>> Seats(string path) =>
            AxesSeatShape.OfSubplot.Seats(subplot, path, JsonSeatNodes.Instance);
    }

    extension(AxesDto subplot)
    {
        /// <summary>Every series of the subplot, wherever it sits: the same seats <c>Seats</c> walks over the JSON nodes,
        /// read from the records the strict pass produced.</summary>
        /// <param name="path">Where the subplot stands, <c>$.subPlots[0]</c>.</param>
        internal IEnumerable<SpecSeat<SeriesDto>> SeriesSeats(string path) =>
            AxesSeatShape.OfSubplot.Seats<object>(subplot, path, DtoSeatNodes.Instance)
                .Where(seat => seat.Kind == SeatKind.Series)
                .Select(seat => new SpecSeat<SeriesDto>(seat.Kind, seat.Path, (SeriesDto)seat.Node));
    }
}
