// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>A series a hover can land on. <see cref="NearestPointFinder.Find"/> asks every series of the subplot in turn, each
/// family answers in its own terms: a series of points by the nearest point on screen, a ternary diagram by the nearest
/// sample where it stands in the frame, a state timeline by the segment under the cursor. A series that is not a hover target
/// is passed over.</summary>
internal interface IHoverTarget
{
    /// <summary>The answer the hover gives once this series has been looked at: <paramref name="best"/>, or this series' own
    /// point when it beats it.</summary>
    /// <param name="search">The cursor and the view of the subplot.</param>
    /// <param name="seriesIndex">The index of this series in its subplot, which names a series without a label.</param>
    /// <param name="best">The answer from the series before this one, or <see langword="null"/>.</param>
    NearestPointResult? Nearest(in HoverSearch search, int seriesIndex, NearestPointResult? best);
}

/// <summary>One hover over one subplot: the axes, the view its series are drawn through, where the cursor is on screen and
/// in data, and how far from the cursor a point may be and still be found.</summary>
/// <param name="Axes">The subplot.</param>
/// <param name="View">The transform the subplot was drawn with, so a log or symlog axis measures what the reader sees.</param>
/// <param name="Cursor">The cursor, in pixels.</param>
/// <param name="DataX">The cursor along X, in data units.</param>
/// <param name="DataY">The cursor along Y, in data units.</param>
/// <param name="MaxPixelDistance">The reach of the cursor, in pixels.</param>
internal readonly record struct HoverSearch(Axes Axes, DataTransform View, Point Cursor, double DataX, double DataY, double MaxPixelDistance)
{
    /// <summary>A hover at (<paramref name="dataX"/>, <paramref name="dataY"/>) through <paramref name="view"/>.</summary>
    internal static HoverSearch At(Axes axes, DataTransform view, double dataX, double dataY, double maxPixelDistance) =>
        new(axes, view, view.DataToPixel(dataX, dataY), dataX, dataY, maxPixelDistance);

    /// <summary>How far the data point (<paramref name="x"/>, <paramref name="y"/>) is from the cursor on screen; NaN for a
    /// point that maps to no pixel (a NaN, a point inside an axis break, zero or below on a log axis).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal double PixelDistanceTo(double x, double y)
    {
        var at = View.DataToPixel(x, y);
        return Cursor.DistanceTo(at);
    }

    /// <summary>Whether a point at <paramref name="pixelDistance"/> is within reach and nearer than <paramref name="best"/>.
    /// Not "&gt; reach": a point at NaN is never within it.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Beats(double pixelDistance, NearestPointResult? best) =>
        pixelDistance <= MaxPixelDistance && (best is null || pixelDistance < best.PixelDistance);
}
