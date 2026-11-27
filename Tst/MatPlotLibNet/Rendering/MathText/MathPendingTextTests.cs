// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>The run a parse is reading: a stretch of the label cut without a copy, the one string a symbol wrote kept as it is, and a
/// copy only once two of them meet.</summary>
public sealed class MathPendingTextTests
{
    [Fact]
    public void ARunOfTheWholeLabel_IsTheLabelItself()
    {
        const string label = "abc";
        var run = new MathTextCursor.MathPendingText(label, new StringBuilder());

        run.AppendSource(0, 2);
        run.AppendSource(2, 1);

        Assert.Same(label, run.Take());
        Assert.True(run.IsEmpty);
    }

    [Fact]
    public void AStretchOfTheLabel_IsCutFromIt_AndTwoStretchesApartAreJoined()
    {
        var run = new MathTextCursor.MathPendingText("a$b$c", new StringBuilder());

        run.AppendSource(2, 1);
        Assert.Equal("b", run.Take());

        run.AppendSource(0, 1);
        run.AppendSource(4, 1);
        Assert.Equal("ac", run.Take());
    }

    [Fact]
    public void TheOneStringASymbolWrote_IsKeptAsItIs()
    {
        const string alpha = "α";
        var run = new MathTextCursor.MathPendingText("$\\alpha$", new StringBuilder());

        run.Append(alpha);

        Assert.False(run.IsEmpty);
        Assert.Same(alpha, run.Take());
    }

    [Theory]
    [InlineData("source then symbol", "xα")]
    [InlineData("symbol then source", "αx")]
    [InlineData("symbol then symbol", "αβ")]
    [InlineData("symbol then character", "α$")]
    public void TwoPiecesThatMeet_AreJoinedInOrder(string order, string expected)
    {
        var run = new MathTextCursor.MathPendingText("x", new StringBuilder());
        switch (order)
        {
            case "source then symbol":
                run.AppendSource(0, 1);
                run.Append("α");
                break;
            case "symbol then source":
                run.Append("α");
                run.AppendSource(0, 1);
                break;
            case "symbol then symbol":
                run.Append("α");
                run.Append("β");
                break;
            default:
                run.Append("α");
                run.Append('$');
                break;
        }

        Assert.Equal(expected, run.Take());
        Assert.True(run.IsEmpty);
    }

    [Fact]
    public void NothingAppended_LeavesTheRunEmpty()
    {
        var run = new MathTextCursor.MathPendingText("x", new StringBuilder());

        run.Append(string.Empty);
        run.AppendSource(0, 0);

        Assert.True(run.IsEmpty);
    }

    /// <summary>A run copied once is emptied by its take, and the next run starts as a stretch of the label again.</summary>
    [Fact]
    public void ARunAfterACopy_IsAStretchAgain()
    {
        const string label = "xy";
        var run = new MathTextCursor.MathPendingText(label, new StringBuilder());
        run.Append("α");
        run.Append('β');
        Assert.Equal("αβ", run.Take());

        run.AppendSource(0, 2);

        Assert.Same(label, run.Take());
    }

    /// <summary>One character cut from the label is the one string of that character: nothing is cut for it.</summary>
    [Fact]
    public void AStretchOfOneCharacter_IsThatCharactersOneString()
    {
        var run = new MathTextCursor.MathPendingText("a$b$c", new StringBuilder());

        run.AppendSource(2, 1);

        Assert.Same(GlyphsBox.TextOf('b'), run.Take());
        Assert.True(run.IsEmpty);
    }

    /// <summary>The builder is the parse's, shared by its runs: a run joined in it leaves it empty for the next.</summary>
    [Fact]
    public void ARunJoinedInTheBuilder_LeavesTheBuilderEmpty()
    {
        var builder = new StringBuilder();
        var run = new MathTextCursor.MathPendingText("xy", builder);
        run.Append("α");
        run.AppendSource(0, 1);

        Assert.Equal("αx", run.Take());

        Assert.Equal(0, builder.Length);
        Assert.True(run.IsEmpty);
    }
}
