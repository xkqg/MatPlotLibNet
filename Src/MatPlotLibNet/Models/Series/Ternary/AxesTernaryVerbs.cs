// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs of <see cref="Axes"/> that place compositions of three parts in a triangle: <c>TernaryPlot</c> and
/// <c>TernaryScatter</c>, which set <see cref="Axes.CoordinateSystem"/> to <see cref="CoordinateSystem.Ternary"/>, and <c>Piper</c>,
/// which draws its own panels and leaves the axes Cartesian. Each refuses a sample that cannot be placed, constructs one series
/// and hands it to <see cref="Axes.AddSeries{T}"/>. On a ternary axes the parts are named by the labels of the axes: the part at the
/// apex by the Y label, at the bottom left by the Z label, at the bottom right by the X label.</summary>
public static class AxesTernaryVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a line through compositions of three parts and sets the coordinate system to ternary.</summary>
        /// <param name="top">The part at the apex, for each sample.</param>
        /// <param name="left">The part at the bottom left corner, for each sample.</param>
        /// <param name="right">The part at the bottom right corner, for each sample.</param>
        /// <returns>The newly created <see cref="TernaryLineSeries"/> for further configuration.</returns>
        /// <exception cref="ArgumentException">The arrays differ in length, or a sample has a negative, infinite or missing part, or
        /// three zeros; the message names the row and the part.</exception>
        public TernaryLineSeries TernaryPlot(double[] top, double[] left, double[] right)
        {
            TernarySeries.RefuseUnplaceable(top, left, right);
            axes.CoordinateSystem = CoordinateSystem.Ternary;
            return axes.AddSeries(new TernaryLineSeries(top, left, right));
        }

        /// <summary>Adds a Piper diagram of the given waters: the cations on one triangle, the anions on a second and both on a
        /// diamond, a marker for each water on each. The axes stays Cartesian and draws no grid, ticks or frame around it.</summary>
        /// <param name="samples">The waters, in mg/L.</param>
        /// <returns>The newly created <see cref="PiperSeries"/> for further configuration.</returns>
        /// <exception cref="ArgumentOutOfRangeException">An amount is negative, infinite or not a number; the message names the row and the ion.</exception>
        /// <exception cref="ArgumentException">A water has no cations or no anions; the message names the row.</exception>
        public PiperSeries Piper(IReadOnlyList<WaterSample> samples)
        {
            PiperSeries.RefuseUnplaceable(samples);
            return axes.AddSeries(new PiperSeries(samples));
        }

        /// <summary>Adds a marker on each composition of three parts and sets the coordinate system to ternary.</summary>
        /// <param name="top">The part at the apex, for each sample.</param>
        /// <param name="left">The part at the bottom left corner, for each sample.</param>
        /// <param name="right">The part at the bottom right corner, for each sample.</param>
        /// <returns>The newly created <see cref="TernaryScatterSeries"/> for further configuration.</returns>
        /// <exception cref="ArgumentException">The arrays differ in length, or a sample has a negative, infinite or missing part, or
        /// three zeros; the message names the row and the part.</exception>
        public TernaryScatterSeries TernaryScatter(double[] top, double[] left, double[] right)
        {
            TernarySeries.RefuseUnplaceable(top, left, right);
            axes.CoordinateSystem = CoordinateSystem.Ternary;
            return axes.AddSeries(new TernaryScatterSeries(top, left, right));
        }
    }
}
