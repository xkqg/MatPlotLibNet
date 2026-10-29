// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>The shape of a legend swatch, drawn by the legend from what a series says about itself.</summary>
internal enum LegendSwatchKind
{
    /// <summary>A filled rectangle: bars, histograms, areas, violins, pies and every series that draws patches.</summary>
    Patch,

    /// <summary>A horizontal line, stroked the way the slot strokes the series.</summary>
    Line,

    /// <summary>A horizontal line with the series' marker at its middle, when the series or the slot names one.</summary>
    LineMarker,

    /// <summary>A solid horizontal line whose width alone is the slot's: a sparkline is always solid.</summary>
    SolidLine,

    /// <summary>One marker, as a scatter point is drawn.</summary>
    Marker,

    /// <summary>A line with a cap at each end: an error bar.</summary>
    ErrorBar,

    /// <summary>Side-by-side stripes, one for each distinct state of the series, in the fill the series draws that state
    /// with: a state timeline.</summary>
    Stripes,
}

/// <summary>How one series is drawn in its legend entry. The series states its own values (its line width and style, its
/// marker); the legend applies what the colour-cycle slot decides to them (<see cref="Rendering.SeriesSlot"/>) and draws
/// the shape, so the swatch cannot disagree with the series and the legend holds no list of series types.</summary>
/// <param name="Kind">The shape of the swatch.</param>
/// <param name="LineWidth">The line width the series holds (the stroke of a line, the width of an error bar's lines).</param>
/// <param name="LineStyle">The line style the series holds.</param>
/// <param name="RestingLineWidth">The width a new series of this kind holds; the slot's cycler names another only for a
/// series that holds this one (<see cref="Rendering.SeriesSlot.LineWidthOf"/>).</param>
/// <param name="Marker">The marker the series names, or <see langword="null"/> when it names none.</param>
/// <param name="MarkerSize">The marker's area in square points.</param>
/// <param name="States">The states a <see cref="LegendSwatchKind.Stripes"/> swatch shows, in the order the series holds them;
/// the legend draws each distinct fill once (<see cref="Rendering.SeriesRenderers.StateSegmentExtensions.DistinctFills"/>).</param>
internal readonly record struct LegendSwatch(
    LegendSwatchKind Kind,
    double LineWidth = 1.5,
    LineStyle LineStyle = LineStyle.Solid,
    double RestingLineWidth = 1.5,
    MarkerStyle? Marker = null,
    double MarkerSize = 0,
    IReadOnlyList<StateSegment>? States = null)
{
    /// <summary>A filled rectangle, the swatch of every series that says nothing else.</summary>
    internal static LegendSwatch Patch => new(LegendSwatchKind.Patch);
}
