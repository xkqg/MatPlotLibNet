// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>Utility methods for handling geographic edge cases: dateline wraparound,
/// NaN filtering, and projection boundary clipping.</summary>
public static class GeoClipping
{
    /// <summary>Splits a coordinate ring that crosses the antimeridian (±180° longitude)
    /// into two separate rings, one for each hemisphere.</summary>
    public static List<List<(double Lon, double Lat)>> SplitAtDateline(List<(double Lon, double Lat)> ring)
    {
        bool crosses = false;
        for (int i = 1; i < ring.Count; i++)
        {
            if (Math.Abs(ring[i].Lon - ring[i - 1].Lon) > 180)
            {
                crosses = true;
                break;
            }
        }

        if (!crosses) return [ring];

        // Split into two halves at the dateline
        var west = new List<(double, double)>();
        var east = new List<(double, double)>();

        foreach (var (lon, lat) in ring)
        {
            if (lon < 0) west.Add((lon, lat));
            else east.Add((lon, lat));
        }

        var result = new List<List<(double Lon, double Lat)>>();
        if (west.Count >= 3) result.Add(west);
        if (east.Count >= 3) result.Add(east);
        return result.Count > 0 ? result : [ring];
    }

    /// <summary>Removes points with NaN coordinates from a projected point list.</summary>
    public static List<(double X, double Y)> FilterNaN(List<(double X, double Y)> points)
    {
        var result = new List<(double, double)>(points.Count);
        foreach (var (x, y) in points)
        {
            if (!double.IsNaN(x) && !double.IsNaN(y))
                result.Add((x, y));
        }
        return result;
    }

    /// <summary>Clips projected points to the projection's bounding box.</summary>
    public static List<(double X, double Y)> ClipToBounds(
        List<(double X, double Y)> points,
        double xMin, double xMax, double yMin, double yMax)
    {
        var result = new List<(double, double)>(points.Count);
        foreach (var (x, y) in points)
        {
            result.Add((Math.Clamp(x, xMin, xMax), Math.Clamp(y, yMin, yMax)));
        }
        return result;
    }
}
