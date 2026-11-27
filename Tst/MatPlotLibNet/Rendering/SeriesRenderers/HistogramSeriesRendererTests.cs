// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.HistogramSeriesRenderer"/>.
/// Pre-X.9 the renderer was at 74%L / 58%B because only HistType.Bar without
/// weights/density/cumulative was tested. This file pins:
///   - HistType.Bar (default)
///   - HistType.Step (outline only — false arm of HistType.StepFilled)
///   - HistType.StepFilled (filled polygon)
///   - Weights set with matching length (BuildHeights)
///   - Density=true (BuildHeights)
///   - Cumulative=true (BuildHeights)
///   - EdgeColor=null path (?? fallback)
///   - Empty Data → early return</summary>
public class HistogramSeriesRendererTests
{
    private static string Render(HistogramSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render_BasicBarHistogram_DrawsRectangles", () => Render(new HistogramSeries([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10]) { Bins = 5 }), "<rect"),
        // HistType.Step (false arm of `if (StepFilled)`) — draws the outline via DrawLines, no polygon fill.
        // Step path emits a polyline (DrawLines) and no series-polygon.
        new DrawnToken("Render_StepHistType_DrawsOutlineOnly", () => Render(new HistogramSeries([1.0, 2, 3, 4, 5]) { Bins = 4, HistType = HistType.Step }), "<polyline") { Absent = ["<polygon"] },
        // HistType.StepFilled — closes the path with bin start and emits a filled polygon.
        new DrawnToken("Render_StepFilledHistType_DrawsPolygon", () => Render(new HistogramSeries([1.0, 2, 3, 4, 5]) { Bins = 4, HistType = HistType.StepFilled }), "<polygon"),
        // Weights with length matching Data (BuildHeights). Each bin accumulates weights instead of raw counts.
        new DrawnToken("Render_Weights_AccumulatesPerBin", () => Render(new HistogramSeries([1.0, 2, 3, 4, 5])
        {
            Bins = 5,
            Weights = new[] { 0.5, 1.0, 1.5, 2.0, 2.5 },
        }), "<rect"),
        // Density=true (BuildHeights) — heights are normalised so total area = 1.
        new DrawnToken("Render_Density_NormalisesHeights", () => Render(new HistogramSeries([1.0, 2, 3, 4, 5, 6, 7, 8])
        {
            Bins = 4,
            Density = true,
        }), "<rect"),
        // Cumulative=true (BuildHeights) — each bin's height adds the previous bins' heights.
        new DrawnToken("Render_Cumulative_StackedHeights", () => Render(new HistogramSeries([1.0, 2, 3, 4, 5, 6])
        {
            Bins = 3,
            Cumulative = true,
        }), "<rect"),
        // EdgeColor explicitly set (left-hand of `??` non-null).
        new DrawnToken("Render_EdgeColor_SetExplicitly_AppliesEdge", () => Render(new HistogramSeries([1.0, 2, 3, 4, 5])
        {
            Bins = 5,
            EdgeColor = Colors.Black,
        }), "<rect"),
        // Without an edge colour (the default theme sets none) the filled step polygon has no outline at all.
        new DrawnToken("Render_StepFilledWithoutEdgeColor_HasNoOutline", () => PolygonTag(Render(new HistogramSeries([1.0, 2, 3, 4, 5]) { Bins = 4, HistType = HistType.StepFilled }))) { Absent = ["stroke="] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A filled step histogram with an edge colour outlines its polygon in that colour: at half a pixel, the width a bar's
    /// edge has, under the library's own themes; at matplotlib's <c>patch.linewidth</c>, 1 pt, under the matplotlib themes
    /// (<c>hist(histtype='stepfilled', edgecolor='k')</c> draws its polygon 1.0 pt wide, matplotlib 3.11.2).</summary>
    [Theory]
    [InlineData("default", "0.5")]
    [InlineData("v2", "1.39")]
    public void Render_StepFilledWithEdgeColor_OutlinesThePolygonAtTheThemesWidth(string theme, string width)
    {
        var svg = Plt.Create().WithSize(400, 300).WithTheme(theme.AsTheme())
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new HistogramSeries([1.0, 2, 3, 4, 5]) { Bins = 4, HistType = HistType.StepFilled, EdgeColor = Colors.Black }))
            .Build().ToSvg();

        Assert.Contains($"stroke=\"#000000\" stroke-width=\"{width}\"", PolygonTag(svg));
    }

    private static string PolygonTag(string svg)
    {
        int start = svg.IndexOf("<polygon", StringComparison.Ordinal);
        Assert.True(start >= 0, "the filled step histogram drew no polygon");
        return svg[start..svg.IndexOf("/>", start, StringComparison.Ordinal)];
    }

}
