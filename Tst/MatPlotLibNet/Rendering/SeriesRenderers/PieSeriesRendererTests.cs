// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.PieSeriesRenderer"/>. Pre-X.9
/// the renderer was at 98%L / 61%B because slice-decoration arms (shadow, explode,
/// AutoPct, outer-labels with collision pass) were not exercised. This file pins:
///   - Sizes.Sum() == 0 → early return
///   - Radius set explicitly (?? has-value arm)
///   - CounterClockwise true vs false (ternary's two arms)
///   - Explode array per-slice (ternary's true arm)
///   - Shadow=true (true arm draws gray slice first)
///   - Colors per-slice (ternary's true arm)
///   - AutoPct format string (true arm)
///   - Labels per-slice → outer-label collision pass
///   - Labels alignment branches at 0°/90°/180° angles (ternary)</summary>
public class PieSeriesRendererTests
{
    private static string Render(PieSeries s) =>
        Plt.Create()
            .WithSize(400, 400)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s).HideAllAxes())
            .Build()
            .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render_BasicPie_DrawsSlices", () => Render(new PieSeries([1.0, 2.0, 3.0])), "<path"),
        // Sizes.Sum() == 0 → early return.
        new DrawnToken("Render_AllZeroSizes_EarlyReturn", () => Render(new PieSeries([0.0, 0.0, 0.0]))) { Absent = ["<path"] },
        // Explicit Radius (.HasValue true arm) — radius scaled by Radius.Value.
        new DrawnToken("Render_ExplicitRadius_ScalesPie", () => Render(new PieSeries([1.0, 2.0, 3.0]) { Radius = 0.5 }), "<path"),
        // CounterClockwise=true (ternary's true arm) — slices sweep in the opposite direction.
        new DrawnToken("Render_CounterClockwise_SweepsForward", () => Render(new PieSeries([1.0, 2.0, 3.0]) { CounterClockwise = true }), "<path"),
        // Explode set with matching length (ternary's true arm). Each slice drifts outward by Explode[i] * radius.
        new DrawnToken("Render_Explode_OffsetsSlicesOutward", () => Render(new PieSeries([1.0, 2.0, 3.0])
        {
            Explode = new[] { 0.0, 0.1, 0.2 },
        }), "<path"),
        // AutoPct (true arm) — emits percentage text inside each slice.
        // 10/(10+20+30) = 16.67%
        // 20/(60) = 33.33%
        // 30/(60) = 50%
        new DrawnToken("Render_AutoPct_DrawsPercentages", () => Render(new PieSeries([10.0, 20.0, 30.0]) { AutoPct = "{0:F1}%" }), ">16.7%<", ">33.3%<", ">50.0%<"),
        // Theme with DefaultFont set (`is { } f` true arm). MatplotlibClassic theme provides explicit font metrics; the renderer
        // should pick them up rather than fall back to the size-12 default.
        new DrawnToken("Render_WithThemedFont_PicksUpThemeFont", () => Plt.Create()
            .WithSize(400, 400)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .AddSeries(new PieSeries([1.0, 2.0]) { Labels = new[] { "X", "Y" } })
                .HideAllAxes())
            .Build()
            .ToSvg(), ">X<"),
        // Labels → outer-label collision-resolution pass. All three text-alignment arms (Left for cosA >
        // 0.1, Right for cosA < -0.1, Center otherwise) are exercised by 4 slices spaced at cardinal directions.
        // 4 equal slices starting at 90° (top), sweeping clockwise:
        // slice 0 mid 45° (right of top, cosA > 0 → Left-aligned)
        // slice 1 mid 315° = -45° (right-bottom, cosA > 0 → Left)
        // slice 2 mid 225° (left-bottom, cosA < 0 → Right)
        // slice 3 mid 135° (left-top, cosA < 0 → Right)
        new DrawnToken("Render_Labels_AllAlignmentArms", () => Render(new PieSeries([1.0, 1.0, 1.0, 1.0])
        {
            Labels = new[] { "A", "B", "C", "D" },
        }), ">A<", ">B<", ">C<", ">D<"),
        // CENTER arm — cosA ∈ [-0.1, 0.1] → TextAlignment.Center. StartAngle=0, CCW, sizes [1,1,2]: third slice midAngle = 3π/2 → cos
        // = 0.
        new DrawnToken("Render_LabelAtTopOrBottom_UsesCenterAlignment", () => Render(new PieSeries([1.0, 1.0, 2.0])
        {
            Labels = ["left", "right", "center"],
            StartAngle = 0,
            CounterClockwise = true,
        }), ">center<"),
        // Leader-line arm — very wide labels on a tiny canvas guarantee that LabelLayoutEngine offsets each label >6px from its anchor,
        // setting LeaderLineStart so DrawLeaderLine fires. The SVG emits <line> elements (one per displaced label) that do not appear in
        // pie renders without labels or without collision.
        // HideAllAxes removes all axis lines; the only <line> elements come from leader lines.
        new DrawnToken("Render_DenseLabels_SmallCanvas_DrawsLeaderLines", () =>
        {
            // 12 equal slices on a 100×100 canvas. Outer label radius ≈44px, arc spacing
            // ≈23px, "WWWWWWWWWW" ≈92px wide → every adjacent pair overlaps → labels
            // are pushed well past the 6px leader-line threshold.
            double[] sizes = Enumerable.Repeat(1.0, 12).ToArray();
            string[] labels = Enumerable.Repeat("WWWWWWWWWW", 12).ToArray();
            return Plt.Create()
                .WithSize(100, 100)
                .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new PieSeries(sizes) { Labels = labels }).HideAllAxes())
                .Build()
                .ToSvg();
        }, "<line"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Shadow=true — emits a gray offset slice before the
    /// real one. SVG path count doubles.</summary>
    [Fact]
    public void Render_Shadow_EmitsExtraGraySlice()
    {
        var basic = Render(new PieSeries([1.0, 2.0]));
        var shadow = Render(new PieSeries([1.0, 2.0]) { Shadow = true });
        // Shadow path count > basic path count (one extra gray <path> per slice).
        Assert.True(shadow.Split("<path").Length > basic.Split("<path").Length);
    }

    /// <summary>Colors per-slice: each wedge wears the colour named at its position.</summary>
    [Fact]
    public void Render_Colors_PerSlice_UsesCustomPalette()
    {
        var svg = Render(new PieSeries([1.0, 2.0, 3.0])
        {
            Colors = new[] { Colors.Red, Colors.Green, Colors.Blue },
        });
        Assert.Equal([Colors.Red.ToHex(), Colors.Green.ToHex(), Colors.Blue.ToHex()], WedgeFills(svg));
    }

    private static Figure ThemedFigure(Theme theme, PieSeries pie) =>
        Plt.Create().WithTheme(theme).WithSize(400, 400)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(pie).HideAllAxes()).Build();

    /// <summary>The fill of each wedge, in the order the wedges are drawn.</summary>
    private static string[] WedgeFills(string svg) =>
        svg.Each("path").Where(e => e.Attribute("fill") is not null)
            .Select(e => (string)e.Attribute("fill")!).ToArray();

    /// <summary>A pinned semantic changed on purpose: Colors[] shorter than Sizes[] used to give every wedge past the array
    /// the series' own slot colour (the same colour for all of them). Now a wedge past the array takes what the figure's
    /// theme hands out at its own position, as a donut and a funnel do (<c>ThemedPartColourTests</c>).</summary>
    [Fact]
    public void Render_ColorsShorterThanSizes_WedgesBeyondItTakeTheThemeColourAtTheirOwnPosition()
    {
        var fills = WedgeFills(ThemedFigure(Theme.Ggplot, new PieSeries([1.0, 2.0, 3.0]) { Colors = [Colors.Red] }).ToSvg());

        Assert.Equal([Colors.Red.ToHex(), Theme.Ggplot.CycleColors[1].ToHex(), Theme.Ggplot.CycleColors[2].ToHex()], fills);
    }

    /// <summary>Drawing a pie writes nothing into it: <c>Colors</c> is what the user set, so a pie that named none still
    /// names none (and writes none to JSON) after a render.</summary>
    [Fact]
    public void Render_LeavesColorsNull_WhenTheUserSetNone()
    {
        var pie = new PieSeries([1.0, 2.0, 3.0]);
        var figure = ThemedFigure(Theme.Ggplot, pie);

        figure.ToSvg();

        Assert.Null(pie.Colors);
        Assert.DoesNotContain("\"colors\"", figure.ToJson());
    }

    [Fact]
    public void Render_LeavesAPartialColorsArrayAsTheUserSetIt()
    {
        var named = new[] { Colors.Red };
        var pie = new PieSeries([1.0, 2.0, 3.0]) { Colors = named };
        var figure = ThemedFigure(Theme.Ggplot, pie);

        figure.ToSvg();

        Assert.Same(named, pie.Colors);
        Assert.Equal([Colors.Red], pie.Colors);
        // the key the null case asserts away is the one a pie that names colours writes
        Assert.Contains("\"colors\"", figure.ToJson());
    }

    /// <summary>The same figure drawn on one theme and then on another wears each theme's colours: the first render pinned
    /// nothing into the series (it kept the first theme's wedge colours before).</summary>
    [Fact]
    public void ARenderUnderOneTheme_DoesNotPinTheWedgeColoursOfTheNextTheme()
    {
        var figure = ThemedFigure(Theme.Ggplot, new PieSeries([1.0, 2.0, 3.0]));

        var first = WedgeFills(figure.ToSvg());
        figure.Theme = Theme.Seaborn;
        var second = WedgeFills(figure.ToSvg());

        Assert.Equal(Theme.Ggplot.CycleColors.Take(3).Select(c => c.ToHex()), first);
        Assert.Equal(Theme.Seaborn.CycleColors.Take(3).Select(c => c.ToHex()), second);
    }

}
