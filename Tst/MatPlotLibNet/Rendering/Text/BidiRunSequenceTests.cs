// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Text;

namespace MatPlotLibNet.Tests.Rendering.Text;

/// <summary>The two scans every rule over an isolating run sequence reads it by, and the direction of a level. The Unicode conformance
/// corpora prove the rules that read them; these facts say what each scan answers.</summary>
public sealed class BidiRunSequenceTests
{
    /// <summary>The whole of <paramref name="text"/> as one sequence at level 0, its classes as written, with right to left before it.</summary>
    private static BidiAlgorithm.BidiRunSequence Whole(string text)
    {
        var paragraph = BidiAlgorithm.BidiText.Of(text, BidiAlgorithm.BidiDirection.LeftToRight);
        return new BidiAlgorithm.BidiRunSequence([.. Enumerable.Range(0, paragraph.Length)], paragraph.Types, 0, BidiClass.R, BidiClass.L);
    }

    [Theory]
    [InlineData("a1", 1, "L")]          // the letter just before
    [InlineData("א,$1", 3, "R")]   // back over a separator and a terminator to the Hebrew letter
    [InlineData(",1", 1, "R")]           // nothing strong before it: the type before the sequence (sos)
    [InlineData("a1", 0, "R")]           // the first position has nothing before it
    public void PrecedingStrong_IsTheFirstStrongTypeWalkingBack_ElseSos(string text, int k, string expected)
    {
        Assert.Equal(Enum.Parse<BidiClass>(expected), Whole(text).PrecedingStrong(k, static type => type is BidiClass.L or BidiClass.R ? type : BidiClass.ON));
    }

    [Theory]
    [InlineData("$$$1", 0, 2)]   // a run of terminators ends before the number
    [InlineData("$1", 0, 0)]     // a run of one
    [InlineData("1$$", 1, 2)]    // a run that reaches the end of the sequence
    public void RunEnd_IsTheLastPositionOfTheRunOfLikeTypes(string text, int k, int expected)
    {
        Assert.Equal(expected, Whole(text).RunEnd(k, static type => type == BidiClass.ET));
    }

    [Theory]
    [InlineData(0, "L")]
    [InlineData(1, "R")]
    [InlineData(124, "L")]
    [InlineData(125, "R")]
    public void DirectionOf_ALevel_IsRightToLeftWhenOdd(int level, string expected)
    {
        Assert.Equal(Enum.Parse<BidiClass>(expected), BidiAlgorithm.BidiRunSequence.DirectionOf(level));
    }
}
