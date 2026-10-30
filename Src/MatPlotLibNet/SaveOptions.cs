// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>Settings for saving a figure with <c>Figure.Save(path, options)</c>. A setting reaches the file when the
/// format can use it and leaves the file alone when it cannot; each property below says which.</summary>
/// <remarks>
/// The resolution sizes a PNG. The indentation and the number of decimals shape an SVG. The title and the author go
/// into an SVG, a PNG and a PDF. A JSON document is data and uses none of them. A setting at its default asks for
/// nothing, so <c>new SaveOptions()</c> writes the same file as saving without options, and a format that cannot use
/// a setting does not complain about it: the same options can be handed to a save whatever the extension of the
/// path. A transform written by someone else receives the options only when it asks for them (see
/// <see cref="Transforms.IFigureTransform"/>).
/// </remarks>
public sealed record SaveOptions
{
    /// <summary>The resolution of a raster image in dots per inch. A figure is laid out in units of one pixel at 96 dpi,
    /// so a PNG is drawn with <c>Dpi / 96</c> pixels for each unit: a 500 by 350 figure saved at 192 dpi is a 1000 by 700
    /// PNG with the same layout, text and line widths, drawn finer. The pixel count is rounded to a whole number.
    /// <para>The default, 96, asks for nothing and leaves the resolution of the figure itself (<see cref="Figure.Dpi"/>,
    /// set with <c>WithDpi</c>) in force; any other value overrides the figure's for this save. A resolution below 1
    /// is refused when the image is drawn.</para>
    /// <para>An SVG and a PDF page are vector drawings with no pixels to count and a JSON document is data, so the
    /// resolution changes none of them. The frames of an animated GIF are not saved through these options; they
    /// are drawn at the <see cref="Figure.Dpi"/> of each frame.</para></summary>
    public int Dpi { get; init; } = Figure.ReferenceDpi;

    /// <summary>Whether an SVG is written indented, one element to a line with each nested element two spaces further
    /// in, instead of the compact layout. The drawing is the same document and text inside an element gets no extra
    /// space. Used by an SVG only; a PNG, a PDF page and JSON have nothing to indent.</summary>
    public bool PrettifySvg { get; init; }

    /// <summary>The number of decimals kept in the coordinates of an SVG, from 0 (whole numbers) to 15, rounding half
    /// away from zero: <c>0.25</c> at one decimal is <c>0.3</c>. It applies to positions and sizes: the attributes
    /// x, y, cx, cy, r, rx, ry, width, height, the path data, the points of a polygon, transforms and the view box.
    /// Stroke widths, dash lengths, opacities and font sizes keep the digits they were written with, since rounding
    /// them would change how the chart looks rather than where it draws; so do the text of labels, the data-*
    /// attributes and the script of an interactive chart. <see langword="null"/>, the default, keeps every digit. A
    /// number outside 0 to 15 is refused. Used by an SVG only; a PNG, a PDF page and JSON have no SVG numbers.</summary>
    public int? SvgDecimalPrecision { get; init; }

    /// <summary>A prefix put in front of every id an SVG defines and in front of every reference to one (<c>id</c>,
    /// <c>url(#id)</c>, <c>href="#id"</c> and the <c>aria-labelledby</c> and <c>aria-describedby</c> lists), so several
    /// figures can share one HTML page without sharing an id: give each figure its own, <c>"revenue-"</c> and
    /// <c>"costs-"</c>. A figure's own subplots never share an id whatever this is. <see langword="null"/> or an empty
    /// string, the default, changes nothing. The prefix has to be a valid XML name (a letter or underscore first, then
    /// letters, digits, <c>-</c>, <c>_</c> or <c>.</c>) and anything else is refused. Used by an SVG only.</summary>
    public string? SvgIdPrefix { get; init; }

    /// <summary>The title of the document. In an SVG it is the Dublin Core title in the <c>metadata</c> element, not the
    /// <c>title</c> element, which stays the accessible name of the chart; in a PDF it is the Title of the document
    /// information; in a PNG it is a text chunk with the keyword Title. Not used by JSON. <see langword="null"/> or an
    /// empty string writes none.</summary>
    public string? Title { get; init; }

    /// <summary>The author of the document: the Dublin Core creator in the <c>metadata</c> element of an SVG, the Author
    /// of the document information of a PDF, a text chunk with the keyword Author in a PNG. Not used by JSON.
    /// <see langword="null"/> or an empty string writes none.</summary>
    public string? Author { get; init; }
}
