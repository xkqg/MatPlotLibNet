// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The tree of a flat view (<see cref="RichText"/>) a caller made by hand and handed a surface to draw (a parse's own view
/// holds the tree it was written from, <see cref="RichText.Box"/>): each kind of span read as the construction the parse makes for
/// it. A span of normal text is set at its own size; every other kind at the size its construction sets it at.</summary>
internal static class MathBoxes
{
    /// <summary>The tree of <paramref name="rich"/>.</summary>
    /// <param name="rich">The rich text.</param>
    /// <returns>The label's tree.</returns>
    internal static LineBox Of(RichText rich)
    {
        if (rich.Box is { } box) return box;

        var spans = rich.Spans;
        var line = new List<MathBox>(spans.Count);
        for (int i = 0; i < spans.Count; i++)
        {
            var span = spans[i];
            switch (span.Kind)
            {
                case TextSpanKind.Superscript or TextSpanKind.Subscript or TextSpanKind.OperatorSuperscript or TextSpanKind.OperatorSubscript:
                    var nucleus = line.Count > 0 ? line[^1] : RowBox.Empty;
                    if (line.Count > 0) line.RemoveAt(line.Count - 1);
                    MathBox? sup = null, sub = null;
                    while (i < spans.Count && IsScript(spans[i].Kind))
                    {
                        if (spans[i].Kind is TextSpanKind.Superscript or TextSpanKind.OperatorSuperscript) sup = GlyphsOf(spans[i]);
                        else sub = GlyphsOf(spans[i]);
                        i++;
                    }

                    i--;
                    line.Add(nucleus.WithScripts(sup, sub));
                    break;
                case TextSpanKind.FractionNumerator or TextSpanKind.FractionDenominator:
                    MathBox numerator = RowBox.Empty, denominator = RowBox.Empty;
                    if (span.Kind == TextSpanKind.FractionNumerator)
                    {
                        numerator = GlyphsOf(span);
                        if (i + 1 < spans.Count && spans[i + 1].Kind == TextSpanKind.FractionDenominator) denominator = GlyphsOf(spans[++i]);
                    }
                    else
                    {
                        denominator = GlyphsOf(span);
                    }

                    line.Add(new FractionBox(numerator, denominator, null, 1));
                    break;
                case TextSpanKind.Radical:
                    line.Add(new RadicalBox(GlyphsOf(span), null));
                    break;
                case TextSpanKind.LargeOperator:
                    line.Add(new TakesLimitsBox(new LargeSymbolBox(span.Text)));
                    break;
                case TextSpanKind.Accent:
                    line.Add(new AccentBox(GlyphsOf(span)));
                    break;
                case TextSpanKind.MatrixStart:
                    line.Add(MatrixOf(spans, ref i));
                    break;
                case TextSpanKind.MatrixCellSeparator or TextSpanKind.MatrixRowSeparator or TextSpanKind.MatrixEnd:
                    break;
                default:
                    line.Add(new GlyphsBox(span.Text, span.Variant, span.OutsideMath, span.FontSizeScale));
                    break;
            }
        }

        return new LineBox(rich.PlainText, [.. line]);
    }

    /// <summary>The characters of <paramref name="span"/>, at the size their construction sets them at.</summary>
    private static GlyphsBox GlyphsOf(TextSpan span) => new(span.Text, span.Variant, span.OutsideMath);

    /// <summary>Whether a span of <paramref name="kind"/> is a script or a limit.</summary>
    private static bool IsScript(TextSpanKind kind) => kind is TextSpanKind.Superscript or TextSpanKind.Subscript
        or TextSpanKind.OperatorSuperscript or TextSpanKind.OperatorSubscript;

    /// <summary>The matrix that opens at <paramref name="i"/>: its cells up to its closing span, which <paramref name="i"/> is left on.</summary>
    private static MathBox MatrixOf(IReadOnlyList<TextSpan> spans, ref int i)
    {
        string environment = spans[i].Text;
        var rows = new List<MathBox[]>();
        var cells = new List<MathBox>();
        for (i++; i < spans.Count && spans[i].Kind != TextSpanKind.MatrixEnd; i++)
        {
            switch (spans[i].Kind)
            {
                case TextSpanKind.MatrixRowSeparator:
                    rows.Add([.. cells]);
                    cells.Clear();
                    break;
                case TextSpanKind.MatrixCellSeparator:
                    break;
                default:
                    cells.Add(GlyphsOf(spans[i]));
                    break;
            }
        }

        if (cells.Count > 0) rows.Add([.. cells]);
        var delimiters = MathEnvironment.DelimitersOf(environment);
        return new MatrixBox(environment, delimiters.Left, delimiters.Right, [.. rows]);
    }
}
