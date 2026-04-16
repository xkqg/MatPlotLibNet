// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="MarkerStyle"/> enum values.</summary>
public class MarkerStyleTests
{
    /// <summary>Verifies that all expected enum values exist.</summary>
    [Theory]
    [InlineData(MarkerStyle.None)]
    [InlineData(MarkerStyle.Circle)]
    [InlineData(MarkerStyle.Square)]
    [InlineData(MarkerStyle.Triangle)]
    [InlineData(MarkerStyle.Diamond)]
    [InlineData(MarkerStyle.Cross)]
    [InlineData(MarkerStyle.Plus)]
    [InlineData(MarkerStyle.Star)]
    [InlineData(MarkerStyle.Pentagon)]
    [InlineData(MarkerStyle.Hexagon)]
    public void EnumValue_IsDefined(MarkerStyle value)
    {
        Assert.True(Enum.IsDefined(value));
    }

    /// <summary>Verifies that Circle is the default value (0 is None, Circle is 1).</summary>
    [Fact]
    public void Circle_IsNotDefault()
    {
        // None is 0 (the CLR default); Circle is 1 — series constructors explicitly set Circle
        Assert.Equal(0, (int)MarkerStyle.None);
        Assert.Equal(1, (int)MarkerStyle.Circle);
    }
}
