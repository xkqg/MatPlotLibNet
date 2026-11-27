// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>One line of a box plot (the box's outline, its median, its whiskers) where its author names nothing: its colour, its width and
/// its dash.</summary>
/// <param name="Color">The colour of the line; <see langword="null"/> draws it in the colour the box is drawn in (the series' own, or
/// the group's).</param>
/// <param name="Width">The width of the line, in pixels.</param>
/// <param name="Style">The dash of the line.</param>
internal readonly record struct BoxStroke(Color? Color, double Width, LineStyle Style = LineStyle.Solid)
{
    /// <summary>The stroke this line is drawn with: the colour its author named, else this look's, else the box's, at this look's width
    /// and dash. The one place a line of a box becomes a stroke.</summary>
    /// <param name="named">The colour the series names for the line; <see langword="null"/> when it names none.</param>
    /// <param name="box">The colour the box is drawn in, which a line whose author and look name none wears.</param>
    /// <returns>The stroke.</returns>
    internal StrokeStyle StrokeOver(Color? named, Color box) => StrokeStyle.Of(ColorOver(named, box), Width, Style);

    /// <summary>The outline of the box drawn by this line: its colour by the rule of <see cref="StrokeOver"/>, this look's width, no fill.</summary>
    /// <param name="named">The colour the series names for the box; <see langword="null"/> when it names none.</param>
    /// <param name="box">The colour the box is drawn in.</param>
    /// <returns>The outline.</returns>
    internal ShapeStyle OutlineOver(Color? named, Color box) => ShapeStyle.Outlined(ColorOver(named, box), Width);

    /// <summary>The colour its author named, else this look's, else the box's.</summary>
    private Color ColorOver(Color? named, Color box) => named ?? Color ?? box;
}
