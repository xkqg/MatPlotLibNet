// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Each kind of series says the zorder matplotlib gives the artist its verb draws (matplotlib 3.11.2, printed with python,
/// <c>artist.get_zorder()</c>): a <c>Line2D</c> or a <c>LineCollection</c> stands at 2 (<c>plot</c>, <c>step</c>, <c>hlines</c>,
/// <c>eventplot</c>, <c>stem</c>, <c>errorbar</c>'s bars, <c>boxplot</c>'s lines, a line <c>contour</c>, <c>streamplot</c>, seaborn's
/// <c>kdeplot</c> line, <c>ecdfplot</c>, <c>rugplot</c>, <c>pointplot</c> and <c>regplot</c> line), an <c>AxesImage</c> at 0
/// (<c>imshow</c>, <c>specgram</c>), a <c>Table</c> at 0 (<c>ax.table</c>, an <c>Artist</c> that sets none), a <c>QuiverKey</c> at its quiver's 1 + 0.1, and every patch or other collection at 1 (a bar,
/// a fill, a scatter, a mesh, a filled contour, a quiver, a violin). The matplotlib themes draw a series given no
/// <c>ZOrder</c> at it.</summary>
public class SeriesArtistZOrderTests
{
    /// <summary>The kinds that are not drawn at 1, with the zorder they are drawn at.</summary>
    private static readonly IReadOnlyDictionary<string, double> NotAtOne = new Dictionary<string, double>(StringComparer.Ordinal)
    {
        [nameof(LineSeries)] = 2,
        [nameof(StepSeries)] = 2,
        [nameof(SignalSeries)] = 2,
        [nameof(SignalXYSeries)] = 2,
        [nameof(SparklineSeries)] = 2,
        [nameof(EcdfSeries)] = 2,
        [nameof(ErrorBarSeries)] = 2,
        [nameof(StemSeries)] = 2,
        [nameof(EventplotSeries)] = 2,
        [nameof(TricontourSeries)] = 2,
        [nameof(StreamplotSeries)] = 2,
        [nameof(RugplotSeries)] = 2,
        [nameof(KdeSeries)] = 2,          // seaborn's kdeplot(fill=False): a Line2D
        [nameof(BoxSeries)] = 2,
        [nameof(OhlcBarSeries)] = 2,
        [nameof(DendrogramSeries)] = 2,
        [nameof(StreamingLineSeries)] = 2,
        [nameof(StreamingSignalSeries)] = 2,
        [nameof(RelativeRotationSeries)] = 2,
        [nameof(PointplotSeries)] = 2,
        [nameof(RegressionSeries)] = 2,
        [nameof(ContourSeries)] = 2,
        [nameof(PolarLineSeries)] = 2,     // plot on a polar axes: a Line2D
        [nameof(TernaryLineSeries)] = 2,   // mpltern 1.0.5's plot: a Line2D
        [nameof(ImageSeries)] = 0,
        [nameof(SpectrogramSeries)] = 0,
        [nameof(HeatmapSeries)] = 0,
        [nameof(TableSeries)] = 0,         // ax.table: a Table artist, zorder 0
        [nameof(QuiverKeySeries)] = 1.1,
    };

    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void EachKindOfSeries_SaysTheZOrderMatplotlibGivesItsArtist(string type)
    {
        var series = DrawnSeriesFixtures.Instance(type);
        var chart = Assert.IsAssignableFrom<ChartSeries>(series);

        Assert.Equal(NotAtOne.GetValueOrDefault(type, 1), chart.ArtistZOrder);
    }

    /// <summary>The table names only kinds the library ships, so a renamed series cannot leave a stale row behind.</summary>
    [Fact]
    public void TheTable_NamesOnlyShippedKinds()
    {
        var shipped = typeof(ChartSeries).Assembly.GetTypes().Where(t => t.IsPublic && typeof(ChartSeries).IsAssignableFrom(t)).Select(t => t.Name).ToHashSet();

        Assert.All(NotAtOne.Keys, name => Assert.Contains(name, shipped));
    }

    /// <summary>A filled contour stands at 1, a line contour at 2, as <c>contourf</c> and <c>contour</c> give their
    /// <c>QuadContourSet</c>; a density curve given no fill at 2, as seaborn's <c>kdeplot(fill=False)</c> draws it (a <c>Line2D</c>),
    /// and one given a fill at 1 (seaborn's <c>fill=True</c>, a <c>PolyCollection</c>).</summary>
    [Fact]
    public void AFilledContourAndAFilledDensity_StandAt1_TheirLinesAt2()
    {
        var contour = new ContourSeries([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } });
        var density = new KdeSeries([1.0, 2.0, 3.0]);

        Assert.Equal(2, contour.ArtistZOrder);
        Assert.Equal(2, density.ArtistZOrder);
        contour.Filled = true;
        density.Fill = true;
        Assert.Equal(1, contour.ArtistZOrder);
        Assert.Equal(1, density.ArtistZOrder);
    }
}
