// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>
/// An axis colour (SetYAxisColor / SetXAxisColor) lives in four places: the spine, the tick marks, the tick labels and the
/// title style. Figures travel as JSON (REST, SignalR, Blazor, the MCP server), so each of the four must survive the
/// trip, on all four axes, and a JSON written before the colour existed must read as "no colour".
/// </summary>
[Collection("ChartSerializerGlobalState")]
public class AxisColorSerializationTests
{
    private static readonly ChartSerializer S = new();
    private static readonly Color Red = Color.FromHex("#CC0000");
    private static readonly Color Green = Color.FromHex("#00AA00");
    private static readonly Color Blue = Color.FromHex("#0000CC");
    private static readonly Color Pink = Color.FromHex("#E4007C");

    private static Figure AllFourAxesColoured() => Plt.Create().WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([1.0, 2, 3], [1.0, 4, 9]).SetXLabel("Time").SetYLabel("Level")
                .SetYAxisColor(Red).SetXAxisColor(Green);
            ax.WithAddRightYAxis(s => s.Plot([1.0, 2, 3], [10.0, 20, 30]).SetYLabel("Right").SetYAxisColor(Blue));
            ax.WithSecondaryXAxis(s => s.PlotXSecondary([10.0, 20, 30], [1.0, 2, 3]).SetXLabel("Top").SetXAxisColor(Pink));
        })
        .Build();

    [Fact]
    public void ColouredRightYAxis_KeepsSpineTicksLabelsAndTitleColour()
    {
        var axes = S.FromJson(S.ToJson(AllFourAxesColoured())).SubPlots[0];

        Assert.Equal(Blue, axes.Spines.Right.Color);
        Assert.Equal(Blue, axes.YAxes.Right[0].Axis.MajorTicks.Color);
        Assert.Equal(Blue, axes.YAxes.Right[0].Axis.MajorTicks.LabelColor);
        Assert.Equal(Blue, axes.YAxes.Right[0].Axis.LabelStyle!.Color);
        Assert.Equal("Right", axes.YAxes.Right[0].Axis.Label);
    }

    [Fact]
    public void ColouredSecondaryXAxis_KeepsSpineTicksLabelsAndTitleColour_andTheAxisItself()
    {
        var axes = S.FromJson(S.ToJson(AllFourAxesColoured())).SubPlots[0];

        Assert.Equal(Pink, axes.Spines.Top.Color);
        Assert.NotNull(axes.SecondaryXAxis);
        Assert.Equal(Pink, axes.SecondaryXAxis.MajorTicks.Color);
        Assert.Equal(Pink, axes.SecondaryXAxis.MajorTicks.LabelColor);
        Assert.Equal(Pink, axes.SecondaryXAxis.LabelStyle!.Color);
        Assert.Equal("Top", axes.SecondaryXAxis.Label);
    }

    [Fact]
    public void SecondaryXAxis_KeepsItsLabelLimitsScaleAndSeries()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [1.0, 4, 9])
                .WithSecondaryXAxis(s => s
                    .SetXLabel("Top").SetXLim(5, 35)
                    .PlotXSecondary([10.0, 20, 30], [1.0, 2, 3], line => line.Label = "upper")
                    .ScatterXSecondary([12.0, 22], [1.5, 2.5])))
            .Build();

        var axes = S.FromJson(S.ToJson(figure)).SubPlots[0];

        Assert.Equal("Top", axes.SecondaryXAxis!.Label);
        Assert.Equal(5, axes.SecondaryXAxis.Min);
        Assert.Equal(35, axes.SecondaryXAxis.Max);
        Assert.Equal(2, axes.XSecondarySeries.Count);
        Assert.IsType<MatPlotLibNet.Models.Series.LineSeries>(axes.XSecondarySeries[0]);
        Assert.Equal("upper", axes.XSecondarySeries[0].Label);
        Assert.IsType<MatPlotLibNet.Models.Series.ScatterSeries>(axes.XSecondarySeries[1]);
        Assert.Empty(axes.YAxes.Right);
    }

    [Fact]
    public void ARestoredFigureDrawsTheSameSvg_asTheOneThatWasSerialised()
    {
        var figure = AllFourAxesColoured();

        Assert.Equal(figure.ToSvg(), S.FromJson(S.ToJson(figure)).ToSvg());
    }

    [Fact]
    public void ASpineWhoseOnlyChangeIsItsColour_isWritten()
    {
        // SpinesAreDefault used to look only at Visible and Position, so a colour-only change wrote no spines at all.
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetYAxisColor(Red)).Build();

        var json = S.ToJson(figure);

        Assert.Contains("spines", json);
        var axes = S.FromJson(json).SubPlots[0];
        Assert.Equal(Red, axes.Spines.Left.Color);
        Assert.Null(axes.Spines.Right.Color);
        Assert.Null(axes.Spines.Top.Color);
        Assert.Null(axes.Spines.Bottom.Color);
    }

    [Fact]
    public void ASpineWhoseOnlyChangeIsItsWidth_isWrittenToo()
    {
        // Same defect as the colour: the default check ignored LineWidth, so a width-only change vanished.
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithSpines(s => s with { Bottom = s.Bottom with { LineWidth = 3 } })).Build();

        var axes = S.FromJson(S.ToJson(figure)).SubPlots[0];

        Assert.Equal(3, axes.Spines.Bottom.LineWidth);
        Assert.Equal(new SpineConfig().LineWidth, axes.Spines.Top.LineWidth);
    }

    [Fact]
    public void AnUncolouredFigure_writesNoneOfTheColourFields()
    {
        var json = S.ToJson(Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2], [3.0, 4]).SetXLabel("x").SetYLabel("y")
            .WithAddRightYAxis(s => s.Plot([1.0, 2], [5.0, 6]))).Build());

        Assert.DoesNotContain("spines", json);
        Assert.DoesNotContain("tickColor", json);
        Assert.DoesNotContain("tickLabelColor", json);
        Assert.DoesNotContain("labelColor", json);
        Assert.DoesNotContain("secondaryXAxis", json);
    }

    [Fact]
    public void AJsonWrittenBeforeAxisColoursExisted_readsAsNoColour()
    {
        const string vintage = """
            {"width":800,"height":600,"subPlots":[{
              "spines":{"left":{"visible":true,"positionValue":0,"lineWidth":0.8}},
              "xAxis":{"label":"Time","scale":"linear"},
              "yAxis":{"label":"Level","scale":"linear"},
              "secondaryYAxis":{"label":"Right","scale":"linear"}}]}
            """;

        var axes = S.FromJson(vintage).SubPlots[0];

        Assert.Null(axes.Spines.Left.Color);
        Assert.Null(axes.YAxis.MajorTicks.Color);
        Assert.Null(axes.YAxis.MajorTicks.LabelColor);
        Assert.Null(axes.YAxis.LabelStyle);
        Assert.Equal("Level", axes.YAxis.Label);
        Assert.Null(axes.YAxes.Right[0].Axis.MajorTicks.Color);
        Assert.Null(axes.YAxes.Right[0].Axis.LabelStyle);
        Assert.Null(axes.SecondaryXAxis);
    }

    [Fact]
    public void AColourNamedInTheJson_isReadBack_evenWhenTheLabelStyleWasNeverSet()
    {
        const string json = """
            {"subPlots":[{"yAxis":{"tickColor":"#CC0000","tickLabelColor":"#00AA00","labelColor":"#0000CC"},
                          "spines":{"left":{"visible":true,"color":"#CC0000"}}}]}
            """;

        var axes = S.FromJson(json).SubPlots[0];

        Assert.Equal(Red, axes.YAxis.MajorTicks.Color);
        Assert.Equal(Green, axes.YAxis.MajorTicks.LabelColor);
        Assert.Equal(Blue, axes.YAxis.LabelStyle!.Color);
        Assert.Equal(Red, axes.Spines.Left.Color);
    }
}
