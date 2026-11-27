// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>The band the figure's title takes at the top: a pad above it, the title as measured, a pad below it. The one owner
/// of the two pads and of how the title is measured: <see cref="ChartRenderer"/> draws the title and starts the first row of
/// subplots by it, and <see cref="ConstrainedLayoutEngine"/> reserves the top margin by it, so the layout cannot reserve one band
/// and the drawing use another.</summary>
internal static class SupTitleBand
{
    /// <summary>The pixels above the title.</summary>
    internal const double TopPad = 8;

    /// <summary>The pixels between the title and what lies below it.</summary>
    internal const double BottomPad = 12;

    /// <summary>The height of the whole band for a title that measures <paramref name="titleHeight"/>.</summary>
    /// <param name="titleHeight">The height of the title as measured, in pixels.</param>
    /// <returns>The pad above, the title and the pad below.</returns>
    internal static double Needed(double titleHeight) => TopPad + titleHeight + BottomPad;

    extension(Figure figure)
    {
        /// <summary>The font the figure's title is drawn in: the theme's figure-title font with the title's style
        /// (<see cref="Figure.TitleStyle"/>) laid over it.</summary>
        internal Font TitleFont => figure.TitleStyle is { } style ? style.ApplyTo(figure.Theme.SupTitleFont()) : figure.Theme.SupTitleFont();

        /// <summary>The height of the figure's title as it is drawn, in its font, a title written as math typeset as math; 0
        /// without a title.</summary>
        /// <param name="ctx">The surface the text is measured with.</param>
        internal double TitleHeight(IRenderContext ctx) =>
            figure.Title is { } title ? ctx.ExtentOf(TextLabel.Of(title), figure.TitleFont).Height : 0;

        /// <summary>The band the figure's title takes for a title that measures <paramref name="titleHeight"/>: the pads around
        /// it (<see cref="Needed"/>) and the extra room its style keeps below it (<see cref="TextStyle.Pad"/>).</summary>
        /// <param name="titleHeight">The height of the title as measured, in pixels.</param>
        internal double TitleBand(double titleHeight) => Needed(titleHeight) + (figure.TitleStyle?.Pad ?? 0);
    }
}
