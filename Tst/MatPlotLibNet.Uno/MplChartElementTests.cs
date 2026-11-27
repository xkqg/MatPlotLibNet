// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Uno.Tests;

/// <summary>
/// Structural tests for the Uno package.
/// MplChartElement cannot be instantiated in a unit-test context because
/// SKCanvasElement is a source-generated type that requires the full Uno build pipeline.
/// These tests verify the adapter types and the interaction layer wiring.
/// </summary>
public class MplChartElementTests
{
    [Fact]
    public void UnoInputAdapter_IsInternalType()
    {
        var type = typeof(UnoInputAdapter);
        Assert.False(type.IsPublic);
    }

    [Theory]
    [InlineData(ModifierKeys.Shift, 1)]
    [InlineData(ModifierKeys.Ctrl, 2)]
    [InlineData(ModifierKeys.Alt, 4)]
    [InlineData(PointerButton.Left, 1)]
    public void TheKeysAndButtonsTheAdapterTranslatesTo_HaveTheirNumbers(Enum member, int value)
    {
        Assert.Equal(value, Convert.ToInt32(member));
    }

    [Fact]
    public void PointerInputArgs_CanBeConstructed()
    {
        var args = new PointerInputArgs(10.0, 20.0, PointerButton.Left, ModifierKeys.Shift, 1);
        Assert.Equal(10.0, args.X);
        Assert.Equal(20.0, args.Y);
        Assert.Equal(PointerButton.Left, args.Button);
        Assert.Equal(ModifierKeys.Shift, args.Modifiers);
    }

    [Fact]
    public void ScrollInputArgs_CanBeConstructed()
    {
        var args = new ScrollInputArgs(5.0, 15.0, 0.0, 3.0, ModifierKeys.None);
        Assert.Equal(5.0, args.X);
        Assert.Equal(3.0, args.DeltaY);
    }

    [Fact]
    public void KeyInputArgs_CanBeConstructed()
    {
        var args = new KeyInputArgs("Home", ModifierKeys.None);
        Assert.Equal("Home", args.Key);
    }
}
