// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

    /// <summary>A hatched shape fills with a pattern reference, not a flat colour.</summary>
    [Fact]
    public void HatchedShape_FillsWithAPatternReference()
    {
        string svg = RenderRect(Hatched(HatchPattern.ForwardDiagonal, Colors.Black));

        Assert.Contains("<pattern", svg);
        Assert.Contains("fill=\"url(#", svg);
    }

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

        Assert.Equal(1, CountOccurrences(ctx.GetOutput(), "<pattern"));
    }

    /// <summary>Two DIFFERENT hatches yield two definitions — de-duplication keys on the pattern and its
    /// colours, not merely on "a hatch is present".</summary>
    [Fact]
    public void DifferentHatches_EmitTwoPatternDefinitions()
    {
        var ctx = new SvgRenderContext();
        ctx.DrawRectangle(new Rect(10, 10, 50, 50), Hatched(HatchPattern.ForwardDiagonal, Colors.Black));
        ctx.DrawRectangle(new Rect(80, 10, 50, 50), Hatched(HatchPattern.BackDiagonal, Colors.Black));

        Assert.Equal(2, CountOccurrences(ctx.GetOutput(), "<pattern"));
    }

    /// <summary>No hatch: the fill attribute keeps its exact byte layout — a flat colour, no pattern, no
    /// defs. This is the regression guard for every existing golden.</summary>
    [Fact]
    public void NoHatch_EmitsAFlatFillAndNoPattern()
    {
        string svg = RenderRect(new ShapeStyle(Colors.Gray, null, 0));

        Assert.Contains($"fill=\"{Colors.Gray.ToHex()}\"", svg);
        Assert.DoesNotContain("<pattern", svg);
    }

    /// <summary>A hatch WITHOUT an explicit hatch colour still paints visible lines — the caller never has
    /// to supply two colours to get a hatch.</summary>
    [Fact]
    public void HatchWithoutHatchColour_StillPaintsVisibleLines()
    {
        string svg = RenderRect(Hatched(HatchPattern.ForwardDiagonal));

        Assert.Contains("<pattern", svg);
        string pattern = svg[svg.IndexOf("<pattern", StringComparison.Ordinal)..];
        Assert.Contains("stroke=", pattern);
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

    /// <summary>End-to-end through the figure: a hatched bar reaches the canvas hatched. This property was
    /// inert until v1.14.</summary>
    [Fact]
    public void BarSeries_WithHatch_RendersHatched()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A", "B"], [3.0, 5.0], s =>
            {
                s.Hatch = HatchPattern.ForwardDiagonal;
                s.HatchColor = Colors.Black;
            }))
            .ToSvg();

        Assert.Contains("<pattern", svg);
        Assert.Contains("fill=\"url(#", svg);
    }

    /// <summary>End-to-end through the figure: a hatched filled area reaches the canvas hatched.</summary>
    [Fact]
    public void AreaSeries_WithHatch_RendersHatched()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.FillBetween([0.0, 1, 2], [1.0, 2, 1], null, s =>
            {
                s.Hatch = HatchPattern.BackDiagonal;
                s.HatchColor = Colors.Black;
            }))
            .ToSvg();

        Assert.Contains("<pattern", svg);
        Assert.Contains("fill=\"url(#", svg);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }

        return count;
    }
}
