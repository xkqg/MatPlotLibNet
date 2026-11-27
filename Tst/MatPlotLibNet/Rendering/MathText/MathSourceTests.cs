// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>The characters written around a symbol, as the space around it is decided from them: spaces skipped, nothing past the
/// dollar signs of the math, nothing past either end of the text.</summary>
public sealed class MathSourceTests
{
    // "a$x + y$b": the math starts at 2 (x), its closing dollar sign stands at 7.
    private static readonly MathSource Source = new("a$x + y$b", 2);

    [Theory]
    [InlineData(4, 'x')]   // before the '+', the space skipped
    [InlineData(2, '\0')]  // at the start of the math: the dollar sign and the text before it are not the math's
    public void Before_IsTheCharacterWrittenBefore_InTheMath(int index, char expected)
    {
        Assert.Equal(expected, Source.Before(index));
    }

    [Theory]
    [InlineData(5, 'y')]   // after the '+', the space skipped
    [InlineData(7, '\0')]  // the closing dollar sign: nothing after the math
    public void After_IsTheCharacterWrittenAfter_InTheMath(int index, char expected)
    {
        Assert.Equal(expected, Source.After(index));
    }

    /// <summary>Math that runs to the end of the text with no closing dollar sign has nothing after its last character.</summary>
    [Fact]
    public void AfterTheLastCharacterOfTheText_IsNothing()
    {
        var open = new MathSource("$x +  ", 1);

        Assert.Equal('\0', open.After(4));
        Assert.Equal('\0', open.After(6));
    }
}
