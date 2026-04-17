// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Equal Earth projection (Šavrič, Patterson, Jenny, 2018) — equal-area pseudo-cylindrical.
/// A modern alternative to Mollweide/Robinson with pleasant aesthetics and true equal-area.</summary>
public sealed class EqualEarth : IGeoProjection
{
    private const double A1 = 1.340264, A2 = -0.081106, A3 = 0.000893, A4 = 0.003796;
    private const double M = 0.7071067811865476; // sqrt(3) / 2 ... actually sin(π/4)

    public string Name => "EqualEarth";

    public (double X, double Y) Forward(double latitude, double longitude)
    {
        double phi = latitude * Math.PI / 180.0;
        double theta = Math.Asin(M * Math.Sin(phi));
        double t2 = theta * theta, t6 = t2 * t2 * t2;
        double d = theta * (A1 + 3 * A2 * t2 + t6 * (7 * A3 + 9 * A4 * t2));
        double x = longitude * Math.Cos(theta) / (M * (A1 + 3 * A2 * t2 + t6 * (7 * A3 + 9 * A4 * t2)));
        double y = d * 180.0 / Math.PI;
        x *= 180.0 / Math.PI * longitude / (longitude == 0 ? 1 : longitude); // normalize
        return (longitude * Math.Cos(theta) * 180.0 / Math.PI / M / (A1 + 3 * A2 * t2 + t6 * (7 * A3 + 9 * A4 * t2)) * Math.PI / 180.0 * longitude, y);
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
