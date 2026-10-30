// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Configures the margins and gaps between subplots in a figure.</summary>
public sealed record SubPlotSpacing
{
    /// <summary>The space between the left edge of the figure and the subplots, in pixels. Default 60.</summary>
    public double MarginLeft { get; init; } = 60;

    /// <summary>The space between the right edge of the figure and the subplots, in pixels. Default 20.</summary>
    public double MarginRight { get; init; } = 20;

    /// <summary>The space between the top edge of the figure and the subplots, in pixels; a figure title that needs more
    /// room pushes the subplots further down. Default 40.</summary>
    public double MarginTop { get; init; } = 40;

    /// <summary>The space between the bottom edge of the figure and the subplots, in pixels. Default 50.</summary>
    public double MarginBottom { get; init; } = 50;

    /// <summary>The space between neighbouring columns of subplots, in pixels. Default 40.</summary>
    public double HorizontalGap { get; init; } = 40;

    /// <summary>The space between neighbouring rows of subplots, in pixels. Default 40.</summary>
    public double VerticalGap { get; init; } = 40;

    /// <summary>When true, the margins and gaps are worked out from the measured size of the tick labels, axis titles and
    /// legends instead of being taken from this record, so nothing is clipped and no space is wasted. Off by default.</summary>
    public bool TightLayout { get; init; }

    /// <summary>When true, the margins and gaps are worked out from the measured size of the tick labels, axis titles and
    /// legends instead of being taken from this record, so nothing overlaps or is clipped; inset axes are then placed within
    /// the inner data area, clear of the labels and ticks. Off by default.</summary>
    public bool ConstrainedLayout { get; init; }

    // --- Fractional-margin support ---

    /// <summary>
    /// When <see langword="true"/> the Fract* fields hold fractional values (0–1) relative to the
    /// figure width/height.  Call <see cref="Resolve"/> to convert to an absolute
    /// <see cref="SubPlotSpacing"/> before layout computation.
    /// </summary>
    public bool IsFractional { get; init; }

    /// <summary>The left margin as a fraction of the figure width, from 0 to 1. Read only while <see cref="IsFractional"/> is on.</summary>
    public double FractLeft   { get; init; }
    /// <summary>The right margin as a fraction of the figure width, from 0 to 1. Read only while <see cref="IsFractional"/> is on.</summary>
    public double FractRight  { get; init; }
    /// <summary>The top margin as a fraction of the figure height, from 0 to 1. Read only while <see cref="IsFractional"/> is on.</summary>
    public double FractTop    { get; init; }
    /// <summary>The bottom margin as a fraction of the figure height, from 0 to 1. Read only while <see cref="IsFractional"/> is on.</summary>
    public double FractBottom { get; init; }

    /// <summary>
    /// Creates a <see cref="SubPlotSpacing"/> whose margins are expressed as fractions of the
    /// figure dimensions.  Use this for themes that should scale with figure size (e.g. the
    /// Matplotlib themes whose default subplot params are <c>left=0.125, right=0.1,
    /// top=0.12, bottom=0.12</c>).
    /// </summary>
    /// <param name="left">Left margin as a fraction of figure width (e.g. 0.125).</param>
    /// <param name="right">Right margin as a fraction of figure width (e.g. 0.10).</param>
    /// <param name="top">Top margin as a fraction of figure height (e.g. 0.12).</param>
    /// <param name="bottom">Bottom margin as a fraction of figure height (e.g. 0.12).</param>
    /// <param name="horizontalGap">Gap between columns in pixels (default 40).</param>
    /// <param name="verticalGap">Gap between rows in pixels (default 40).</param>
    /// <returns>A fractional <see cref="SubPlotSpacing"/> sentinel.</returns>
    public static SubPlotSpacing FromFractions(
        double left, double right, double top, double bottom,
        double horizontalGap = 40, double verticalGap = 40) =>
        new()
        {
            IsFractional  = true,
            FractLeft     = left,
            FractRight    = right,
            FractTop      = top,
            FractBottom   = bottom,
            HorizontalGap = horizontalGap,
            VerticalGap   = verticalGap,
        };

    /// <summary>
    /// Returns an absolute <see cref="SubPlotSpacing"/> with pixel margins computed from the
    /// figure dimensions.  If <see cref="IsFractional"/> is <see langword="false"/> returns
    /// <c>this</c> unchanged.
    /// </summary>
    /// <param name="figureWidth">Figure width in pixels.</param>
    /// <param name="figureHeight">Figure height in pixels.</param>
    /// <returns>Absolute <see cref="SubPlotSpacing"/> ready for layout.</returns>
    public SubPlotSpacing Resolve(double figureWidth, double figureHeight) =>
        IsFractional
            ? this with
            {
                IsFractional  = false,
                MarginLeft    = Math.Round(figureWidth  * FractLeft),
                MarginRight   = Math.Round(figureWidth  * FractRight),
                MarginTop     = Math.Round(figureHeight * FractTop),
                MarginBottom  = Math.Round(figureHeight * FractBottom),
            }
            : this;
}
