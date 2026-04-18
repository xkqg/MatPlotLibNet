// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 9 of the v1.7.2 plan — opt-in URL-hash state persistence.
/// Scripts read/write a #mpl-{id}=... segment so refresh restores zoom/pan state.</summary>
public class StatePersistenceTests
{
    [Fact]
    public void ZoomPanScript_ContainsPersistOptInBranch()
    {
        var svg = Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("data-mpl-persist", svg);
        Assert.Contains("persistVB", svg);
        Assert.Contains("restoreVB", svg);
    }
}
