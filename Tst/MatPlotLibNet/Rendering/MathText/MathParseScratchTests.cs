// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>What a parse writes on is kept by its thread between parses (<see cref="KeptByThread{T}"/>): the rows it has open
/// (<see cref="Rendering.MathText.MathTextCursor.MathRows"/>), the builder a run is joined in (<see cref="Rendering.MathText.MathTextCursor.MathParseScratch"/>) and the writer of the flat view
/// (<see cref="MathSpanWriter"/>). A use takes it and hands it back emptied; one grown past what a label needs is let go of.</summary>
public sealed class MathParseScratchTests
{
    private sealed class Kept;

    private static GlyphsBox Box(string text) => new(text, FontVariant.Roman);

    [Fact]
    public void WhatAThreadKeeps_IsTakenOnce_AndIsItsAgainOnceHandedBack()
    {
        var first = KeptByThread<Kept>.Take();
        var second = KeptByThread<Kept>.Take();
        Assert.NotSame(first, second);

        KeptByThread<Kept>.Return(first);

        Assert.Same(first, KeptByThread<Kept>.Take());
    }

    [Fact]
    public void WhatOneThreadKeeps_IsNotAnothers()
    {
        var mine = KeptByThread<Kept>.Take();
        KeptByThread<Kept>.Return(mine);
        Kept? theirs = null;

        var other = new Thread(() => theirs = KeptByThread<Kept>.Take());
        other.Start();
        Assert.True(other.Join(TimeSpan.FromSeconds(30)));

        Assert.NotNull(theirs);
        Assert.NotSame(mine, theirs);
        Assert.Same(mine, KeptByThread<Kept>.Take());
    }

    [Fact]
    public void ARow_IsClosedAsItsBoxes_AndTheRowItWasOpenedInIsReadAgain()
    {
        var rows = new MathTextCursor.MathRows();
        MathBox a = Box("a"), b = Box("b"), c = Box("c");

        var line = rows.Open();
        rows.Add(a);
        var group = rows.Open();
        rows.Add(b);
        rows.Add(c);

        Assert.Equal([b, c], rows.Close(group));
        Assert.Equal([a], rows.Close(line));
        Assert.Empty(rows.Close(rows.Open()));
    }

    [Fact]
    public void ARowOfOneBox_ClosesAsThatBox_AndAnyOtherAsARow()
    {
        var rows = new MathTextCursor.MathRows();
        MathBox a = Box("a"), b = Box("b");

        var one = rows.Open();
        rows.Add(a);
        Assert.Same(a, rows.CloseAsBox(one));

        var none = rows.Open();
        Assert.IsType<RowBox>(rows.CloseAsBox(none));

        var two = rows.Open();
        rows.Add(a);
        rows.Add(b);
        Assert.IsType<RowBox>(rows.CloseAsBox(two));
        Assert.Null(rows.TakeLast());
    }

    /// <summary>A script takes the last box of the math it stands in, never a box written before that math, nor one of the row its
    /// own row was opened in.</summary>
    [Fact]
    public void TheLastBox_IsTakenFromTheMathBeingRead_Only()
    {
        var rows = new MathTextCursor.MathRows();
        MathBox outside = Box("out"), a = Box("a"), b = Box("b");

        var line = rows.Open();
        rows.Add(outside);
        rows.StartMath();
        Assert.Null(rows.TakeLast());

        rows.Add(a);
        var group = rows.Open();
        Assert.Null(rows.TakeLast());
        rows.Add(b);
        Assert.Same(b, rows.TakeLast());
        Assert.Empty(rows.Close(group));

        Assert.Same(a, rows.TakeLast());
        Assert.Null(rows.TakeLast());
        Assert.Equal([outside], rows.Close(line));
    }

    [Fact]
    public void Rows_AreEmptiedForTheNextParse_AndLetGoOfOnceGrownPastALabel()
    {
        var rows = new MathTextCursor.MathRows();
        rows.Open();
        rows.Add(Box("a"));
        rows.Open();
        rows.StartMath();

        rows.Clear();

        Assert.True(rows.IsWorthKeeping);
        Assert.Null(rows.TakeLast());
        Assert.Empty(rows.Close(default));

        for (int i = 0; i < 300; i++) rows.Add(Box("a"));
        rows.Clear();
        Assert.False(rows.IsWorthKeeping);
    }

