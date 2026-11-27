// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using HarfBuzzSharp;
using MatPlotLibNet.Rendering.TextMeasurement;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace MatPlotLibNet.Skia;

/// <summary>How the glyphs of one face stand when matplotlib's Agg backend sets them (matplotlib 3.11, FreeType with its hinting on): a
/// glyph's advance and the top and the foot of its ink, each rounded to whole pixels at the font's size rounded to whole pixels (the
/// face asks for whole pixel sizes, bit 3 of its <c>head</c> flags), an overshoot past a zone of the face standing on the zone, except
/// the ink of a glyph the hinter leaves where it is (a simple glyph with no instructions of its own in a face hinted by its own), which
/// FreeType's grid fit stands on the pixels outside it, the top on the pixel above and the foot on the pixel below; and the
/// face's typographic ascender and descender (OS/2), the least a line of its text takes. The glyphs are read from the font file itself
/// by HarfBuzz, so every platform reads the same numbers; they are read the first time the face is measured so, never for a face only
/// ever drawn.</summary>
/// <param name="typeface">The face whose glyphs are read.</param>
/// <remarks>Measured against matplotlib 3.11.2 for DejaVu Sans: the box of a line under the default style is matplotlib's to the
/// pixel at every size from 10 to 20 points, and a glyph's advance at every size but a few glyphs at 8 and 9 points, where the
/// face's own hinting moves them further. The ink of a glyph the hinter leaves where it is is matplotlib's for 10,735 of the 10,740
/// tops and 10,736 of the feet such glyphs of DejaVu Sans have at the sizes from 8 to 24 points (all of DejaVu Sans Display's). The classic style hints by FreeType's autohinter, which
/// moves some zones and advances by up to a pixel more; it is not modelled.</remarks>
internal sealed class FaceInk(SKTypeface typeface)
{
    private readonly object _gate = new();
    private volatile Tables? _tables;

    /// <summary>The face's tables, read the first time they are asked for; a read after that takes no lock and makes nothing.</summary>
    private Tables Read => _tables ?? ReadOnce();

    private Tables ReadOnce()
    {
        lock (_gate) return _tables ??= Tables.Of(typeface);
    }

    /// <summary>How the glyphs of <paramref name="shaped"/> stand at <paramref name="size"/>.</summary>
    /// <param name="shaped">The text as shaped at <paramref name="size"/>.</param>
    /// <param name="size">The size it is shaped at, in pixels.</param>
    /// <returns>The advance and the ink of the glyphs, each as the Agg backend places it.</returns>
    internal GlyphInk InkOf(ShapedText shaped, float size)
    {
        var tables = Read;
        var band = tables.BandAt(size);
        if (shaped.Glyphs.Length == 0) return new GlyphInk(0, 0, 0, 0, band);

        double ppem = Math.Floor(size + 0.5);
        double pixel = ppem / tables.UnitsPerEm, linear = size / (double)tables.UnitsPerEm;
        double rounded = 0, unrounded = 0, rise = 0, drop = 0;
        foreach (ushort glyph in shaped.Glyphs)
        {
            int advance = tables.Font.GetHorizontalGlyphAdvance(glyph);
            rounded += WholePixels(advance * pixel);
            unrounded += advance * linear;
            if (!tables.Font.TryGetGlyphExtents(glyph, out var extents) || (extents.Width == 0 && extents.Height == 0)) continue;

            int top = extents.YBearing, foot = extents.YBearing + extents.Height;
            if (tables.KeepsItsOutline(glyph))
            {
                rise = Math.Max(rise, Math.Ceiling(OnTheGrid(top * pixel)));
                drop = Math.Max(drop, -Math.Floor(Math.Min(OnTheGrid(foot * pixel), 0)));
            }
            else
            {
                rise = Math.Max(rise, WholePixels(tables.OnZone(top) * pixel));
                drop = Math.Max(drop, -WholePixels(Math.Min(tables.OnBaseline(foot), 0) * pixel));
            }
        }

        return new GlyphInk(shaped.Width, shaped.Width + rounded - unrounded, rise, drop, band);
    }

