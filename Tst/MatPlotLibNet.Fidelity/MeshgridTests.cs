// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Fidelity;

/// <summary>The grid a fidelity chart lays its arrows on, listed as numpy's <c>meshgrid</c> then <c>ravel</c> lists it.</summary>
public sealed class MeshgridTests : FidelityTest
{
    [Fact]
    public void TheGridPoints_RunAlongX_ForEachY()
    {
        var (x, y) = Meshgrid([1.0, 2.0, 3.0], [10.0, 20.0]);

        Assert.Equal([1.0, 2.0, 3.0, 1.0, 2.0, 3.0], x);
        Assert.Equal([10.0, 10.0, 10.0, 20.0, 20.0, 20.0], y);
    }

    [Fact]
    public void AFigure_OfAFidelityRow_HasTheSizeEveryFidelityRenderUses_AndTheThemeOfTheRow()
    {
        var figure = FidelityFigure("v2").Build();

        Assert.Equal(FigWidth, figure.Width);
        Assert.Equal(FigHeight, figure.Height);
        Assert.Equal("matplotlib-v2", figure.Theme.Name);
    }
}
