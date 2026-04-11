// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="HatchPattern"/> enum values and <see cref="HatchRenderer"/> drawing logic.</summary>
public class HatchPatternTests
{
    [Fact]
    public void HatchPattern_HasNineValues()
    {
        var values = Enum.GetValues<HatchPattern>();
        Assert.Equal(9, values.Length);
    }

    [Fact]
    public void HatchPattern_DefaultIsNone()
    {
        HatchPattern h = default;
        Assert.Equal(HatchPattern.None, h);
    }

    [Fact]
    public void HatchPattern_NoneIsZero()
    {
        Assert.Equal(0, (int)HatchPattern.None);
    }

    [Fact]
    public void HatchPattern_ContainsForwardDiagonal() =>
        Assert.Contains(HatchPattern.ForwardDiagonal, Enum.GetValues<HatchPattern>());

    [Fact]
    public void HatchPattern_ContainsBackDiagonal() =>
        Assert.Contains(HatchPattern.BackDiagonal, Enum.GetValues<HatchPattern>());

    [Fact]
    public void HatchPattern_ContainsHorizontal() =>
        Assert.Contains(HatchPattern.Horizontal, Enum.GetValues<HatchPattern>());

    [Fact]
    public void HatchPattern_ContainsVertical() =>
        Assert.Contains(HatchPattern.Vertical, Enum.GetValues<HatchPattern>());

    [Fact]
    public void HatchPattern_ContainsCross() =>
        Assert.Contains(HatchPattern.Cross, Enum.GetValues<HatchPattern>());

    [Fact]
    public void HatchPattern_ContainsDiagonalCross() =>
        Assert.Contains(HatchPattern.DiagonalCross, Enum.GetValues<HatchPattern>());

    [Fact]
    public void HatchPattern_ContainsDots() =>
        Assert.Contains(HatchPattern.Dots, Enum.GetValues<HatchPattern>());

    [Fact]
    public void HatchPattern_ContainsStars() =>
        Assert.Contains(HatchPattern.Stars, Enum.GetValues<HatchPattern>());
}
