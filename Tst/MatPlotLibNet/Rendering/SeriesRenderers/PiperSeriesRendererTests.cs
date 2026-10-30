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

    private static RecordingRenderContext Draw(Axes axes)
    {
        var ctx = new RecordingRenderContext();
        AxesRenderer.Create(axes, Box, ctx, Theme.Default).Render();
        return ctx;
    }

    private static Point[][] Outlines(RecordingRenderContext ctx) =>
        [.. ctx.OfKind("DrawPolygon").Select(c => (dynamic)c.Args!).Where(a => a.fill is null).Select(a => (Point[])a.points)];

    private static (Point[] Cations, Point[] Anions, Point[] Diamond) Panels(RecordingRenderContext ctx)
    {
        var outlines = Outlines(ctx);
        var triangles = outlines.Where(o => o.Length == 3).OrderBy(o => o.Min(p => p.X)).ToArray();
        return (triangles[0], triangles[1], outlines.Single(o => o.Length == 4));
    }

    private static double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    [Fact]
    public void TheThreePanels_AreTwoEquilateralTrianglesAndADiamond_AtOneScale()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        var (cations, anions, diamond) = Panels(Draw(axes));

        double side = Distance(cations[0], cations[1]);
        Assert.Equal(side, Distance(cations[1], cations[2]), 6);
        Assert.Equal(side, Distance(cations[0], cations[2]), 6);
        Assert.Equal(side, Distance(anions[0], anions[1]), 6);
        Assert.Equal(side, Distance(anions[1], anions[2]), 6);
        // The diamond's four sides are one triangle's side too.
        for (int i = 0; i < 4; i++)
            Assert.Equal(side, Distance(diamond[i], diamond[(i + 1) % 4]), 6);
    }

    [Fact]
    public void TheAnionTriangle_StandsOneAndTwoTenthsSidesToTheRight_OnTheSameBase()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        var (cations, anions, _) = Panels(Draw(axes));

        double side = cations.Max(p => p.X) - cations.Min(p => p.X);
        Assert.Equal(1.2 * side, anions.Min(p => p.X) - cations.Min(p => p.X), 6);
        Assert.Equal(cations.Max(p => p.Y), anions.Max(p => p.Y), 9);   // the same base line
    }

    [Fact]
    public void TheWholeFrame_IsCentredInThePlotArea_AndFillsItsLimitingSide()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        var (cations, anions, diamond) = Panels(Draw(axes));

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

        var ctx = Draw(axes);
        var (cations, anions, diamond) = Panels(ctx);
        var circles = ctx.OfKind("DrawCircle").Select(c => (dynamic)c.Args!).ToArray();

        Assert.Equal(3, circles.Length);
        Assert.All(circles, c => Assert.Equal(5.0, (double)c.radius));
        Assert.Single(circles.Select(c => (Color?)c.fill).Distinct());

        double unit = cations.Max(p => p.X) - cations.Min(p => p.X);
        double baseY = cations.Max(p => p.Y);
        double cationLeft = cations.Min(p => p.X);
        var cationMarker = (Point)circles[0].center;
        var anionMarker = (Point)circles[1].center;
        var diamondMarker = (Point)circles[2].center;

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

        var circles = Draw(axes).OfKind("DrawCircle").Select(c => (dynamic)c.Args!).ToArray();

        Assert.Equal(6, circles.Length);
        Assert.Single(circles.Take(3).Select(c => (Color?)c.fill).Distinct());
        Assert.Single(circles.Skip(3).Select(c => (Color?)c.fill).Distinct());
        Assert.NotEqual((Color?)circles[0].fill, (Color?)circles[3].fill);
    }

    [Fact]
    public void AnExplicitColour_IsWornByItsSample()
    {
        var axes = new Axes();
        axes.Piper([OneEach, Seawater]).Colors = [Color.FromHex("#C03030")];

        var circles = Draw(axes).OfKind("DrawCircle").Select(c => (dynamic)c.Args!).ToArray();

        Assert.All(circles.Take(3), c => Assert.Equal(Color.FromHex("#C03030"), (Color?)c.fill));
    }

    [Fact]
    public void TheGrid_IsTwelveLinesInEachTriangleAndEightInTheDiamond()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        Assert.Equal(32, Draw(axes).CountOf("DrawLine"));
    }

    [Fact]
    public void TheCornersAndTheTicks_AreNamed()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);

        var texts = Draw(axes).OfKind("DrawText").Select(c => (string)((dynamic)c.Args!).text).ToArray();

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

        var names = Draw(axes).OfKind("DrawText").Select(c => (dynamic)c.Args!).ToArray();
        var cationSide = names.Single(t => (string)t.text == "Na+K");
        var anionSide = names.Single(t => (string)t.text == "HCO3+CO3");

        Assert.Equal(TextAlignment.Right, (TextAlignment)cationSide.alignment);
        Assert.Equal(TextAlignment.Left, (TextAlignment)anionSide.alignment);
        Assert.True(cationSide.position.X < anionSide.position.X, "the sodium corner is left of the bicarbonate corner");
    }

    [Fact]
    public void ADiagramWithNoSamples_DrawsTheFrameAndNoMarker()
    {
        var axes = new Axes();
        axes.Piper([]);

        var ctx = Draw(axes);

        Assert.Equal(0, ctx.CountOf("DrawCircle"));
        Assert.Equal(3, Outlines(ctx).Length);
    }

    [Fact]
    public void APiperFigure_RendersToSvg()
    {
        var figure = new Figure { Width = 700, Height = 450 };
        figure.AddSubPlot(1, 1, 1).Piper([OneEach, Seawater]);

        string svg = figure.ToSvg();

        Assert.Equal(6, System.Text.RegularExpressions.Regex.Matches(svg, "<circle").Count);
    }

    [Fact]
    public void ABoxTooSmallForTheNamesAndTheTicks_DrawsNothing_AndDoesNotThrow()
    {
        var axes = new Axes();
        axes.Piper([OneEach]);
        var ctx = new RecordingRenderContext();

        var thrown = Record.Exception(() => AxesRenderer.Create(axes, new Rect(0, 0, 20, 20), ctx, Theme.Default).Render());

        Assert.Null(thrown);
        Assert.Empty(ctx.OfKind("DrawPolygon"));
    }
}
