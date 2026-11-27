// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of the axes builder that add a series of the field family (stems, arrows, streamlines, wind barbs): each
/// adds the series through the axes verb of the same name (<see cref="AxesFieldVerbs"/>) and hands it to the caller's
/// callback.</summary>
public static class AxesBuilderFieldVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a stem series to the axes.</summary>
        public AxesBuilder Stem(double[] x, double[] y, Action<StemSeries>? configure = null)
            => axes.Configured(axes.Axes.Stem(x, y), configure);

        /// <summary>Adds a quiver (vector field) series to the axes.</summary>
        public AxesBuilder Quiver(double[] x, double[] y, double[] u, double[] v, Action<QuiverSeries>? configure = null)
            => axes.Configured(axes.Axes.Quiver(x, y, u, v), configure);

        /// <summary>Adds a streamplot (vector field streamlines) series to the axes.</summary>
        public AxesBuilder Streamplot(double[] x, double[] y, double[,] u, double[,] v, Action<StreamplotSeries>? configure = null)
            => axes.Configured(axes.Axes.Streamplot(x, y, u, v), configure);

        /// <summary>Adds a quiver key series to the axes.</summary>
        public AxesBuilder QuiverKey(double x, double y, double u, string label, Action<QuiverKeySeries>? configure = null)
            => axes.Configured(axes.Axes.QuiverKey(x, y, u, label), configure);

        /// <summary>Adds a wind barb series to the axes.</summary>
        public AxesBuilder Barbs(double[] x, double[] y, double[] speed, double[] direction, Action<BarbsSeries>? configure = null)
            => axes.Configured(axes.Axes.Barbs(x, y, speed, direction), configure);
    }
}
