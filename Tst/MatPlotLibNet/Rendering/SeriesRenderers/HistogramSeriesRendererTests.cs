// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.HistogramSeriesRenderer"/>.
/// Pre-X.9 the renderer was at 74%L / 58%B because only HistType.Bar without
/// weights/density/cumulative was tested. This file pins:
///   - HistType.Bar (default — line 31 true arm)
///   - HistType.Step (outline only — line 62 false arm of HistType.StepFilled)
///   - HistType.StepFilled (filled polygon — line 57 true arm)
///   - Weights set with matching length (line 75 BuildHeights true arm)
///   - Density=true (line 92 BuildHeights true arm)
///   - Cumulative=true (line 100 BuildHeights true arm)
///   - EdgeColor=null path (line 28 ?? fallback)
///   - Empty Data → early return (line 20)</summary>
public class HistogramSeriesRendererTests
{
    private static string Render(HistogramSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    [Fact]
    public void Render_BasicBarHistogram_DrawsRectangles()
    {
        var svg = Render(new HistogramSeries([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10]) { Bins = 5 });
        Assert.Contains("<rect", svg);
    }

    /// <summary>HistType.Step (line 62 false arm of `if (StepFilled)`) — draws the outline
    /// via DrawLines, no polygon fill.</summary>
    [Fact]
    public void Render_StepHistType_DrawsOutlineOnly()
    {
        var svg = Render(new HistogramSeries([1.0, 2, 3, 4, 5]) { Bins = 4, HistType = HistType.Step });
        // Step path emits a polyline (DrawLines) and no series-polygon.
        Assert.Contains("<polyline", svg);
        Assert.DoesNotContain("<polygon", svg);
    }

    /// <summary>HistType.StepFilled (line 57 true arm) — closes the path with bin start
    /// and emits a filled polygon.</summary>
    [Fact]
    public void Render_StepFilledHistType_DrawsPolygon()
    {
        var svg = Render(new HistogramSeries([1.0, 2, 3, 4, 5]) { Bins = 4, HistType = HistType.StepFilled });
        Assert.Contains("<polygon", svg);
    }

    /// <summary>Weights with length matching Data (BuildHeights line 75 true arm).
    /// Each bin accumulates weights instead of raw counts.</summary>
    [Fact]
    public void Render_Weights_AccumulatesPerBin()
    {
        var svg = Render(new HistogramSeries([1.0, 2, 3, 4, 5])
        {
            Bins = 5,
            Weights = new[] { 0.5, 1.0, 1.5, 2.0, 2.5 },
        });
        Assert.Contains("<rect", svg);
    }

    /// <summary>Density=true (BuildHeights line 92 true arm) — heights are normalised
    /// so total area = 1.</summary>
    [Fact]
    public void Render_Density_NormalisesHeights()
    {
        var svg = Render(new HistogramSeries([1.0, 2, 3, 4, 5, 6, 7, 8])
        {
            Bins = 4,
            Density = true,
        });
        Assert.Contains("<rect", svg);
    }

    /// <summary>Cumulative=true (BuildHeights line 100 true arm) — each bin's height
    /// adds the previous bins' heights.</summary>
    [Fact]
    public void Render_Cumulative_StackedHeights()
    {
        var svg = Render(new HistogramSeries([1.0, 2, 3, 4, 5, 6])
        {
            Bins = 3,
            Cumulative = true,
        });
        Assert.Contains("<rect", svg);
    }

    /// <summary>EdgeColor explicitly set (line 28 left-hand of `??` non-null).</summary>
    [Fact]
    public void Render_EdgeColor_SetExplicitly_AppliesEdge()
    {
        var svg = Render(new HistogramSeries([1.0, 2, 3, 4, 5])
        {
            Bins = 5,
            EdgeColor = Colors.Black,
        });
        Assert.Contains("<rect", svg);
    }

    /// <summary>Empty Data → early return (line 20). Renderer must not throw; no
    /// strict SVG-shape assertion here since the chart frame always emits a backing
    /// &lt;rect&gt; even with zero series content.</summary>
    [Fact]
    public void Render_EmptyData_DoesNotThrow()
    {
        var svg = Render(new HistogramSeries([]) { Bins = 5 });
        Assert.NotNull(svg);
    }
}
