// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

public sealed class ConnectionStyleOutputContractTests : EnumOutputContractTests<ConnectionStyle>
{
    protected override Func<ConnectionStyle, string> RenderDelegate => style =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 10.0], [0.0, 10.0]);
                ax.Annotate("Annotation", 2, 8, arrowX: 7, arrowY: 2, a =>
                {
                    a.ConnectionStyle = style;
                    a.ConnectionRad = 0.3;
                });
            })
            .ToSvg();
}
