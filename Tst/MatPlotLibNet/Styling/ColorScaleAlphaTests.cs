// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary><c>Color.ScaleAlpha</c>: a colour's own opacity scaled by a factor, the one rule a series' alpha and a patch's
/// alpha are applied by. A translucent colour becomes more translucent, a colour with no opacity stays without, and the
/// factor is held between 0 and 1.</summary>
public class ColorScaleAlphaTests
{
    [Theory]
    [InlineData(255, 0.5, 128)]
    [InlineData(100, 0.5, 50)]
    [InlineData(0, 0.7, 0)]
    [InlineData(200, 1.0, 200)]
    [InlineData(200, 0.0, 0)]
    [InlineData(200, 1.5, 200)]
    [InlineData(200, -0.5, 0)]
    public void ScaleAlpha_MultipliesTheColoursOwnOpacity_AndKeepsItsChannels(int alpha, double factor, int expected)
    {
        var scaled = new Color(10, 20, 30, (byte)alpha).ScaleAlpha(factor);

        Assert.Equal(new Color(10, 20, 30, (byte)expected), scaled);
    }
}
