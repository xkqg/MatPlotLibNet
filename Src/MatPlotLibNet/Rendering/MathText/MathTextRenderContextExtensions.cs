// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The one verb a text is drawn by: a <see cref="TextLabel"/> that is math as the layout of the tree it parsed to (its runs and
/// rules, <see cref="MathInk"/>, drawn through the public <c>DrawText</c> and <c>DrawRectangle</c> of any surface), any other label as
/// plain text, its baseline where its placement's vertical alignment puts it (<see cref="TextBox"/>). Every title, axis label, tick
/// label, legend entry, direct label, annotation and name in a series is drawn by it, and the layout measures by its twin
/// (<see cref="TextMeasurement.TextExtentExtensions"/>, which makes the one layout), so what is reserved is what is drawn.</summary>
internal static class MathTextRenderContextExtensions
{
    extension(IRenderContext ctx)
    {
        /// <summary>Draws <paramref name="label"/> where <paramref name="placement"/> puts it: as rich text when it is math, otherwise
        /// as plain text, through the overload that turns it when it is turned. A text that is not placed on its baseline is measured
        /// as this surface measures it, and drawn where its box puts the baseline.</summary>
        /// <param name="label">The text.</param>
        /// <param name="font">The font.</param>
        /// <param name="placement">Where its anchor stands, how it stands on the anchor and how far it is turned.</param>
        internal void Draw(TextLabel label, Font font, in TextPlacement placement)
        {
            // Read once: the measure that places the label and its draw read one tree.
            label = label.Parsed(ctx.MathTextsTold);
            if (placement.Vertical == TextVerticalAlignment.Baseline)
            {
                ctx.DrawLeveled(label, font, placement);
                return;
            }

            // A label of math is laid out with its ink before it is measured, so the measure that places it reads the layout it is
            // drawn by: one layout.
            if (label.Tree is { } math) ctx.LayOut(math, font);
            ctx.Draw(label, font, placement, ctx.ExtentToPlace(label, font, placement.Vertical));
        }

        /// <summary>Draws <paramref name="label"/>, measured as <paramref name="extent"/> already, where its box puts the baseline
        /// (<see cref="TextBox.Baseline"/>): the call of a caller that measured the text for its own layout (a tick label).</summary>
        /// <param name="label">The text.</param>
        /// <param name="font">The font.</param>
        /// <param name="placement">Where its anchor stands, how it stands on the anchor and how far it is turned.</param>
        /// <param name="extent">The text's extent, as this surface measures it.</param>
        internal void Draw(TextLabel label, Font font, in TextPlacement placement, TextExtent extent) =>
            ctx.DrawLeveled(label.Parsed(ctx.MathTextsTold), font, placement.Vertical == TextVerticalAlignment.Baseline
                ? placement
                : placement with { Anchor = new TextBox(placement, extent).Baseline });

        /// <summary>Draws <paramref name="label"/> with its anchor on its baseline, and the level of a tree row stated around it.</summary>
        private void DrawLeveled(TextLabel label, Font font, in TextPlacement placement)
        {
            // A row of a tree says how deep it stands in the text it draws, the one text this call draws.
            if (placement.Level is { } level)
            {
                ctx.SetNextTextLevel(level);
                ctx.DrawAt(label, font, placement);
                ctx.SetNextTextLevel(null);
                return;
            }

            ctx.DrawAt(label, font, placement);
        }

        /// <summary>Draws <paramref name="label"/>, its tree read (<see cref="TextLabel.Parsed"/>), where <paramref name="placement"/> puts
        /// it, level aside.</summary>
        private void DrawAt(TextLabel label, Font font, in TextPlacement placement)
        {
            if (label.Tree is { } math)
            {
                ctx.DrawMath(ctx.LayOut(math, font), font, placement);
                return;
            }

            if (placement.FitWidth is { } width) ctx.SetNextTextLength(width);
            ctx.DrawPlain(label.Text, font, placement);
        }

        /// <summary>Draws a text as one run through the surface's own <c>DrawText</c>, which places it by its alignment.</summary>
        private void DrawPlain(string text, Font font, in TextPlacement placement)
        {
            if (placement.Rotation == 0)
                ctx.DrawText(text, placement.Anchor, font, placement.Alignment);
            else
                ctx.DrawText(text, placement.Anchor, font, placement.Alignment, placement.Rotation);
        }

        /// <summary>Draws the layout of a label of math with its baseline's start where the alignment puts it, turned about the anchor:
        /// each run of glyphs through the surface's public <c>DrawText</c> in its face and size, each rule (a fraction's line, a radical's
        /// bar) through <c>DrawRectangle</c>, or <c>DrawPolygon</c> when the label is turned, in the label's colour. A layout of one run on
        /// the baseline is drawn as a plain text in that run's font, placed by the surface's own measure of it.</summary>
        private void DrawMath(MathInk ink, Font font, in TextPlacement placement)
        {
            if (ink.Single is { } run)
            {
                ctx.DrawPlain(run.Text, run.Font, placement);
                return;
            }

            var anchor = placement.Anchor;
            double start = placement.Alignment.StartOffset(ink.Extent.Width), rotation = placement.Rotation;
            double radians = rotation.ToRadians(), sin = Math.Sin(radians), cos = Math.Cos(radians);
            var runs = ink.Glyphs;
            for (int i = 0; i < runs.Count; i++)
            {
                var glyphs = runs[i];
                var at = anchor.InTurnedFrame(start + glyphs.X, glyphs.Y, sin, cos);
                ctx.DrawPlain(glyphs.Text, glyphs.Font, new TextPlacement(at, TextAlignment.Left) { Rotation = rotation });
            }

            var rules = ink.Rules;
            var fill = ShapeStyle.Filled(font.Color ?? Colors.Black);
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                double left = start + rule.X, right = left + rule.Width, bottom = rule.Y + rule.Height;
                if (rotation == 0)
                {
                    ctx.DrawRectangle(new Rect(anchor.X + left, anchor.Y + rule.Y, rule.Width, rule.Height), fill);
                    continue;
                }

                ctx.DrawPolygon([anchor.InTurnedFrame(left, rule.Y, sin, cos), anchor.InTurnedFrame(right, rule.Y, sin, cos),
                    anchor.InTurnedFrame(right, bottom, sin, cos), anchor.InTurnedFrame(left, bottom, sin, cos)], fill);
            }
        }
    }
}
