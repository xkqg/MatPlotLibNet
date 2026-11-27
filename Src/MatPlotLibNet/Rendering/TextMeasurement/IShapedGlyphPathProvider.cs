// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>A glyph path provider that makes its outlines from a shaping of the text, and hands out the advance of that shaping
/// with each outline, so a writer places a centred or right-aligned outline without measuring the text again. The Skia
/// package's provider is one. A writer reaches it through <see cref="GlyphPathProviderExtensions"/> on any
/// <see cref="IGlyphPathProvider"/>.</summary>
/// <remarks>A separate internal interface, not a member of the public <see cref="IGlyphPathProvider"/>, so that an application
/// can still implement, proxy or mock the public interface.</remarks>
internal interface IShapedGlyphPathProvider : IGlyphPathProvider
{
    /// <summary>The outline of <paramref name="text"/> in the face of <paramref name="font"/> at its size, and the advance of the glyphs
    /// it is made of, or <see langword="null"/> where <see cref="IGlyphPathProvider.GetPathData"/> gives none. The face and the size come
    /// apart, so a piece of a rich text set at its span's size is outlined without a font being made for it.</summary>
    /// <param name="text">The text.</param>
    /// <param name="font">The face and the size the text is set in.</param>
    /// <returns>The outline and the advance, or <see langword="null"/> when no outline is available.</returns>
    ShapedOutline? GetOutline(string text, SpanFont font);
}
