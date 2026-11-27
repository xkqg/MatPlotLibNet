// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>What an SVG draw writes for the browser — the data attributes of interactive series, the mapping a data cursor
/// converts a click with, the view the server script reads and the vertex data a 3-D axes is turned with — follows from the
/// figure as it is when it is drawn, and the draw writes nothing of it into the model: the figure serialises the same before
/// and after a draw, and an interaction switched off is gone from the next draw.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class SvgEmitTests
{
    private static readonly double[] X = [0.0, 1, 2, 3];
    private static readonly double[] Y = [1.0, 3, 2, 4];

    private static Figure Interactive()
    {
        var figure = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 2, 1, ax => ax.Plot(X, Y, s => s.Label = "a").WithLegend())
            .AddSubPlot(1, 2, 2, ax => ax.Surface([0.0, 1, 2], [0.0, 1, 2], new double[,] { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } }))
            .With3DRotation()
            .Build();
        figure.EnableLegendToggle = true;
        figure.ServerInteraction = true;
        figure.EnableDataCursor = true;
        figure.ChartId = "c";
        return figure;
    }

    [Fact]
    public void ADraw_WritesNothingIntoTheModel()
    {
        var figure = Interactive();
        string before = figure.ToJson();

        _ = figure.ToSvg();

        Assert.Equal(before, figure.ToJson());
        Assert.All(figure.SubPlots, axes =>
        {
            Assert.False(axes.EnableInteractiveAttributes);
            Assert.False(axes.Emit3DVertexData);
        });
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ADraw_StillWritesWhatTheFigureAsksFor", () => Interactive().ToSvg(), "data-series-index", "data-v3d"),
        new DrawnToken("TheUsersOwnSwitches_StillWriteTheAttributes", () =>
        {
            var figure = Plt.Create().Plot(X, Y).Build();
            figure.SubPlots[0].EnableInteractiveAttributes = true;
            return figure.ToSvg();
        }, "data-series-index"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void InteractionSwitchedOff_IsGoneFromTheNextDraw_AsFromAFigureThatNeverHadIt()
    {
        static void Off(Figure f)
        {
            f.EnableLegendToggle = false;
            f.ServerInteraction = false;
            f.EnableDataCursor = false;
            f.Enable3DRotation = false;
        }

        var figure = Interactive();
        _ = figure.ToSvg();
        Off(figure);
        var fresh = Interactive();
        Off(fresh);

        string drawn = figure.ToSvg();

        Assert.Equal(fresh.ToSvg(), drawn);
        Assert.DoesNotContain("data-series-index", drawn);
        Assert.DoesNotContain("data-v3d", drawn);
    }

    [Fact]
    public void AnInset_IsNotGivenTheMarksOfTheSubplotItStandsIn()
    {
        var figure = Plt.Create().WithSize(400, 300).Plot(X, Y).Build();
        var inset = figure.SubPlots[0].AddInset(0.6, 0.6, 0.35, 0.35);
        inset.Plot(X, Y);
        figure.EnableLegendToggle = true;
        var withoutInset = Plt.Create().WithSize(400, 300).Plot(X, Y).Build();
        withoutInset.EnableLegendToggle = true;

        int Count(string svg) => svg.Split("data-series-index").Length - 1;

        Assert.Equal(Count(withoutInset.ToSvg()), Count(figure.ToSvg()));
    }
}
