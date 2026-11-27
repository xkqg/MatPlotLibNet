// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using H3.Extensions;
using H3.Model;
using H3;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Numerics;
using System.Globalization;

namespace MatPlotLibNet.Geo;

public static partial class GeoAxesExtensions
{
    // The cells of Uber's H3 index as regions of a map, through a managed port of the H3 library: a cell is the polygon of
    // its boundary, in longitude and latitude, closed as GeoJSON closes a ring, cut where it crosses the antimeridian and closed along
    // a pole it goes round (<see cref="GeoClipping.SplitAtDateline(IReadOnlyList{GeoPosition})"/>). <c>H3Cells</c> and
    // <c>H3Bins</c> both draw by it.

    /// <summary>The coarsest resolution of the index: 122 cells over the globe.</summary>
    internal const int H3CoarsestResolution = 0;

    /// <summary>The finest resolution of the index: cells of under a square metre.</summary>
    internal const int H3FinestResolution = 15;

    /// <summary>The property a cell's region holds its index in, written as H3 writes one (lower-case hexadecimal).</summary>
    internal const string H3IndexProperty = "h3";

    /// <summary>The cells the indexes name, each with its value, in the order of the dictionary.</summary>
    /// <exception cref="ArgumentException">An index is not a cell of the H3 index; nothing is made.</exception>
    /// <param name="valueOf">The value it works on.</param>
    internal static H3CellMap AsH3Cells(IReadOnlyDictionary<string, double> valueOf)
    {
        var cells = new List<GeoFeature>(valueOf.Count);
        var values = new double[valueOf.Count];
        foreach (var (index, value) in valueOf)
        {
            values[cells.Count] = value;
            cells.Add(H3RegionOf(H3CellNamed(index)));
        }

        return new H3CellMap(cells, values);
    }

    /// <summary>The places counted into the cells of one resolution: each cell that holds a place, with the number of places
    /// it holds, in the order the cells are first met. A place with a coordinate that is not a finite number is left out.</summary>
    /// <param name="longitudes">The longitude of each place, in degrees.</param>
    /// <param name="resolution">The resolution of the cells, <see cref="H3CoarsestResolution"/> to <see cref="H3FinestResolution"/>.</param>
    /// <exception cref="ArgumentException">The arrays differ in length.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index has no such resolution.</exception>
    /// <param name="latitudes">The value it works on.</param>
    internal static H3CellMap BinnedIntoH3Cells(double[] latitudes, double[] longitudes, int resolution)
    {
        latitudes.Length.ThrowIfMisaligned(longitudes.Length);
        resolution.ThrowIfNotIn(H3CoarsestResolution, H3FinestResolution, nameof(resolution));

        var slotOf = new Dictionary<H3Index, int>();
        var counts = new List<double>();
        for (int i = 0; i < latitudes.Length; i++)
        {
            if (!double.IsFinite(latitudes[i]) || !double.IsFinite(longitudes[i])) continue;
            var cell = H3Index.FromLatLng(new LatLng(latitudes[i].ToRadians(), longitudes[i].ToRadians()), resolution);
            if (slotOf.TryGetValue(cell, out int slot))
            {
                counts[slot]++;
            }
            else
            {
                slotOf[cell] = counts.Count;
                counts.Add(1);
            }
        }

        return new H3CellMap([.. slotOf.Keys.Select(H3RegionOf)], [.. counts]);
    }

    /// <summary>The cell <paramref name="index"/> names, read as hexadecimal in either case.</summary>
    /// <exception cref="ArgumentException">The text is not the index of a cell.</exception>
    private static H3Index H3CellNamed(string index)
    {
        if (index.TryReadInvariant(NumberStyles.AllowHexSpecifier, out ulong value)
            && new H3Index(value) is { IsValidCell: true } cell)
            return cell;
        throw new ArgumentException($"'{index}' is not the index of an H3 cell.", nameof(index));
    }

    /// <summary>The region of a cell: its boundary as a closed ring of longitude and latitude, cut at the antimeridian.</summary>
    private static GeoFeature H3RegionOf(H3Index cell)
    {
        Span<LatLng> vertices = stackalloc LatLng[H3GeometryExtensions.MaxCellBoundaryVertices];
        int count = cell.GetCellBoundaryVertices(vertices);
        var ring = new GeoPosition[count + 1];
        for (int i = 0; i < count; i++) ring[i] = new GeoPosition(vertices[i].LongitudeDegrees, vertices[i].LatitudeDegrees);
        ring[count] = ring[0];

        var geometry = GeoGeometry.OfPolygons([.. GeoClipping.SplitAtDateline(ring).Select(piece => GeoPolygon.FromRings(piece, []))]);
        return new GeoFeature(geometry, new Dictionary<string, string> { [H3IndexProperty] = cell.ToString() });
    }
}
