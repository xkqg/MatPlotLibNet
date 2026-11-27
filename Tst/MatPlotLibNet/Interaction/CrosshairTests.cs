// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

public sealed class CrosshairTests
{
    [Fact]
    public void CrosshairModifier_InsidePlotArea_ProducesState()
    {
        var layout = TestLayout(new Rect(50, 50, 400, 400), new DataRange(0, 10, 0, 100));
        var mod = new CrosshairModifier(layout);
        mod.UpdatePosition(250, 250);

        Assert.NotNull(mod.ActiveCrosshair);
        Assert.Equal(250, mod.ActiveCrosshair!.Value.PixelX);
    }

    [Fact]
    public void CrosshairModifier_OutsidePlotArea_NullsState()
    {
        var layout = TestLayout(new Rect(50, 50, 400, 400), new DataRange(0, 10, 0, 100));
        var mod = new CrosshairModifier(layout);
        mod.UpdatePosition(10, 10); // outside

        Assert.Null(mod.ActiveCrosshair);
    }

    [Fact]
    public void CrosshairModifier_DoesNotClaimEvents()
    {
        var layout = TestLayout(new Rect(50, 50, 400, 400), new DataRange(0, 10, 0, 100));
        var mod = new CrosshairModifier(layout);
        Assert.False(mod.HandlesPointerPressed(new PointerInputArgs(100, 100, PointerButton.Left, ModifierKeys.None)));
        Assert.False(mod.HandlesScroll(new ScrollInputArgs(100, 100, 0, 1)));
        Assert.False(mod.HandlesKeyDown(new KeyInputArgs("Home")));
    }

    private static TestChartLayout TestLayout(Rect plotArea, DataRange dataRange) => new(plotArea, dataRange) { HitsOnlyOnThePlot = true };
}
