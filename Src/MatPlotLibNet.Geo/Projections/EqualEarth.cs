// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Equal Earth projection (Šavrič, Patterson, Jenny, 2018) — equal-area pseudo-cylindrical.
/// A modern alternative to Mollweide/Robinson with pleasant aesthetics and true equal-area.</summary>
public sealed class EqualEarth : IGeoProjection
{
    private const double A1 = 1.340264, A2 = -0.081106, A3 = 0.000893, A4 = 0.003796;
    private const double M = 0.8660254037844386; // sqrt(3) / 2

    public string Name => "EqualEarth";

    public ProjectedPoint Forward(double latitude, double longitude)
    {
        double phi = latitude.ToRadians();
        double lam = longitude.ToRadians();
        double theta = Math.Asin(M * Math.Sin(phi));
        double t2 = theta * theta, t6 = t2 * t2 * t2;

        // y = θ(A1 + A2θ² + A3θ⁶ + A4θ⁸)  (Šavrič, Patterson, Jenny 2018)
        double py = A1 + A2 * t2 + A3 * t6 + A4 * t6 * t2;
        double y = (theta * py).ToDegrees();

        // x = 2√3·λ·cos(θ) / [3·(A1 + 3A2θ² + 7A3θ⁶ + 9A4θ⁸)]
        double pd = A1 + 3 * A2 * t2 + 7 * A3 * t6 + 9 * A4 * t6 * t2;
        double x = (2 * Math.Sqrt(3) * lam * Math.Cos(theta) / (3 * pd)).ToDegrees();

        return new(x, y);
    }

    public GeoCoordinate? Inverse(double x, double y) => null;

    public GeoBounds Bounds
    {
        get
        {
            var (xMax, _) = Forward(0, 180);
            var (_, yMax) = Forward(90, 0);
            return new(-xMax, xMax, -yMax, yMax);
        }
    }
}
