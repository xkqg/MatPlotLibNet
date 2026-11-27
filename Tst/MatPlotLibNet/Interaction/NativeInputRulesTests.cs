// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>The WPF, Avalonia and Uno controls each turned their own pointer and key events into the neutral records with a
/// copy of the same three rules: which keys are held, which button a move is made with, and how far one notch of the wheel
/// turns. The rules now live once, beside the records they make, and a control only reads its platform's flags.</summary>
public class NativeInputRulesTests
{
    [Theory]
    [InlineData(false, false, false, ModifierKeys.None)]
    [InlineData(true, false, false, ModifierKeys.Shift)]
    [InlineData(false, true, false, ModifierKeys.Ctrl)]
    [InlineData(false, false, true, ModifierKeys.Alt)]
    [InlineData(true, true, true, ModifierKeys.Shift | ModifierKeys.Ctrl | ModifierKeys.Alt)]
    public void TheKeysHeld_AreTheOnesThePlatformSaysAreDown(bool shift, bool ctrl, bool alt, ModifierKeys expected) =>
        Assert.Equal(expected, ModifierKeys.Held(shift, ctrl, alt));

    [Theory]
    [InlineData(true, false, false, PointerButton.Left)]
    [InlineData(true, true, true, PointerButton.Left)]
    [InlineData(false, true, true, PointerButton.Right)]
    [InlineData(false, false, true, PointerButton.Middle)]
    [InlineData(false, false, false, PointerButton.None)]
    public void AMove_IsMadeWithTheLeftButtonBeforeTheRightBeforeTheMiddle(bool left, bool right, bool middle, PointerButton expected) =>
        Assert.Equal(expected, PointerButton.MovedWith(left, right, middle));

    [Fact]
    public void ANotchOfTheWheel_IsADeltaOfOne()
    {
        var up = ScrollInputArgs.OfWheel(10, 20, 120, ModifierKeys.Ctrl);
        var twoDown = ScrollInputArgs.OfWheel(10, 20, -240, ModifierKeys.None);

        Assert.Equal(new ScrollInputArgs(10, 20, 0, 1.0, ModifierKeys.Ctrl), up);
        Assert.Equal(-2.0, twoDown.DeltaY);
        Assert.Equal(0.0, twoDown.DeltaX);
    }
}
