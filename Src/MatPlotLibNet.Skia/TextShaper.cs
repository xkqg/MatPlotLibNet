// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        var glyphs = new List<ushort>(text.Length);
        var positions = new List<SKPoint>(text.Length);
        float cursor = 0f;
        foreach (var run in VisualOrder(runs))
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

        for (int i = 0; i < text.Length;)
        {
            int length = 1;
            int codePoint = text[i];
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                length = 2;
            }

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

            i += length;
        }

        runs.Add(new Run(runStart, text.Length - runStart, runLevel, runScript));
        return runs;
    }

    /// <summary>Rule L2 of UAX #9 applied to runs: from the highest level down to the lowest odd one, every stretch
    /// of runs at that level or higher is reversed.</summary>
    private static List<Run> VisualOrder(List<Run> runs)
    {
        byte highest = 0;
        byte lowestOdd = byte.MaxValue;
        foreach (var run in runs)
        {
            highest = Math.Max(highest, run.Level);
            if ((run.Level & 1) == 1)
            {
                lowestOdd = Math.Min(lowestOdd, run.Level);
            }
        }

        var ordered = new List<Run>(runs);
        for (int level = highest; lowestOdd != byte.MaxValue && level >= lowestOdd; level--)
        {
            for (int k = 0; k < ordered.Count; k++)
            {
                if (ordered[k].Level < level)
                {
                    continue;
                }

                int end = k;
                while (end + 1 < ordered.Count && ordered[end + 1].Level >= level)
                {
                    end++;
                }

                ordered.Reverse(k, end - k + 1);
                k = end;
            }
        }

        return ordered;
    }
}
