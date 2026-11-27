// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>The glyph outlines a test hands the SVG writer: the path of a text as <paramref name="path"/> gives it, so a text is drawn
/// as an outline without a font on the machine. It does not shape; <see cref="TestShapedGlyphPaths"/> does.</summary>
/// <param name="path">The path of a text; <see langword="null"/> for a text it has no outline of.</param>
internal class TestGlyphPaths(Func<string, string?> path) : IGlyphPathProvider
{
    /// <summary>Outlines that give <paramref name="pathData"/> for every text.</summary>
    /// <param name="pathData">The path of every text.</param>
    public TestGlyphPaths(string? pathData) : this(_ => pathData) { }

    public string? GetPathData(string text, Font font) => path(text);
}

/// <summary>Glyph outlines that shape, as the Skia ones do: the outline of each span with its advance as <paramref name="outline"/> gives
/// it, keeping the fonts it was asked in.</summary>
/// <param name="path">The path of a text.</param>
/// <param name="outline">The outline of a span and its advance in a font; <see langword="null"/> for none.</param>
internal sealed class TestShapedGlyphPaths(Func<string, string?> path, Func<string, SpanFont, ShapedOutline?> outline)
    : TestGlyphPaths(path), IShapedGlyphPathProvider
{
    /// <summary>The font of every span the outlines were asked for, in order.</summary>
    public List<SpanFont> Fonts { get; } = [];

    public ShapedOutline? GetOutline(string text, SpanFont font)
    {
        Fonts.Add(font);
        return outline(text, font);
    }
}
