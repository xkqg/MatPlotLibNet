// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>The transforms the rendering tests draw through, written once over the plot rectangle a test class holds. A log axis is
/// drawn in decades, so its data range is the exponent (0 to 2 for 1 to 100) and the full range the values: the exponent is worked
/// out by the rule of the scale, as the renderer works it out.</summary>
public static class DataTransformFixtures
{
    extension(Rect plot)
    {
        /// <summary>A linear transform of the range given: the data from (<paramref name="xMin"/>, <paramref name="yMin"/>) to
        /// (<paramref name="xMax"/>, <paramref name="yMax"/>) over the rectangle; a range that runs backwards draws that axis upside
        /// down.</summary>
        /// <param name="xMin">The data x at the left edge.</param>
        /// <param name="xMax">The data x at the right edge.</param>
        /// <param name="yMin">The data y at the bottom edge.</param>
        /// <param name="yMax">The data y at the top edge.</param>
        public DataTransform Linear(double xMin = 0, double xMax = 10, double yMin = 0, double yMax = 10) =>
            new(xMin, xMax, yMin, yMax, plot);

        /// <summary>A log Y axis (by default from 1 at the bottom edge to 100 at the top edge) under a linear X axis from 0 to
        /// <paramref name="xMax"/>.</summary>
        /// <param name="xMax">The data x at the right edge.</param>
        /// <param name="yMin">The value at the bottom edge.</param>
        /// <param name="yMax">The value at the top edge.</param>
        public DataTransform LogY(double xMax = 10, double yMin = 1, double yMax = 100) =>
            plot.Scaled(AxisScale.Linear, 0, xMax, AxisScale.Log, yMin, yMax);

        /// <summary>A log Y axis that runs backwards: <paramref name="yMax"/> (by default 100) at the bottom edge, 1 at the top, under
        /// a linear X axis from 0 to <paramref name="xMax"/>.</summary>
        /// <param name="xMax">The data x at the right edge.</param>
        /// <param name="yMax">The value at the bottom edge.</param>
        public DataTransform LogYReversed(double xMax = 10, double yMax = 100) =>
            plot.Scaled(AxisScale.Linear, 0, xMax, AxisScale.Log, yMax, 1);

        /// <summary>A log X axis (by default from 1 at the left edge to 100 at the right edge) over a linear Y axis (by default from 0
        /// to 10).</summary>
        /// <param name="yMax">The data y at the top edge.</param>
        /// <param name="xMin">The value at the left edge.</param>
        /// <param name="xMax">The value at the right edge.</param>
        /// <param name="yMin">The data y at the bottom edge.</param>
        public DataTransform LogX(double yMax = 10, double xMin = 1, double xMax = 100, double yMin = 0) =>
            plot.Scaled(AxisScale.Log, xMin, xMax, AxisScale.Linear, yMin, yMax);

        /// <summary>A log X axis that runs backwards: 100 at the left edge, 1 at the right.</summary>
        public DataTransform LogXReversed() =>
            plot.Scaled(AxisScale.Log, 100, 1, AxisScale.Linear, 0, 10);

        /// <summary>A log axis on both sides, each from 1 to its own top (by default 100).</summary>
        /// <param name="xMax">The value at the right edge.</param>
        /// <param name="yMax">The value at the top edge.</param>
        public DataTransform LogLog(double xMax = 100, double yMax = 100) =>
            plot.Scaled(AxisScale.Log, 1, xMax, AxisScale.Log, 1, yMax);

        /// <summary>A symmetric-log Y axis from -100 to 100, its threshold 1.</summary>
        public DataTransform SymLogY() =>
            plot.Scaled(AxisScale.Linear, 0, 10, AxisScale.SymLog, -100, 100);

        /// <summary>A logit Y axis from 0.1 to 0.9.</summary>
        public DataTransform LogitY() =>
            plot.Scaled(AxisScale.Linear, 0, 10, AxisScale.Logit, 0.1, 0.9);

        /// <summary>A linear transform of <paramref name="range"/> whose axes are cut at <paramref name="xBreaks"/> and
        /// <paramref name="yBreaks"/> and drawn in parts a fifth of their mean part apart, as the matplotlib themes stand their subplots
        /// (<c>figure.subplot.wspace</c> and <c>hspace</c> 0.2).</summary>
        /// <param name="range">The data range, in data units.</param>
        /// <param name="xBreaks">The breaks of the X axis.</param>
        /// <param name="yBreaks">The breaks of the Y axis.</param>
        public DataTransform InParts(DataRange range, AxisBreak[] xBreaks, AxisBreak[] yBreaks) =>
            DataTransform.Of(range, plot, new AxisMapping(ScaleRule.Linear, xBreaks, 0.2), new AxisMapping(ScaleRule.Linear, yBreaks, 0.2));

        /// <summary>A transform with the scale of each direction and the values at its two edges, no breaks and the linear threshold 1:
        /// what the data range is in the drawn space is worked out by the rule of the scale.</summary>
        private DataTransform Scaled(AxisScale xScale, double xLow, double xHigh, AxisScale yScale, double yLow, double yHigh)
        {
            var x = xScale.RuleAt(1).ForwardRange(xLow, xHigh);
            var y = yScale.RuleAt(1).ForwardRange(yLow, yHigh);
            return new(x.Min, x.Max, y.Min, y.Max, plot, null, null, xLow, xHigh, yLow, yHigh, xScale, yScale, 1, 1);
        }
    }
}
