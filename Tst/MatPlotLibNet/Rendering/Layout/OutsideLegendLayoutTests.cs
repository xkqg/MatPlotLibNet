// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>
/// Covers the Feature C deliverable: outside legend reservation in
/// <see cref="ConstrainedLayoutEngine"/> via the shared <see cref="LegendLayoutCalculator.MeasureLegendBox"/>.
/// </summary>
public class OutsideLegendLayoutTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Measured legend box invariants
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LegendBox_EmptyLabels_ReturnsZeroSize()
    {
        var axes = new Axes();
        axes.Plot([1.0, 2.0], [1.0, 2.0]);  // series with no label
        var size = LegendLayoutCalculator.MeasureLegendBox(axes, new SvgRenderContext(), Theme.MatplotlibV2);
        Assert.Equal(Size.Empty, size);
    }

    [Fact]
    public void LegendBox_LongLabels_ProduceLargerBoxThanShortLabels()
    {
        var shortAxes = new Axes();
        shortAxes.Plot([1.0, 2.0], [1.0, 2.0]).Label = "A";
        var longAxes = new Axes();
        longAxes.Plot([1.0, 2.0], [1.0, 2.0]).Label = "A very long descriptive label indeed";

        var ctx = new SvgRenderContext();
        var shortBox = LegendLayoutCalculator.MeasureLegendBox(shortAxes, ctx, Theme.MatplotlibV2);
        var longBox = LegendLayoutCalculator.MeasureLegendBox(longAxes, ctx, Theme.MatplotlibV2);

        Assert.True(longBox.Width > shortBox.Width,
            $"Expected long label to widen box; got short={shortBox.Width}, long={longBox.Width}");
    }

    /// <summary>Whether a position stands outside the plot is said by its strategy, the one owner of what a legend position
    /// does: the four outside positions, and no other.</summary>
    [Fact]
    public void OnlyTheFourOutsidePositions_StandOutsideThePlot()
    {
        LegendPosition[] outside = [LegendPosition.OutsideRight, LegendPosition.OutsideLeft, LegendPosition.OutsideTop, LegendPosition.OutsideBottom];
        foreach (LegendPosition position in Enum.GetValues<LegendPosition>())
            Assert.Equal(outside.Contains(position), LegendPositionStrategyFactory.Create(position).StandsOutside);
    }

    private static readonly Sides<double> Needs = new(Left: 10, Bottom: 20, Top: 30, Right: 40);
    private static readonly Size Box = new(50, 25);

    private static Sides<double> Reserved(LegendPosition position, Sides<double> needs, Sides<double>? past = null) =>
        LegendPositionStrategyFactory.Create(position).ReserveFor(Box, needs, past ?? needs);

    /// <summary>An outside box keeps what its edge needs for what the box stands past (what the axes draws there and the pad), the box
    /// and the gap on either side of it (16), and never less than the edge needs already: a colour bar in the margin stands beside the
    /// legend, not under it. Box 50 x 25.</summary>
    [Theory]
    [InlineData(LegendPosition.OutsideRight, 40, 0, 66)]    // nothing drawn past the right spine: the box and its gaps
    [InlineData(LegendPosition.OutsideRight, 100, 0, 100)]  // a colour bar needs more
    [InlineData(LegendPosition.OutsideRight, 40, 40, 106)]  // mirrored labels past the right spine: the box stands past them
    [InlineData(LegendPosition.OutsideLeft, 10, 10, 76)]
    [InlineData(LegendPosition.OutsideTop, 30, 30, 71)]
    [InlineData(LegendPosition.OutsideBottom, 20, 20, 61)]
    [InlineData(LegendPosition.OutsideBottom, 100, 20, 100)] // a colour bar below needs more
    public void AnOutsideLegend_KeepsWhatItStandsPast_TheBoxAndItsGaps_AtLeastWhatTheEdgeNeeds(LegendPosition position, double need, double past, double kept)
    {
        Sides<double> On(double value) => position switch
        {
            LegendPosition.OutsideRight => Needs with { Right = value },
            LegendPosition.OutsideLeft => Needs with { Left = value },
            LegendPosition.OutsideTop => Needs with { Top = value },
            _ => Needs with { Bottom = value },
        };

        Assert.Equal(On(kept), Reserved(position, On(need), On(past)));
    }

    [Fact]
    public void ALegendInsideThePlot_ReservesNothing()
    {
        foreach (LegendPosition position in Enum.GetValues<LegendPosition>())
            if (!LegendPositionStrategyFactory.Create(position).StandsOutside)
                Assert.Equal(Needs, Reserved(position, Needs));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ConstrainedLayoutEngine — outside legend reservation
    // ──────────────────────────────────────────────────────────────────────────

    private static Figure BuildFigure(LegendPosition position, params string[] labels)
    {
        var fb = new FigureBuilder()
            .WithSize(800, 600)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                foreach (var label in labels)
                    ax.Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = label);
                ax.WithLegend(l => l with { Position = position });
            });
        return fb.Build();
    }

    [Fact]
    public void OutsideRight_InflatesRightMargin_AboveDefault()
    {
        var insideFig = BuildFigure(LegendPosition.UpperRight,
            "Alpha", "Beta", "Gamma", "Delta — a longer label");
        var outsideFig = BuildFigure(LegendPosition.OutsideRight,
            "Alpha", "Beta", "Gamma", "Delta — a longer label");

        var engine = new ConstrainedLayoutEngine();
        var ctx = new SvgRenderContext();
        var inside = engine.Compute(insideFig, ctx);
        var outside = engine.Compute(outsideFig, ctx);

        Assert.True(outside.MarginRight > inside.MarginRight,
            $"OutsideRight should widen right margin; got inside={inside.MarginRight}, outside={outside.MarginRight}");
    }

    [Fact]
    public void OutsideLeft_InflatesLeftMargin_AboveDefault()
    {
        var insideFig = BuildFigure(LegendPosition.UpperLeft,
            "Alpha", "Beta", "Gamma");
        var outsideFig = BuildFigure(LegendPosition.OutsideLeft,
            "Alpha", "Beta", "Gamma");

        var engine = new ConstrainedLayoutEngine();
        var ctx = new SvgRenderContext();
        var inside = engine.Compute(insideFig, ctx);
        var outside = engine.Compute(outsideFig, ctx);

        Assert.True(outside.MarginLeft > inside.MarginLeft,
            $"OutsideLeft should widen left margin; got inside={inside.MarginLeft}, outside={outside.MarginLeft}");
    }

    [Fact]
    public void OutsideBottom_InflatesBottomMargin_AboveDefault()
    {
        var insideFig = BuildFigure(LegendPosition.LowerCenter, "A", "B");
        var outsideFig = BuildFigure(LegendPosition.OutsideBottom, "A", "B");

        var engine = new ConstrainedLayoutEngine();
        var ctx = new SvgRenderContext();
        var inside = engine.Compute(insideFig, ctx);
        var outside = engine.Compute(outsideFig, ctx);

        Assert.True(outside.MarginBottom > inside.MarginBottom,
            $"OutsideBottom should widen bottom margin; got inside={inside.MarginBottom}, outside={outside.MarginBottom}");
    }

    [Fact]
    public void OutsideRight_WiderLabels_ProduceWiderRightMargin()
    {
        var narrowFig = BuildFigure(LegendPosition.OutsideRight, "A");
        var wideFig = BuildFigure(LegendPosition.OutsideRight,
            "A very long descriptive legend label that definitely needs more space");

        var engine = new ConstrainedLayoutEngine();
        var ctx = new SvgRenderContext();
        var narrow = engine.Compute(narrowFig, ctx);
        var wide = engine.Compute(wideFig, ctx);

        Assert.True(wide.MarginRight > narrow.MarginRight,
            $"Wider label should produce wider right margin; got narrow={narrow.MarginRight}, wide={wide.MarginRight}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AxesRenderer — outside legend SVG placement
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Legend frame is a rectangle and the labels are rendered as glyph paths — the most
        // reliable invariant is that the SVG was produced without throwing and contains the
        // "Chart legend" accessibility group that RenderLegend always emits.
        new DrawnToken("OutsideRight_Renders_WithoutErrorAndEmitsLegendElements", () => Plt.Create()
            .WithSize(900, 500)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0, 3.0], [1.0, 4.0, 2.0], s => s.Label = "SeriesA");
                ax.Plot([1.0, 2.0, 3.0], [2.0, 1.0, 3.0], s => s.Label = "SeriesB");
                ax.WithLegend(l => l with { Position = LegendPosition.OutsideRight });
            })
            .ToSvg(), "Chart legend", "<svg"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
