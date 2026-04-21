// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Playground;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Samples;

/// <summary>
/// Unit tests for <see cref="PlaygroundController"/> — the pure-C# logic extracted
/// from the Blazor page so that selection and build branches are testable without Blazor.
/// </summary>
public class PlaygroundControllerTests
{
    private static readonly (Theme Theme, string Label)[] _themeChoices =
    [
        (Theme.Default, "Default"),
        (Theme.Dark,    "Dark"),
        (Theme.Seaborn, "Seaborn"),
    ];

    private static readonly IColorMap[] _colorMapChoices =
        [ColorMaps.Viridis, ColorMaps.Plasma, ColorMaps.Inferno];

    // ─── SelectThemeByIndex ───────────────────────────────────────────────────

    [Fact]
    public void SelectThemeByIndex_ValidIndex_ReturnsTheme()
    {
        var result = PlaygroundController.SelectThemeByIndex("1", _themeChoices);
        Assert.NotNull(result);
        Assert.Same(Theme.Dark, result);
    }

    [Fact]
    public void SelectThemeByIndex_NullValue_ReturnsNull()
        => Assert.Null(PlaygroundController.SelectThemeByIndex(null, _themeChoices));

    [Fact]
    public void SelectThemeByIndex_NonIntegerValue_ReturnsNull()
        => Assert.Null(PlaygroundController.SelectThemeByIndex("abc", _themeChoices));

    [Fact]
    public void SelectThemeByIndex_NegativeIndex_ReturnsNull()
        => Assert.Null(PlaygroundController.SelectThemeByIndex("-1", _themeChoices));

    [Fact]
    public void SelectThemeByIndex_IndexAtLength_ReturnsNull()
        => Assert.Null(PlaygroundController.SelectThemeByIndex("3", _themeChoices));

    [Fact]
    public void SelectThemeByIndex_IndexBeyondLength_ReturnsNull()
        => Assert.Null(PlaygroundController.SelectThemeByIndex("999", _themeChoices));

    // ─── SelectColorMapByIndex ────────────────────────────────────────────────

    [Fact]
    public void SelectColorMapByIndex_ValidIndex_ReturnsColorMap()
    {
        var result = PlaygroundController.SelectColorMapByIndex("2", _colorMapChoices);
        Assert.NotNull(result);
        Assert.Same(ColorMaps.Inferno, result);
    }

    [Fact]
    public void SelectColorMapByIndex_NullValue_ReturnsNull()
        => Assert.Null(PlaygroundController.SelectColorMapByIndex(null, _colorMapChoices));

    [Fact]
    public void SelectColorMapByIndex_NonIntegerValue_ReturnsNull()
        => Assert.Null(PlaygroundController.SelectColorMapByIndex("abc", _colorMapChoices));

    [Fact]
    public void SelectColorMapByIndex_NegativeIndex_ReturnsNull()
        => Assert.Null(PlaygroundController.SelectColorMapByIndex("-1", _colorMapChoices));

    [Fact]
    public void SelectColorMapByIndex_IndexAtLength_ReturnsNull()
        => Assert.Null(PlaygroundController.SelectColorMapByIndex("3", _colorMapChoices));

    // ─── TryBuild ─────────────────────────────────────────────────────────────

    [Fact]
    public void TryBuild_ValidExample_ReturnsTrueWithSvgAndCode()
    {
        var opts = new PlaygroundOptions { Title = "Test" };
        bool ok = PlaygroundController.TryBuild(PlaygroundExample.LineChart, opts,
            out string svg, out string code, out string error);

        Assert.True(ok);
        Assert.Contains("<svg", svg);
        Assert.False(string.IsNullOrWhiteSpace(code));
        Assert.Equal("", error);
    }

    [Fact]
    public void TryBuild_UnregisteredExample_ReturnsFalseWithErrorSvg()
    {
        var opts = new PlaygroundOptions { Title = "Test" };
        bool ok = PlaygroundController.TryBuild((PlaygroundExample)999, opts,
            out string svg, out string code, out string error);

        Assert.False(ok);
        Assert.Contains("color:red", svg);
        Assert.Equal("", code);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
