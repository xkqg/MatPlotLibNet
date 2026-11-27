// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>The canvas and context the Maui render-context tests share.</summary>
public class MauiRigTests
{
    [Fact]
    public void TheRig_HoldsAContextThatDrawsOntoItsCanvas_AndEveryRigIsItsOwn()
    {
        var (canvas, context) = MauiRig.Fresh();

        context.DrawLine(new Rendering.Point(1, 2), new Rendering.Point(3, 4), new Styling.StrokeStyle(Styling.Colors.Red, 1.0, Styling.LineStyle.Solid));

        canvas.Received(1).DrawLine(1f, 2f, 3f, 4f);
        Assert.NotSame(canvas, MauiRig.Fresh().Canvas);
    }
}
