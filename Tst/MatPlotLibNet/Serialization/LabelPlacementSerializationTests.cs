// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>
/// Where a series shows its name (<see cref="SeriesLabelPlacement"/>) travels with the figure: REST, SignalR, Blazor and
/// the MCP server all move figures as JSON, and a line whose name was asked at the line end must not fall back to a
/// legend entry on the other side. The placement is written and read at the ONE generic label seam, so every
/// <see cref="IHasLabelPlacement"/> carrier gets it on the primary axis and on both secondary axes alike. A JSON
/// written before the field existed reads as the default, and a default figure writes byte-identical JSON (the golden
/// corpus holds that). Every assertion reads the restored VALUE, never a count.
/// </summary>
[Collection("ChartSerializerGlobalState")]
public class LabelPlacementSerializationTests
{
    private static readonly ChartSerializer S = new();
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [2.0, 3.0, 4.0];

    [Fact]
    public void APrimaryLine_AskedForItsNameAtTheEnd_KeepsThatAfterTheRoundTrip()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "history")
            .Plot(X, Y, s => s.Label = "forecast")
            .WithDirectLabel()).Build();

        var series = S.FromJson(S.ToJson(figure)).SubPlots[0].Series;

        Assert.Equal(SeriesLabelPlacement.Legend, ((LineSeries)series[0]).LabelPlacement);
        Assert.Equal(SeriesLabelPlacement.LineEnd, ((LineSeries)series[1]).LabelPlacement);
        Assert.Equal("forecast", series[1].Label);
    }

    [Fact]
    public void ASecondaryYLine_AskedForItsNameAtTheEnd_KeepsThatAfterTheRoundTrip()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "left")
            .WithSecondaryYAxis(y2 => y2
                .Plot(X, Y, s => s.Label = "plain")
                .Plot(X, Y, s => s.Label = "right")
                .WithDirectLabel())).Build();

        var secondary = S.FromJson(S.ToJson(figure)).SubPlots[0].SecondarySeries;

        Assert.Equal(SeriesLabelPlacement.Legend, ((LineSeries)secondary[0]).LabelPlacement);
        Assert.Equal(SeriesLabelPlacement.LineEnd, ((LineSeries)secondary[1]).LabelPlacement);
        Assert.Equal("right", secondary[1].Label);
    }

    [Fact]
    public void ASecondaryXLine_AskedForItsNameAtTheEnd_KeepsThatAfterTheRoundTrip()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot(X, Y);
            ax.WithSecondaryXAxis(x2 => x2.PlotXSecondary(X, Y, s =>
            {
                s.Label = "top";
                s.LabelPlacement = SeriesLabelPlacement.LineEnd;
            }));
        }).Build();

        var secondary = S.FromJson(S.ToJson(figure)).SubPlots[0].XSecondarySeries;

        Assert.Equal(SeriesLabelPlacement.LineEnd, ((LineSeries)secondary[0]).LabelPlacement);
    }

    [Fact]
    public void ALineInAnInset_KeepsItsPlacement_BecauseInsetsRestoreThroughTheSamePath()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y)
            .AddInset(0.5, 0.5, 0.3, 0.3, inset => inset
                .Plot(X, Y, s => s.Label = "zoom")
                .WithDirectLabel())).Build();

        var inset = S.FromJson(S.ToJson(figure)).SubPlots[0].Insets[0];

        Assert.Equal(SeriesLabelPlacement.LineEnd, ((LineSeries)inset.Series[0]).LabelPlacement);
    }

    [Fact]
    public void ADefaultFigure_WritesNoPlacementField_SoItsJsonIsUnchanged()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "a")
            .WithSecondaryYAxis(y2 => y2.Plot(X, Y, s => s.Label = "b"))).Build();

        Assert.DoesNotContain("labelPlacement", S.ToJson(figure), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AMarkedLine_WritesThePlacementByName_NotByOrdinal()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => s.Label = "a").WithDirectLabel()).Build();

        string json = S.ToJson(figure);

        Assert.Contains("\"labelPlacement\":\"lineend\"", json);
    }

    [Fact]
    public void AJsonWrittenBeforeThePlacementExisted_ReadsAsALegendEntry()
    {
        const string vintage = """
        {"width":400,"height":300,"subPlots":[{
          "series":[{"type":"line","label":"old","xData":[1,2,3],"yData":[2,3,4]}],
          "secondaryYAxis":{"scale":"linear"},
          "secondarySeries":[{"type":"line","label":"old2","xData":[1,2,3],"yData":[2,3,4]}]
        }]}
        """;

        var axes = S.FromJson(vintage).SubPlots[0];

        Assert.Equal(SeriesLabelPlacement.Legend, ((LineSeries)axes.Series[0]).LabelPlacement);
        Assert.Equal(SeriesLabelPlacement.Legend, ((LineSeries)axes.SecondarySeries[0]).LabelPlacement);
        Assert.Equal("old", axes.Series[0].Label);
    }

    [Theory]
    [InlineData("LineEnd")]
    [InlineData("lineend")]
    [InlineData("LINEEND")]
    public void ThePlacementIsReadWithoutRegardToCase(string written)
    {
        string json = $$"""
        {"width":400,"height":300,"subPlots":[{"series":[
          {"type":"line","label":"a","xData":[1,2,3],"yData":[2,3,4],"labelPlacement":"{{written}}"}]}]}
        """;

        var line = (LineSeries)S.FromJson(json).SubPlots[0].Series[0];

        Assert.Equal(SeriesLabelPlacement.LineEnd, line.LabelPlacement);
    }

    [Fact]
    public void AnUnknownPlacementName_IsLenient_AndLeavesTheLegendEntry()
    {
        // The serializer's lenience is wire compatibility; the MCP reader is the door that refuses the typo.
        const string json = """
        {"width":400,"height":300,"subPlots":[{"series":[
          {"type":"line","label":"a","xData":[1,2,3],"yData":[2,3,4],"labelPlacement":"somewhere"}]}]}
        """;

        var line = (LineSeries)S.FromJson(json).SubPlots[0].Series[0];

        Assert.Equal(SeriesLabelPlacement.Legend, line.LabelPlacement);
    }

    [Fact]
    public void APlacementOnASeriesThatCannotCarryIt_IsIgnored_AndTheSeriesStillLoads()
    {
        const string json = """
        {"width":400,"height":300,"subPlots":[{"series":[
          {"type":"scatter","label":"dots","xData":[1,2,3],"yData":[2,3,4],"labelPlacement":"lineEnd"}]}]}
        """;

        var series = S.FromJson(json).SubPlots[0].Series;

        Assert.IsType<ScatterSeries>(series[0]);
        Assert.Equal("dots", series[0].Label);
    }

    [Fact]
    public void AStreamingLine_WritesItsPlacement_AndNowSurvivesFromJson()
    {
        // Was: a streaming series serialised as Type "streaming" (its ring buffer and capacity were not part of
        // the wire format, and the same discriminator covered four classes), no factory was registered for it,
        // and FromJson dropped it. Fixed (JSON-roundtrip plan, cluster B): each streaming kind now writes and
        // reads its own discriminator, carrying its current snapshot.
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot(X, Y, s => s.Label = "history");
            ax.StreamingPlot(configure: s => { s.Label = "live"; s.LabelPlacement = SeriesLabelPlacement.LineEnd; });
        }).Build();

        string json = S.ToJson(figure);
        var restored = S.FromJson(json).SubPlots[0];

        Assert.Contains("\"type\":\"streaming_line\"", json);
        Assert.Contains("\"labelPlacement\":\"lineend\"", json);
        Assert.Equal(2, restored.Series.Count);
        Assert.Equal("history", restored.Series[0].Label);
        var live = Assert.IsType<StreamingLineSeries>(restored.Series[1]);
        Assert.Equal("live", live.Label);
        Assert.Equal(SeriesLabelPlacement.LineEnd, live.LabelPlacement);
    }

    [Fact]
    public void ASecondaryStreamingLine_WritesItsPlacement_ButIsStillDroppedByFromJson()
    {
        // The streaming series itself now round-trips (see the Fact above); a secondary-axis series restore
        // still only knows "line"/"scatter" (ChartSerializer.AddSecondarySeriesFromDtos) — a different, narrower
        // gap than the one this cluster fixed (roadmap: "More kinds of series on a second axis"), unchanged here.
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y)
            .WithSecondaryYAxis(y2 =>
            {
                var live = y2.StreamingPlot(configure: s => s.Label = "live");
                live.LabelPlacement = SeriesLabelPlacement.LineEnd;
            })).Build();

        string json = S.ToJson(figure);
        var restored = S.FromJson(json).SubPlots[0];

        Assert.Contains("\"type\":\"streaming_line\"", json);
        Assert.Contains("\"labelPlacement\":\"lineend\"", json);
        Assert.NotNull(restored.SecondaryYAxis);
        Assert.Empty(restored.SecondarySeries);
    }
}
