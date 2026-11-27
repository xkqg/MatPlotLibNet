// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What every drawing surface of the library does with a call, whatever it draws on: the rules are the core's (an opacity is
/// held between 0 and 1, a turn is counter-clockwise, a font is bold when its weight is bold), and a surface holds only the conversion
/// of each to its own canvas. The SVG surface is held here; the PNG and PDF surface by the same-named class of the Skia suite, the MAUI
/// surface by the one of its suite, and the source of all three by the rows of <c>OneOwnerSourceContractTests</c> and the facts below.</summary>
public class RenderContextConformanceTests
{
    /// <summary>An opacity a caller states outside 0 to 1 is held at the nearest end, by every surface: the PNG surface always did,
    /// the SVG surface wrote the number it was given (<c>opacity="-0.5"</c> is no opacity a viewer reads).</summary>
    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.25, 0.25)]
    [InlineData(1.0, 1.0)]
    [InlineData(1.5, 1.0)]
    public void ASurface_HoldsAnOpacityBetweenZeroAndOne(double asked, double held)
    {
        var svg = new SvgRenderContext();

        svg.SetOpacity(asked);

        Assert.Equal(held, ((IOpacityRenderContext)svg).Opacity);
    }

    /// <summary>The group the SVG surface opens for a translucent run of marks writes the held opacity, so a negative opacity writes
    /// none a viewer cannot read, and an opacity of 1 or more opens no group.</summary>
    [Theory]
    [InlineData(-0.5, "<g opacity=\"0\">")]
    [InlineData(0.5, "<g opacity=\"0.5\">")]
    public void TheSvgGroupOfATranslucentRun_WritesTheHeldOpacity(double asked, string group)
    {
        var svg = new SvgRenderContext();

        svg.SetOpacity(asked);

        Assert.Contains(group, svg.GetOutput(), StringComparison.Ordinal);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AnOpacityOfOneOrMore_OpensNoSvgGroup", () =>
        {
            var svg = new SvgRenderContext();
            svg.SetOpacity(1.5);
            return svg.GetOutput();
        }) { Absent = ["<g opacity"] },
        // A polygon is drawn from the points it is given, the outline of two points being the line between them, on every surface: the SVG
        // surface always wrote it, the PNG surface returned before it drew a stroke a single-sample area is outlined with.
        new DrawnToken("APolygonOfTwoPoints_IsWrittenAsTheOutlineItIs", () =>
        {
            var svg = new SvgRenderContext();
            svg.DrawPolygon([new Point(3, 4), new Point(3, 20)], new ShapeStyle(null, Colors.Black, 1));
            return svg.GetOutput();
        }, "<polygon points=\"3,4 3,20\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A polyline needs two points to be a line, whichever surface draws it: the rule is the core's (<c>FormsALine</c>), and a
    /// line of fewer points writes nothing on every surface.</summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    public void APolyline_FormsALine_FromTwoPoints(int count, bool forms)
    {
        var points = Enumerable.Range(0, count).Select(i => new Point(i, i)).ToList();

        Assert.Equal(forms, points.FormsALine);
    }

    [Fact]
    public void ALineOfOnePoint_WritesNothing()
    {
        var svg = new SvgRenderContext();

        svg.DrawLines([new Point(3, 4)], StrokeStyle.Solid(Colors.Black, 1));

        Assert.Equal(string.Empty, svg.GetOutput());
    }
}
