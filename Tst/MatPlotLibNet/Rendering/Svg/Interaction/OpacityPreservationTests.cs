// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 8 of the v1.7.2 plan — highlight + sankey-hover scripts must preserve
/// the original opacity of each element across hover cycles. Pre-Phase-8 they restored
/// to a hard-coded 1.0, which clobbered any explicit opacity on the series.</summary>
public class OpacityPreservationTests
{
    [Fact]
    public void HighlightScript_CapturesOriginalOpacityViaDataAttribute()
    {
        var svg = Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();
        // The script source must reference data-mpl-opacity-base — that's the attribute
        // it writes on first hover and reads on restore.
        Assert.Contains("data-mpl-opacity-base", svg);
    }
}
