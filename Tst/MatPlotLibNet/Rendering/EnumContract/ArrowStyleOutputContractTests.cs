// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

public sealed class ArrowStyleOutputContractTests : EnumOutputContractTests<ArrowStyle>
{
    protected override Func<ArrowStyle, string> RenderDelegate => style =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 10.0], [0.0, 10.0]);
                ax.Annotate("Target", 5, 5, arrowX: 7, arrowY: 7, a => { a.ArrowStyle = style; });
            })
            .ToSvg();

    protected override IEnumerable<ArrowStyle> Exclude => [ArrowStyle.None];

    [Fact]
    public void ArrowStyle_CurveA_and_CurveB_StandTheirHeadAtDifferentEnds()
    {
        string a = RenderDelegate(ArrowStyle.CurveA);
        string b = RenderDelegate(ArrowStyle.CurveB);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ArrowStyle_BracketA_and_BracketB_StandTheirBracketAtDifferentEnds()
    {
        string a = RenderDelegate(ArrowStyle.BracketA);
        string b = RenderDelegate(ArrowStyle.BracketB);
        Assert.NotEqual(a, b);
    }
}
