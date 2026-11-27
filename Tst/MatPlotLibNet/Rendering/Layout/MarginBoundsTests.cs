// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Layout;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>A margin or a gap held between its floor and its cap, the cap lifted by what is measured there.</summary>
public sealed class MarginBoundsTests
{
    [Theory]
    [InlineData(5, 0, 30)]
    [InlineData(70, 0, 70)]
    [InlineData(200, 0, 120)]
    [InlineData(200, 160, 160)]
    [InlineData(200, 260, 200)]
    [InlineData(100, 60, 100)]
    public void AValue_IsHeldBetweenTheFloorAndTheCap_TheCapLiftedByWhatIsMeasured(double value, double measured, double expected)
    {
        Assert.Equal(expected, new MarginBounds(Floor: 30, Cap: 120).Clamp(value, measured));
    }

    [Theory]
    [InlineData(-50)]
    [InlineData(0)]
    [InlineData(5000)]
    public void NoBound_LeavesEveryValueAsItIs(double value)
    {
        Assert.Equal(value, MarginBounds.None.Clamp(value, 0));
    }
}
