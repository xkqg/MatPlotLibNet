// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>Classifies a text span for rendering purposes.</summary>
public enum TextSpanKind
{
    /// <summary>Normal inline text.</summary>
    Normal,

    /// <summary>Text raised above the baseline (superscript).</summary>
    Superscript,

    /// <summary>Text lowered below the baseline (subscript).</summary>
    Subscript,

    /// <summary>Fraction numerator — rendered above a horizontal bar at reduced size.</summary>
    FractionNumerator,

    /// <summary>Fraction denominator — rendered below a horizontal bar at reduced size.</summary>
    FractionDenominator,

    /// <summary>Content under a radical sign (√).</summary>
    Radical,

    /// <summary>An accent (hat, bar, tilde, dot, vec) drawn above the preceding character.</summary>
    Accent,
}

/// <summary>Font variant for mixed-font math mode (<c>\mathrm</c>, <c>\mathbf</c>, etc.).</summary>
public enum FontVariant
{
    /// <summary>Default math italic.</summary>
    Default,

    /// <summary>Roman (upright) — <c>\mathrm</c> or <c>\text</c>.</summary>
    Roman,

    /// <summary>Bold — <c>\mathbf</c>.</summary>
    Bold,

    /// <summary>Italic — <c>\mathit</c>.</summary>
    Italic,

    /// <summary>Calligraphic — <c>\mathcal</c>.</summary>
    Calligraphic,

    /// <summary>Blackboard bold — <c>\mathbb</c>.</summary>
    BlackboardBold,
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