    [Fact]
    public void AParsesScratch_IsTheThreadsAgainOnceEmptied_UnlessALabelGrewItPastKeeping()
    {
        var scratch = MathTextCursor.MathParseScratch.Take();
        scratch.Rows.Add(Box("a"));
        scratch.Text.Append("joined");

        scratch.Return();

        var again = MathTextCursor.MathParseScratch.Take();
        Assert.Same(scratch, again);
        Assert.Equal(0, again.Text.Length);
        Assert.Null(again.Rows.TakeLast());

        for (int i = 0; i < 300; i++) again.Rows.Add(Box("a"));
        again.Return();

        Assert.NotSame(again, MathTextCursor.MathParseScratch.Take());
    }

    /// <summary>A parse of a label hands its scratch back, so the next parse on the thread writes on the same one; a parse inside
    /// another (none is made by the library) would get one of its own.</summary>
    [Fact]
    public void AParse_HandsItsScratchBack()
    {
        var scratch = MathTextCursor.MathParseScratch.Take();
        scratch.Return();

        _ = MathTextParser.Parse(@"$\frac{\alpha}{2} + x_i^2$");

        Assert.Same(scratch, MathTextCursor.MathParseScratch.Take());
    }

    /// <summary>The writer of a flat view is the thread's between views; a label written as more spans than a label of a chart holds
    /// is written whole, and its writer is let go of.</summary>
    [Fact]
    public void AFlatViewsWriter_IsTheThreadsAgain_UnlessALabelGrewItPastKeeping()
    {
        var writer = KeptByThread<MathSpanWriter>.Take();
        KeptByThread<MathSpanWriter>.Return(writer);

        Assert.Equal(3, MathTextParser.Parse("$x_i^2$").Spans.Count);
        var again = KeptByThread<MathSpanWriter>.Take();
        Assert.Same(writer, again);
        KeptByThread<MathSpanWriter>.Return(again);

        string many = "$" + string.Concat(Enumerable.Repeat("x^2", 200)) + "$";
        Assert.Equal(400, MathTextParser.Parse(many).Spans.Count);
        Assert.NotSame(writer, KeptByThread<MathSpanWriter>.Take());
    }

    /// <summary>A span joined of several pieces leaves the writer's builder empty: the next span of the same view, and the next view,
    /// start from nothing.</summary>
    [Fact]
    public void AJoinedSpan_LeavesNothingForTheNext()
    {
        var first = MathTextParser.Parse("$a + b^c + d$");
        var second = MathTextParser.Parse("$a + b^c + d$");

        Assert.Equal(["a + b", "c", " + d"], first.Spans.Select(span => span.Text));
        Assert.Equal(first.Spans, second.Spans);
    }

    [Theory]
    [InlineData("x", FontVariant.Default, false, true)]
    [InlineData("+", FontVariant.Default, false, true)]
    [InlineData("x", FontVariant.Bold, false, false)]
    [InlineData("x", FontVariant.Default, true, false)]
    [InlineData("xy", FontVariant.Default, false, false)]
    [InlineData("α", FontVariant.Default, false, false)]
    public void ARunOfOneAsciiCharacterInMath_IsTheOneBoxOfThatCharacter(string text, FontVariant variant, bool outsideMath, bool shared)
    {
        var first = GlyphsBox.Of(text, variant, outsideMath);
        var second = GlyphsBox.Of(text, variant, outsideMath);

        Assert.Equal(shared, ReferenceEquals(first, second));
        Assert.Equal(text, MathSpanWriter.TextOf(first));
    }

    [Fact]
    public void ACharacterAsAText_IsTheOneStringOfAnAsciiCharacter()
    {
        Assert.Same(GlyphsBox.TextOf('x'), GlyphsBox.TextOf('x'));
        Assert.Equal("x", GlyphsBox.TextOf('x'));
        Assert.Equal("α", GlyphsBox.TextOf('α'));
    }
}
