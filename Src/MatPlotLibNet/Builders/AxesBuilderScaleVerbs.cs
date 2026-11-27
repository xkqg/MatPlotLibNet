// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>How the values of an axis map onto it: linear, logarithmic, symmetric-logarithmic or dates.</summary>
public static class AxesBuilderScaleVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Sets the X-axis scale type (e.g., linear or logarithmic).</summary>
        public AxesBuilder SetXScale(AxisScale scale) { axes.X.Scale(scale); return axes; }

        /// <summary>Sets the Y-axis scale type (e.g., linear or logarithmic).</summary>
        public AxesBuilder SetYScale(AxisScale scale) { axes.Y.Scale(scale); return axes; }

        /// <summary>Sets the Y-axis to symmetric logarithmic scale, linear within the theme's threshold of zero (matplotlib's 2 under the
        /// matplotlib themes, as <c>set_yscale('symlog')</c>; <see cref="Rendering.SymlogTransform.DefaultLinThresh"/> under the library's
        /// own) and logarithmic outside.</summary>
        /// <returns>This builder.</returns>
        public AxesBuilder WithSymlogYScale() { axes.Y.Symlog(null); return axes; }

        /// <summary>Sets the Y-axis to symmetric logarithmic scale with the specified linear threshold.
        /// Linear within [-linthresh, linthresh], logarithmic outside.</summary>
        /// <param name="linthresh">Linear threshold, kept under every theme.</param>
        public AxesBuilder WithSymlogYScale(double linthresh = Rendering.SymlogTransform.DefaultLinThresh) { axes.Y.Symlog(linthresh); return axes; }

        /// <summary>Sets the X-axis to symmetric logarithmic scale, linear within the theme's threshold of zero (matplotlib's 2 under the
        /// matplotlib themes, as <c>set_xscale('symlog')</c>; <see cref="Rendering.SymlogTransform.DefaultLinThresh"/> under the library's
        /// own) and logarithmic outside.</summary>
        /// <returns>This builder.</returns>
        public AxesBuilder WithSymlogXScale() { axes.X.Symlog(null); return axes; }

        /// <summary>Sets the X-axis to symmetric logarithmic scale with the specified linear threshold.</summary>
        /// <param name="linthresh">Linear threshold, kept under every theme.</param>
        public AxesBuilder WithSymlogXScale(double linthresh = Rendering.SymlogTransform.DefaultLinThresh) { axes.X.Symlog(linthresh); return axes; }

        /// <summary>Sets the X-axis to date scale with intelligent tick placement and automatic format selection.</summary>
        /// <remarks>
        /// Installs an <see cref="Rendering.TickLocators.AutoDateLocator"/> and a paired
        /// <see cref="Rendering.TickFormatters.AutoDateFormatter"/> that together choose granularity
        /// (years, months, weeks, days, hours, minutes, or seconds) from the visible range.
        /// </remarks>
        public AxesBuilder SetXDateAxis() { axes.X.AutoDates(); return axes; }

        /// <summary>Pins the X axis to the time window that ends at <paramref name="end"/> and reaches back by
        /// <paramref name="span"/>, on a date axis with time-aware ticks: EXACT bounds, ROUND ticks.
        /// <para>The distinction is the whole reason a rolling chart reads well or badly. The ticks should sit on round clock
        /// instants and glide leftwards. The <i>bounds</i> must not be rounded: an auto axis expands them outward to the nearest
        /// nice number, which makes the axis stand perfectly still while the data grows into it and then jump a whole step at
        /// once. Pinning the bounds and letting the locator round only the ticks is what turns that lurch into a glide. The
        /// granularity follows the window: an ops screen is minutes wide, and a fixed <c>yyyy-MM-dd</c> printed the same date on
        /// every tick of it, so the automatic locator and formatter read the visible range and pick minutes, hours or
        /// days.</para></summary>
        /// <param name="end">The right edge of the window.</param>
        /// <param name="span">How much time the window shows.</param>
        /// <returns>This builder.</returns>
        internal AxesBuilder PinTimeWindow(DateTime end, TimeSpan span)
        {
            axes.SetXLim((end - span).ToOADate(), end.ToOADate());
            return axes.SetXDateAxis();
        }
    }
}
