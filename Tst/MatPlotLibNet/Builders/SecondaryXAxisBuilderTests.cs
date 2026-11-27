// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Phase 7 — covers <see cref="SecondaryXAxisBuilder"/>. Each builder method must (1) mutate the underlying
/// axes state, (2) return self for fluent chaining, (3) propagate the configure lambda. The right-hand counterpart is
/// <see cref="YAxisSlotBuilder"/>, covered by <c>YAxisSlotBuilderTests</c>.</summary>
public class SecondaryXAxisBuilderTests
{
    [Fact]
    public void SecondaryXAxis_SetXLabel_AndSetXLim_PropagateAndChain()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5))
                .WithSecondaryXAxis(s => s
                    .SetXLabel("top")
                    .SetXLim(-50, 50)))
            .Build();

        var sec = fig.SubPlots[0].SecondaryXAxis!;
        Assert.Equal("top", sec.Label);
        Assert.Equal(-50, sec.Min);
        Assert.Equal(50, sec.Max);
    }

}
