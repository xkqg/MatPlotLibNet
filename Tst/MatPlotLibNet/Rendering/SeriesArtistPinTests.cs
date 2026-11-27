// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Packaging;
using MatPlotLibNet.Tests.Serialization;
using Xunit;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What each kind of series is to matplotlib, pinned one row per kind: the call its colour is asked with (which of the axes'
/// two cycles it takes it from), the z-order of the artist its verb draws, and the colour the theme gives it where its author names
/// none, alone and inside a colour group. The table was written down from the tree before the three questions had one owner, and
/// holds every kind as it was, so moving them cannot change a picture. A row changes only with a fix that names it: a Gantt chart asks
/// with the patch call, as matplotlib's <c>barh</c> takes the patch cycle; a table stands at 0, as matplotlib's <c>Table</c> does
/// (printed with 3.11.2 under both styles: <c>ax.table(...).get_zorder()</c> is 0).</summary>
public class SeriesArtistPinTests
{
    private static readonly Color Body = Colors.Red;
    private static readonly Color Stats = Colors.Green;
    private static readonly Color Vector = Colors.Blue;
    private static readonly Color ScatterFace = Colors.Orange;
    private static readonly Color BoxOutline = Colors.RebeccaPurple;

    /// <summary>A theme that gives every kind with a colour of its own a distinct one, so a row names which source a series reads.</summary>
    private static readonly Theme Roles = new("roles", Theme.Default)
    {
        ViolinBodyColor = Body,
        ViolinStatsColor = Stats,
        Looks = ThemeLooks.Library with
        {
            VectorField = new VectorFieldLook { Color = Vector },
            Scatter = new ScatterLook(null, null, ScatterFace, null, null, null),
            Box = BoxLook.Matplotlib(BoxOutline, Colors.Black, Colors.Black, LineStyle.Solid, Colors.Black, 0.5, 1,
                MarkerStyle.Circle, null, Colors.Black, MarkerStyle.Diamond, Colors.Black, Colors.Black, 6),
        },
    };

    private static string Source(Color colour, Color cycle)
    {
        if (colour == cycle) return "cycle";
        if (colour == Body) return "violin";
        if (colour == Vector) return "vector";
        if (colour == ScatterFace) return "scatter";
        if (colour == BoxOutline) return "box";
        if (colour == Colors.Teal) return "named";
        return colour.ToHex();
    }

    private static IEnumerable<(string Name, ChartSeries Series)> Everything()
    {
        foreach (var type in DrawnSeriesFixtures.DrawnTypes)
        {
            yield return (type.Name, (ChartSeries)DrawnSeriesFixtures.Instance(type.Name));
        }

        // The kinds whose call or z-order depends on how the instance is set up.
        yield return ("Kde(filled)", new KdeSeries([1.0, 2, 3]) { Fill = true });
        yield return ("Contour(filled)", new ContourSeries([0.0, 1], [0.0, 1], new double[,] { { 1, 2 }, { 3, 4 } }) { Filled = true });
        yield return ("Pie(coloured)", new PieSeries([30.0, 70.0]) { Colors = [Colors.Red, Colors.Blue] });
        yield return ("Donut(coloured)", new DonutSeries([30.0, 70.0]) { Colors = [Colors.Red, Colors.Blue] });
        yield return ("Scatter(C)", new ScatterSeries([1.0], [2.0]) { C = [1.0] });
        yield return ("Scatter(Colors)", new ScatterSeries([1.0], [2.0]) { Colors = [Colors.Red] });
        yield return ("Scatter3D(map)", new Scatter3DSeries(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 }) { ColorMap = ColorMaps.Viridis });
        yield return ("Trisurf3D(map)", new Trisurf3DSeries(new double[] { 0.0, 1.0, 0.5 }, new double[] { 0.0, 0.0, 1.0 }, new double[] { 1.0, 2.0, 3.0 }) { ColorMap = ColorMaps.Viridis });
        yield return ("Line(coloured)", new LineSeries([1.0], [2.0]) { Color = Colors.Teal });
        yield return ("Line(asks as scatter)", new LineSeries([1.0], [2.0]) { AsksAs = CycleCall.Scatter });
    }

    /// <summary>One row per kind: name, call, artist z-order, then the colour source alone and inside a colour group.</summary>
    internal static string Table()
    {
        var slot = Roles.SeriesSlot(3);
        var text = new StringBuilder();
        foreach (var (name, series) in Everything())
        {
            series.ColorGroup = null;
            var alone = Source(slot.ColorOf(series), slot.CycleColor);
            series.ColorGroup = 7;
            var grouped = Source(slot.ColorOf(series), slot.CycleColor);
            series.ColorGroup = null;
            text.Append(name).Append('|').Append(series.CycleCall).Append('|').Append(series.ArtistZOrder.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append('|').Append(alone).Append('|').Append(grouped).Append('\n');
        }
        return text.ToString();
    }

    [Fact]
    public void EveryKind_IsAsItWas()
    {
        var actual = Table();
        Assert.True(SourceFile.LineFeedTextOf(Expected) + "\n" == actual, "The table of kinds moved. Actual:\n" + actual);
    }

    private const string Expected = """
        LineSeries|Line|2|cycle|cycle
        ScatterSeries|Scatter|1|scatter|cycle
        StemSeries|Neither|2|cycle|cycle
        BarSeries|Patch|1|cycle|cycle
        AreaSeries|FillBetween|1|cycle|cycle
        ErrorBarSeries|Line|2|cycle|cycle
        HeatmapSeries|Neither|0|cycle|cycle
        StepSeries|Line|2|cycle|cycle
        StackedAreaSeries|Line|1|cycle|cycle
        ViolinSeries|Violin|1|violin|violin
        BoxSeries|Neither|2|box|cycle
        StripplotSeries|Scatter|1|cycle|cycle
        KdeSeries|Line|2|cycle|cycle
        WaterfallSeries|Line|1|cycle|cycle
        ContourfSeries|Neither|1|cycle|cycle
        HexbinSeries|Neither|1|cycle|cycle
        Histogram2DSeries|Neither|1|cycle|cycle
        ImageSeries|Neither|0|cycle|cycle
        PcolormeshSeries|Neither|1|cycle|cycle
        SpectrogramSeries|Neither|0|cycle|cycle
        TricontourSeries|Neither|2|cycle|cycle
        TripcolorSeries|Neither|1|cycle|cycle
        ClustermapSeries|Line|1|cycle|cycle
        DendrogramSeries|Line|2|cycle|cycle
        SunburstSeries|Line|1|cycle|cycle
        TreemapSeries|Line|1|cycle|cycle
        PolarHeatmapSeries|Neither|1|cycle|cycle
        Scatter3DSeries|Scatter|1|scatter|cycle
        SurfaceSeries|Neither|1|cycle|cycle
        Trisurf3DSeries|Line|1|cycle|cycle
        Bar3DSeries|Patch|1|cycle|cycle
        BarbsSeries|Neither|1|vector|cycle
        BrokenBarSeries|Neither|1|cycle|cycle
        BubbleSeries|Scatter|1|scatter|cycle
        BulletGraphSeries|Line|1|cycle|cycle
        CandlestickSeries|Line|1|cycle|cycle
        Contour3DSeries|Neither|1|cycle|cycle
        ContourSeries|Neither|2|cycle|cycle
        CountSeries|Patch|1|cycle|cycle
        DonutSeries|Patch|1|cycle|cycle
        EcdfSeries|Line|2|cycle|cycle
        EventplotSeries|Neither|2|cycle|cycle
        FunnelSeries|Line|1|cycle|cycle
        GanttSeries|Patch|1|cycle|cycle
        GaugeSeries|Line|1|cycle|cycle
        HistogramSeries|Line|1|cycle|cycle
        Line3DSeries|Line|1|cycle|cycle
        NetworkGraphSeries|Line|1|cycle|cycle
        OhlcBarSeries|Line|2|cycle|cycle
        PairGridSeries|Line|1|cycle|cycle
        PieSeries|Patch|1|cycle|cycle
        WaffleSeries|Line|1|cycle|cycle
        PointAndFigureSeries|Line|1|cycle|cycle
        IcicleSeries|Line|1|cycle|cycle
        PointplotSeries|Line|2|cycle|cycle
        PolarBarSeries|Patch|1|cycle|cycle
        PolarLineSeries|Line|2|cycle|cycle
        PolarScatterSeries|Scatter|1|scatter|cycle
        TernaryLineSeries|Line|2|cycle|cycle
        TernaryScatterSeries|Scatter|1|scatter|cycle
        PiperSeries|Line|1|cycle|cycle
        ChordSeries|Line|1|cycle|cycle
        WordCloudSeries|Line|1|cycle|cycle
        ProgressBarSeries|Line|1|cycle|cycle
        Quiver3DSeries|Neither|1|cycle|cycle
        QuiverKeySeries|Neither|1.1|vector|cycle
        QuiverSeries|Neither|1|vector|cycle
        RadarSeries|Line|1|cycle|cycle
        RegressionSeries|Line|2|cycle|cycle
        RelativeRotationSeries|Line|2|cycle|cycle
        ResidualSeries|Line|1|cycle|cycle
        RugplotSeries|Line|2|cycle|cycle
        SankeySeries|Line|1|cycle|cycle
        SignalSeries|Line|2|cycle|cycle
        SignalXYSeries|Line|2|cycle|cycle
        SparklineSeries|Line|2|cycle|cycle
        StatTileSeries|Line|1|cycle|cycle
        StateTimelineSeries|Line|1|cycle|cycle
        Stem3DSeries|Neither|1|cycle|cycle
        StreamplotSeries|Line|2|cycle|cycle
        SwarmplotSeries|Scatter|1|cycle|cycle
        TableSeries|Line|0|cycle|cycle
        Text3DSeries|Neither|1|cycle|cycle
        TreeGridSeries|Line|1|cycle|cycle
        VoxelSeries|Patch|1|cycle|cycle
        WireframeSeries|Neither|1|cycle|cycle
        PlanarBar3DSeries|Patch|1|cycle|cycle
        StreamingLineSeries|Line|2|cycle|cycle
        StreamingScatterSeries|Scatter|1|cycle|cycle
        StreamingSignalSeries|Line|2|cycle|cycle
        StreamingCandlestickSeries|Line|1|cycle|cycle
        Kde(filled)|FillBetween|1|cycle|cycle
        Contour(filled)|Neither|1|cycle|cycle
        Pie(coloured)|Neither|1|cycle|cycle
        Donut(coloured)|Neither|1|cycle|cycle
        Scatter(C)|Neither|1|scatter|cycle
        Scatter(Colors)|Neither|1|scatter|cycle
        Scatter3D(map)|Neither|1|scatter|cycle
        Trisurf3D(map)|Neither|1|cycle|cycle
        Line(coloured)|Neither|2|named|named
        Line(asks as scatter)|Scatter|2|cycle|cycle
        """;
}
