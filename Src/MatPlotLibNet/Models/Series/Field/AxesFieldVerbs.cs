// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The vector-field-family verbs of <see cref="Axes"/> (<c>Quiver</c>, <c>Streamplot</c>, <c>Barbs</c>, …),
/// each constructing one series and handing it to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesFieldVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a stem plot series from the given X and Y data arrays.</summary>
        /// <param name="x">The X-axis data values.</param>
        /// <param name="y">The Y-axis data values.</param>
        /// <returns>The newly created <see cref="StemSeries"/> for further configuration.</returns>
        public StemSeries Stem(double[] x, double[] y)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            return axes.AddSeries(new StemSeries(x, y));
        }

        /// <summary>Adds a quiver (vector field) series from the given position and vector data.</summary>
        public QuiverSeries Quiver(double[] x, double[] y, double[] u, double[] v)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(u.Length);
            x.Length.ThrowIfMisaligned(v.Length);
            return axes.AddSeries(new QuiverSeries(x, y, u, v));
        }

        /// <summary>Adds a streamplot (vector field streamlines) series from the given grid and velocity data.</summary>
        public StreamplotSeries Streamplot(double[] x, double[] y, double[,] u, double[,] v)
        {
            u.ThrowIfMisaligned(y.Length, x.Length);
            v.ThrowIfMisaligned(y.Length, x.Length);
            return axes.AddSeries(new StreamplotSeries(x, y, u, v));
        }

        /// <summary>Adds a quiver key (reference arrow) series.</summary>
        public QuiverKeySeries QuiverKey(double x, double y, double u, string label) => axes.AddSeries(new QuiverKeySeries(x, y, u, label));

        /// <summary>Adds a wind barb series.</summary>
        public BarbsSeries Barbs(Numerics.Vec x, Numerics.Vec y, Numerics.Vec speed, Numerics.Vec direction)
        {
            x.Length.ThrowIfMisaligned(y.Length);
            x.Length.ThrowIfMisaligned(speed.Length);
            x.Length.ThrowIfMisaligned(direction.Length);
            return axes.AddSeries(new BarbsSeries(x, y, speed, direction));
        }
    }
}
