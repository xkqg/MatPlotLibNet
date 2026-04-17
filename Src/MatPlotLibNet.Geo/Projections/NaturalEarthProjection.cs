// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Natural Earth projection — a compromise pseudo-cylindrical projection designed
/// by Tom Patterson (2012). Similar to Robinson but with smoother edges.</summary>
public sealed class NaturalEarthProjection : IGeoProjection
{
    public string Name => "NaturalEarth";

    public (double X, double Y) Forward(double latitude, double longitude)
    {
        double phi = latitude * Math.PI / 180.0;
        double phi2 = phi * phi, phi4 = phi2 * phi2, phi6 = phi4 * phi2;
        double x = longitude * (0.8707 - 0.131979 * phi2 + 0.003971 * phi4 - 0.001529 * phi6);
        double y = latitude * (1.007226 + 0.015085 * phi2 - 0.044475 * phi4 + 0.028874 * phi6 - 0.005916 * phi2 * phi6);
        return (x, y);
    }

    public (double Lat, double Lon)? Inverse(double x, double y) => null;

    public (double XMin, double XMax, double YMin, double YMax) Bounds
    {
        get
        {
            var (xMax, _) = Forward(0, 180);
            var (_, yMax) = Forward(90, 0);
            return (-xMax, xMax, -yMax, yMax);
        }
    }
}
