// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>The size math is being laid out at, with what it is laid out by: the font set and its constants, the label's font, the
/// measure, the size in pixels and the level (how many times it has shrunk). A value passed down the tree; a script, a fraction's
/// parts and a radical's index are laid out one level smaller (<see cref="Smaller"/>).</summary>
internal readonly struct MathStyle
{
    /// <summary>The style of a label set in <paramref name="label"/> by <paramref name="set"/>, measured by <paramref name="measure"/>: the
    /// label's size, the text's level.</summary>
    /// <param name="set">The math font set.</param>
    /// <param name="label">The label's font.</param>
    /// <param name="measure">How a run is measured.</param>
    internal MathStyle(MathFontSet set, Font label, MathMeasure measure)
    {
        Set = set;
        Label = label;
        Measure = measure;
        Size = label.Size;
    }

    private MathStyle(in MathStyle from, double size, int level)
    {
        Set = from.Set;
        Label = from.Label;
        Measure = from.Measure;
        Size = size;
        Level = level;
    }

    /// <summary>The math font set.</summary>
    internal MathFontSet Set { get; }

    /// <summary>The set's constants.</summary>
    internal MathConstants Constants => Set.Constants;

    /// <summary>The label's font, whose family, weight and slant the set's label face takes.</summary>
    internal Font Label { get; }

    /// <summary>How a run is measured.</summary>
    internal MathMeasure Measure { get; }

    /// <summary>The size math is set at here, in pixels.</summary>
    internal double Size { get; }

    /// <summary>How many times the math here has shrunk from the text's size.</summary>
    internal int Level { get; }

    /// <summary>One level smaller (matplotlib's <c>Node.shrink</c>): the size times the shrink factor while the level stays below the
    /// last of the set's sizes, the same size past it.</summary>
    internal MathStyle Smaller
    {
        get
        {
            var c = Constants;
            int level = Level + 1;
            return new(this, level < c.SizeLevels ? Size * c.ShrinkFactor : Size, level);
        }
    }

    /// <summary>The run <paramref name="text"/> in <paramref name="face"/> at this size: its font and its box, the box of a face that
    /// raises its ink (<see cref="MathFace.RaisesInk"/>) taller by the lift and shallower by it, as matplotlib's <c>Char</c> reads the
    /// glyph of <c>Cmex10</c>.</summary>
    /// <param name="face">The face.</param>
    /// <param name="text">The characters as the face codes them.</param>
    internal MeasuredRun Run(MathFace face, string text)
    {
        var font = new SpanFont(face.In(Label), Size);
        var box = Measure.Of(text, font);
        if (!face.RaisesInk) return new(font, box, 0);
        double lift = (box.Height + box.Depth) / 2 + Size / 3;
        return new(font, new MathExtent(box.Width, box.Height + lift, box.Depth - lift), lift);
    }

    /// <summary>The same level at <paramref name="factor"/> times the size: a large operator, a delimiter grown to its content.</summary>
    /// <param name="factor">The factor.</param>
    internal MathStyle Scaled(double factor) => factor == 1 ? this : new(this, Size * factor, Level);

    /// <summary>The x-height at this size, in pixels.</summary>
    internal double XHeight => Constants.XHeight * Size;

    /// <summary>The thickness of a rule at this size, in pixels.</summary>
    internal double Rule => Constants.RuleThickness * Size;

    /// <summary>The height of the math axis at this size, in pixels.</summary>
    internal double AxisHeight => Constants.AxisHeight * Size;

    /// <summary>The width of <paramref name="em"/> ems of math space at this size, in pixels.</summary>
    /// <param name="em">The width in ems (in quads).</param>
    internal double Space(double em) => em * Constants.Quad * Size;
}

/// <summary>A run measured for a layout: the font it is drawn in, its box, and how far its baseline is raised (<see cref="MathStyle.Run"/>).</summary>
/// <param name="Font">The font of the run at its size.</param>
/// <param name="Extent">The box of the run.</param>
/// <param name="Lift">How far the run is drawn above the baseline its box stands on.</param>
internal readonly record struct MeasuredRun(SpanFont Font, MathExtent Extent, double Lift);
