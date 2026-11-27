// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Where a text stands, how it is anchored there and how far it is turned: the value the one drawing verb of a text takes
/// (<see cref="MathText.MathTextRenderContextExtensions"/>), and what a placement rule hands it (the label of a drawn line, the outer
/// label of a Sankey node, the offset text of an axis). How the text stands against its anchor across the line is part of the value
/// (<see cref="Vertical"/>), so no site works out a baseline by hand: <see cref="TextBox"/> says where the baseline stands.</summary>
/// <param name="Anchor">Pixel position at which the text is anchored.</param>
/// <param name="Alignment">Which side of the anchor the text flows toward.</param>
internal readonly record struct TextPlacement(Point Anchor, TextAlignment Alignment)
{
    /// <summary>How the text stands against <see cref="Anchor"/> across its line: on its baseline (the default), centred on it, hung
    /// from it or standing on it.</summary>
    internal TextVerticalAlignment Vertical { get; init; }

    /// <summary>The turn in degrees, counter-clockwise; nothing (0) for an upright text.</summary>
    internal double Rotation { get; init; }

    /// <summary>The same placement one line further down the picture, for the line under this text: its anchor moved down by the
    /// height of <paramref name="line"/>, the extent this text was measured as.</summary>
    /// <param name="line">The extent of the line above.</param>
    /// <returns>The placement of the next line.</returns>
    internal TextPlacement LineUnder(TextMeasurement.TextExtent line) => this with { Anchor = new Point(Anchor.X, Anchor.Y + line.Height) };

    /// <summary>The depth of the text in a tree (a row of a tree grid), 1-based, which a surface that writes markup states
    /// (<c>aria-level</c>), whether the text is plain or math; <see langword="null"/> for a text that is no row of a tree.</summary>
    internal int? Level { get; init; }

    /// <summary>The width a plain text is set to whatever font the viewer finds for it (<see cref="IRenderContext.SetNextTextLength"/>):
    /// a word of a word cloud, packed at the width it was measured at. A text of math is laid out run by run where its layout puts each,
    /// so it has no one text to fit. <see langword="null"/>: the text's own width.</summary>
    internal double? FitWidth { get; init; }
}
