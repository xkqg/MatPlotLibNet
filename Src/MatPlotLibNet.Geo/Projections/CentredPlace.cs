// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>A place as the centre of an azimuthal projection sees it (<see cref="CentredFrame.Of(double, double)"/>): the sine and the
/// cosine of its latitude and of its turn from the centre's meridian. An azimuthal projection places it at
/// <c>k · (cos φ sin Δλ, cos φ0 sin φ − sin φ0 cos φ cos Δλ)</c>, where the scale <c>k</c> is the projection's own (1 for the
/// orthographic, <c>c / sin c</c> for the azimuthal equidistant, <c>2 / (1 + cos c)</c> for the stereographic) and <c>c</c> is the
/// place's angle from the centre (<see cref="CosDistance"/>).</summary>
/// <param name="Frame">The centre.</param>
/// <param name="SinLat">The sine of the place's latitude.</param>
/// <param name="CosLat">The cosine of the place's latitude.</param>
/// <param name="SinTurn">The sine of the place's longitude less the centre's.</param>
/// <param name="CosTurn">The cosine of the place's longitude less the centre's.</param>
internal readonly record struct CentredPlace(CentredFrame Frame, double SinLat, double CosLat, double SinTurn, double CosTurn)
{
    /// <summary>The cosine of the place's angle from the centre: below 0 on the far side of the globe.</summary>
    internal double CosDistance => CosDistanceAfter(0);

    /// <summary>The cosine of the place's angle from the centre added to <paramref name="start"/>, the sum taken from the left (the
    /// stereographic scale reads <c>1 + cos c</c> so, which keeps every number it placed).</summary>
    /// <param name="start">What the cosine is added to.</param>
    internal double CosDistanceAfter(double start) => start + Frame.SinLat0 * SinLat + Frame.CosLat0 * CosLat * CosTurn;

    /// <summary>How far east of the centre the place stands, at the scale <paramref name="scale"/>, in radians.</summary>
    /// <param name="scale">The projection's scale at the place.</param>
    internal double EastBy(double scale) => scale * CosLat * SinTurn;

    /// <summary>How far north of the centre the place stands, at the scale <paramref name="scale"/>, in radians.</summary>
    /// <param name="scale">The projection's scale at the place.</param>
    internal double NorthBy(double scale) => scale * (Frame.CosLat0 * SinLat - Frame.SinLat0 * CosLat * CosTurn);
}
