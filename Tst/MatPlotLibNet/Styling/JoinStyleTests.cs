// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The corner of a stroke is a value of the stroke (<see cref="StrokeStyle.Join"/>, <see cref="ShapeStyle.StrokeJoin"/>) and of the
/// theme (<see cref="StrokeLook.Join"/>). matplotlib 3.11.2 (python): <c>GraphicsContextBase().get_joinstyle()</c> is 'round' and so is
/// the join of a <c>Line2D</c> (<c>lines.solid_joinstyle</c> and <c>lines.dash_joinstyle</c> 'round' under both styles) and of a collection
/// (<c>PolyCollection().get_joinstyle()</c> is None, which draws with that default); a <c>Rectangle</c> (every bar and every patch) is
/// 'miter'. So a line and a barb are rounded under the matplotlib themes, a bar keeps its corner, and a library theme keeps its mitres.</summary>
public class JoinStyleTests
{
    private static string Svg(Action<SvgRenderContext> draw)
    {
        var ctx = new SvgRenderContext();
        draw(ctx);
        return ctx.GetOutput();
    }

    [Fact]
    public void AStrokeThatNamesNoJoin_IsMitred_AndWritesNoAttribute()
    {
        Assert.Null(new StrokeStyle(Colors.Black, 1, LineStyle.Solid).Join);
        Assert.Equal(JoinStyle.Miter, new ShapeStyle(null, Colors.Black, 1).StrokeJoin);
        Assert.DoesNotContain("stroke-linejoin", Svg(c => c.DrawLines([new Point(0, 0), new Point(5, 5), new Point(10, 0)], new StrokeStyle(Colors.Black, 2, LineStyle.Solid))));
    }

