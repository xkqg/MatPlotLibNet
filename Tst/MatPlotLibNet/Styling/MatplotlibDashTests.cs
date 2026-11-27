// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The matplotlib themes dash a line as matplotlib 3.11.2 does (read with python,
/// <c>plt.style.context(...)</c> and <c>Line2D(..., linestyle=..., linewidth=2.0)._dash_pattern</c>), in every backend:
/// <list type="bullet">
/// <item>the default style (<see cref="Theme.MatplotlibV2"/>): <c>lines.dashed_pattern</c> [3.7, 1.6], <c>lines.dotted_pattern</c> [1, 1.65],
/// <c>lines.dashdot_pattern</c> [6.4, 1.6, 1, 1.6] with <c>lines.scale_dashes</c> True: a line of 2 pt is dashed [7.4, 3.2],
/// [2, 3.3] and [12.8, 3.2, 2, 3.2] points, each length times the width;</item>
/// <item>the classic style (<see cref="Theme.MatplotlibClassic"/>): [6, 6], [1, 3] and [3, 5, 1, 5] with <c>lines.scale_dashes</c>
/// False: the same points at any width;</item>
/// </list>
/// and a point is 100 / 72 pixels. A library theme keeps its own pixels (<see cref="DashLookTests"/>).</summary>
public class MatplotlibDashTests
{
    private const double Pt = 100.0 / 72;

    private static string Dashes(Theme theme, LineStyle style, double width)
    {
        var ctx = new SvgRenderContext();
        ctx.UseLooksOf(theme);
        ctx.DrawLine(new Point(0, 0), new Point(10, 10), new StrokeStyle(Colors.Black, width, style));
        var svg = ctx.GetOutput();
        int at = svg.IndexOf("stroke-dasharray=\"", StringComparison.Ordinal) + "stroke-dasharray=\"".Length;
        return svg[at..svg.IndexOf('"', at)];
    }

    [Theory]
    [InlineData(LineStyle.Dashed, 2.0, "7.4,3.2")]
    [InlineData(LineStyle.Dotted, 2.0, "2,3.3")]
    [InlineData(LineStyle.DashDot, 2.0, "12.8,3.2,2,3.2")]
    [InlineData(LineStyle.Dashed, 1.0, "3.7,1.6")]
    public void UnderTheDefaultStyle_EachLengthIsTheMatplotlibPatternTimesTheWidth(LineStyle style, double width, string expected)
    {
        Assert.Equal(expected, Dashes(Theme.MatplotlibV2, style, width));
    }

    [Theory]
    [InlineData(LineStyle.Dashed, 2.0, "8.33,8.33")]
    [InlineData(LineStyle.Dashed, 0.69, "8.33,8.33")]
    [InlineData(LineStyle.Dotted, 3.0, "1.39,4.17")]
    [InlineData(LineStyle.DashDot, 1.0, "4.17,6.94,1.39,6.94")]
    public void UnderTheClassicStyle_EachLengthIsTheMatplotlibPatternInPoints_WhateverTheWidth(LineStyle style, double width, string expected)
    {
        Assert.Equal(expected, Dashes(Theme.MatplotlibClassic, style, width));
    }

    [Fact]
    public void TheLooksHoldTheMatplotlibRcParams()
    {
        var v2 = Theme.MatplotlibV2.Looks.Strokes.Dashes;
        var classic = Theme.MatplotlibClassic.Looks.Strokes.Dashes;

        Assert.Equal([3.7, 1.6], v2.Dashed);
        Assert.Equal([1.0, 1.65], v2.Dotted);
        Assert.Equal([6.4, 1.6, 1.0, 1.6], v2.DashDot);
        Assert.True(v2.ScalesWithWidth);
        Assert.Equal(1.0, v2.Unit);
        Assert.Equal([6.0, 6.0], classic.Dashed);
        Assert.Equal([1.0, 3.0], classic.Dotted);
        Assert.Equal([3.0, 5.0, 1.0, 5.0], classic.DashDot);
        Assert.False(classic.ScalesWithWidth);
        Assert.Equal(Pt, classic.Unit);
    }

    /// <summary>A line two points wide drawn dashed on a figure of the given theme.</summary>
    private static string DashedSvg(Theme theme) => Plt.Create().WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0], s => { s.LineWidth = 2; s.LineStyle = LineStyle.Dashed; }))
        .Build().ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ADashedSeriesOfAFigure_IsDashedByTheFiguresTheme (#1)", () => DashedSvg(Theme.MatplotlibV2), "stroke-width=\"2\" stroke-dasharray=\"7.4,3.2\""),
        new DrawnToken("ADashedSeriesOfAFigure_IsDashedByTheFiguresTheme (#2)", () => DashedSvg(Theme.MatplotlibClassic), "stroke-width=\"2\" stroke-dasharray=\"8.33,8.33\""),
        new DrawnToken("ADashedSeriesOfAFigure_IsDashedByTheFiguresTheme (#3)", () => DashedSvg(Theme.Default), "stroke-width=\"2\" stroke-dasharray=\"5,2\""),
        //  classic: a dotted grid line is 1 pt on and 3 pt off: 1.39 and 4.17 px, at any width.
        new DrawnToken("ADashedGridLine_IsDashedByTheFiguresTheme", () => Plt.Create().WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).WithGrid(g => g with { Visible = true, LineStyle = LineStyle.Dotted }))
            .Build().ToSvg(), "stroke-dasharray=\"1.39,4.17\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void TheDashLookOfAMatplotlibTheme_SurvivesTheFigureJson_AndALibraryThemeWritesNone()
    {
        var v2 = ThemeDto.From(Theme.MatplotlibV2).ToTheme();
        var classic = ThemeDto.From(Theme.MatplotlibClassic).ToTheme();

        Assert.Equal(Theme.MatplotlibV2.Looks.Strokes.Dashes.Dashed, v2.Looks.Strokes.Dashes.Dashed);
        Assert.Equal(Theme.MatplotlibV2.Looks.Strokes.Dashes.DashDot, v2.Looks.Strokes.Dashes.DashDot);
        Assert.True(v2.Looks.Strokes.Dashes.ScalesWithWidth);
        Assert.Equal(Theme.MatplotlibClassic.Looks.Strokes.Dashes.Dotted, classic.Looks.Strokes.Dashes.Dotted);
        Assert.False(classic.Looks.Strokes.Dashes.ScalesWithWidth);
        Assert.Equal(100.0 / 72, classic.Looks.Strokes.Dashes.Unit);

        var library = ThemeDto.From(Theme.Default);
        Assert.Null(library.DashedPattern);
        Assert.Null(library.DottedPattern);
        Assert.Null(library.DashDotPattern);
        Assert.Null(library.DashesScaleWithWidth);
        Assert.Null(library.DashUnit);
        Assert.Same(DashLook.Library, Theme.Default.ThroughDto().Looks.Strokes.Dashes);
    }
}
