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
    private static readonly ThemeChoice[] _themeChoices =
    [
        new(Theme.Default, "Default"),
        new(Theme.Dark,    "Dark"),
        new(Theme.Seaborn, "Seaborn"),
    ];

    private static readonly IColorMap[] _colorMapChoices =
        [ColorMaps.Viridis, ColorMaps.Plasma, ColorMaps.Inferno];

    // ─── selecting a theme or a colour map by its index ─────────────────────────

    [Fact]
    public void AnIndexOfAChoice_SelectsThatChoice()
    {
        Assert.Same(Theme.Dark, PlaygroundController.SelectThemeByIndex("1", _themeChoices));
        Assert.Same(ColorMaps.Inferno, PlaygroundController.SelectColorMapByIndex("2", _colorMapChoices));
    }

    /// <summary>A value that is no number, or no index of a choice, selects nothing, in either list.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("3")]
    [InlineData("999")]
    public void AValueThatIsNoIndexOfAChoice_SelectsNothing(string? value)
    {
        Assert.Null(PlaygroundController.SelectThemeByIndex(value, _themeChoices));
        Assert.Null(PlaygroundController.SelectColorMapByIndex(value, _colorMapChoices));
    }

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

    // ─── BuildDataTable ───────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  The playground shows what the library does, and what it does now is give the picture a readable alternative. The preview pane
        // can show it because the figure can produce it — same figure, same options.
        new DrawnToken("BuildDataTable_ValidExample_IsTheFiguresDataAsAnHtmlTable", () => PlaygroundController.BuildDataTable(PlaygroundExample.LineChart, new PlaygroundOptions { Title = "Test" }), "<table", "<caption>Test", "scope=\"col\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>An example that cannot be built has no table either — and says so with nothing, not with an
    /// exception: the preview pane already reports the failure once, on the chart.</summary>
    [Fact]
    public void BuildDataTable_UnregisteredExample_IsEmpty()
        => Assert.Equal("", PlaygroundController.BuildDataTable((PlaygroundExample)999, new PlaygroundOptions()));
}
