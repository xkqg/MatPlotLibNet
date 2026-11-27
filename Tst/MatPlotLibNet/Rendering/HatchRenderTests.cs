// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that <see cref="HatchPattern"/> actually reaches the canvas.
/// <para>Before v1.14 the enum and the <c>Hatch</c> properties on <see cref="BarSeries"/> and
/// <see cref="AreaSeries"/> existed but no renderer consumed them: setting a hatch produced a plain
/// solid fill and no error — decorative public API. These tests pin the hatch down to the emitted
/// SVG so the property can never silently do nothing again.</para></summary>
public class HatchRenderTests
{
    private static string RenderRect(ShapeStyle shape)
    {
        var ctx = new SvgRenderContext();
        ctx.DrawRectangle(new Rect(10, 10, 50, 50), shape);
        return ctx.GetOutput();
    }

    private static ShapeStyle Hatched(HatchPattern hatch, Color? hatchColor = null) =>
        new(Colors.Gray, null, 0) { Hatch = hatch, HatchColor = hatchColor };

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // A hatched shape fills with a pattern reference, not a flat colour.
        new DrawnToken("HatchedShape_FillsWithAPatternReference", () => RenderRect(Hatched(HatchPattern.ForwardDiagonal, Colors.Black)), "<pattern", "fill=\"url(#"),
        // End-to-end through the figure: a hatched bar reaches the canvas hatched. This property was inert until v1.14.
        new DrawnToken("BarSeries_WithHatch_RendersHatched", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A", "B"], [3.0, 5.0], s =>
            {
                s.Hatch = HatchPattern.ForwardDiagonal;
                s.HatchColor = Colors.Black;
            }))
            .ToSvg(), "<pattern", "fill=\"url(#"),
        // End-to-end through the figure: a hatched filled area reaches the canvas hatched.
        new DrawnToken("AreaSeries_WithHatch_RendersHatched", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.FillBetween([0.0, 1, 2], [1.0, 2, 1], null, s =>
            {
                s.Hatch = HatchPattern.BackDiagonal;
                s.HatchColor = Colors.Black;
            }))
            .ToSvg(), "<pattern", "fill=\"url(#"),
        // No hatch: the fill attribute keeps its exact byte layout — a flat colour, no pattern, no defs. This is the regression guard for
        // every existing golden.
        new DrawnToken("NoHatch_EmitsAFlatFillAndNoPattern", () => RenderRect(new ShapeStyle(Colors.Gray, null, 0)), $"fill=\"{Colors.Gray.ToHex()}\"") { Absent = ["<pattern"] },
        // Opaque colours write the tile they always wrote: no opacity anywhere in it.
        new DrawnToken("AnOpaqueFaceAndHatch_WriteNoOpacityInTheTile", () =>
        {
            string svg = RenderRect(Hatched(HatchPattern.Cross, Colors.Black));
            return svg[svg.IndexOf("<pattern", StringComparison.Ordinal)..svg.IndexOf("</pattern>", StringComparison.Ordinal)];
        }) { Absent = ["opacity"] },
        new DrawnToken("ATranslucentDotHatch_FadesEachDotAsOne", () =>
        {
            string svg = RenderRect(new ShapeStyle(Colors.Gray, null, 0) { Hatch = HatchPattern.Dots, HatchColor = Colors.Black.WithAlpha(64) });
            return svg[svg.IndexOf("<pattern", StringComparison.Ordinal)..svg.IndexOf("</pattern>", StringComparison.Ordinal)];
        }, $"fill=\"#000000\" stroke=\"#000000\" opacity=\"{(64 / 255.0).ToString("G", CultureInfo.InvariantCulture)}\""),
        // A hatch WITHOUT an explicit hatch colour still paints visible lines — the caller never has to supply two colours to get a hatch.
        new DrawnToken("HatchWithoutHatchColour_StillPaintsVisibleLines (the pattern)", () => RenderRect(Hatched(HatchPattern.ForwardDiagonal)), "<pattern"),
        new DrawnToken("HatchWithoutHatchColour_StillPaintsVisibleLines (the lines)", () =>
        {
            string svg = RenderRect(Hatched(HatchPattern.ForwardDiagonal));
            return svg[svg.IndexOf("<pattern", StringComparison.Ordinal)..];
        }, "stroke="),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>The pattern carries BOTH the base fill and the hatch strokes: a background rect in the
    /// shape's own colour, the lines on top of it in the hatch colour.</summary>
    [Fact]
    public void HatchPattern_CarriesBaseFillAndHatchColour()
    {
        string svg = RenderRect(Hatched(HatchPattern.ForwardDiagonal, Colors.Red));

        int defs = svg.IndexOf("<defs", StringComparison.Ordinal);
        int end = svg.IndexOf("</defs>", StringComparison.Ordinal);
        Assert.True(defs >= 0 && end > defs, "no <defs> block was emitted for the hatch");

        string block = svg[defs..end];
        Assert.Contains(Colors.Gray.ToHex(), block);
        Assert.Contains(Colors.Red.ToHex(), block);
    }

    /// <summary>Two shapes with the SAME hatch share one pattern definition. Gradients allocate a fresh id
    /// per call (one gradient per Sankey link); a hatch is a series-wide style repeated across every mark,
    /// so it de-duplicates on (pattern, fill, hatch-colour).</summary>
    [Fact]
    public void SameHatchTwice_EmitsOnePatternDefinition()
    {
        var style = Hatched(HatchPattern.ForwardDiagonal, Colors.Black);

        var ctx = new SvgRenderContext();
        ctx.DrawRectangle(new Rect(10, 10, 50, 50), style);
        ctx.DrawRectangle(new Rect(80, 10, 50, 50), style);

        Assert.Equal(1, ctx.GetOutput().CountOf("<pattern"));
    }

    /// <summary>Two DIFFERENT hatches yield two definitions — de-duplication keys on the pattern and its
    /// colours, not merely on "a hatch is present".</summary>
    [Fact]
    public void DifferentHatches_EmitTwoPatternDefinitions()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawRectangle(new Rect(10, 10, 50, 50), Hatched(HatchPattern.ForwardDiagonal, Colors.Black));
        ctx.DrawRectangle(new Rect(80, 10, 50, 50), Hatched(HatchPattern.BackDiagonal, Colors.Black));

        Assert.Equal(2, ctx.GetOutput().CountOf("<pattern"));
    }

    /// <summary>The tile of a hatch keeps the opacity of the face it is laid over and of its own colour, as a flat fill and a
    /// stroke keep theirs and as Skia paints the tile: the hex colour carries none, so a translucent face writes
    /// <c>fill-opacity</c> on the tile's face and a translucent hatch line is written by the one writer of a stroke drawn from a
    /// colour, with <c>stroke-opacity</c> after its width, as a line of the chart is (a line is a stroke alone, so this draws what
    /// the <c>opacity</c> the tile first wrote drew). Before, a hatched face with an opacity was drawn opaque in the SVG.</summary>
    [Fact]
    public void ATranslucentFaceAndHatch_KeepTheirOpacityInTheTile()
    {
        string svg = RenderRect(new ShapeStyle(Colors.Gray.WithAlpha(128), null, 0)
        {
            Hatch = HatchPattern.Cross,
            HatchColor = Colors.Black.WithAlpha(64),
        });
        string tile = svg[svg.IndexOf("<pattern", StringComparison.Ordinal)..svg.IndexOf("</pattern>", StringComparison.Ordinal)];

        Assert.Contains($"fill=\"{Colors.Gray.ToHex()}\" fill-opacity=\"{(128 / 255.0).ToString("G", CultureInfo.InvariantCulture)}\"", tile);
        Assert.Equal(2, tile.CountOf($"stroke=\"#000000\" stroke-width=\"1\" stroke-opacity=\"{(64 / 255.0).ToString("G", CultureInfo.InvariantCulture)}\" />"));
    }

    /// <summary>Every declared pattern is reachable: each member of the enum renders its own definition.</summary>
    [Theory]
    [InlineData(HatchPattern.ForwardDiagonal)]
    [InlineData(HatchPattern.BackDiagonal)]
    [InlineData(HatchPattern.Horizontal)]
    public void EveryHatchPattern_EmitsADefinition(HatchPattern pattern)
    {
        string svg = RenderRect(Hatched(pattern, Colors.Black));

        Assert.Contains("<pattern", svg);
        Assert.Contains("fill=\"url(#", svg);
    }
}
