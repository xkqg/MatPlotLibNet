// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies color bar rendering in SVG output.</summary>
public class ColorBarRenderingTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that a visible color bar renders gradient rectangles in SVG.
        // ColorBar renders as multiple small rects forming a gradient
        new DrawnToken("ColorBar_Visible_RendersGradientRects", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar())
            .ToSvg(), "<rect"),
        // Verifies that a color bar with a label renders the label text.
        new DrawnToken("ColorBar_WithLabel_RendersLabel", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar(cb => cb with { Label = "Temperature" }))
            .ToSvg(), "Temperature"),
        // Verifies that no color bar renders when not configured.
        new DrawnToken("NoColorBar_NoExtraElements", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }))
            .ToSvg()) { Absent = ["class=\"colorbar\""] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Verifies that WithColorBar fluent method sets the property.</summary>
    [Fact]
    public void WithColorBar_SetsProperty()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
                .WithColorBar())
            .Build();

        Assert.NotNull(figure.SubPlots[0].ColorBar);
        Assert.True(figure.SubPlots[0].ColorBar!.Visible);
    }
}
