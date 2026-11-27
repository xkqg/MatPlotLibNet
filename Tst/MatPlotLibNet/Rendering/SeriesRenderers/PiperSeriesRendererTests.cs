// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A Piper diagram is drawn as a whole in the plot area, centred, at the one scale: the two triangles and the diamond
/// are equal parts of one frame, 2.2 triangle sides wide, so the triangles are equilateral at any size of the figure. Each
/// sample is a marker on each of the three panels, one colour and one size, so a reader follows a water from panel to panel.</summary>
public class PiperSeriesRendererTests
{
    private static readonly Rect Box = new(100, 50, 600, 400);

    private static WaterSample OneEach => new() { Ca = 20.039, Mg = 12.1525, Na = 22.98977, HCO3 = 61.0168, SO4 = 48.0313, Cl = 35.453 };

    private static WaterSample Seawater => new() { Cl = 19350, Na = 10760, SO4 = 2710, Mg = 1290, Ca = 411, K = 399, HCO3 = 142 };

    private static Point[][] Outlines(RecordingRenderContext ctx) =>
        [.. ctx.Of<DrawPolygonCall>().Where(a => a.Fill is null).Select(a => a.Points)];

    private static (Point[] Cations, Point[] Anions, Point[] Diamond) Panels(RecordingRenderContext ctx)
    {
        var outlines = Outlines(ctx);
        var triangles = outlines.Where(o => o.Length == 3).OrderBy(o => o.Min(p => p.X)).ToArray();
        return (triangles[0], triangles[1], outlines.Single(o => o.Length == 4));
    }

    [Fact]
    public void TheThreePanels_AreTwoEquilateralTrianglesAndADiamond_AtOneScale()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        var (cations, anions, diamond) = Panels(axes.Drawn(Box));