    [Theory]
    [InlineData(JoinStyle.Round, "round")]
    [InlineData(JoinStyle.Bevel, "bevel")]
    public void ALineWithAJoin_WritesItAfterItsDashes(JoinStyle join, string name)
    {
        var svg = Svg(c => c.DrawLines([new Point(0, 0), new Point(5, 5), new Point(10, 0)], new StrokeStyle(Colors.Black, 2, LineStyle.Dashed) { Join = join }));

        Assert.Contains($"stroke-width=\"2\" stroke-dasharray=\"5,2\" stroke-linejoin=\"{name}\"", svg);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AStrokeOfOneSegment_HasNoCorner_AndWritesNoJoin (#1)", () =>
        {
            var round = new StrokeStyle(Colors.Black, 2, LineStyle.Solid) { Join = JoinStyle.Round };
            return Svg(c => c.DrawLine(new Point(0, 0), new Point(10, 10), round));
        }) { Absent = ["stroke-linejoin"] },
        new DrawnToken("AStrokeOfOneSegment_HasNoCorner_AndWritesNoJoin (#2)", () =>
        {
            var round = new StrokeStyle(Colors.Black, 2, LineStyle.Solid) { Join = JoinStyle.Round };
            return Svg(c => c.DrawLines([new Point(0, 0), new Point(10, 10)], round));
        }) { Absent = ["stroke-linejoin"] },
        new DrawnToken("AStrokeOfOneSegment_HasNoCorner_AndWritesNoJoin (#3)", () =>
        {
            var round = new StrokeStyle(Colors.Black, 2, LineStyle.Solid) { Join = JoinStyle.Round };
            return Svg(c => c.DrawLines([new Point(0, 0), new Point(5, 5), new Point(10, 0)], round));
        }, "stroke-linejoin=\"round\""),
        new DrawnToken("AShapeWithAJoin_WritesItAfterItsStroke", () => Svg(c => c.DrawPolygon([new Point(0, 0), new Point(10, 0), new Point(5, 8)], new ShapeStyle(Colors.Red, Colors.Black, 2) { StrokeJoin = JoinStyle.Round })), "stroke-width=\"2\" stroke-linejoin=\"round\""),
        //  matplotlib's Rectangle is joined 'miter': the bar's corner is not rounded, nor is anything of the plot but its lines.
        new DrawnToken("ABarOfAFigure_KeepsItsMitre_UnderEveryTheme (#1)", () => FigureSvg(Theme.MatplotlibV2, ax => ax.Bar(["a", "b"], [1.0, 2.0]))) { Absent = ["stroke-linejoin"] },
        new DrawnToken("ABarOfAFigure_KeepsItsMitre_UnderEveryTheme (#2)", () => FigureSvg(Theme.MatplotlibClassic, ax => ax.Bar(["a", "b"], [1.0, 2.0]))) { Absent = ["stroke-linejoin"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A style is handed by value to every shape drawn, so a member that fits the padding of the layout costs none of that: the
    /// join of a shape is a byte, the join of a stroke (which may name none) a byte and its flag, and neither style grows by it.</summary>
    [Fact]
    public void TheStyles_AreNoBiggerForHoldingAJoin()
    {
        Assert.Equal(1, System.Runtime.CompilerServices.Unsafe.SizeOf<JoinStyle>());
        Assert.Equal(24, System.Runtime.CompilerServices.Unsafe.SizeOf<StrokeStyle>());
        Assert.Equal(96, System.Runtime.CompilerServices.Unsafe.SizeOf<ShapeStyle>());
    }

    [Fact]
    public void TheMatplotlibThemes_RoundTheCornerOfALine_AndALibraryThemeKeepsItsMitre()
    {
        Assert.Equal(JoinStyle.Round, Theme.MatplotlibV2.Looks.Strokes.Join);
        Assert.Equal(JoinStyle.Round, Theme.MatplotlibClassic.Looks.Strokes.Join);
        foreach (var theme in new[] { Theme.Default, Theme.Dark, Theme.Seaborn })
            Assert.Equal(JoinStyle.Miter, theme.Looks.Strokes.Join);
    }

    private static string FigureSvg(Theme theme, Action<AxesBuilder> axes) =>
        Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, axes).Build().ToSvg();

    [Fact]
    public void ALineSeriesOfAFigure_IsJoinedByTheFiguresTheme()
    {
        string Line(Theme theme) => FigureSvg(theme, ax => ax.Plot([0.0, 1.0, 2.0], [0.0, 1.0, 0.0], s => s.LineWidth = 2));

        Assert.Contains("stroke-width=\"2\" stroke-linejoin=\"round\"", Line(Theme.MatplotlibV2));
        Assert.Contains("stroke-width=\"2\" stroke-linejoin=\"round\"", Line(Theme.MatplotlibClassic));
        Assert.DoesNotContain("stroke-linejoin", Line(Theme.Default));
    }

    [Fact]
    public void ABarbOfAFigure_IsRoundedUnderTheMatplotlibThemes()
    {
        // quiver.Barbs is a PolyCollection, drawn with the default round join.
        string Barbs(Theme theme) => FigureSvg(theme, ax => ax.Barbs([0.0], [0.0], [65.0], [180.0]));

        Assert.Contains("stroke-linejoin=\"round\"", Barbs(Theme.MatplotlibV2));
        Assert.DoesNotContain("stroke-linejoin", Barbs(Theme.Default));
    }

    [Fact]
    public void TheJoinOfAMatplotlibTheme_SurvivesTheFigureJson_AndALibraryThemeWritesNone()
    {
        Assert.Equal(JoinStyle.Round, Theme.MatplotlibV2.ThroughDto().Looks.Strokes.Join);
        Assert.Equal(JoinStyle.Round, Theme.MatplotlibClassic.ThroughDto().Looks.Strokes.Join);
        Assert.Null(ThemeDto.From(Theme.Default).StrokeJoin);
        Assert.Equal(JoinStyle.Miter, Theme.Default.ThroughDto().Looks.Strokes.Join);
    }
}
