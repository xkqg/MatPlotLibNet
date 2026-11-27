// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A patch of a ternary axes is cut off at the triangle, as mpltern clips every artist of a ternary axes to the axes
/// patch (printed, mpltern 1.0.5 on matplotlib 3.11.2: a <c>Rectangle((0.8, 0.6), 0.5, 0.5)</c> added to a ternary axes has
/// <c>clip_on True</c> and a <c>TransformedPatchPath</c> of the axes' triangle <c>Polygon</c> as its clip path, under the default
/// and the classic style). It was drawn past the triangle's right edge. A series of the axes is a composition closed to
/// fractions, which always lies in the triangle, so it is not clipped.</summary>
public class TernaryClipTests
{
    private static readonly Rect Area = new(100, 100, 400, 400);

    private static int IndexOf<TCall>(RecordingRenderContext ctx) where TCall : CallRecord => ctx.Calls.FindIndex(c => c is TCall);

    [Fact]
    public void APatchPastTheTriangle_IsDrawnInsideTheClipOfTheTriangle()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Ternary };
        axes.AddPatch(new Rectangle(new(0.8, 0.6), 0.5, 0.5));
        var frame = new DataTransform(0, 1, 0, TernaryFrame.Height, Area);

        var ctx = axes.Drawn(Area, Theme.MatplotlibV2);

        int clip = IndexOf<PushClipOutlineCall>(ctx);
        int path = IndexOf<DrawPathCall>(ctx);
        Assert.InRange(clip, 0, path - 1);
        Assert.InRange(ctx.Calls.FindIndex(path, c => c is PopClipCall), path + 1, int.MaxValue);
        var outline = ((PushClipOutlineCall)ctx.Calls[clip]).Outline;
        Assert.Equal(
            [
                new MoveToSegment(frame.DataToPixel(0.5, TernaryFrame.Height)),
                new LineToSegment(frame.DataToPixel(0, 0)),
                new LineToSegment(frame.DataToPixel(1, 0)),
                new CloseSegment(),
            ],
            outline);
    }

    [Fact]
    public void TheSeriesOfATernaryAxes_AreNotClipped()
    {
        var axes = new Axes();
        axes.TernaryScatter([0.2, 0.6], [0.3, 0.2], [0.5, 0.2]);

        var ctx = axes.Drawn(Area, Theme.MatplotlibV2);

        Assert.Equal(0, ctx.Count<PushClipOutlineCall>());
        Assert.Equal(0, ctx.Count<PushClipCall>());
    }

    [Fact]
    public void TheSvg_CutsThePatchAtTheTriangle_WithAClipPathOfTheTriangle()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.CoordinateSystem = CoordinateSystem.Ternary;
        axes.AddPatch(new Rectangle(new(0.8, 0.6), 0.5, 0.5));

        Assert.Contains(figure.ToSvg().Each("clipPath"), clip => clip.Children("path").SingleOrDefault()?.Attribute("d")?.Value is { } d
            && d.StartsWith("M ", StringComparison.Ordinal) && d.Split(" L ").Length >= 3 && d.EndsWith(" Z ", StringComparison.Ordinal));
    }
}
