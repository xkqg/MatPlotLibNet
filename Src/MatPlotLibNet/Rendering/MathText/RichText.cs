// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>Classifies a text span for rendering purposes.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum TextSpanKind
{
    /// <summary>Normal inline text.</summary>
    Normal = 0,

    /// <summary>Text raised above the baseline (superscript).</summary>
    Superscript = 1,

    /// <summary>Text lowered below the baseline (subscript).</summary>
    Subscript = 2,

    /// <summary>Fraction numerator — rendered above a horizontal bar at reduced size.</summary>
    FractionNumerator = 3,

    /// <summary>Fraction denominator — rendered below a horizontal bar at reduced size.</summary>
    FractionDenominator = 4,

    /// <summary>Content under a radical sign (√).</summary>
    Radical = 5,

    /// <summary>An accent (hat, bar, tilde, dot, vec) drawn above the preceding character.</summary>
    Accent = 6,

    /// <summary>A large operator symbol (∫, Σ, Π, lim) rendered at increased size.</summary>
    LargeOperator = 7,

    /// <summary>Lower limit positioned centered below the preceding large operator.</summary>
    OperatorSubscript = 8,

    /// <summary>Upper limit positioned centered above the preceding large operator.</summary>
    OperatorSuperscript = 9,

    /// <summary>Marks the start of a matrix environment. <see cref="TextSpan.Text"/> holds the delimiter
    /// style: "matrix" (none), "pmatrix" (parentheses), "bmatrix" (brackets), "vmatrix" (bars).</summary>
    MatrixStart = 10,

    /// <summary>A single cell in a matrix row. Cells are separated by MatrixCellSeparator.</summary>
    MatrixCell = 11,

    /// <summary>Separator between matrix cells (the &amp; column separator).</summary>
    MatrixCellSeparator = 12,

    /// <summary>Row separator in a matrix (the \\\\ row break).</summary>
    MatrixRowSeparator = 13,

    /// <summary>Marks the end of a matrix environment.</summary>
    MatrixEnd = 14,
}

/// <summary>Font variant for mixed-font math mode (<c>\mathrm</c>, <c>\mathbf</c>, etc.).</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum FontVariant
{
    /// <summary>Default math italic.</summary>
    Default = 0,

    /// <summary>Roman (upright) — <c>\mathrm</c> or <c>\text</c>.</summary>
    Roman = 1,

    /// <summary>Bold — <c>\mathbf</c>.</summary>
    Bold = 2,

    /// <summary>Italic — <c>\mathit</c>.</summary>
    Italic = 3,

    /// <summary>Calligraphic — <c>\mathcal</c>.</summary>
    Calligraphic = 4,

    /// <summary>Blackboard bold — <c>\mathbb</c>.</summary>
    BlackboardBold = 5,
}

/// <summary>A fragment of rich text with an associated <see cref="TextSpanKind"/> and optional font-size scale.</summary>
/// <param name="Text">The Unicode string for this span.</param>
/// <param name="Kind">Whether the span is normal, superscript, or subscript.</param>
/// <param name="FontSizeScale">Multiplier applied to the base font size; typically 0.70 for super/subscript.</param>
/// <param name="Variant">Font variant for mixed-font math mode. Default is <see cref="FontVariant.Default"/>.</param>
public sealed record TextSpan(
    string Text,
    TextSpanKind Kind = TextSpanKind.Normal,
    double FontSizeScale = 1.0,
    FontVariant Variant = FontVariant.Default);

/// <summary>Parsed representation of a potentially math-mode label, consisting of one or more <see cref="TextSpan"/> objects.</summary>
/// <param name="Spans">The ordered sequence of text spans.</param>
public sealed record RichText(IReadOnlyList<TextSpan> Spans);
