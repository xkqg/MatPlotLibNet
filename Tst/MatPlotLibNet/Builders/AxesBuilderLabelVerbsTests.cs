// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The texts of an axes: a title or an axis title given no style edit keeps no style, and an edit is made over the style
/// already held, so two edits add up.</summary>
public class AxesBuilderLabelVerbsTests
{
    [Fact]
    public void ATitle_GivenNoStyleEdit_KeepsNoStyle_AndTheChainGoesOn()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithTitle("Chart", configure: null)
            .SetXLabel("X", configure: null)
            .SetYLabel("Y", configure: null)).Build().SubPlots[0];

        Assert.Equal(["Chart", "X", "Y"], new[] { axes.Title, axes.XAxis.Label, axes.YAxis.Label });
        Assert.Equal([null, null, null], new[] { axes.TitleStyle, axes.XAxis.LabelStyle, axes.YAxis.LabelStyle });
    }

    [Fact]
    public void WithTitle_CalledTwice_AccumulatesStyle()
    {
        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("T", s => s with { FontSize = 14 })
                .WithTitle("T", s => s with { FontWeight = FontWeight.Bold }))
            .Build().SubPlots[0];

        Assert.Equal(14, axes.TitleStyle!.FontSize);
        Assert.Equal(FontWeight.Bold, axes.TitleStyle!.FontWeight);
    }
}
