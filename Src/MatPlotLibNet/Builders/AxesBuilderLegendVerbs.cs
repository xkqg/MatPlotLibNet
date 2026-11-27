// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>What names the marks of the axes: the legend, the values it appends, the colour bar and the tooltips.</summary>
public static class AxesBuilderLegendVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Enables or disables native SVG tooltips for data elements.</summary>
        public AxesBuilder WithTooltips(bool enabled = true) { axes.Axes.EnableTooltips = enabled; return axes; }

        /// <summary>Enables a color bar alongside the plot area. Auto-detects colormap and range from heatmap/contour series.</summary>
        public AxesBuilder WithColorBar(Func<ColorBar, ColorBar>? configure = null)
        {
            axes.Axes.ColorBar = ColorBar.AskedFor(axes.Axes.ColorBar, configure);
            return axes;
        }

        /// <summary>Configures the legend display for this axes. Preserves every other
        /// <see cref="Legend"/> property already set earlier in the chain (e.g. via
        /// <see cref="WithLegendValues"/> or the <see cref="extension(AxesBuilder).WithLegend(Func{Legend, Legend})"/>
        /// transform overload) — only <see cref="Legend.Visible"/> and <see cref="Legend.Position"/>
        /// are updated. A legend that names no position stands at <see cref="LegendPosition.Best"/>, where it covers the
        /// fewest marks (the upper right under the classic theme).</summary>
        public AxesBuilder WithLegend(LegendPosition position = LegendPosition.Best, bool visible = true)
        {
            axes.Axes.Legend = axes.Axes.Legend with { Visible = visible, Position = position };
            return axes;
        }

        /// <summary>Configures the legend using a transform function, preserving all other properties.</summary>
        public AxesBuilder WithLegend(Func<Legend, Legend> configure) { axes.Axes.Legend = configure(axes.Axes.Legend); return axes; }

        /// <summary>Enables or disables the legend-values suffix that appends each labelled series'
        /// last data value to its legend entry — e.g. "Signal" becomes "Signal = 2.70"
        /// (InvariantCulture, F2 format). Only <see cref="Models.Series.XYSeries"/> instances
        /// yield a value; other series types display the label text only. Default is
        /// <see langword="false"/>.</summary>
        public AxesBuilder WithLegendValues(bool enabled = true) { axes.Axes.Legend = axes.Axes.Legend with { LegendValues = enabled }; return axes; }
    }
}
