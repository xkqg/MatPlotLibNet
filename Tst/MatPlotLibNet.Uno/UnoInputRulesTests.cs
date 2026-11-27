// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using Windows.System;

namespace MatPlotLibNet.Uno.Tests;

/// <summary>The Uno elements hand the interaction layer what the WPF and Avalonia controls hand it: a key with the modifiers
/// held, and a press that knows it is the second of a double click (which Home answers).</summary>
public class UnoInputRulesTests
{
    [Theory]
    [InlineData(VirtualKeyModifiers.None, ModifierKeys.None)]
    [InlineData(VirtualKeyModifiers.Shift, ModifierKeys.Shift)]
    [InlineData(VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu, ModifierKeys.Ctrl | ModifierKeys.Alt)]
    public void AKey_CarriesTheModifiersHeld(VirtualKeyModifiers held, ModifierKeys expected)
    {
        // The Uno adapter made a key from its name alone, so a Shift or Ctrl held with it never reached a modifier.
        var args = UnoInputAdapter.ToKeyArgs(VirtualKey.Home, held);

        Assert.Equal("Home", args.Key);
        Assert.Equal(expected, args.Modifiers);
    }

    [Fact]
    public void TwoPressesCloseInTimeAndPlace_AreADoubleClick()
    {
        // The element always said one click, so a double click never reached Home.
        var clicks = new ClickCounter();

        Assert.Equal(1, clicks.Press(1_000_000, 50, 50));
        Assert.Equal(2, clicks.Press(1_200_000, 51, 49));
    }

    [Fact]
    public void APressLongAfterTheLast_StartsCountingAgain()
    {
        var clicks = new ClickCounter();
        clicks.Press(1_000_000, 50, 50);

        Assert.Equal(1, clicks.Press(1_000_000 + ClickCounter.DoubleClickMicroseconds + 1, 50, 50));
    }

    [Fact]
    public void APressFarFromTheLast_StartsCountingAgain()
    {
        var clicks = new ClickCounter();
        clicks.Press(1_000_000, 50, 50);

        Assert.Equal(1, clicks.Press(1_100_000, 50 + ClickCounter.DoubleClickDistance + 1, 50));
        Assert.Equal(1, clicks.Press(1_150_000, 50, 50));
    }

    [Fact]
    public void AThirdQuickPress_IsCountedOn()
    {
        var clicks = new ClickCounter();
        clicks.Press(1_000_000, 50, 50);
        clicks.Press(1_100_000, 50, 50);

        Assert.Equal(3, clicks.Press(1_200_000, 50, 50));
    }
}
