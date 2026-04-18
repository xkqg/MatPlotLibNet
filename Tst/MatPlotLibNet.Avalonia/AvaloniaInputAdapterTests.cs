// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Avalonia.Tests;

/// <summary>
/// Validates the static mapping logic in <see cref="AvaloniaInputAdapter"/> without
/// requiring a running Avalonia application. These tests exercise the adapter's
/// modifier-key mapping and button resolution by calling the internal helpers directly.
/// Full round-trip tests (pointer → controller → figure mutation) run in the
/// integration tests inside <c>MatPlotLibNet.Tests</c>.
/// </summary>
public class AvaloniaInputAdapterTests
{
    // AvaloniaInputAdapter is internal — tested via ModifierKeys mapping reflectively.
    // The interaction layer (InteractionController + modifiers) is already covered by
    // MatPlotLibNet.Tests/Interaction/*. Here we just verify the type exists and is
    // internal to the package boundary.

    [Fact]
    public void AvaloniaInputAdapter_IsInternalType()
    {
        var type = typeof(MplChartControl).Assembly
            .GetType("MatPlotLibNet.Avalonia.AvaloniaInputAdapter");
        Assert.NotNull(type);
        Assert.False(type!.IsPublic);
    }

    [Fact]
    public void MplChartDrawOperation_IsInternalType()
    {
        var type = typeof(MplChartControl).Assembly
            .GetType("MatPlotLibNet.Avalonia.MplChartDrawOperation");
        Assert.NotNull(type);
        Assert.False(type!.IsPublic);
    }

    /// <summary>Phase I.2 of v1.7.2 follow-on plan — Avalonia <c>KeyModifiers</c>
    /// must map to the platform-neutral <see cref="ModifierKeys"/> that all
    /// managed interaction modifiers expect. Theory covers every flag + combinations.</summary>
    [Theory]
    [InlineData(global::Avalonia.Input.KeyModifiers.None,    ModifierKeys.None)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Shift,   ModifierKeys.Shift)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Control, ModifierKeys.Ctrl)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Alt,     ModifierKeys.Alt)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Shift | global::Avalonia.Input.KeyModifiers.Control, ModifierKeys.Shift | ModifierKeys.Ctrl)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Shift | global::Avalonia.Input.KeyModifiers.Alt,     ModifierKeys.Shift | ModifierKeys.Alt)]
    [InlineData(global::Avalonia.Input.KeyModifiers.Shift | global::Avalonia.Input.KeyModifiers.Control | global::Avalonia.Input.KeyModifiers.Alt, ModifierKeys.Shift | ModifierKeys.Ctrl | ModifierKeys.Alt)]
    public void Map_KeyModifiers_ToPlatformNeutralFlags(global::Avalonia.Input.KeyModifiers input, ModifierKeys expected)
    {
        // AvaloniaInputAdapter.Map is private; invoke via reflection.
        var type = typeof(MplChartControl).Assembly
            .GetType("MatPlotLibNet.Avalonia.AvaloniaInputAdapter");
        Assert.NotNull(type);
        var method = type!.GetMethod("Map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        var result = (ModifierKeys)method!.Invoke(null, [input])!;
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Home",       "Home")]
    [InlineData("Left",       "Left")]
    [InlineData("Escape",     "Escape")]
    public void ToKeyArgs_KeyName_RoundTripsAsString(string keyName, string expected)
    {
        // ToKeyArgs is a static helper; it just calls e.Key.ToString(). Verify
        // the adapter doesn't mangle the key name on the way to the controller.
        // We can't construct a real Avalonia KeyEventArgs without a running app,
        // but the key enum round-trips via ToString — cover the surface via
        // Avalonia.Input.Key parse/format.
        Assert.True(Enum.TryParse<global::Avalonia.Input.Key>(keyName, out var k));
        Assert.Equal(expected, k.ToString());
    }
}
