// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 5 of the v1.7.2 plan — 2D zoom-pan must clamp viewBox dimensions
/// to a sensible range so the user can't zoom 100× past the original or pan the
/// chart entirely off-screen.</summary>
public class TwoDZoomCompletenessTests
{
    [Fact]
    public void ZoomPanScript_ContainsMinMaxClamp()
    {
        var svg = Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        // Min/max scale tracked against the original viewBox extents — assert the script
        // captures origVb.slice() AND consults it during scaling.
        Assert.Contains("origVb", svg);
        // Hard-cap values (arbitrary but documented in the script comments).
        Assert.Contains("MIN_ZOOM", svg);
        Assert.Contains("MAX_ZOOM", svg);
    }

    [Fact]
    public void ZoomPanScript_ContainsAspectLockBranch()
    {
        var svg = Plt.Create().WithZoomPan().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        // Aspect-lock toggled via data-aspect-lock="true" on the SVG element.
        Assert.Contains("data-aspect-lock", svg);
    }
}
