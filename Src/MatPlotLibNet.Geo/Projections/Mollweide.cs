// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Mollweide projection — equal-area pseudo-cylindrical. Popular for global thematic maps.
/// Preserves area but distorts shapes, especially near the edges.</summary>
public sealed class Mollweide : IGeoProjection
{
    private const double R = 1.0;
    private const double Sqrt2 = 1.4142135623730951;

    public string Name => "Mollweide";

    public ProjectedPoint Forward(double latitude, double longitude)
    {
        double phi = latitude.ToRadians();
        double lam = longitude.ToRadians();
        double theta = SolveTheta(phi);
        double x = (R * 2 * Sqrt2 / Math.PI * lam * Math.Cos(theta)).ToDegrees();
        double y = (R * Sqrt2 * Math.Sin(theta)).ToDegrees();
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

    private static double SolveTheta(double phi)
    {
        // Newton's method: 2θ + sin(2θ) = π sin(φ)
        double target = Math.PI * Math.Sin(phi);
        double theta = phi; // initial guess
        for (int i = 0; i < 20; i++)
        {
            double f = 2 * theta + Math.Sin(2 * theta) - target;
            double fp = 2 + 2 * Math.Cos(2 * theta);
            if (Math.Abs(fp) < 1e-12) break;
            theta -= f / fp;
            if (Math.Abs(f) < 1e-10) break;
        }
        return theta;
    }
}
