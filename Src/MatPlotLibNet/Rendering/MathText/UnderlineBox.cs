// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary><c>\underline{x}</c> (matplotlib's <c>Parser.underline</c>): a rule three rules below the body's foot, and one more rule of
/// room below it.</summary>
/// <param name="body">The body.</param>
internal sealed class UnderlineBox(MathBox body) : MathBox
{
    /// <inheritdoc />
    internal override MathExtent Lay(MathInk ink, in MathStyle style)
    {
        double thickness = style.Rule;
        var inner = body.Lay(ink, style);
        ink.AddRule(0, inner.Depth + 3 * thickness, inner.Width, thickness);
        return new MathExtent(inner.Width, inner.Height, inner.Depth + 5 * thickness);
    }

    /// <inheritdoc />
    internal override bool IsCharNode => false;

    /// <inheritdoc />
    internal override void Flatten(MathSpanWriter writer) => body.Flatten(writer);
}
