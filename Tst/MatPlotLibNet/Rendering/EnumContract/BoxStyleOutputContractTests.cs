// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

public sealed class BoxStyleOutputContractTests : EnumOutputContractTests<BoxStyle>
{
    protected override Func<BoxStyle, string> RenderDelegate => style =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 10.0], [0.0, 10.0]);
                ax.Annotate("Labeled point", 5, 5, a => { a.BoxStyle = style; });
            })
            .ToSvg();

    protected override IEnumerable<BoxStyle> Exclude => [BoxStyle.None];

}
