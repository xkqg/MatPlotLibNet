// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Robinson projection — a compromise pseudo-cylindrical projection designed for
/// world maps. Neither conformal nor equal-area but minimises overall distortion.
/// Uses polynomial interpolation of Robinson's tabulated values.</summary>
public sealed class Robinson : IGeoProjection
{
    // Robinson's tabulated values at 5° intervals (latitude 0° to 90°)
    private static readonly double[] _plen = // parallel length
    [
        1.0000, 0.9986, 0.9954, 0.9900, 0.9822, 0.9730, 0.9600, 0.9427,
        0.9216, 0.8962, 0.8679, 0.8350, 0.7986, 0.7597, 0.7186, 0.6732,
        0.6213, 0.5722, 0.5322
    ];
    private static readonly double[] _pdfe = // parallel distance from equator
    [
        0.0000, 0.0620, 0.1240, 0.1860, 0.2480, 0.3100, 0.3720, 0.4340,
        0.4958, 0.5571, 0.6176, 0.6769, 0.7346, 0.7903, 0.8435, 0.8936,
        0.9394, 0.9761, 1.0000
    ];

    /// <inheritdoc />
    public string Name => "Robinson";

    /// <inheritdoc />
    public (double X, double Y) Forward(double latitude, double longitude)
    {
        double absLat = Math.Min(Math.Abs(latitude), 90);
        int index = (int)(absLat / 5);
        if (index >= _plen.Length - 1) index = _plen.Length - 2;
        double frac = (absLat - index * 5) / 5.0;

        double plen = _plen[index] + frac * (_plen[index + 1] - _plen[index]);
        double pdfe = _pdfe[index] + frac * (_pdfe[index + 1] - _pdfe[index]);

        double x = 0.8487 * plen * longitude;
        double y = Math.Sign(latitude) * 1.3523 * pdfe * 90;

        return (x, y);
    }

    /// <inheritdoc />
    public (double Lat, double Lon)? Inverse(double x, double y) => null; // not invertible analytically

    /// <inheritdoc />
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
