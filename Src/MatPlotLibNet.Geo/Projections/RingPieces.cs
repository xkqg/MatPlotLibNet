// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Geo.Projections;

/// <summary>The pieces a ring or a line is drawn in: one for a ring that is not cut, which is every ring but those that cross the
/// horizon of a globe, so that the common case holds its one list and nothing more; none for a ring that is not shown at all.</summary>
internal readonly struct RingPieces
{
    private readonly List<Point>? _whole;
    private readonly List<List<Point>>? _pieces;

    /// <summary>The ring, uncut.</summary>
    /// <param name="whole">The points of the ring.</param>
    internal RingPieces(List<Point> whole)
    {
        _whole = whole;
        _pieces = null;
    }

    /// <summary>The pieces of a ring that was cut; none for a ring that is not shown.</summary>
    /// <param name="pieces">The points of each piece.</param>
    internal RingPieces(List<List<Point>> pieces)
    {
        _whole = null;
        _pieces = pieces;
    }

    /// <summary>How many pieces.</summary>
    internal int Count => _pieces?.Count ?? (_whole is null ? 0 : 1);

    /// <summary>The piece at <paramref name="index"/>.</summary>
    /// <param name="index">The place of the piece.</param>
    internal List<Point> this[int index] => _pieces is null ? _whole! : _pieces[index];
}
