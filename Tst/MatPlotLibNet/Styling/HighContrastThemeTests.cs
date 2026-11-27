// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>What only the HighContrast theme (WCAG AAA target) has: a bold font, minimums above the other themes' and a dark grid (what
/// every theme states is a row of <c>ThemeTests.Facts</c>).</summary>
public class HighContrastThemeTests
{
    [Fact]
    public void HighContrastTheme_HasBoldFont()
    {
        Assert.Equal(FontWeight.Bold, Theme.HighContrast.DefaultFont.Weight);
    }

    public static IEnumerable<object[]> Minimums() =>
    [
        ["the default font size", (Func<Theme, double>)(theme => theme.DefaultFont.Size), 13.0],
        ["the grid line width", (Func<Theme, double>)(theme => theme.DefaultGrid.LineWidth), 1.5],
        ["the number of cycle colours", (Func<Theme, double>)(theme => theme.CycleColors.Length), 8.0],
    ];

    /// <summary>The theme reads larger and bolder than the others: a larger font, a bolder grid, and at least eight colours in its cycle.</summary>
    [Theory]
    [MemberData(nameof(Minimums))]
    public void HighContrastTheme_IsAtLeast(string what, Func<Theme, double> measure, double least)
    {
        Assert.True(measure(Theme.HighContrast) >= least, $"{what} is {measure(Theme.HighContrast)}, below {least}");
    }

    [Fact]
    public void HighContrastTheme_GridIsDarker()
    {
        // The grid color should be dark (#666666 or similar)
        var c = Theme.HighContrast.DefaultGrid.Color;
        int luminance = (c.R + c.G + c.B) / 3;
        Assert.True(luminance <= 120, $"Grid color is too light: R={c.R} G={c.G} B={c.B}");
    }
}
