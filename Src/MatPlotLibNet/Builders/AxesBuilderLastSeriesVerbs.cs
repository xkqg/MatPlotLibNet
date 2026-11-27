// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verbs that change the series added last: its bar labels, its point budget, its colour map and normaliser, and
/// its name at the end of its line.</summary>
public static class AxesBuilderLastSeriesVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>
        /// Enables bar value labels on the last <see cref="BarSeries"/> added to this axes.
        /// </summary>
        /// <param name="format">Optional .NET format string (e.g. "F1"). When null, a value is written with up to six
        /// significant digits and no trailing zeros, as matplotlib's <c>bar_label</c> writes it by default (<c>'%g'</c>):
        /// 12500 reads <c>12500</c>, 0.5 reads <c>0.5</c>, and only a value below 1e-4 or from 1e6 takes an exponent.</param>
        /// <remarks>Only the last <see cref="BarSeries"/> in the series list is affected. Call this method once per <see cref="BarSeries"/> immediately after adding it.</remarks>
        public AxesBuilder WithBarLabels(string? format = null)
        {
            if (axes.Axes.LastSeriesOf<BarSeries>() is { } bar)
            {
                bar.ShowLabels = true;
                bar.LabelFormat = format;
            }
            return axes;
        }

        /// <summary>
        /// Enables LTTB downsampling on the last XY series (Line, Area, Step, Signal, SignalXY) added to this axes.
        /// Viewport culling is applied first; if the result still exceeds <paramref name="maxPoints"/>, LTTB reduces it.
        /// A scatter series is culled to the viewport but never reduced further, because each marker may carry its own
        /// size and colour; a scatter that carries any of those per-point arrays is drawn whole.
        /// </summary>
        /// <param name="maxPoints">Maximum number of points to display. Default <see cref="XYSeries.DefaultPointBudget"/>.</param>
        /// <remarks>Only the last XY series is affected; call after each series you want to downsample. The two-stage pipeline (viewport cull → LTTB) is opt-in — series without <c>MaxDisplayPoints</c> set are rendered at full resolution.</remarks>
        public AxesBuilder WithDownsampling(int maxPoints = XYSeries.DefaultPointBudget)
        {
            if (axes.Axes.LastSeriesOf<Models.Series.XYSeries>() is { } xy)
                xy.MaxDisplayPoints = maxPoints;
            return axes;
        }

        /// <summary>Sets the colormap on the last heatmap/contour/surface series by name (resolved
        /// from <see cref="Styling.ColorMaps.ColorMapRegistry"/>).
        /// <para>Throws <see cref="ArgumentException"/> if <paramref name="name"/> is not a
        /// registered colormap name — surfacing typos or renamed colormaps instead of silently
        /// falling back to the renderer default. If you want a
        /// defensive lookup that returns the builder unchanged on unknown names, call
        /// <see cref="Styling.ColorMaps.ColorMapRegistry.Get"/> yourself and invoke the
        /// <see cref="extension(AxesBuilder).WithColorMap(Styling.ColorMaps.IColorMap)"/> overload conditionally.</para></summary>
        /// <exception cref="ArgumentException">When <paramref name="name"/> is not registered.</exception>
        public AxesBuilder WithColorMap(string name)
        {
            var map = Styling.ColorMaps.ColorMapRegistry.Get(name)
                ?? throw new ArgumentException(
                    $"Unknown colormap '{name}'. Registered colormaps: " +
                    string.Join(", ", Styling.ColorMaps.ColorMapRegistry.Names),
                    nameof(name));
            return axes.WithColorMap(map);
        }

        /// <summary>Sets the colormap on the last colormappable series (heatmap, image, histogram2d, contour, surface, scatter, hierarchical).</summary>
        public AxesBuilder WithColorMap(Styling.ColorMaps.IColorMap colorMap)
        {
            if (axes.Axes.LastSeriesIs<Models.Series.IColormappable>() is { } c) c.ColorMap = colorMap;
            return axes;
        }

        /// <summary>Sets the normalizer on the last normalizable series (heatmap, image, histogram2d).</summary>
        public AxesBuilder WithNormalizer(Styling.ColorMaps.INormalizer normalizer)
        {
            if (axes.Axes.LastSeriesIs<Models.Series.INormalizable>() is { } n) n.Normalizer = normalizer;
            return axes;
        }

        /// <summary>Draws the name of the series added last at the end of its line, in the line's colour, instead of
        /// in the legend. Applies to a line or a streaming line (<see cref="Models.Series.IHasLabelPlacement"/>) that
        /// carries a label; when the last series cannot carry a placement, nothing changes.</summary>
        /// <remarks>The same as setting <c>LabelPlacement = SeriesLabelPlacement.LineEnd</c> on the series. Polar and 3D axes
        /// draw no end labels, so there the series keeps its legend entry.</remarks>
        /// <returns>This builder, for chaining.</returns>
        public AxesBuilder WithDirectLabel() { axes.Axes.YAxes.Left[0].LabelLastAtLineEnd(); return axes; }
    }
}
