// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The polar-family verbs of <see cref="Axes"/> (<c>PolarPlot</c>, <c>PolarScatter</c>, …), each
/// constructing one series, setting <see cref="Axes.CoordinateSystem"/> to <see cref="CoordinateSystem.Polar"/>,
/// and handing the series to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesPolarVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a polar line series and sets coordinate system to Polar.</summary>
        public PolarLineSeries PolarPlot(double[] r, double[] theta)
        {
            r.Length.ThrowIfMisaligned(theta.Length);
            axes.CoordinateSystem = CoordinateSystem.Polar;
            return axes.AddSeries(new PolarLineSeries(r, theta));
        }

        /// <summary>Adds a polar scatter series and sets coordinate system to Polar.</summary>
        public PolarScatterSeries PolarScatter(double[] r, double[] theta)
        {
            r.Length.ThrowIfMisaligned(theta.Length);
            axes.CoordinateSystem = CoordinateSystem.Polar;
            return axes.AddSeries(new PolarScatterSeries(r, theta));
        }

        /// <summary>Adds a polar bar series and sets coordinate system to Polar.</summary>
        public PolarBarSeries PolarBar(double[] r, double[] theta)
        {
            r.Length.ThrowIfMisaligned(theta.Length);
            axes.CoordinateSystem = CoordinateSystem.Polar;
            return axes.AddSeries(new PolarBarSeries(r, theta));
        }

        /// <summary>Adds a polar heatmap series and sets coordinate system to Polar.
        /// Each cell is a wedge defined by a theta bin and a radial bin.</summary>
        /// <param name="data">2D data matrix [thetaBins, rBins].</param>
        /// <param name="thetaBins">Number of angular divisions.</param>
        /// <param name="rBins">Number of radial divisions.</param>
        public PolarHeatmapSeries PolarHeatmap(double[,] data, int thetaBins, int rBins)
        {
            axes.CoordinateSystem = CoordinateSystem.Polar;
            return axes.AddSeries(new PolarHeatmapSeries(data, thetaBins, rBins));
        }
    }
}
