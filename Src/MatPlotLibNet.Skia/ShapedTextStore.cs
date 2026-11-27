// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Skia;

/// <summary>Every text the Skia package has shaped, by what its shaping depends on: the face and shaper the font resolved to, the
/// size Skia draws it at, and the text. The width the layout measures (<see cref="SkiaFontMetrics"/>), the outline the vector
/// writer draws (<see cref="SkiaGlyphPathProvider"/>) and the glyphs a canvas paints (<see cref="SkiaRenderContext"/>) are read
/// from the one shaping kept here, so a label measured by every pass of a layout and drawn on every frame is shaped once.</summary>
/// <remarks>A chart redrawn live asks for the same few hundred labels on every draw; a label that changes all the time (a clock)
/// adds one shaping per new text, and starting over with an empty store once <see cref="Capacity"/> is reached is what bounds
/// that. The store is replaced whole when full, never cleared in place, so a draw on another thread reads either the old store
/// or the new one.</remarks>
internal sealed class ShapedTextStore
{
    /// <summary>How many shapings one store keeps before it starts over with an empty one.</summary>
    internal const int Capacity = 4096;

    /// <summary>The shapings the package keeps for the life of the process: the font metrics and the glyph outlines it installs
    /// and every <see cref="SkiaRenderContext"/> an application makes read them.</summary>
    internal static ShapedTextStore Shared { get; } = new();

    /// <summary>What a shaping depends on. Not the colour, which is no part of a shape. A registration resolves every family
    /// again with a new shaper, so a shaping made before it is never handed out after it.</summary>
    private readonly record struct Key(ResolvedTypeface Face, float Size, string Text);

    private volatile ConcurrentDictionary<Key, ShapedRun> _kept = new();

    /// <summary>The number of shapings kept now.</summary>
    internal int Count => _kept.Count;

    /// <summary>The shaping of <paramref name="text"/> in <paramref name="font"/>: the one kept, or one made now and kept.</summary>
    /// <param name="text">The text; an empty one is no glyphs and no width.</param>
    /// <param name="font">The font; its family, weight, style and size decide the shaping.</param>
    internal ShapedRun Of(string text, Font font) => Of(text, SkiaFonts.Resolve(font), (float)font.Size);

    /// <summary>The shaping of <paramref name="text"/> in the face <paramref name="font"/> resolved to, at
    /// <paramref name="size"/>: a span of rich text, which is drawn at its own size in the face of its font.</summary>
    /// <param name="text">The text; an empty one is no glyphs and no width.</param>
    /// <param name="font">The face and shaper the font resolved to.</param>
    /// <param name="size">The size Skia draws it at.</param>
    internal ShapedRun Of(string text, ResolvedTypeface font, float size)
    {
        var key = new Key(font, size, text);
        var kept = _kept;
        if (kept.TryGetValue(key, out var run)) return run;

        var made = ShapedRun.Of(text, font, size);
        if (kept.Count >= Capacity)
        {
            kept = new ConcurrentDictionary<Key, ShapedRun>();
            _kept = kept;
        }

        return kept.GetOrAdd(key, made);
    }
}
