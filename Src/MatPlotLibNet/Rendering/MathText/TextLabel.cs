// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>A text as the renderers hold it: the string, whether it is math (it holds math markup, <see cref="MathTextParser.ContainsMath"/>),
/// and once it is read, the tree of boxes it parses to. Whether it is math is decided once, when the label is made; the tree is read when
/// the label is drawn or measured (<see cref="Parsed"/>), from the texts the figure being drawn keeps from one draw to the next
/// (<see cref="KeptMathTexts"/>), so a label is parsed once for as long as its figure draws it, and the measure and the draw of a label
/// read one tree (<see cref="MathTextRenderContextExtensions"/> draws it, <see cref="TextMeasurement.TextExtentExtensions"/> measures it).</summary>
/// <remarks>A label is a value, made where the text is asked for: making one parses nothing and allocates nothing. The trees outlive a
/// frame only in the store of the figure that drew them, never in a store of the process. The tree of a math label keeps its layouts, one per style
/// (<see cref="LineBox.LaidOut"/>), so the measure and the draw of one label lay it out once.</remarks>
internal readonly record struct TextLabel
{
    private TextLabel(string text, bool isMath, LineBox? tree)
    {
        Text = text;
        IsMath = isMath;
        Tree = tree;
    }

    /// <summary>The text as it was written.</summary>
    internal string Text { get; }

    /// <summary>Whether the label is set as math.</summary>
    internal bool IsMath { get; }

    /// <summary>The tree of boxes the label is set by once it is read (<see cref="Parsed"/>), and the one a rich text was written from
    /// (<see cref="OfRich"/>); <see langword="null"/> for a plain text, and for a math label not read yet.</summary>
    internal LineBox? Tree { get; }

    /// <summary>The label of <paramref name="text"/>: math when the text holds a pair of <c>$</c>, plain otherwise. Nothing is parsed.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The label.</returns>
    internal static TextLabel Of(string text) => new(text, MathTextParser.ContainsMath(text), null);

    /// <summary>The label of the rich text <paramref name="rich"/> a surface was handed to draw: math, its tree the one the rich text was
    /// written from, or the one it reads as when it was made by hand (<see cref="MathBoxes.Of"/>), its string the spans' text.</summary>
    /// <param name="rich">The rich text.</param>
    /// <returns>The label.</returns>
    internal static TextLabel OfRich(RichText rich) => new(rich.PlainText, true, MathBoxes.Of(rich));

    /// <summary>The label of <paramref name="text"/> set exactly as written, whatever it holds: the text a surface was handed through its
    /// public <c>DrawText</c>, which is raw by contract. No renderer makes one; every text it draws is <see cref="Of"/>.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The label.</returns>
    internal static TextLabel Plain(string text) => new(text, false, null);

    /// <summary>This label with its tree read: the tree <paramref name="texts"/> keeps for the text, else its parse, kept there; the parse
    /// alone where no texts are kept (a surface of your own, which the drawing sequence tells nothing). A plain label, and a label read
    /// already, is itself.</summary>
    /// <param name="texts">The texts the figure being drawn keeps, as its surface was told them; <see langword="null"/> for none.</param>
    /// <returns>The label, its tree read when it is math.</returns>
    internal TextLabel Parsed(KeptMathTexts? texts)
    {
        if (!IsMath || Tree is not null) return this;
        if (texts is null) return new(Text, true, MathTextCursor.Parse(Text));
        return new(Text, true, texts.TryTake(Text, out var kept) ? kept : texts.Keep(Text, MathTextCursor.Parse(Text)));
    }
}
