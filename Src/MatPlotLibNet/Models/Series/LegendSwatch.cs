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

    /// <summary>One marker, drawn as it is, whatever its theme says (a Piper diagram's).</summary>
    Marker,

    /// <summary>A line with a cap at each end: an error bar.</summary>
    ErrorBar,

    /// <summary>Side-by-side stripes, one for each distinct state of the series, in the fill the series draws that state
    /// with: a state timeline.</summary>
    Stripes,

    /// <summary>One marker, as a scatter point is drawn: its size, colour and edge where its series names none are the theme's
    /// (<see cref="Styling.ThemeLooks.Scatter"/>).</summary>
    Scatter,

    /// <summary>One marker, as a 3-D scatter point is drawn: the marker the series names, at the size
    /// <see cref="Styling.ThemeLooks.Scatter"/> gives a 3-D scatter (<see cref="Rendering.SeriesSlot.Scatter3DSizeOf"/>), which is not a
    /// 2-D scatter's.</summary>
    Scatter3D,

    /// <summary>A filled rectangle with the edge the series draws its patches with: under the matplotlib themes the theme's rule
    /// (<see cref="Styling.PatchLook.EdgeOver"/>) over the edge colour and width the series was given, as matplotlib's <c>HandlerPatch</c>
    /// copies the edge of the patch it stands for; under the library's own themes no edge, as <see cref="Patch"/>.</summary>
    EdgedPatch,
}

/// <summary>How one series is drawn in its legend entry. The series hands the values it holds for its own drawing (the stroke a line
/// was given, its marker; an edged patch's edge the legend reads from the series, its one owner); the legend applies what the colour-cycle slot and the theme decide to them
/// (<see cref="Rendering.SeriesSlot"/>, <see cref="Styling.PatchLook.EdgeOver"/>) and draws the shape, so the swatch cannot disagree
/// with the series and the legend holds no list of series types.</summary>
/// <param name="Kind">The shape of the swatch.</param>
/// <param name="Stroke">The stroke the series was given (the width and dash of a line, the width of an error bar's lines); each part
/// <see langword="null"/> while it was given none and the slot decides.</param>
/// <param name="WidthRule">The rule the series' kind draws its line width by, where it is not a line's (a regression line's own 2
/// beats the theme's); <see langword="null"/> for a line (<see cref="LineRule"/>).</param>
/// <param name="Marker">The marker the series names, or <see langword="null"/> when it names none.</param>
/// <param name="MarkerSize">The marker's size across, in points: the one size every marker is drawn at, a scatter's area in square
/// points having been taken to its square root; for a scatter, <see langword="null"/> when it was given none and its theme decides
/// (<see cref="Rendering.SeriesSlot.ScatterSizeOf"/>).</param>
/// <param name="MarkerFace">The fill the series named for its markers, or <see langword="null"/> when it named none and they wear the
/// colour of its line.</param>
/// <param name="MarkerEdge">The outline colour the series named for its markers, or <see langword="null"/> when it named none.</param>
/// <param name="MarkerEdgeWidth">The outline width the series named for its markers, in pixels, or <see langword="null"/> when it named
/// none and the theme decides (<see cref="Rendering.SeriesSlot.LineMarkerStyleOf"/>).</param>
/// <param name="CapSize">The reach of an error bar's caps the series was given, in pixels; <see langword="null"/> when it was given
/// none and its theme decides (<see cref="Styling.SeriesLineLook.ErrorBarCapSize"/>).</param>
/// <param name="States">The states a <see cref="LegendSwatchKind.Stripes"/> swatch shows, in the order the series holds them;
/// the legend draws each distinct fill once (<see cref="Rendering.SeriesRenderers.StateSegmentExtensions.DistinctFills"/>).</param>
internal readonly record struct LegendSwatch(
    LegendSwatchKind Kind,
    GivenStroke Stroke = default,
    Styling.LineWidthRule? WidthRule = null,
    MarkerStyle? Marker = null,
    double? MarkerSize = null,
    IReadOnlyList<StateSegment>? States = null,
    double? CapSize = null,
    Color? MarkerFace = null,
    Color? MarkerEdge = null,
    double? MarkerEdgeWidth = null)
{
    /// <summary>A filled rectangle, the swatch of every series that says nothing else.</summary>
    internal static LegendSwatch Patch => new(LegendSwatchKind.Patch);

    /// <summary>The rule the swatch's line width is drawn by: the series kind's (<see cref="WidthRule"/>), else a line's.</summary>
    internal Styling.LineWidthRule LineRule => WidthRule ?? Styling.LineWidthRule.Line;

    /// <summary>A line stroked as the series' line is, from the stroke it was given: the one handle of a series drawn as a line with
    /// no marker of its own.</summary>
    /// <param name="stroke">The stroke the series was given.</param>
    /// <returns>The swatch.</returns>
    internal static LegendSwatch Line(GivenStroke stroke) => new(LegendSwatchKind.Line, stroke);

    /// <summary>A line stroked as the line of <paramref name="series"/> is: the stroke it was given, under its kind's width rule.</summary>
    /// <param name="series">The kind that states its given stroke and its rule.</param>
    /// <returns>The swatch.</returns>
    internal static LegendSwatch Line(Styling.IGivenStroked series) => new(LegendSwatchKind.Line, series.Stroke, series.WidthRule);

    /// <summary>A swatch of <paramref name="kind"/> whose line is as wide as the line of <paramref name="series"/>: the width it was given,
    /// under its kind's width rule.</summary>
    /// <param name="kind">What the swatch draws.</param>
    /// <param name="series">The kind that states its given width and its rule.</param>
    /// <returns>The swatch.</returns>
    internal static LegendSwatch Of(LegendSwatchKind kind, Styling.IWidthGiven series) =>
        new(kind, new GivenStroke(series.GivenLineWidth, null), series.WidthRule);

    /// <summary>A filled rectangle edged as the patches of the series it stands for are: the legend reads the one edge the series gives
    /// them (<see cref="Styling.IEdgedPatch.GivenEdge"/>) from the series itself, so the handle holds no copy of it.</summary>
    internal static LegendSwatch EdgedPatch => new(LegendSwatchKind.EdgedPatch);
}
