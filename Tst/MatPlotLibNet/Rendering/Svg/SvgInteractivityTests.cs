// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG interactivity (zoom/pan) behavior.</summary>
public class SvgInteractivityTests
{
    /// <summary>Verifies that enabling zoom/pan injects a script element into the SVG output.</summary>
    [Fact]
    public void EnableZoomPan_InjectsScriptElement()
    {
        string svg = Plt.Create()
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("<script", svg);
    }

    /// <summary>Verifies that without zoom/pan enabled, no script element is present in the SVG output.</summary>
    [Fact]
    public void DisabledZoomPan_NoScriptElement()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain("<script", svg);
    }

    /// <summary>Verifies that the injected script contains viewBox manipulation via setAttribute.</summary>
    [Fact]
    public void ScriptContains_ViewBoxManipulation()
    {
        string svg = Plt.Create()
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("viewBox", svg);
        Assert.Contains("setAttribute", svg);
    }

    /// <summary>Verifies that the injected script includes a wheel event handler for zoom.</summary>
    [Fact]
    public void ScriptContains_WheelEventHandler()
    {
        string svg = Plt.Create()
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("wheel", svg);
    }

    /// <summary>Phase 0.1 — root cause of the v1.7.2 "wheel scrolls page instead of zooming" bug.
    /// Modern browsers (Chrome 56+, Firefox 31+, Safari 11+) treat wheel listeners as <c>{ passive: true }</c>
    /// by default; calling <c>e.preventDefault()</c> in a passive listener is silently ignored.
    /// The wheel listener MUST be registered with <c>{ passive: false }</c> for the zoom to actually
    /// override the browser's default scroll behaviour.</summary>
    [Fact]
    public void ScriptContains_NonPassiveWheelListener()
    {
        string svg = Plt.Create()
            .WithZoomPan()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("passive: false", svg);
    }
}
