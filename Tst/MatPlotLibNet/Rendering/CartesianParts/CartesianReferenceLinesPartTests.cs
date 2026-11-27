// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianReferenceLinesPart"/> (Phase B.6).
/// Covers horizontal + vertical orientation, color fallback, label anchoring.
/// </summary>
public class CartesianReferenceLinesPartTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly DataRange Range = new(0, 10, 0, 10);

    private static CartesianReferenceLinesPart Build(Axes axes, RecordingRenderContext ctx) =>
        CartesianParts.PartFactory.ReferenceLines(axes, Plot, ctx, DefaultTheme, Plot.Linear(), Range);

    private static RecordingRenderContext Drawn(Axes axes) => CartesianParts.PartFactory.Drawn(ctx => Build(axes, ctx));

    [Fact]
    public void Render_VerticalLine_DrawsFullHeightLine()
    {
        var ax = new Axes();
        ax.AxVLine(5);
        var ctx = Drawn(ax);
        var line = ctx.Of<DrawLineCall>()[0];
        var p1 = line.P1;
        var p2 = line.P2;
        Assert.Equal(p1.X, p2.X);
        Assert.Equal(Plot.Y, p1.Y);
        Assert.Equal(Plot.Y + Plot.Height, p2.Y);
    }

    [Fact]
    public void Render_LineWithColor_UsesIt()
    {
        var ax = new Axes();
        var l = ax.AxHLine(5);
        l.Color = Color.FromHex("#FF0000");
        var ctx = Drawn(ax);
        Assert.Equal(Color.FromHex("#FF0000"),
            ctx.Of<DrawLineCall>()[0].Color);
    }

    [Fact]
    public void Render_LineWithoutColor_FallsBackToTheThemesNeutral()
    {
        var ax = new Axes();
        ax.AxVLine(5);
        var ctx = Drawn(ax);
        Assert.Equal(DefaultTheme.Neutral,
            ctx.Of<DrawLineCall>()[0].Color);
        Assert.Equal(Colors.Gray, DefaultTheme.Neutral);
    }

    /// <summary>The neutral is the theme's: Nord's plot is dark enough that #808080 stands at 2.55 : 1 against it.</summary>
    [Fact]
    public void Render_LineWithoutColor_UnderNord_WearsNordsNeutral()
    {
        var ax = new Axes();
        ax.AxVLine(5);
        var ctx = CartesianParts.PartFactory.Drawn(c => CartesianParts.PartFactory.ReferenceLines(ax, Plot, c, Theme.Nord, Plot.Linear(), Range));
        Assert.Equal(Color.FromHex("#8D8D8D"),
            ctx.Of<DrawLineCall>()[0].Color);
    }

    [Fact]
    public void Render_VerticalLineWithLabel_LeftAligned()
    {
        var ax = new Axes();
        var l = ax.AxVLine(5);
        l.Label = "Mark";
        var ctx = Drawn(ax);
        Assert.Equal(TextAlignment.Left,
            ctx.Of<DrawTextCall>()[0].Alignment);
    }

    /// <summary>What the reference lines of an axes draw: how many calls of a kind the part makes for the axes the row arranges.</summary>
    public static TheoryData<Action<Axes>, Type, int> Counts { get; } = new()
    {
        { ax => ax.AxHLine(5), typeof(DrawTextCall), 0 }, // Render_HorizontalLine_NoLabel_NoText
        { ax => ax.AxVLine(5), typeof(DrawTextCall), 0 }, // Render_VerticalLine_NoLabel_NoText
        { ax =>
        {
            ax.AxHLine(1);
            ax.AxVLine(2);
            ax.AxHLine(3);
        }, typeof(DrawLineCall), 3 }, // Render_MultipleLines_AllRendered
    };

    [Theory]
    [MemberData(nameof(Counts))]
    public void Render_DrawsAsManyCallsOfAKindAsTheAxesAsks(Action<Axes> arrange, Type call, int expected) =>
        PartFactory.AssertCallsDrawn(arrange, Drawn, call, expected);
}
