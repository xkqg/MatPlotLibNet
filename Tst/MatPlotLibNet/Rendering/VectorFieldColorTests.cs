// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The colour a quiver, its key and barbs are drawn in when they name none. matplotlib 3.11.2: <c>Quiver.__init__(...,
/// color='k', ...)</c> with <c>kwargs.setdefault('facecolors', color)</c>, and <c>Barbs.__init__</c>: "Set to facecolor passed in
/// or default to black", <c>kwargs.setdefault('facecolors', 'k')</c>; a <c>QuiverKey</c> draws its arrow with its quiver's
/// properties. These are defaults of the calls, not of a style, so both matplotlib styles draw them black; the matplotlib
/// themes do the same. The library's own themes keep the colour of the series' place in the cycle, which a dark theme can show.
/// A colour given wins under every theme, and a quiver that shares a colour group with other series (a connected scatter's
/// arrows) keeps the group's colour.</summary>
public sealed class VectorFieldColorTests
{
    private static readonly double[] X = [0.0, 1, 2];
    private static readonly double[] Y = [0.0, 1, 0];
    private static readonly double[] U = [1.0, 0.5, -1];
    private static readonly double[] V = [0.5, 1, 0.5];

    private static string Svg(Theme theme, Func<AxesBuilder, AxesBuilder> draw) =>
        Plt.Create().WithTheme(theme).WithSize(300, 200).AddSubPlot(1, 1, 1, ax => draw(ax)).ToSvg();

    private static string Stroke(Color color) => $"stroke=\"{color.ToHex()}\"";

    /// <summary>The arrow of an autoscaled quiver (the matplotlib themes') is a polygon filled with its colour and has no stroke.</summary>
    private static bool HasArrow(string svg, Color color) => svg.Elements("polygon").Any(polygon => polygon["fill"] == color.ToHex());

    private static string FirstCycleColor(Theme theme) => Stroke(theme.CycleColors[0]);

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void MatplotlibThemes_DrawAQuiverAndItsKeyBlack(string themeId)
    {
        var theme = themeId.AsTheme();
        var svg = Svg(theme, ax => ax.Quiver(X, Y, U, V).QuiverKey(0.9, 0.9, 1, "1 unit"));

        Assert.DoesNotContain(FirstCycleColor(theme), svg, StringComparison.Ordinal);
        Assert.DoesNotContain(Stroke(theme.CycleColors[1]), svg, StringComparison.Ordinal);
        Assert.False(HasArrow(svg, theme.CycleColors[0]));
        Assert.True(HasArrow(svg, Colors.Black));                                              // the quiver
        Assert.Contains(Stroke(Colors.Black), svg, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void MatplotlibThemes_DrawBarbsBlack(string themeId)
    {
        var theme = themeId.AsTheme();
        var svg = Svg(theme, ax => ax.Barbs(X, Y, [25.0, 40, 65], [0.0, 45, 90]));

        Assert.DoesNotContain(FirstCycleColor(theme), svg, StringComparison.Ordinal);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("TheLibrarysThemes_KeepTheCycleColour (#1)", () => Svg(Theme.Default, ax => ax.Quiver(X, Y, U, V)), FirstCycleColor(Theme.Default)),
        new DrawnToken("TheLibrarysThemes_KeepTheCycleColour (#2)", () => Svg(Theme.Dark, ax => ax.Barbs(X, Y, [25.0, 40, 65], [0.0, 45, 90])), FirstCycleColor(Theme.Dark)),
        // A connected scatter draws its steps as quiver arrows in the colour group of its points, so the arrows and the points share the
        // cycle's colour under the matplotlib themes too.
        new DrawnToken("AConnectedScattersArrows_KeepTheirGroupsColour", () => Svg(Theme.MatplotlibV2, ax => ax.ConnectedScatter([0.0, 1, 2], [0.0, 1, 0])), FirstCycleColor(Theme.MatplotlibV2)),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void AGivenColour_Wins()
    {
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Quiver(X, Y, U, V, s => s.Color = Colors.Red));

        Assert.True(HasArrow(svg, Colors.Red));
    }

    /// <summary>The rule travels with the theme: built from a matplotlib theme and read back from JSON.</summary>
    [Fact]
    public void TheThemeCarriesTheColour()
    {
        Assert.Equal(Colors.Black, Theme.MatplotlibV2.Looks.VectorField.Color);
        Assert.Equal(Colors.Black, Theme.MatplotlibClassic.Looks.VectorField.Color);
        Assert.Null(Theme.Default.Looks.VectorField.Color);
        Assert.Equal(Colors.Black, Theme.MatplotlibV2.Cloned().Looks.VectorField.Color);

        var back = Plt.Create().WithTheme(Theme.MatplotlibV2).Build().RoundTripped();
        Assert.Equal(Colors.Black, back.Theme.Looks.VectorField.Color);
        Assert.Null(Theme.Dark.ThroughJson().Looks.VectorField.Color);
    }
}
