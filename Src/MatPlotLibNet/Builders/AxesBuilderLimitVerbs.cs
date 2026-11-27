// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>The range each axis shows: its limits, the margin the automatic range keeps, which way it runs, and the regions it
/// leaves out.</summary>
public static class AxesBuilderLimitVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Sets the X-axis data range limits.</summary>
        public AxesBuilder SetXLim(double min, double max) { axes.X.Limits(min, max); return axes; }

        /// <summary>Sets the Y-axis data range limits.</summary>
        public AxesBuilder SetYLim(double min, double max) { axes.Y.Limits(min, max); return axes; }

        /// <summary>Sets the Z-axis data range limits (3D charts only).</summary>
        public AxesBuilder SetZLim(double min, double max) { axes.Z.Limits(min, max); return axes; }

        /// <summary>Sets the auto-scale margin on each side of the X axis (default <see cref="Styling.Theme.AxisXMargin"/>). Use 0 for bar/candlestick charts that already include half-bar-width padding.</summary>
        public AxesBuilder SetXMargin(double margin) { axes.X.Margin(margin); return axes; }

        /// <summary>Sets the auto-scale margin on each side of the Y axis (default <see cref="Styling.Theme.AxisYMargin"/>).</summary>
        public AxesBuilder SetYMargin(double margin) { axes.Y.Margin(margin); return axes; }

        /// <summary>Removes all data padding — data touches the axis spines directly.
        /// Equivalent to <c>SetXMargin(0).SetYMargin(0)</c>.</summary>
        public AxesBuilder WithTightMargins() { axes.X.Margin(0); axes.Y.Margin(0); return axes; }

        /// <summary>Turns the X axis round, as matplotlib's <c>invert_xaxis()</c> does: high values on the left. A toggle, as matplotlib's is
        /// (<c>set_inverted(not get_inverted())</c>): called twice, the axis runs the way it did. The axis holds it (<see cref="Axis.Inverted"/>),
        /// which the JSON carries.</summary>
        /// <returns>This builder.</returns>
        /// <example>
        /// <code>
        /// string svg = Plt.Create()
        ///     .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).InvertXAxis())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder InvertXAxis() { axes.X.Invert(); return axes; }

        /// <summary>Turns the Y axis round, as matplotlib's <c>invert_yaxis()</c> does: high values at the bottom. A toggle, as matplotlib's is:
        /// a chart whose rows already read from the top down (a Gantt chart, an image under the matplotlib themes) is turned the other way.
        /// The axis holds it (<see cref="Axis.Inverted"/>), which the JSON carries.</summary>
        /// <returns>This builder.</returns>
        /// <example>
        /// <code>
        /// string svg = Plt.Create()
        ///     .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).InvertYAxis())
        ///     .ToSvg();
        /// </code>
        /// </example>
        public AxesBuilder InvertYAxis() { axes.Y.Invert(); return axes; }

        /// <summary>Adds a discontinuous region to the X-axis.</summary>
        /// <param name="from">Data-space start of the hidden region (inclusive).</param>
        /// <param name="to">Data-space end of the hidden region (inclusive).</param>
        /// <param name="style">Visual marker style at the break boundaries (default <see cref="AxisBreak.RestingStyle"/>).</param>
        public AxesBuilder WithXBreak(double from, double to, BreakStyle style = AxisBreak.RestingStyle)
        {
            axes.Axes.AddXBreak(from, to, style);
            return axes;
        }

        /// <summary>Adds a discontinuous region to the Y-axis.</summary>
        /// <param name="from">Data-space start of the hidden region (inclusive).</param>
        /// <param name="to">Data-space end of the hidden region (inclusive).</param>
        /// <param name="style">Visual marker style at the break boundaries (default <see cref="AxisBreak.RestingStyle"/>).</param>
        public AxesBuilder WithYBreak(double from, double to, BreakStyle style = AxisBreak.RestingStyle)
        {
            axes.Axes.AddYBreak(from, to, style);
            return axes;
        }
    }
}