    /// <summary>The face's typographic ascender and descender at <paramref name="size"/>.</summary>
    /// <param name="size">The size, in pixels.</param>
    /// <returns>The ascender and the descender of the face, in pixels.</returns>
    internal FontBand BandAt(double size) => Read.BandAt(size);

    /// <summary>FreeType's rounding to whole pixels: halves up.</summary>
    private static double WholePixels(double value) => Math.Floor(value + 0.5);

    /// <summary>A length in pixels as FreeType scales an outline: to the nearest 64th of a pixel (26.6 fixed point).</summary>
    private static double OnTheGrid(double value) => Math.Round(value * 64) / 64;

    /// <summary>The face's tables, in its own units: its HarfBuzz font (advances and glyph extents), its typographic ascender and
    /// descender, and the zones an overshooting glyph's ink stands on (the tops of 'x', 'H' and 'l' and the baseline).</summary>
    private sealed class Tables
    {
        /// <summary>How far past a zone an overshoot reaches, as a part of the em: DejaVu Sans' round letters overshoot by 27 to 29 of
        /// its 2048 units.</summary>
        private const double OvershootEm = 0.02;

        private readonly int[] _zones;
        private readonly double _overshoot;
        private readonly bool[]? _keepsOutline;

        private Tables(HarfBuzzSharp.Font font, int unitsPerEm, double ascender, double descender, int[] zones, bool[]? keepsOutline)
        {
            Font = font;
            UnitsPerEm = unitsPerEm;
            Ascender = ascender;
            Descender = descender;
            _zones = zones;
            _overshoot = OvershootEm * unitsPerEm;
            _keepsOutline = keepsOutline;
        }

        /// <summary>Whether FreeType leaves the outline of <paramref name="glyph"/> where it is: a simple glyph with no instructions
        /// of its own, in a face FreeType hints by the face's own instructions (<see cref="UninstructedGlyphs"/>).</summary>
        internal bool KeepsItsOutline(ushort glyph) => _keepsOutline is { } keeps && glyph < keeps.Length && keeps[glyph];

        /// <summary>The HarfBuzz font at the face's own units; lives as long as the face, as its shaper does.</summary>
        internal HarfBuzzSharp.Font Font { get; }

        internal int UnitsPerEm { get; }

        /// <summary>The typographic ascender, in units.</summary>
        internal double Ascender { get; }

        /// <summary>The typographic descender, in units, positive below the baseline.</summary>
        internal double Descender { get; }

        /// <summary>The top of a glyph's ink, on the zone it overshoots, in units.</summary>
        internal double OnZone(int top)
        {
            foreach (int zone in _zones)
                if (top > zone && top <= zone + _overshoot) return zone;
            return top;
        }

        /// <summary>The typographic ascender and descender at <paramref name="size"/>, in pixels.</summary>
        internal FontBand BandAt(double size) => new(Ascender * size / UnitsPerEm, Descender * size / UnitsPerEm);

        /// <summary>The foot of a glyph's ink, on the baseline when it overshoots it, in units.</summary>
        internal double OnBaseline(int foot) => foot < 0 && foot >= -_overshoot ? 0 : foot;

        internal static Tables Of(SKTypeface typeface)
        {
            // As SKShaper reads its face: the blob takes the stream and lets it go with itself.
            using var blob = typeface.OpenStream(out int index).ToHarfBuzzBlob();
            using var face = new Face(blob, index);
            var font = new HarfBuzzSharp.Font(face);
            int units = face.UnitsPerEm;
            font.SetScale(units, units);
            font.SetFunctionsOpenType();

            var (ascender, descender) = TypographicBand(face, font);
            var zones = new List<int> { 0 };
            foreach (char letter in "xHl")
                if (font.TryGetGlyph(letter, out uint glyph) && font.TryGetGlyphExtents(glyph, out var extents)) zones.Add(extents.YBearing);
            return new Tables(font, units, ascender, descender, [.. zones], UninstructedGlyphs(face));
        }

