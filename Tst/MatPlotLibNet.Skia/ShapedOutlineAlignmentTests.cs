// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>With Skia loaded, the SVG writer places a centred or right-aligned glyph outline by the advance of the shaping the
/// outline was made from: the installed font metrics are not asked for the width again. Swaps the process-wide metrics, so it
/// runs alone.</summary>
[Collection("SkiaFontsGlobalState")]
public sealed partial class ShapedOutlineAlignmentTests
{
    private static readonly Font Font13 = new() { Family = "DejaVu Sans", Size = 13 };

    private static double AlignOffset(TextAlignment alignment, out int measured)
    {
        double advance = SkiaGlyphPathProvider.MeasureAdvance("Ta", Font13);
        Assert.IsType<SkiaGlyphPathProvider>(GlyphPathProviderDefault.Current);
        var counting = new TestFontMetrics(FontMetricsDefault.Current.Measure);
        string svg;
        using (FontMetricsDefault.Holding(counting))
        {
            var ctx = new SvgRenderContext();
            ctx.DrawText("Ta", new Point(50, 60), Font13, alignment);
            svg = ctx.GetOutput();
        }

        measured = counting.Calls;
        const string Prefix = "translate(50,60) translate(";
        string? transform = svg.AttributeValues("transform").FirstOrDefault(value => value.StartsWith(Prefix, StringComparison.Ordinal) && value.EndsWith(",0)", StringComparison.Ordinal));
        Assert.True(transform is not null, svg);
        Assert.True(advance > 0);
        return transform![Prefix.Length..^3].Number() / advance;
    }

    [Theory]
    [InlineData(TextAlignment.Center, -0.5)] // a centred label starts half its shaped advance before the anchor
    [InlineData(TextAlignment.Right, -1.0)]  // a right-aligned label ends at the anchor
    public void AnAlignedLabel_StandsByItsShapedAdvance_WithoutBeingMeasuredAgain(TextAlignment alignment, double share)
    {
        double offset = AlignOffset(alignment, out int measured);

        Assert.Equal(0, measured);
        Assert.Equal(share, offset, 0.01);
    }

}