        double side = cations[0].DistanceTo(cations[1]);
        Assert.Equal(side, cations[1].DistanceTo(cations[2]), 6);
        Assert.Equal(side, cations[0].DistanceTo(cations[2]), 6);
        Assert.Equal(side, anions[0].DistanceTo(anions[1]), 6);
        Assert.Equal(side, anions[1].DistanceTo(anions[2]), 6);
        // The diamond's four sides are one triangle's side too.
        for (int i = 0; i < 4; i++)
            Assert.Equal(side, diamond[i].DistanceTo(diamond[(i + 1) % 4]), 6);
    }

    [Fact]
    public void TheAnionTriangle_StandsOneAndTwoTenthsSidesToTheRight_OnTheSameBase()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        var (cations, anions, _) = Panels(axes.Drawn(Box));

        double side = cations.Max(p => p.X) - cations.Min(p => p.X);
        Assert.Equal(1.2 * side, anions.Min(p => p.X) - cations.Min(p => p.X), 6);
        Assert.Equal(cations.Max(p => p.Y), anions.Max(p => p.Y), 9);   // the same base line
    }

    [Fact]
    public void TheWholeFrame_IsCentredInThePlotArea_AndFillsItsLimitingSide()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        var (cations, anions, diamond) = Panels(axes.Drawn(Box));

        var all = cations.Concat(anions).Concat(diamond).ToArray();
        double left = all.Min(p => p.X), right = all.Max(p => p.X), top = all.Min(p => p.Y), bottom = all.Max(p => p.Y);
        Assert.Equal(Box.X + Box.Width / 2, (left + right) / 2, 6);
        Assert.Equal(Box.Y + Box.Height / 2, (top + bottom) / 2, 6);
        Assert.True(left >= Box.X && right <= Box.X + Box.Width && top >= Box.Y && bottom <= Box.Y + Box.Height);
        Assert.Equal((right - left) / (bottom - top), 2.2 / (2.2 * Math.Sqrt(3) / 2), 6);   // the frame's own shape
    }

    [Fact]
    public void ASample_IsAMarkerOnEachPanel_InOneColour_AtWhereItsSharesStand()
    {
        var axes = new Axes();
        axes.Piper([OneEach]).MarkerSize = 10;

        var ctx = axes.Drawn(Box);
        var (cations, anions, diamond) = Panels(ctx);
        var circles = ctx.Of<DrawCircleCall>().ToArray();

        Assert.Equal(3, circles.Length);
        Assert.All(circles, c => Assert.Equal(10 * (100.0 / 72.0) / 2, c.Radius, 9));   // 10 points across
        Assert.Single(circles.Select(c => c.Fill).Distinct());

        double unit = cations.Max(p => p.X) - cations.Min(p => p.X);
        double baseY = cations.Max(p => p.Y);
        double cationLeft = cations.Min(p => p.X);
        var cationMarker = circles[0].Center;
        var anionMarker = circles[1].Center;
        var diamondMarker = circles[2].Center;

        // P3 stands at (0.5, 0.288675) and (1.7, 0.288675) of the frame, and the diamond at (1.1, 1.327906).
        Assert.Equal(cationLeft + 0.5 * unit, cationMarker.X, 6);
        Assert.Equal(baseY - 0.288675 * unit, cationMarker.Y, 4);
        Assert.Equal(cationLeft + 1.7 * unit, anionMarker.X, 6);
        Assert.Equal(baseY - 0.288675 * unit, anionMarker.Y, 4);
        double diamondBottom = diamond.Max(p => p.Y), diamondTop = diamond.Min(p => p.Y);
        Assert.Equal(cationLeft + 1.1 * unit, diamondMarker.X, 6);
        Assert.Equal((1.327906 - 0.173205) / (1.905256 - 0.173205), (diamondBottom - diamondMarker.Y) / (diamondBottom - diamondTop), 5);
    }

    [Fact]
    public void EachSample_WearsItsOwnColour_ThroughAllThreePanels()
    {
        var axes = new Axes();
        axes.Piper([OneEach, Seawater]);

        var circles = axes.Drawn(Box).Of<DrawCircleCall>().ToArray();

        Assert.Equal(6, circles.Length);
        Assert.Single(circles.Take(3).Select(c => c.Fill).Distinct());
        Assert.Single(circles.Skip(3).Select(c => c.Fill).Distinct());
        Assert.NotEqual(circles[0].Fill, circles[3].Fill);
    }

    [Fact]
    public void AnExplicitColour_IsWornByItsSample()
    {
        var axes = new Axes();
        axes.Piper([OneEach, Seawater]).Colors = [Color.FromHex("#C03030")];

        var circles = axes.Drawn(Box).Of<DrawCircleCall>().ToArray();

        Assert.All(circles.Take(3), c => Assert.Equal(Color.FromHex("#C03030"), c.Fill));
    }

    [Fact]
    public void TheGrid_IsTwelveLinesInEachTriangleAndEightInTheDiamond()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        Assert.Equal(32, axes.Drawn(Box).Count<DrawLineCall>());
    }

    [Fact]
    public void TheCornersAndTheTicks_AreNamed()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        var texts = axes.Drawn(Box).Of<DrawTextCall>().Select(c => c.Text).ToArray();

        foreach (string name in new[] { "Ca", "Mg", "Na+K", "HCO3+CO3", "SO4", "Cl" })
            Assert.Contains(name, texts);
        foreach (string tick in new[] { "20", "40", "60", "80" })
            Assert.Equal(6, texts.Count(t => t == tick));   // three per triangle edge set, two triangles
    }

    /// <summary>The two corners that face each other across the gap between the triangles (the sodium and potassium of the cations, the
    /// bicarbonate and carbonate of the anions) are named away from each other: the first ends at its corner and the second starts at
    /// its own, so the names do not run into one another over the gap, which is a fifth of a triangle wide.</summary>
    [Fact]
    public void TheCornersFacingEachOther_AreNamedAwayFromEachOther()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        var names = axes.Drawn(Box).Of<DrawTextCall>().ToArray();
        var cationSide = names.Single(t => t.Text == "Na+K");
        var anionSide = names.Single(t => t.Text == "HCO3+CO3");

        Assert.Equal(TextAlignment.Right, cationSide.Alignment);
        Assert.Equal(TextAlignment.Left, anionSide.Alignment);
        Assert.True(cationSide.Position.X < anionSide.Position.X, "the sodium corner is left of the bicarbonate corner");
    }

    [Fact]
    public void ADiagramWithNoSamples_DrawsTheFrameAndNoMarker()
    {
        var axes = new Axes();
        axes.Piper([]);

        var ctx = axes.Drawn(Box);

        Assert.Equal(0, ctx.Count<DrawCircleCall>());
        Assert.Equal(3, Outlines(ctx).Length);
    }

    [Fact]
    public void APiperFigure_RendersToSvg()
    {
        var figure = new Figure { Width = 700, Height = 450 };
        figure.AddSubPlot(1, 1, 1).Piper([OneEach, Seawater]);

        string svg = figure.ToSvg();

        Assert.Equal(6, svg.Elements("circle").Count);
    }

    [Fact]
    public void ABoxTooSmallForTheNamesAndTheTicks_DrawsNothing_AndDoesNotThrow()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);
        var ctx = new RecordingRenderContext();

        var thrown = Record.Exception(() => AxesRenderer.Create(axes, new Rect(0, 0, 20, 20), ctx, Theme.Default).Render());

        Assert.Null(thrown);
        Assert.Empty(ctx.Of<DrawPolygonCall>());
    }
}
