// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Bar value labels: matplotlib's <c>bar_label</c> stands each label beyond the bar's end in the data direction.</summary>
public class BarLabelFidelityTests : FidelityTest
{
    private static readonly string[] Categories = ["a", "b", "c"];
    private static readonly double[] Values = [3.0, 5.0, -2.0];

    private static Figure Build(string themeId, string title, BarOrientation orientation, bool backwards) =>
        Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithTitle(title)
                  .Bar(Categories, Values, s => { s.ShowLabels = true; s.Orientation = orientation; });
                (double low, double high) = backwards ? (8.0, -4.0) : (-4.0, 8.0);
                if (orientation == BarOrientation.Horizontal) ax.SetXLim(low, high);
                else ax.SetYLim(low, high);
            })
            .Build();

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.55, DeltaE = 70)]   // AA grey text vs matplotlib black
    public void BarLabels_PositiveAndNegative_MatchMatplotlib(string themeId) =>
        AssertFidelity(Build(themeId, "Bar labels — positive and negative", BarOrientation.Vertical, false), "bar_label_signs");

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.55, DeltaE = 70)]
    public void BarLabels_OnABackwardsYAxis_MatchMatplotlib(string themeId) =>
        AssertFidelity(Build(themeId, "Bar labels — backwards Y axis", BarOrientation.Vertical, true), "bar_label_inverted");

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.55, DeltaE = 70)]
    public void BarLabels_OnHorizontalBars_MatchMatplotlib(string themeId) =>
        AssertFidelity(Build(themeId, "Bar labels — horizontal bars", BarOrientation.Horizontal, false), "bar_label_horizontal");

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.55, DeltaE = 70)]
    public void BarLabels_OnHorizontalBarsOnABackwardsAxis_MatchMatplotlib(string themeId) =>
        AssertFidelity(Build(themeId, "Bar labels — horizontal bars, backwards axis", BarOrientation.Horizontal, true), "bar_label_horizontal_inverted");
}
