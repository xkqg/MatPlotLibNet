// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>A drawn annotation line: a reference line, a span's border, a trendline, a horizontal level, a Fibonacci retracement and a
/// zoom indicator. Each names a colour (or none), a width and a dash; what it is drawn in where it names no colour, and how wide, is
/// its kind's (a value, never a branch in a renderer): the theme's neutral and the width it holds, unless the kind says otherwise.
/// The stroke itself is made in one place (<see cref="StrokedToolExtensions"/>' <c>StrokeUnder</c>).</summary>
/// <remarks>An internal interface beside the public models, so their public surface is unchanged.</remarks>
internal interface IStrokedTool
{
    /// <summary>The colour the line names; <see langword="null"/> for the one its kind takes (<see cref="UnnamedColorUnder"/>).</summary>
    Color? Color { get; }

    /// <summary>The width the line holds, in pixels.</summary>
    double LineWidth { get; }

    /// <summary>The dash pattern of the line.</summary>
    LineStyle LineStyle { get; }

    /// <summary>The colour the line is drawn in where it names none: the theme's neutral.</summary>
    /// <param name="theme">The theme the line is drawn under.</param>
    /// <returns>The colour.</returns>
    Color UnnamedColorUnder(Theme theme) => theme.Neutral;

    /// <summary>How wide the line is drawn: as wide as it holds.</summary>
    /// <param name="theme">The theme the line is drawn under.</param>
    /// <returns>The width, in pixels.</returns>
    double WidthUnder(Theme theme) => LineWidth;
}

/// <summary>A tool a viewer draws on an axes and takes off again (a trendline, a horizontal level, a Fibonacci retracement): each names
/// the one list of the axes' overlays it is held in. The other lines drawn as a tool is (a reference line, a span, a zoom indicator)
/// are no tools, so an event that takes a tool off leaves them standing.</summary>
internal interface IDrawingTool : IStrokedTool
{
    /// <summary>Puts this tool in its list of <paramref name="overlays"/>.</summary>
    void AddTo(AxesOverlays overlays);

    /// <summary>Takes this tool out of its list of <paramref name="overlays"/>; nothing happens when it is not there.</summary>
    void RemoveFrom(AxesOverlays overlays);
}

/// <summary>The one place a drawn annotation line becomes a stroke.</summary>
internal static class StrokedToolExtensions
{
    extension<TTool>(TTool tool) where TTool : IStrokedTool
    {
        /// <summary>The colour the line and its label are drawn in under <paramref name="theme"/>: the colour it names, else the one its
        /// kind takes.</summary>
        /// <param name="theme">The theme the line is drawn under.</param>
        /// <returns>The colour.</returns>
        internal Color ColorUnder(Theme theme) => tool.Color ?? tool.UnnamedColorUnder(theme);

        /// <summary>The stroke the line is drawn with under <paramref name="theme"/>: its colour (<see cref="ColorUnder"/>), its width as
        /// its kind draws it and its dash.</summary>
        /// <param name="theme">The theme the line is drawn under.</param>
        /// <returns>The stroke.</returns>
        internal StrokeStyle StrokeUnder(Theme theme) => StrokeStyle.Of(tool.ColorUnder(theme), tool.WidthUnder(theme), tool.LineStyle);
    }
}
