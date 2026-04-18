// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

/// <summary>
/// Phase N.2 — <see cref="ConnectionStyle"/> has 4 members (Straight, Arc3,
/// Angle, Angle3). Each drives a distinct arrow-path geometry; pre-Phase-N
/// no Theory pinned that the switch was exhaustive.
/// </summary>
public class ConnectionStyleContractTests
{
    [Fact]
    public void EveryConnectionStyle_ProducesDistinctSvg()
    {
        EnumOutputContract.EveryValueRendersDistinctOutput<ConnectionStyle>(RenderWithConnection);
    }

    private static string RenderWithConnection(ConnectionStyle style) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([0.0, 10.0], [0.0, 10.0]);
                ax.Annotate("Annotation", 2, 8, arrowX: 7, arrowY: 2, a =>
                {
                    a.ConnectionStyle = style;
                    // Non-zero rad so Arc3 + Angle3 variants actually curve.
                    a.ConnectionRad = 0.3;
                });
            })
            .ToSvg();
}
