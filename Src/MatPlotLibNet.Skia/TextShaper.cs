// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using HarfBuzzSharp;
using MatPlotLibNet.Rendering.Text;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using Buffer = HarfBuzzSharp.Buffer;

namespace MatPlotLibNet.Skia;

/// <summary>Turns a string into the glyphs a font draws for it. The bidi algorithm in Core says which direction each
/// character reads in; HarfBuzz's own tables say which script it belongs to; the text is cut into runs where either
/// changes, each run is shaped in its own script and direction (so Arabic letters join and Latin pairs kern), and the
/// runs are laid out in visual order. matplotlib does the same through libraqm.</summary>
internal static class TextShaper
{
    /// <summary>One stretch of text that shares a direction and a script.</summary>
    private readonly record struct Run(int Start, int Length, byte Level, Script Script);

    /// <summary>Shapes <paramref name="text"/> with <paramref name="font"/> (which carries the size) through
    /// <paramref name="shaper"/> (which carries the typeface).</summary>
    public static ShapedText Shape(string text, SKFont font, SKShaper shaper)
    {
        if (string.IsNullOrEmpty(text))
        {
            return ShapedText.Empty;
        }

        List<Run> runs = Itemize(text);
        InVisualOrder(runs);
        var glyphs = new List<ushort>(text.Length);
        var positions = new List<SKPoint>(text.Length);
        float cursor = 0f;
        foreach (var run in runs)
        {
            using var buffer = new Buffer();
            buffer.AddUtf16(text.Substring(run.Start, run.Length));
            buffer.Direction = (run.Level & 1) == 1 ? Direction.RightToLeft : Direction.LeftToRight;
            buffer.Script = run.Script;
            buffer.Language = Language.Default;

            var shaped = shaper.Shape(buffer, font);
            for (int i = 0; i < shaped.Codepoints.Length; i++)
            {
                glyphs.Add((ushort)shaped.Codepoints[i]);
                positions.Add(new SKPoint(shaped.Points[i].X + cursor, shaped.Points[i].Y));
            }

            cursor += shaped.Width;
        }

        return new ShapedText([.. glyphs], [.. positions], cursor);
    }

    /// <summary>Cuts the text into runs of one direction and one script. Punctuation, spaces and digits carry no
    /// script of their own and stay with the run they sit in; a run that opens with such characters takes the
    /// script of the first real letter after them.</summary>
    private static List<Run> Itemize(string text)
    {
        byte[] levels = BidiAlgorithm.Resolve(text).RenderLevels();
        var runs = new List<Run>();
        int runStart = 0;
        byte runLevel = levels[0];
        Script runScript = Script.Common;

        for (int i = 0; i < text.Length; i += text.UnitsAt(i))
        {
            int at = i;
            int codePoint = text.CodePointAt(ref at);
            Script script = UnicodeFunctions.Default.GetScript(codePoint);
            bool neutral = script == Script.Common || script == Script.Inherited || script == Script.Unknown;
            bool scriptChanges = !neutral && runScript != Script.Common && script != runScript;
            if (levels[i] != runLevel || scriptChanges)
            {
                runs.Add(new Run(runStart, i - runStart, runLevel, runScript));
                runStart = i;
                runLevel = levels[i];
                runScript = Script.Common;
            }

            if (!neutral && runScript == Script.Common)
            {
                runScript = script;
            }
        }

        runs.Add(new Run(runStart, text.Length - runStart, runLevel, runScript));
        return runs;
    }

    /// <summary>Puts the runs in visual order in place, by rule L2 of the bidi algorithm (<see cref="BidiAlgorithm.ReverseByLevels"/>).</summary>
    private static void InVisualOrder(List<Run> runs)
    {
        Span<byte> levels = runs.Count <= 64 ? stackalloc byte[runs.Count] : new byte[runs.Count];
        for (int k = 0; k < runs.Count; k++) levels[k] = runs[k].Level;
        BidiAlgorithm.ReverseByLevels(CollectionsMarshal.AsSpan(runs), levels);
    }
}
