// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;
using FibonacciRetracementTool = MatPlotLibNet.Models.Tools.FibonacciRetracement;

namespace MatPlotLibNet;

/// <summary>What is drawn over the data of the axes: a note, a reference line or span, a threshold, a patch, and the trading
/// tools (a signal, a trendline, a level, a Fibonacci retracement).</summary>
public static class AxesBuilderOverlayVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a buy or sell signal marker at the specified data coordinates.</summary>
        public AxesBuilder AddSignal(double x, double y, SignalDirection direction, Action<SignalMarker>? configure = null)
            => axes.Configured(axes.Axes.AddSignal(x, y, direction), configure);

        /// <summary>Adds a text annotation at the specified data coordinates.</summary>
        public AxesBuilder Annotate(string text, double x, double y, Action<Annotation>? configure = null)
            => axes.Configured(axes.Axes.Annotate(text, x, y), configure);

        /// <summary>Adds a text annotation with an arrow pointing to the given target coordinates.</summary>
        public AxesBuilder Annotate(string text, double x, double y, double arrowX, double arrowY,
            Action<Annotation>? configure = null)
            => axes.Annotate(text, x, y, annotation =>
            {
                annotation.ArrowTargetX = arrowX;
                annotation.ArrowTargetY = arrowY;
                configure?.Invoke(annotation);
            });

        /// <summary>Adds a trendline between two data-coordinate points.</summary>
        public AxesBuilder AddTrendline(double x1, double y1, double x2, double y2,
            Action<Trendline>? configure = null)
            => axes.Configured(axes.Axes.AddTrendline(x1, y1, x2, y2), configure);

        /// <summary>Adds a horizontal price level (support/resistance) at the given value.
        /// <para>The automatic range of the Y axis holds the level, so a level beyond the data is drawn inside the plot; a
        /// level beyond a limit you set is not drawn, nor its label.</para></summary>
        public AxesBuilder AddLevel(double value, Action<HorizontalLevel>? configure = null)
            => axes.Configured(axes.Axes.AddLevel(value), configure);

        /// <summary>Adds a Fibonacci retracement overlay between a price high and a price low.
        /// <para>The automatic range of the Y axis holds every level, from the high to the low; a level beyond a limit you
        /// set is not drawn, nor its label.</para></summary>
        public AxesBuilder AddFibonacci(double priceHigh, double priceLow,
            Action<FibonacciRetracementTool>? configure = null)
            => axes.Configured(axes.Axes.AddFibonacci(priceHigh, priceLow), configure);

        /// <summary>Adds a horizontal reference line at the specified Y value.
        /// <para>The automatic range of the Y axis holds the line, as matplotlib's <c>axhline</c> updates the data limits,
        /// so a line beyond the data is drawn inside the plot; a line beyond a limit you set is not drawn, nor its
        /// label.</para></summary>
        public AxesBuilder AxHLine(double y, Action<ReferenceLine>? configure = null)
            => axes.Configured(axes.Axes.AxHLine(y), configure);

        /// <summary>Adds a vertical reference line at the specified X value.
        /// <para>The automatic range of the X axis holds the line, as matplotlib's <c>axvline</c> updates the data limits,
        /// so a line beyond the data is drawn inside the plot; a line beyond a limit you set is not drawn, nor its
        /// label.</para></summary>
        public AxesBuilder AxVLine(double x, Action<ReferenceLine>? configure = null)
            => axes.Configured(axes.Axes.AxVLine(x), configure);

        /// <summary>Adds a patch the caller has made (a rectangle, a polygon, an arrow: a shape in data coordinates) to this axes, as
        /// matplotlib's <c>add_patch</c> does. One method for every kind of patch: the patch is styled through its own properties
        /// before or after it is added, since the axes holds that very patch.</summary>
        /// <param name="patch">The patch to add.</param>
        /// <returns>This builder, for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="patch"/> is null.</exception>
        /// <example>
        /// matplotlib's <c>ax.add_patch(Rectangle((1, 1), 2, 1, angle=30))</c>:
        /// <code>
        /// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
        ///     .Plot(x, y)
        ///     .AddPatch(new Rectangle(new(1, 1), 2, 1) { Angle = 30, Alpha = 0.5 })
        ///     .AddPatch(new Arrow(new(0, 0), 2, 2) { Width = 0.05 }));
        /// </code>
        /// </example>
        public AxesBuilder AddPatch(Patch patch) { axes.Axes.AddPatch(patch); return axes; }

        /// <summary>Adds a horizontal shaded span between the specified Y values.</summary>
        public AxesBuilder AxHSpan(double yMin, double yMax, Action<SpanRegion>? configure = null)
            => axes.Configured(axes.Axes.AxHSpan(yMin, yMax), configure);

        /// <summary>Adds a vertical shaded span between the specified X values.</summary>
        public AxesBuilder AxVSpan(double xMin, double xMax, Action<SpanRegion>? configure = null)
            => axes.Configured(axes.Axes.AxVSpan(xMin, xMax), configure);

        /// <summary>Adds a threshold marker: a dashed <see cref="ReferenceLine"/> at <paramref name="value"/>
        /// plus a shaded <see cref="SpanRegion"/> covering the breach zone on the <paramref name="breach"/>
        /// side. An optional text <paramref name="label"/> is added as an annotation at the threshold value.</summary>
        /// <param name="value">The threshold value in data coordinates.</param>
        /// <param name="orientation">Horizontal (constant Y) or Vertical (constant X).</param>
        /// <param name="breach">Which side of the threshold is the breach zone.</param>
        /// <param name="color">Optional color for the line and span. Null uses the renderer cycle color.</param>
        /// <param name="label">Optional label text. Null adds no annotation.</param>
        public AxesBuilder Threshold(double value, Orientation orientation, ThresholdBreach breach,
            Color? color = null, string? label = null)
        {
            axes.Axes.AddThreshold(value, orientation, breach, color, label);
            return axes;
        }

        /// <summary>Adds a buy signal marker at the given position.</summary>
        public AxesBuilder BuyAt(double x, double y, Action<SignalMarker>? configure = null)
            => axes.AddSignal(x, y, SignalDirection.Buy, configure);

        /// <summary>Adds a sell signal marker at the given position.</summary>
        public AxesBuilder SellAt(double x, double y, Action<SignalMarker>? configure = null)
            => axes.AddSignal(x, y, SignalDirection.Sell, configure);
    }
}
