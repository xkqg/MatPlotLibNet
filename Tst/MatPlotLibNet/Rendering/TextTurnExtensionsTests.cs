// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The library turns a text counter-clockwise (a positive rotation lifts the end of the text) and every canvas turns clockwise
/// (SVG's <c>rotate</c>, Skia's <c>RotateDegrees</c>, MAUI's <c>Rotate</c>): the convention is written once, here.</summary>
public class TextTurnExtensionsTests
{
    [Theory]
    [InlineData(90.0, -90.0)]
    [InlineData(-45.5, 45.5)]
    [InlineData(0.0, 0.0)]
    public void ACounterClockwiseTurn_IsTheNegatedClockwiseOne(double counterClockwise, double clockwise)
    {
        Assert.Equal(clockwise, counterClockwise.ToClockwise());
    }
}
