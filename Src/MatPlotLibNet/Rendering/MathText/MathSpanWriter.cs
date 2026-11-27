// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The public flat view of a label's math (<see cref="RichText"/>), written from its tree (<see cref="MathBox.Flatten"/>): one
/// span for each run of text of one kind, one size and one font switch, a script, a fraction's part, a limit, a radical's body and a
/// matrix's cell marked by its <see cref="TextSpanKind"/>, a space written as the Unicode space of the nearest width
/// (<see cref="MathGrammar.SpaceOf"/>). With <see cref="MathBoxes.Of"/>, the reading of a flat view back into a tree, the one place a
/// kind of span is written or read: the layout reads the tree. A thread keeps one writer between views
/// (<see cref="KeptByThread{T}"/>), so a view allocates its spans, a joined text and their one row.</summary>
internal sealed class MathSpanWriter
{
    private readonly List<TextSpan> _spans = [];

    /// <summary>The pieces of the span being written once it is more than one piece, joined; empty while it is one.</summary>
    private readonly StringBuilder _joined = new();

    /// <summary>The text of the span being written while it is one piece.</summary>
    private string? _pending;
    private TextSpanKind _pendingKind;
    private double _pendingScale;
    private FontVariant _pendingVariant;
    private bool _pendingOutside;

    /// <summary>The constants the flat view's sizes are written by: the library's own.</summary>
    internal static MathConstants Constants => MathConstants.Library;

    /// <summary>The kind the text being written is marked as.</summary>
    private TextSpanKind _kind = TextSpanKind.Normal;

    /// <summary>The size the text being written is set at, against the label's.</summary>
    private double _scale = 1;

    /// <summary>The flat view of <paramref name="box"/>.</summary>
    /// <param name="box">The label's tree.</param>
    /// <returns>The spans, holding the tree they were written from.</returns>
    internal static RichText Write(LineBox box) => new(SpansOf(box)) { Box = box };

    /// <summary>The text of <paramref name="box"/>, every span's one after the other.</summary>
    /// <param name="box">A box.</param>
    internal static string TextOf(MathBox box) => new RichText(SpansOf(box)).PlainText;

    /// <summary>The spans of <paramref name="box"/>, written by the writer this thread keeps.</summary>
    private static TextSpan[] SpansOf(MathBox box)
    {
        var writer = KeptByThread<MathSpanWriter>.Take();
        box.Flatten(writer);
        writer.FlushPending();
        TextSpan[] spans = [.. writer._spans];
        writer._spans.Clear();
        if (writer._spans.Capacity <= KeptCapacity) KeptByThread<MathSpanWriter>.Return(writer);
        return spans;
    }

    /// <summary>The most spans a writer kept between views may have room for: more than any label of a chart is written as.</summary>
    private const int KeptCapacity = 256;

    /// <summary>Writes a construction marked by its kind at the size of the text it stands in (a large operator, an integral, an accent,
    /// a radical's body, a matrix cell): <paramref name="box"/> marked as <paramref name="kind"/>.</summary>
    internal void Marked(MathBox box, TextSpanKind kind) => WriteAs(box, kind, 1);

    /// <summary>Writes a script or a limit: <paramref name="box"/> marked as <paramref name="kind"/>, one size smaller.</summary>
    internal void Script(MathBox box, TextSpanKind kind) => Part(box, kind, 1);

    /// <summary>Writes a part (a fraction's numerator or denominator, a radical's index) marked as <paramref name="kind"/>,
    /// <paramref name="levels"/> sizes smaller.</summary>
    internal void Part(MathBox box, TextSpanKind kind, int levels) => WriteAs(box, kind, Constants.ShrunkBy(levels));

    /// <summary>Writes <paramref name="box"/> marked as <paramref name="kind"/>, at <paramref name="factor"/> times the size written at,
    /// and puts the kind and the size back after it.</summary>
    private void WriteAs(MathBox box, TextSpanKind kind, double factor)
    {
        var outerKind = _kind;
        double outerScale = _scale;
        _kind = kind;
        _scale *= factor;
        box.Flatten(this);
        _kind = outerKind;
        _scale = outerScale;
    }

    /// <summary>Writes characters in their font switch, at <paramref name="scale"/> times the size written at.</summary>
    internal void Glyphs(string text, FontVariant variant, bool outside, double scale) => Text(text, _kind, _scale * scale, variant, outside);

    /// <summary>Writes a space, as the Unicode spaces of its width (<see cref="MathGrammar.SpaceOf"/>).</summary>
    internal void Space(string spaces) => Text(spaces, _kind, _scale, FontVariant.Default, outside: false);

    /// <summary>Writes a delimiter's character; nothing for one that is not drawn.</summary>
    internal void Delimiter(string glyph) => Text(glyph, _kind, _scale, FontVariant.Default, outside: false);

    /// <summary>Writes a matrix: a span that opens it, carrying the environment's name, each cell's text marked as a cell and followed
    /// by a separator, each row followed by one, and a span that closes it.</summary>
    internal void Matrix(string environment, MathBox[][] rows)
    {
        Mark(environment, TextSpanKind.MatrixStart);
        foreach (var row in rows)
        {
            foreach (var cell in row)
            {
                Marked(cell, TextSpanKind.MatrixCell);
                Mark(string.Empty, TextSpanKind.MatrixCellSeparator);
            }

            Mark(string.Empty, TextSpanKind.MatrixRowSeparator);
        }

        Mark(environment, TextSpanKind.MatrixEnd);
    }

    /// <summary>Writes a span that marks a place in the text, never joined to the text around it.</summary>
    private void Mark(string text, TextSpanKind kind)
    {
        FlushPending();
        _spans.Add(new TextSpan(text, kind, _scale));
    }

    /// <summary>Writes text: joined to the span being written when it has the same kind, size, font switch and side of the dollar
    /// signs, else a span of its own.</summary>
    private void Text(string text, TextSpanKind kind, double scale, FontVariant variant, bool outside)
    {
        if (text.Length == 0) return;
        if (_pending is not null)
        {
            if (kind == _pendingKind && scale == _pendingScale && variant == _pendingVariant && outside == _pendingOutside)
            {
                if (_joined.Length == 0) _joined.Append(_pending);
                _joined.Append(text);
                return;
            }

            FlushPending();
        }

        _pendingKind = kind;
        _pendingScale = scale;
        _pendingVariant = variant;
        _pendingOutside = outside;
        _pending = text;
    }

    /// <summary>Ends the span being written.</summary>
    private void FlushPending()
    {
        if (_pending is null) return;
        string text = _pending;
        if (_joined.Length > 0)
        {
            text = _joined.ToString();
            _joined.Clear();
        }

        _spans.Add(new TextSpan(text, _pendingKind, _pendingScale, _pendingVariant) { OutsideMath = _pendingOutside });
        _pending = null;
    }
}
