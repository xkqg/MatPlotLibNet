// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The cap of a margin bounds what the tight layout estimates; what an axes is measured to draw at that edge lifts it, so a
/// margin is never cut below it. A colour bar in the library's margin beside the plot and an outside legend are measured, and they
/// lift the cap of their edge as the measured tick label bands always did (and as a colour bar below the plot always did): the right
/// margin was held at its cap of 140 px while the bar beside a secondary axis needed 210 px (its label drawn past the figure's edge, at
/// x 623 of 600), and the left margin at the legend's own lift of 252 px while the legend past the Y tick labels needed 312 px.</summary>
public sealed class EdgeCapLiftTests
{
    private static readonly SvgRenderContext Ctx = new();

    /// <summary>What the only axes of <paramref name="figure"/> needs beside its plot, as the layout measures it under the library's
    /// pads (the layout of these figures keeps them).</summary>
    private static double LeftNeedOf(Figure figure) => SidesOf(figure).Start;

    private static double RightNeedOf(Figure figure) => SidesOf(figure).End;

    private static AxisNeeds SidesOf(Figure figure)
    {
        var axes = figure.SubPlots[0];
        var bands = AxesRenderer.Create(axes, new Rect(0, 0, 0, figure.Height), Ctx, figure.Theme, null, null).MeasureSideBands();
        return new AxesEdgeNeeds().MeasureSides(axes, figure, Ctx, bands);
    }

    [Fact]
    public void AColourBarBesideASecondaryAxis_IsNotCutByTheCapOfTheRightMargin()
    {
        var figure = Plt.Create().WithTheme(Theme.Default).WithSize(600, 400).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar(b => b with { Label = "intensity of the field" })
                .WithAddRightYAxis(s => s.Plot([0.0, 1.0], [1_000_000.0, 2_000_000.0]).SetYLabel("secondary")))
            .Build();
        var engine = new ConstrainedLayoutEngine();

        var spacing = engine.Compute(figure, Ctx);

        double need = RightNeedOf(figure);
        Assert.True(need > 140, $"precondition: the right edge needs more than the cap, {need}");
        Assert.True(spacing.MarginRight >= need, $"the right margin {spacing.MarginRight} cuts the bar, which needs {need}");
    }

    [Fact]
    public void AnOutsideLegendPastTheYTickLabels_IsNotCutByTheCapOfTheLeftMargin()
    {
        var figure = Plt.Create().WithTheme(Theme.Default).WithSize(600, 400).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 1.0], [1_000_000.0, 2_000_000.0], l => l.Label = "a rather long legend entry")
                .SetYLabel("values")
                .WithLegend(LegendPosition.OutsideLeft))
            .Build();
        var engine = new ConstrainedLayoutEngine();

        var spacing = engine.Compute(figure, Ctx);

        double need = LeftNeedOf(figure);
        Assert.True(spacing.MarginLeft >= need, $"the left margin {spacing.MarginLeft} cuts the legend, which needs {need}");
    }

    /// <summary>The bar's label stays inside the figure.</summary>
    [Fact]
    public void TheLabelOfAColourBarBesideASecondaryAxis_IsDrawnInsideTheFigure()
    {
        var svg = Plt.Create().WithTheme(Theme.Default).WithSize(600, 400).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar(b => b with { Label = "intensity of the field" })
                .WithAddRightYAxis(s => s.Plot([0.0, 1.0], [1_000_000.0, 2_000_000.0]).SetYLabel("secondary")))
            .Build().ToSvg();

        var label = svg.Each("text").Single(e => e.Value == "intensity of the field");
        // Turned a quarter, the label is as wide as its font is high, centred on its x.
        double x = label.Number("x");
        double size = double.Parse(((string)label.Attribute("font-size")!).Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(x + size / 2 <= 600, $"the bar's label reaches x {x + size / 2}, past the figure's edge");
    }
}
