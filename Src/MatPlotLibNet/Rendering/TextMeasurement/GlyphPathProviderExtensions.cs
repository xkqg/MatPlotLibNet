// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.TextMeasurement;

/// <summary>What a writer asks of any glyph path provider.</summary>
internal static class GlyphPathProviderExtensions
{
    extension(IGlyphPathProvider provider)
    {
        /// <summary>The outline of <paramref name="text"/> and the advance that places it at <paramref name="alignment"/>: the
        /// shaping's own advance from a provider that shapes, else the width the installed font metrics measure, which a
        /// left-aligned text does not need and is not measured for (it stays 0).</summary>
        /// <returns><see langword="null"/> when the provider gives no outline; the writer then writes a text element.</returns>
        internal ShapedOutline? OutlineOf(string text, Font font, TextAlignment alignment)
        {
            if (provider is IShapedGlyphPathProvider shaped) return shaped.GetOutline(text, new SpanFont(font, font.Size));
            if (provider.GetPathData(text, font) is not { } pathData) return null;
            return new ShapedOutline(pathData, alignment is TextAlignment.Left ? 0 : FontMetricsDefault.Current.ExtentOf(text, font).Width);
        }
    }
}
