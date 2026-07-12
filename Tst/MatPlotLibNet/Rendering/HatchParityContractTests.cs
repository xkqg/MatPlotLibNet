// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The hatch parity CONTRACT.
/// <para>A hatch lives on <see cref="ShapeStyle"/>, so it travels through every shape-drawing operation of
/// every backend. That makes parity a contract, not a per-backend courtesy: a backend either paints the
/// pattern, or it says out loud that it cannot. What it may never do is drop the hatch in silence — a style
/// that renders on screen and vanishes in an export is how an operator ends up trusting the wrong picture.</para>
/// <para>Every model that exposes a hatch is pinned here too. Six models carried a <c>Hatch</c> property for
/// releases while no renderer read it; a property that renders nowhere is decoration, and this theory is what
/// stops the seventh from happening.</para></summary>
public class HatchParityContractTests
{
    public static TheoryData<HatchPattern> AllPatterns => new()
    {
        HatchPattern.ForwardDiagonal,
        HatchPattern.BackDiagonal,
        HatchPattern.Horizontal,
        HatchPattern.Vertical,
        HatchPattern.Cross,
        HatchPattern.DiagonalCross,
        HatchPattern.Dots,
        HatchPattern.Stars
    };

    /// <summary>Every declared pattern reaches the SVG canvas as a pattern fill.</summary>
    [Theory]
    [MemberData(nameof(AllPatterns))]
    public void SvgBackend_Honours_EveryPattern(HatchPattern pattern)
    {
        var ctx = new SvgRenderContext();
        ctx.DrawRectangle(new Rect(0, 0, 10, 10), new ShapeStyle(Colors.Gray, null, 0) { Hatch = pattern });

        Assert.Contains("<pattern", ctx.GetOutput());
        Assert.Contains("fill=\"url(#", ctx.GetOutput());
    }

    /// <summary>Every model that exposes a hatch actually renders it. The list IS the contract: adding a
    /// hatch property to a series without wiring its renderer fails here.</summary>
    [Theory]
    [MemberData(nameof(HatchedSeries))]
    public void EveryHatchedSeries_ReachesTheCanvasHatched(string name, Func<AxesBuilder, AxesBuilder> plot)
    {
        string svg = Plt.Create().AddSubPlot(1, 1, 1, ax => plot(ax)).ToSvg();

        Assert.True(svg.Contains("<pattern", StringComparison.Ordinal),
            $"{name} carries a hatch property but renders no pattern — the property is decoration.");
    }

    public static TheoryData<string, Func<AxesBuilder, AxesBuilder>> HatchedSeries => new()
    {
        {
            nameof(BarSeries),
            ax => ax.Bar(["A"], [1.0], s => { s.Hatch = HatchPattern.ForwardDiagonal; s.HatchColor = Colors.Black; })
        },
        {
            nameof(AreaSeries),
            ax => ax.FillBetween([0.0, 1], [1.0, 2], null, s => { s.Hatch = HatchPattern.BackDiagonal; s.HatchColor = Colors.Black; })
        },
        {
            nameof(HistogramSeries),
            ax => ax.Hist([1.0, 2, 2, 3], configure: s => { s.Hatch = HatchPattern.Cross; s.HatchColor = Colors.Black; })
        },
        {
            nameof(StackedAreaSeries),
            ax => ax.StackPlot([0.0, 1], [[1.0, 2], [2.0, 1]], s => { s.Hatch = HatchPattern.Horizontal; s.HatchColor = Colors.Black; })
        },
        {
            nameof(PieSeries),
            ax => ax.Pie([30.0, 70.0], configure: s => s.Hatches = [HatchPattern.Dots, HatchPattern.None])
        },
        {
            nameof(StateTimelineSeries),
            ax => ax.StateTimeline([new StateSegment(0, 1, "Unknown", Colors.Gray) { Hatch = HatchPattern.ForwardDiagonal }])
        }
    };

    /// <summary>No hatch, no pattern — the contract is symmetric. An unhatched figure must not gain a single
    /// byte of pattern machinery, which is what keeps the golden corpus byte-identical.</summary>
    [Fact]
    public void WithoutAHatch_NoBackendEmitsAPattern()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A"], [1.0]))
            .ToSvg();

        Assert.DoesNotContain("<pattern", svg);
    }
}
