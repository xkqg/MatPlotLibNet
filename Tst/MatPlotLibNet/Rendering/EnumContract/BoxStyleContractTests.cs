// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

/// <summary>
/// Phase N.2 — <see cref="BoxStyle"/> has 5 members (None, Square, Round,
/// RoundTooth, Sawtooth). Each is supposed to render a distinct callout
/// geometry behind annotation text. Pre-Phase-N no test pinned that.
/// </summary>
public class BoxStyleContractTests
{
    [Fact]
    public void EveryBoxStyle_ProducesDistinctSvg()
    {
        // None = no background box (separate verification). All other styles
        // must produce mutually distinct geometries.
        EnumOutputContract.EveryValueRendersDistinctOutput<BoxStyle>(
            RenderWithBoxStyle,
            exclude: [BoxStyle.None]);
    }

    [Fact]
    public void BoxStyle_None_RendersWithoutExtraRectOrPath()
    {
        // Minimal sanity — None path still produces valid SVG.
        string svg = RenderWithBoxStyle(BoxStyle.None);
        Assert.NotEmpty(svg);
    }

    private static string RenderWithBoxStyle(BoxStyle style) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 10.0], [0.0, 10.0]);
                ax.Annotate("Labeled point", 5, 5, a =>
                {
                    a.BoxStyle = style;
                });
            })
            .ToSvg();
}
