// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>The one owner of how a legend writes a series' value (<see cref="Models.Legend.LegendValues"/>). A legend row
/// asks it for the text of a series row, and an end-of-line label (<see cref="Layout.DirectLabelMetrics"/>) asks it to append a
/// value, so the two write a value the same way.</summary>
internal static class LegendValueTextExtensions
{
    extension(string baseLabel)
    {
        /// <summary>The legend-values format: <paramref name="baseLabel"/> followed by " = " and <paramref name="value"/> with
        /// two decimals in the invariant culture.</summary>
        /// <param name="value">The value to append.</param>
        /// <returns>The label with its value.</returns>
        internal string WithLegendValue(double value) =>
            baseLabel + " = " + value.InvariantText("F2");
    }

    extension(ISeries series)
    {
        /// <summary>The display label of a legend row for <paramref name="series"/>. When <paramref name="legendValues"/> is
        /// <see langword="true"/> and the series is an <see cref="XYSeries"/> with a point that can be drawn, the label ends with
        /// the value of the end of its line: the last such point (<see cref="MissingValueExtensions.LastDrawableIndex"/>, the rule
        /// the end-of-line label stands by), so a trailing gap never writes NaN. Any other series keeps its label.</summary>
        /// <param name="baseLabel">The label of the row before any value is appended.</param>
        /// <param name="legendValues">Whether the legend shows values (<see cref="Models.Legend.LegendValues"/>).</param>
        /// <returns>The text the row prints.</returns>
        internal string LegendLabel(string baseLabel, bool legendValues)
        {
            if (!legendValues) return baseLabel;
            if (series is XYSeries xySeries && xySeries.XData.LastDrawableIndex(xySeries.YData) is var end and >= 0)
                return baseLabel.WithLegendValue(xySeries.YData[end]);
            return baseLabel;
        }
    }
}
