// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Rendering.TextMeasurement;
using SkiaSharp;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Skia;

/// <summary>One text shaped in one face at one size: its advance width, and the glyph outline and the text blob made from the
/// same glyphs when each is first asked for.</summary>
internal sealed class ShapedRun
{
    /// <summary>The builder an outline is written into, one for each thread, so a label's outline costs its own string and no builder
    /// beside it; one made for an unusually long text is let go rather than kept.</summary>
    [ThreadStatic]
    private static StringBuilder? t_outline;

    private const int KeptBuilderCapacity = 64 * 1024;

    private readonly ResolvedTypeface _face;
    private readonly float _size;
    private readonly ShapedText _shaped;
    private string? _outline;
    private SKTextBlob? _blob;
    private GlyphInk _ink;
    private volatile bool _inkRead;

    private ShapedRun(ResolvedTypeface face, float size, ShapedText shaped)
    {
        _face = face;
        _size = size;
        _shaped = shaped;
    }

    /// <summary>Shapes <paramref name="text"/> in <paramref name="face"/> at <paramref name="size"/>.</summary>
    internal static ShapedRun Of(string text, ResolvedTypeface face, float size)
    {
        using var skFont = new SKFont(face.Typeface, size);
        return new ShapedRun(face, size, TextShaper.Shape(text, skFont, face.Shaper));
    }

    /// <summary>The advance width of the shaped glyphs.</summary>
    internal float Width => _shaped.Width;

    /// <summary>How the shaped glyphs stand when matplotlib's Agg backend sets them (<see cref="FaceInk"/>), worked out the first time it
    /// is asked and kept: a label measured on every frame under a matplotlib theme reads its glyphs once.</summary>
    internal GlyphInk Ink
    {
        get
        {
            // A racing thread works out the same value; the flag is written after the value, so a reader that sees it sees the value.
            if (_inkRead) return _ink;
            _ink = _face.Ink.InkOf(_shaped, _size);
            _inkRead = true;
            return _ink;
        }
    }

    /// <summary>The outline of every glyph joined at the place the shaper gave it, as SVG path data (M/L/Q/C/Z, two decimals); made once, and
    /// every thread that asks gets the same string.</summary>
    internal string Outline
    {
        get
        {
            // The first thread to finish an outline keeps it; every thread reads the one kept.
            if (_outline is null) Interlocked.CompareExchange(ref _outline, MakeOutline(), null);
            return _outline!;
        }
    }

    /// <summary>The shaped glyphs at their positions as one text blob a canvas paints, or <see langword="null"/> when the text
    /// has no glyph. Made once; a blob is immutable, so every canvas on every thread draws the same one.</summary>
    internal SKTextBlob? Blob
    {
        get
        {
            // The first thread to finish a blob keeps it; a blob a racing thread made as well is left to its finalizer.
            if (_blob is null && _shaped.Glyphs.Length > 0) Interlocked.CompareExchange(ref _blob, MakeBlob(), null);
            return _blob;
        }
    }

    private SKTextBlob MakeBlob()
    {
        using var skFont = new SKFont(_face.Typeface, _size);
        using var builder = new SKTextBlobBuilder();
        var run = builder.AllocatePositionedRun(skFont, _shaped.Glyphs.Length);
        run.SetGlyphs(_shaped.Glyphs);
        run.SetPositions(_shaped.Positions);
        return builder.Build()!;
    }

    private string MakeOutline()
    {
        using var skFont = new SKFont(_face.Typeface, _size);
        var outline = t_outline ??= new StringBuilder();
        outline.Clear();
        for (int i = 0; i < _shaped.Glyphs.Length; i++)
        {
            using var glyphPath = skFont.GetGlyphPath(_shaped.Glyphs[i]);
            // Glyph outlines from Skia are positioned with their origin at (0, 0) on the baseline; ascents are at negative
            // y, matching SVG's y-down coordinate system when the caller translates to (x, baselineY). Each is written at
            // the place the shaper gave it, as the one path data the SVG's <path d="..."> takes.
            if (glyphPath is { IsEmpty: false }) AppendSvgPathData(outline, glyphPath, _shaped.Positions[i].X, _shaped.Positions[i].Y);
        }

        string path = outline.ToString();
        if (outline.Capacity > KeptBuilderCapacity) t_outline = null;
        return path;
    }

    /// <summary>The number of quads a conic is cut into (2 to the power of this): four, which follow a conic arc of up to a quarter
    /// turn to far below the hundredth of a pixel the numbers are written to. A font's outline holds none, since TrueType and
    /// CFF outlines are quads and cubics; a path from elsewhere may.</summary>
    private const int ConicQuadPower = 2;

    /// <summary>Appends the segments of <paramref name="path"/>, each point moved by (<paramref name="dx"/>,
    /// <paramref name="dy"/>), as SVG path data: <c>M</c>, <c>L</c>, <c>Q</c>, <c>C</c> and <c>Z</c> in absolute
    /// coordinates, every number written by <c>AppendSvgNumber</c> (two decimals). A conic is written as quads. The raw
    /// segments are read, so a contour that does not end where it began is closed by its <c>Z</c> alone, with no line
    /// of its own to the start, which draws the same shape.</summary>
    /// <param name="path">The path to write.</param>
    /// <param name="dx">The shift of every point along x.</param>
    /// <param name="dy">The shift of every point along y.</param>
    /// <returns><paramref name="sb"/>, for chaining.</returns>
    /// <param name="sb">The <see cref="StringBuilder"/> it works on.</param>
    internal static StringBuilder AppendSvgPathData(StringBuilder sb, SKPath path, float dx, float dy)
    {
        using var segments = path.CreateRawIterator();
        Span<SKPoint> p = stackalloc SKPoint[4];
        SKPathVerb verb;
        while ((verb = segments.Next(p)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    AppendPoint(sb.Append('M'), p[0], dx, dy);
                    break;
                case SKPathVerb.Line:
                    AppendPoint(sb.Append('L'), p[1], dx, dy);
                    break;
                case SKPathVerb.Quad:
                    AppendPoint(AppendPoint(sb.Append('Q'), p[1], dx, dy).Append(' '), p[2], dx, dy);
                    break;
                case SKPathVerb.Conic:
                    // The first of the points is the start; each quad after it is a control point and an end.
                    var quads = SKPath.ConvertConicToQuads(p[0], p[1], p[2], segments.ConicWeight(), ConicQuadPower);
                    for (int i = 1; i + 1 < quads.Length; i += 2)
                        AppendPoint(AppendPoint(sb.Append('Q'), quads[i], dx, dy).Append(' '), quads[i + 1], dx, dy);
                    break;
                case SKPathVerb.Cubic:
                    AppendPoint(AppendPoint(AppendPoint(sb.Append('C'), p[1], dx, dy).Append(' '), p[2], dx, dy).Append(' '), p[3], dx, dy);
                    break;
                default:
                    sb.Append('Z');
                    break;
            }
        }
        return sb;
    }

    private static StringBuilder AppendPoint(StringBuilder sb, SKPoint point, float dx, float dy) =>
        sb.AppendSvgNumber(point.X + dx).Append(' ').AppendSvgNumber(point.Y + dy);
}