        /// <summary>For each glyph of a TrueType face FreeType hints by the face's own instructions, whether it is a simple glyph
        /// that has none of its own; <see langword="null"/> for a face FreeType hints by its autohinter (no instructions at all:
        /// <c>maxp</c>'s <c>maxSizeOfInstructions</c> 0, no <c>fpgm</c>, no <c>prep</c>, as <c>FT_Load_Glyph</c> in
        /// <c>src/base/ftobjs.c</c> decides) and for one with no <c>glyf</c> outlines. The hinter does not move such a glyph, and
        /// FreeType's grid fit (<c>ft_glyphslot_grid_fit_metrics</c>) then stands the top of its ink on the pixel above it and the foot
        /// on the pixel below. A composite glyph is hinted through its parts and keeps the rule of a hinted glyph.</summary>
        private static bool[]? UninstructedGlyphs(Face face)
        {
            using var maxp = face.ReferenceTable(new Tag('m', 'a', 'x', 'p'));
            using var fpgm = face.ReferenceTable(new Tag('f', 'p', 'g', 'm'));
            using var prep = face.ReferenceTable(new Tag('p', 'r', 'e', 'p'));
            var limits = maxp.AsSpan();
            bool hintsByItsOwn = (limits.Length >= 28 && BinaryPrimitives.ReadUInt16BigEndian(limits[26..]) > 0)
                || fpgm.Length > 0 || prep.Length > 0;
            if (!hintsByItsOwn) return null;

            using var head = face.ReferenceTable(new Tag('h', 'e', 'a', 'd'));
            using var locaTable = face.ReferenceTable(new Tag('l', 'o', 'c', 'a'));
            using var glyfTable = face.ReferenceTable(new Tag('g', 'l', 'y', 'f'));
            var header = head.AsSpan();
            var loca = locaTable.AsSpan();
            var glyf = glyfTable.AsSpan();
            if (header.Length < 52 || loca.Length == 0 || glyf.Length == 0) return null;

            // head's indexToLocFormat: 0 for offsets of 16 bits counted in words, 1 for offsets of 32 bits
            bool wide = BinaryPrimitives.ReadInt16BigEndian(header[50..]) != 0;
            int count = Math.Min(face.GlyphCount, loca.Length / (wide ? 4 : 2) - 1);
            var keeps = new bool[Math.Max(count, 0)];
            for (int glyph = 0; glyph < keeps.Length; glyph++)
            {
                int start = wide ? (int)BinaryPrimitives.ReadUInt32BigEndian(loca[(4 * glyph)..]) : 2 * BinaryPrimitives.ReadUInt16BigEndian(loca[(2 * glyph)..]);
                int end = wide ? (int)BinaryPrimitives.ReadUInt32BigEndian(loca[(4 * glyph + 4)..]) : 2 * BinaryPrimitives.ReadUInt16BigEndian(loca[(2 * glyph + 2)..]);
                if (start < 0 || end > glyf.Length || end - start < 12) continue;

                // A simple glyph: its contour count, its box, the last point of each contour, then the length of its instructions
                short contours = BinaryPrimitives.ReadInt16BigEndian(glyf[start..]);
                int lengthAt = start + 10 + 2 * contours;
                if (contours > 0 && lengthAt + 2 <= end) keeps[glyph] = BinaryPrimitives.ReadUInt16BigEndian(glyf[lengthAt..]) == 0;
            }

            return keeps;
        }

        /// <summary>The OS/2 table's <c>sTypoAscender</c> and <c>sTypoDescender</c> (bytes 68 to 71), as matplotlib's
        /// <c>Text._get_layout</c> reads them; the horizontal header's ascent and descent for a face without one.</summary>
        private static (double Ascender, double Descender) TypographicBand(Face face, HarfBuzzSharp.Font font)
        {
            using var os2 = face.ReferenceTable(new Tag('O', 'S', '/', '2'));
            var bytes = os2.AsSpan();
            if (bytes.Length >= 72)
                return (BinaryPrimitives.ReadInt16BigEndian(bytes[68..]), -BinaryPrimitives.ReadInt16BigEndian(bytes[70..]));

            font.TryGetHorizontalFontExtents(out var extents);
            return (extents.Ascender, -extents.Descender);
        }
    }
}
