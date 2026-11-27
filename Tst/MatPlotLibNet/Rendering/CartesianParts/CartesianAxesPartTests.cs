// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>What every part of a Cartesian render has in common (<see cref="MatPlotLibNet.Rendering.CartesianParts.CartesianAxesPart"/>):
/// a part with nothing to draw (no annotations, no breaks, no reference lines, no patches, no signals, no spans, no tools) draws
/// nothing, not a clip, not a group. Each part keeps the tests of what it draws in its own class.</summary>
public class CartesianAxesPartTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly DataRange Range = new(0, 10, 0, 10);

    /// <summary>The parts of a Cartesian render, as each is built over axes with nothing of its own.</summary>
    private static readonly Dictionary<string, Func<Axes, RecordingRenderContext, MatPlotLibNet.Rendering.CartesianParts.CartesianAxesPart>> Parts = new()
    {
        ["Annotations"] = (axes, ctx) => PartFactory.Annotations(axes, Plot, ctx, DefaultTheme, Plot.Linear()),
        ["BreakMarkers"] = (axes, ctx) => PartFactory.BreakMarkers(axes, Plot, ctx, DefaultTheme, Plot.Linear()),
        ["Patches"] = (axes, ctx) => PartFactory.Patches(axes, Plot, ctx, DefaultTheme, Plot.Linear(), axes.RangeOf(DefaultTheme).PatchesMayReachPastPlot),
        ["ReferenceLines"] = (axes, ctx) => PartFactory.ReferenceLines(axes, Plot, ctx, DefaultTheme, Plot.Linear(), Range),
        ["Signals"] = (axes, ctx) => PartFactory.Signals(axes, Plot, ctx, DefaultTheme, Plot.Linear()),
        ["Spans"] = (axes, ctx) => PartFactory.Spans(axes, Plot, ctx, DefaultTheme, Plot.Linear(), Range),
        ["Tools"] = (axes, ctx) => PartFactory.Tools(axes, Plot, ctx, DefaultTheme, Plot.Linear()),
    };

    public static TheoryData<string> EveryPart { get; } = [.. Parts.Keys];

    [Theory]
    [MemberData(nameof(EveryPart))]
    public void APartWithNothingToDraw_DrawsNothing(string name)
    {
        var ctx = PartFactory.Drawn(c => Parts[name](new Axes(), c));

        Assert.Empty(ctx.Calls);
    }

    /// <summary>A part of your own reads the tick and label fonts of the theme it is drawn under through the conveniences of the base.</summary>
    [Fact]
    public void APartOfYourOwn_ReadsTheTickAndLabelFontsOfItsTheme()
    {
        var part = new OwnPart(Theme.MatplotlibV2);

        Assert.Equal(Theme.MatplotlibV2.TickFont(), part.Tick);
        Assert.Equal(Theme.MatplotlibV2.LabelFont(), part.Label);
    }

    /// <summary>A part as a library user writes one: it draws nothing and hands out the fonts of the base.</summary>
    private sealed class OwnPart(Theme theme) : MatPlotLibNet.Rendering.CartesianParts.CartesianAxesPart(new Axes(), new Rect(0, 0, 100, 100),
        new MatPlotLibNet.Rendering.Svg.SvgRenderContext(), theme, new DataTransform(0, 1, 0, 1, new Rect(0, 0, 100, 100)))
    {
        internal Font Tick => TickFont();

        internal Font Label => LabelFont();

        public override void Render() { }
    }
}
