// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>The colour of the levels of a Fibonacci retracement. They are <c>axhline</c>s, and matplotlib 3.11.2's <c>axhline(y)</c> with no
/// colour draws in <c>lines.color</c> (<c>'C0'</c>, <c>#1f77b4</c>, under the default style; <c>'b'</c>, <c>#0000ff</c>, under the classic one;
/// printed with python: <c>plt.rcParams['lines.color']</c>) and asks no colour cycle. The library's own themes keep the orange the
/// indicator chose; a colour the caller gave wins under every theme.</summary>
public sealed class FibonacciColourTests
{
    private static string Svg(Theme theme, Color? color = null) =>
        Plt.Create().WithSize(500, 400).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Indicator(new FibonacciRetracement(90, 110) { Color = color })).Build().ToSvg();

    private static int Strokes(string svg, string hex) => svg.AllElements().Count(element => string.Equals(element["stroke"], hex, StringComparison.OrdinalIgnoreCase));

    [Theory]
    [InlineData("v2", "#1F77B4")]
    [InlineData("classic", "#0000FF")]
    public void UnderAMatplotlibStyle_TheLevels_AreLinesColor(string themeId, string hex) => Assert.Equal(5, Strokes(Svg(themeId.AsTheme()), hex));

    [Fact]
    public void TheLibrarysOwnThemes_KeepTheOrangeTheIndicatorChose()
    {
        Assert.Equal(5, Strokes(Svg(Theme.Default), Colors.FibonacciOrange.ToHex()));
        Assert.Equal(0, Strokes(Svg(Theme.MatplotlibV2), Colors.FibonacciOrange.ToHex()));
    }

    [Theory]
    [InlineData("default")]
    [InlineData("v2")]
    [InlineData("classic")]
    public void AColourTheCallerGave_WinsUnderEveryTheme(string themeId)
    {
        var theme = themeId.AsTheme();

        Assert.Equal(5, Strokes(Svg(theme, Colors.Red), Colors.Red.ToHex()));
    }

    [Theory]
    [InlineData("default")]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheChosenColour_SurvivesTheJson_SoTheFigureDrawsTheSameAfterARoundTrip(string themeId)
    {
        var theme = themeId.AsTheme();
        var figure = Plt.Create().WithSize(500, 400).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Indicator(new FibonacciRetracement(90, 110))).Build();

        string json = figure.ToJson();
        var restored = ChartSerializerDefault.Current.FromJson(json);

        Assert.Contains("\"verbColor\"", json, StringComparison.Ordinal);
        Assert.Equal(figure.ToSvg(), restored.ToSvg());
        Assert.Equal(json, restored.ToJson());
    }
}
