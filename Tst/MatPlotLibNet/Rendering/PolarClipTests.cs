// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A mark of a polar axes that reaches past the outer ring runs on past it on the same scale and is cut off at the circle,
/// as matplotlib clips every line, collection and patch of a polar axes to the axes patch (printed, matplotlib 3.11.2:
/// <c>ax.plot([0, 1], [1, 5]); ax.set_rmax(2)</c> gives the line <c>clip_on True</c> and a <c>TransformedPatchPath</c> of the
/// <c>Wedge</c> as its clip path; a <c>Rectangle((0.5, 6), 0.5, 2)</c> added to it the same). It was flattened onto the rim. The
/// clip is pushed for every mark under the matplotlib themes; under the library's own only where a mark can reach past the circle (a radial
/// limit the caller set), so a polar axes whose scale holds everything draws as it did.</summary>
public class PolarClipTests
{
    private static readonly Rect Area = new(100, 100, 400, 400);
    private const double Radius = 200;
    private static readonly Point Centre = new(300, 300);

    private static double FromCentre(Point p) => Math.Sqrt((p.X - Centre.X) * (p.X - Centre.X) + (p.Y - Centre.Y) * (p.Y - Centre.Y));

    /// <summary>The index of the first call of <paramref name="kind"/>.</summary>
    private static int IndexOf<TCall>(RecordingRenderContext ctx) where TCall : CallRecord => ctx.Calls.FindIndex(c => c is TCall);

    /// <summary>The disc the clip of the axes is, at the plot's circle.</summary>
    private static void AssertTheDisc(CallRecord clip)
    {
        var outline = Assert.IsType<PushClipOutlineCall>(clip).Outline;
        Assert.Equal(new MoveToSegment(new Point(Centre.X + Radius, Centre.Y)), outline[0]);
        Assert.Equal(new ArcSegment(Centre, Radius, Radius, 0, 180), outline[1]);
    }

    [Fact]
    public void ALineToARadiusPastASetLimit_RunsPastTheRing_InsideTheClipOfTheCircle()
    {
        var axes = new Axes();
        axes.PolarPlot([1.0, 5.0], [0.0, 1.0]);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 2;

        var ctx = axes.Drawn(Area, Theme.MatplotlibV2);

        int clip = IndexOf<PushClipOutlineCall>(ctx);
        int line = IndexOf<DrawLinesCall>(ctx);
        Assert.InRange(clip, 0, line - 1);
        Assert.InRange(ctx.Calls.FindIndex(line, c => c is PopClipCall), line + 1, int.MaxValue);
        AssertTheDisc(ctx.Calls[clip]);
        var points = ((DrawLinesCall)ctx.Calls[line]).Points;
        Assert.Equal(Radius * 5 / 2, FromCentre(points[^1]), 9);
    }

    [Fact]
    public void APatchPastTheRing_UnderAMatplotlibTheme_IsDrawnPastItInsideTheClipOfTheCircle()
    {
        // Under the matplotlib themes add_patch asks for no range: the axes keeps (0, 1), and the rectangle at r 6..8 lies outside.
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        axes.AddPatch(new Rectangle(new(0.5, 6), 0.5, 2));

        var ctx = axes.Drawn(Area, Theme.MatplotlibV2);

        int clip = IndexOf<PushClipOutlineCall>(ctx);
        int path = IndexOf<DrawPathCall>(ctx);
        Assert.InRange(clip, 0, path - 1);
        AssertTheDisc(ctx.Calls[clip]);
        var segments = ((DrawPathCall)ctx.Calls[path]).Segments;
        var corner = Assert.IsType<MoveToSegment>(segments[0]).Point;
        Assert.True(FromCentre(corner) > Radius * 5, $"the corner at r 6 stands {FromCentre(corner)} px out");
        Assert.InRange(ctx.Calls.FindIndex(path, c => c is PopClipCall), path + 1, int.MaxValue);
    }

    [Fact]
    public void AnOriginBelowASetLowEnd_ClipsToTheRing()
    {
        var axes = new Axes();
        axes.PolarPlot([1.0, 5.0], [0.0, 1.0]);
        axes.YAxis.Min = 2;
        axes.YAxis.Max = 6;
        axes.ROrigin = 0;

        var ctx = axes.Drawn(Area, Theme.MatplotlibV2);

        var outline = ctx.Of<PushClipOutlineCall>()[0].Outline;
        Assert.Equal(2, outline.Count(s => s is MoveToSegment));
    }

    /// <summary>Under the library's own themes a polar axes whose scale holds every mark pushes no clip: it draws as it did.</summary>
    [Fact]
    public void APolarAxesWhoseScaleHoldsEveryMark_PushesNoClip_UnderTheLibrarysTheme()
    {
        var axes = new Axes();
        axes.PolarPlot([1.0, 5.0], [0.0, 1.0]);
        axes.PolarBar([2.0, 3.0], [0.5, 2.0]);

        var ctx = axes.Drawn(Area, Theme.Default);

        Assert.Equal(0, ctx.Count<PushClipOutlineCall>());
        Assert.Equal(0, ctx.Count<PushClipCall>());
    }

    /// <summary>Under the matplotlib themes every mark of a polar axes is cut off at the disc whether or not a limit was set, as matplotlib
    /// clips every artist to the axes patch (<c>clip_on True</c> under both styles): a bar's edge or a marker on the outer ring stops at the
    /// circle. One clip per series, the disc of the plot.</summary>
    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void UnderTheMatplotlibThemes_EveryMarkOfAPolarAxes_IsClippedToTheDisc(string theme)
    {
        var axes = new Axes();
        axes.PolarPlot([1.0, 5.0], [0.0, 1.0]);
        axes.PolarBar([2.0, 3.0], [0.5, 2.0]);

        var ctx = axes.Drawn(Area, theme.AsTheme());

        var clips = ctx.Of<PushClipOutlineCall>();
        Assert.Equal(2, clips.Count);
        AssertTheDisc(clips[0]);
    }

    /// <summary>Only the radial limit decides: a polar axes has no range along the angle, so a limit on its X axis lets no mark
    /// reach past the circle.</summary>
    [Fact]
    public void ALimitOnTheAngleAlone_PushesNoClip()
    {
        var axes = new Axes();
        axes.PolarPlot([1.0, 5.0], [0.0, 1.0]);
        axes.XAxis.Min = 0;
        axes.XAxis.Max = 1;

        Assert.Equal(0, axes.Drawn(Area, Theme.Default).Count<PushClipOutlineCall>());
    }

    [Fact]
    public void APatchThatRangesTheRadius_UnderTheLibrarysTheme_PushesNoClip()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        axes.AddPatch(new Rectangle(new(0.5, 6), 0.5, 2));

        var ctx = axes.Drawn(Area, Theme.Default);

        Assert.Equal(0, ctx.Count<PushClipOutlineCall>());
    }

    [Fact]
    public void TheSvg_CutsTheLineAtTheCircle_WithAClipPathOfTheCircle()
    {
        var svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 5.0], [0.0, 1.0]).SetYLim(0, 2)).Build().ToSvg();

        Assert.Contains(svg.Each("clipPath"), clip => clip.Children("path").SingleOrDefault()?.Attribute("d")?.Value is { } d
            && d.StartsWith("M ", StringComparison.Ordinal) && d.Split(" A ").Length >= 3 && d.EndsWith(" Z ", StringComparison.Ordinal));
    }
}
