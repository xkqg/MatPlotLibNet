// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies figure-level colorbar model and rendering.</summary>
public class FigureColorBarTests
{
    [Fact]
    public void AFigure_HasNoColourBar_UntilOneIsAskedFor()
    {
        var fig = new Figure();
        Assert.Null(fig.FigureColorBar);
    }

    [Fact]
    public void FigureBuilder_WithColorBar_SetsFigureColorBar()
    {
        var fig = Plt.Create()
            .WithColorBar()
            .Build();
        Assert.NotNull(fig.FigureColorBar);
        Assert.True(fig.FigureColorBar!.Visible);
    }

    [Fact]
    public void FigureBuilder_WithColorBar_Configure_AppliesOptions()
    {
        var fig = Plt.Create()
            .WithColorBar(cb => cb with { Label = "Intensity" })
            .Build();
        Assert.Equal("Intensity", fig.FigureColorBar!.Label);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("FigureColorBar_WithLabel_RendersLabel", () =>
        {
            double[,] data = { { 0.0, 1.0 }, { 0.5, 0.8 } };
            return Plt.Create()
                .WithColorBar(cb => cb with { Label = "Temperature" })
                .AddSubPlot(1, 1, 1, ax => ax.Heatmap(data))
                .Build()
                .ToSvg();
        }, "Temperature"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
