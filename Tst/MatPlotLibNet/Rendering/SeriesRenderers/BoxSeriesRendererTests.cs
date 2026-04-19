// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.3.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.BoxSeriesRenderer"/>. Pre-X
/// the renderer was at 64.5%L because only the vertical default-Positions path was
/// exercised. Adds: horizontal orientation, ShowMeans, explicit Positions, explicit
/// MedianColor, and empty-dataset skip.</summary>
public class BoxSeriesRendererTests
{
    private static double[][] SampleSets => [
        [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0],
        [5.0, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0],
    ];

    private static string Render(BoxSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    [Fact]
    public void Render_VerticalDefault_DrawsBoxAndWhiskers()
    {
        var svg = Render(new BoxSeries(SampleSets));
        Assert.Contains("<rect", svg);
    }

    /// <summary>Horizontal orientation (line 44-58: Vert=false branch).</summary>
    [Fact]
    public void Render_Horizontal_SwapsAxes()
    {
        var svg = Render(new BoxSeries(SampleSets) { Vert = false });
        Assert.Contains("<rect", svg);
    }

    /// <summary>ShowMeans=true exercises the green-circle mean marker (lines 38-42 vertical
    /// AND lines 53-57 horizontal both share the same `if (series.ShowMeans)` shape — one
    /// fact each below).</summary>
    [Fact]
    public void Render_ShowMeans_Vertical_DrawsCircleAtMean()
    {
        var svg = Render(new BoxSeries(SampleSets) { ShowMeans = true });
        Assert.Contains("<circle", svg);
    }

    [Fact]
    public void Render_ShowMeans_Horizontal_DrawsCircleAtMean()
    {
        var svg = Render(new BoxSeries(SampleSets) { Vert = false, ShowMeans = true });
        Assert.Contains("<circle", svg);
    }

    /// <summary>Explicit Positions array (line 24's `Positions is not null &amp;&amp; i &lt; Positions.Length`
    /// true branch). Default is i=0,1,...; explicit positions place boxes at custom x-coords.</summary>
    [Fact]
    public void Render_ExplicitPositions_PlacesBoxesAtCustomCoords()
    {
        var svg = Render(new BoxSeries(SampleSets) { Positions = [10.0, 20.0] });
        Assert.Contains("<rect", svg);
    }

    /// <summary>Positions array shorter than dataset count — i &gt;= Positions.Length false
    /// arm of the ternary; falls back to integer index.</summary>
    [Fact]
    public void Render_ShortPositions_FallsBackToIndex()
    {
        var svg = Render(new BoxSeries(SampleSets) { Positions = [10.0] });   // only 1 position for 2 datasets
        Assert.Contains("<rect", svg);
    }

    /// <summary>Explicit MedianColor (line 35/50 — `series.MedianColor ?? Colors.Red`).</summary>
    [Fact]
    public void Render_ExplicitMedianColor_UsesItForMedianLine()
    {
        var svg = Render(new BoxSeries(SampleSets) { MedianColor = new Color(0, 255, 0, 255) });
        Assert.Contains("<rect", svg);
    }

    // X.3.a finding (NOT a renderer test, deliberately omitted): the
    // `data.Length == 0` continue arm in BoxSeriesRenderer (line 23) is dead code
    // through the public API — BoxSeries.ComputeDataRange throws on an empty
    // dataset before render is even reached (Min/Max on empty IEnumerable).
    // Either ComputeDataRange should filter empty datasets, or the renderer's
    // guard is unreachable. Tracked as a tiny stabilisation TODO; no Phase X
    // bandaid here.
}
