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
}

/// <summary>A fragment of rich text with an associated <see cref="TextSpanKind"/> and optional font-size scale.</summary>
/// <param name="Text">The Unicode string for this span.</param>
/// <param name="Kind">Whether the span is normal, superscript, or subscript.</param>
/// <param name="FontSizeScale">Multiplier applied to the base font size; typically 0.70 for super/subscript.</param>
public sealed record TextSpan(string Text, TextSpanKind Kind = TextSpanKind.Normal, double FontSizeScale = 1.0);

/// <summary>Parsed representation of a potentially math-mode label, consisting of one or more <see cref="TextSpan"/> objects.</summary>
/// <param name="Spans">The ordered sequence of text spans.</param>
public sealed record RichText(IReadOnlyList<TextSpan> Spans);
