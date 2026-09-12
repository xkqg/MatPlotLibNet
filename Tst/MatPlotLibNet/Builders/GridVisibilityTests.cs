// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>
/// Turning the grid off on one axes. The renderer used to read <c>Axes.Grid.Visible ? Axes.Grid :
/// Theme.DefaultGrid</c>, which cannot tell "the caller asked for no grid" from "the caller said nothing" — so
/// under any theme that draws a grid, asking for none silently drew one anyway. Measured on
/// <see cref="Theme.OpsPanel"/>, which is a gridded theme: the request changed nothing at all.
/// </summary>
public class GridVisibilityTests
{
    /// <summary>Grid lines are the thin ones drawn in the theme's grid colour, before the series.</summary>
    private static int GridLines(string svg, string colour) =>
        Regex.Matches(svg, @"<line[^>]*stroke=""" + colour + @"""", RegexOptions.IgnoreCase).Count;

    private static string Render(bool hideGrid)
    {
        var figure = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0, 3.0], [1.0, 4.0, 9.0]);
                if (hideGrid)
                {
                    ax.WithGrid(g => g with { Visible = false });
                }
            })
            .WithTheme(Theme.OpsPanel);

        return figure.ToSvg();
    }

    [Fact]
    public void AGriddedTheme_DrawsItsGrid()
    {
        Assert.True(GridLines(Render(hideGrid: false), "#B7BDC0") > 0,
            "Theme.OpsPanel is a gridded theme; without one this test proves nothing");
    }

    [Fact]
    public void AskingForNoGrid_TurnsItOff_EvenUnderAGriddedTheme()
    {
        Assert.Equal(0, GridLines(Render(hideGrid: true), "#B7BDC0"));
    }

    [Fact]
    public void AskingForADifferentGrid_StillGetsThatGrid()
    {
        string svg = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [1.0, 2.0])
                .WithGrid(g => g with { Visible = true, Color = Colors.Red }))
            .WithTheme(Theme.OpsPanel)
            .ToSvg();

        Assert.True(GridLines(svg, "#FF0000") > 0);
    }

    [Fact]
    public void SayingNothing_LeavesTheThemeInCharge()
    {
        // A theme without a grid still draws none, and a theme with one still draws it — the fallback is what
        // every chart that never mentions the grid depends on.
        Assert.True(GridLines(Render(hideGrid: false), "#B7BDC0") > 0);
    }

    [Fact]
    public void AskingForMoreGrid_KeepsTheThemesGridAndAddsToIt()
    {
        // Measured before this: a request that did not mention visibility was thrown away whole, so asking for
        // minor grid lines on a themed chart drew the theme's major-only grid. The Playground's own MinorGrid
        // example was one of them.
        int themed = GridLines(Plt.Create().WithSize(400, 300).WithTheme(Theme.OpsPanel)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 4.0, 9.0]).WithMinorTicks())
            .ToSvg(), "#B7BDC0");

        int withMinor = GridLines(Plt.Create().WithSize(400, 300).WithTheme(Theme.OpsPanel)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 4.0, 9.0]).WithMinorTicks()
                .WithGrid(g => g with { Which = GridWhich.Both }))
            .ToSvg(), "#B7BDC0");

        Assert.True(withMinor > themed, $"asking for minor grid lines gave {withMinor}, the theme alone gave {themed}");
    }

    [Fact]
    public void AskingForOneProperty_LeavesTheRestOfTheThemeAlone()
    {
        string svg = Plt.Create().WithSize(400, 300).WithTheme(Theme.OpsPanel)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0])
                .WithGrid(g => g with { LineWidth = 3.0 }))
            .ToSvg();

        // The theme's colour survives; only the width changed.
        Assert.True(GridLines(svg, "#B7BDC0") > 0);
        Assert.Contains("stroke-width=\"3\"", svg, StringComparison.Ordinal);
    }
}
