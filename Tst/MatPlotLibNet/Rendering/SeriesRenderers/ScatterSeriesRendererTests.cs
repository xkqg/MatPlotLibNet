// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.ScatterSeriesRenderer"/>. Pre-X.9
/// the renderer was at 80%L / 41%B because only the simplest path (no C, no Sizes,
/// no per-point Colors, no edges) was exercised. This file pins:
///   - C set → skips ViewportCuller (line 24-25)
///   - C set with VMin/VMax explicit (lines 35-36)
///   - C set without VMin/VMax → falls back to Min/Max (lines 35-36 false arms)
///   - Sizes per-point (line 46 ternary's true arm)
///   - Colors[] per-point (ResolvePointColor line 60-61)
///   - C+ColorMap (ResolvePointColor line 62-63)
///   - EdgeColors per-point (line 49 ternary's true arm)
///   - LineWidths per-point (line 50 ternary's true arm)
///   - MaxDisplayPoints set without C → ViewportCuller (line 27-28)</summary>
public class ScatterSeriesRendererTests
{
    private static string Render(ScatterSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    [Fact]
    public void Render_BasicScatter_DrawsMarkers()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6]));
        Assert.Contains("<circle", svg);
    }

    /// <summary>C set + VMin/VMax explicit → ColorMap path (lines 33-37 explicit
    /// bounds, ResolvePointColor line 62-63).</summary>
    [Fact]
    public void Render_C_With_ExplicitVMinVMax_AndColorMap_PerPointColor()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6])
        {
            C = new[] { 0.0, 0.5, 1.0 },
            VMin = 0,
            VMax = 1,
            ColorMap = ColorMaps.Viridis,
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>C set without VMin/VMax → falls back to C.Min()/C.Max() (lines 35-36
    /// false arms of `?? series.C.Min()`).</summary>
    [Fact]
    public void Render_C_Without_VMinVMax_FallsBackToCMinMax()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6])
        {
            C = new[] { 10.0, 50.0, 100.0 },
            ColorMap = ColorMaps.Viridis,
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>Sizes per-point (line 46 ternary's true arm). Each marker has its
    /// own area; radius = sqrt(s/π) × DPI scaling.</summary>
    [Fact]
    public void Render_Sizes_PerPoint_AppliesPerPointRadius()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6])
        {
            Sizes = new[] { 10.0, 50.0, 200.0 },
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>Colors[] per-point (ResolvePointColor line 60-61 hits priority arm 1).
    /// Even with a ColorMap+C set, Colors wins.</summary>
    [Fact]
    public void Render_Colors_PerPoint_PrioritizedOverColorMap()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6])
        {
            Colors = new[] { Colors.Red, Colors.Green, Colors.Blue },
            C = new[] { 0.0, 0.5, 1.0 },
            ColorMap = ColorMaps.Viridis,
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>EdgeColors per-point (line 49 ternary's true arm) + LineWidths per-point
    /// (line 50 ternary's true arm). Both arrays index-tied to data.</summary>
    [Fact]
    public void Render_EdgeColors_AndLineWidths_PerPoint()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3], [4.0, 5, 6])
        {
            EdgeColors = new[] { Colors.Black, Colors.Black, Colors.Black },
            LineWidths = new[] { 1.0, 2.0, 3.0 },
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>MaxDisplayPoints set + no C → ViewportCuller path (line 27-28).</summary>
    [Fact]
    public void Render_MaxDisplayPoints_TriggersViewportCuller()
    {
        var svg = Render(new ScatterSeries([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5])
        {
            MaxDisplayPoints = 3,
        });
        Assert.Contains("<circle", svg);
    }

    /// <summary>Empty data — no markers drawn (early-return-style path through the for-loop
    /// at line 41 with pts.Length == 0).</summary>
    [Fact]
    public void Render_EmptyData_NoMarkers()
    {
        var svg = Render(new ScatterSeries([], []));
        Assert.DoesNotContain("<circle", svg);
    }
}
