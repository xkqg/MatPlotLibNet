// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Kagi</c> verb: the lines themselves are worked out by <see cref="Numerics.KagiExtensions"/> (held to a hand
/// trace in <c>KagiLineTests</c>); this holds what the verb draws with them. Two lines of one colour, thick and thin, each stretch
/// of one width a polyline of its own parted from the next by a missing value; a leg on the centre of its column, a connector from
/// one centre to the next, and the width changing on the price of the break in the middle of a line.</summary>
public class KagiTests
{
    private static readonly double[] Closes = [10, 10.5, 12, 12, 13, 12, 11, 10, 11, 14, 15, 14, 12, 9, 10];

    private static KagiParts PartsOf(double[] closes, double reversal = 1)
    {
        KagiParts parts = default;
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kagi(closes, reversal, p => parts = p)).Build();
        return parts;
    }

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.KagiByHand(false).ToSvg(), ComposedChartShapes.Named("kagi").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.KagiByHand(true).ToSvg(), ComposedChartShapes.Named("kagi").Build(true).ToSvg());
    }

    [Fact]
    public void ALegStandsOnTheCentreOfItsColumn_AndAConnectorRunsFromOneCentreToTheNext()
    {
        var parts = PartsOf(Closes);

        // The thin line: column 0 up from 10 to 13, the connector at 13 to column 1, down to 10, the connector at 10 to column 2,
        // up to 13 where the leg turns thick. The thick line takes over from there.
        Assert.Equal([0.5, 0.5, 1.5, 1.5, 2.5, 2.5, double.NaN, 3.5, 3.5, 4.5, 4.5], parts.Thin.XData);
        Assert.Equal([10.0, 13, 13, 10, 10, 13, double.NaN, 10, 9, 9, 10], parts.Thin.YData);
        Assert.Equal([2.5, 2.5, 3.5, 3.5], parts.Thick.XData);
        Assert.Equal([13.0, 15, 15, 10], parts.Thick.YData);
    }

    [Fact]
    public void TheWidthChange_IsOnThePriceOfTheBreak_WhereTheTwoLinesMeet()
    {
        var parts = PartsOf(Closes);

        // Column 2: the thin line ends at 13 (the shoulder) and the thick one begins at 13, on the same column.
        Assert.Equal((2.5, 13.0), (parts.Thin.XData[5], parts.Thin.YData[5]));
        Assert.Equal((2.5, 13.0), (parts.Thick.XData[0], parts.Thick.YData[0]));
        // Column 3: the thick line goes down to the waist at 10 and the thin one carries on from there to 9.
        Assert.Equal((3.5, 10.0), (parts.Thick.XData[3], parts.Thick.YData[3]));
        Assert.Equal((3.5, 10.0), (parts.Thin.XData[7], parts.Thin.YData[7]));
    }

    [Fact]
    public void TheXAxis_CarriesNoTicks_BecauseItIsOnlyTheOrderOfTheLegs()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kagi(Closes, 1)).Build().SubPlots[0];

        Assert.IsType<FixedLocator>(axes.XAxis.TickLocator);
        Assert.Empty(axes.XAxis.TickLocator!.Locate(0, 5));
    }

    [Fact]
    public void TheThickLineIsThreePointsWide_TheThinOneOne()
    {
        var parts = PartsOf(Closes);

        Assert.Equal(3.0, parts.Thick.LineWidth);
        Assert.Equal(1.0, parts.Thin.LineWidth);
    }

    [Fact]
    public void TheTwoLines_AreOneColour_OneSlotOfTheCycle()
    {
        var figure = Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax.Kagi(Closes, 1).Plot([0.0, 1.0], [0.0, 1.0])).Build();

        var lines = Drawn(figure).OfKind("DrawLines").Select(call => (dynamic)call.Args!).ToList();

        // The thin line and the thick line both wear the first colour of the cycle; the line drawn after them wears the second.
        Assert.All(lines.Where(args => (double)args.thickness is 1.0 or 3.0), args => Assert.Equal(Theme.Default.CycleColors[0], (Color)args.color));
        // The thin line is two stretches parted by a missing value, each drawn as a line of its own; the thick one is one.
        Assert.Equal(2, lines.Count(args => (double)args.thickness == 1.0));
        Assert.Equal(1, lines.Count(args => (double)args.thickness == 3.0));
        Assert.Contains(lines, args => (Color)args.color == Theme.Default.CycleColors[1]);
    }

    [Fact]
    public void TheParts_AreTheTwoSeriesOnTheAxes()
    {
        KagiParts parts = default;
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kagi(Closes, 1, p => parts = p)).Build().SubPlots[0];

        Assert.Equal(new ISeries[] { parts.Thin, parts.Thick }, axes.Series);
    }

    [Fact]
    public void WhatTheCallbackSets_WinsOverWhatTheVerbDid()
    {
        var parts = PartsOf(Closes);
        Assert.NotEqual(Colors.Red, parts.Thick.Color);

        KagiParts styled = default;
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kagi(Closes, 1, p => { p.Thick.Color = Colors.Red; p.Thin.LineWidth = 2; styled = p; })).Build();

        Assert.Equal(Colors.Red, styled.Thick.Color);
        Assert.Equal(2.0, styled.Thin.LineWidth);
    }

    [Fact]
    public void BothLinesExist_EvenWhenThePricesDrawNothingOfOneWidth()
    {
        var parts = PartsOf([10, 12, 11]);

        Assert.NotEmpty(parts.Thin.XData);
        Assert.Empty(parts.Thick.XData);
    }

    [Fact]
    public void PricesThatNeverMoveAReversal_LeaveTwoEmptyLines_AndDoNotBreakTheFigure()
    {
        var figure = Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax.Kagi([10, 10.4, 9.7], 1)).Build();

        Assert.All(figure.SubPlots[0].Series.Cast<LineSeries>(), line => Assert.Empty(line.XData));
        Assert.NotEmpty(figure.ToSvg());
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax.Kagi(Closes, 1)).Build();

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Equal(2, back.SubPlots[0].Series.Count);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AReversalThatCoversNoPrice_IsRefusedByName(double reversal)
    {
        Assert.Equal("reversal", Assert.Throws<ArgumentOutOfRangeException>(() => PartsOf(Closes, reversal)).ParamName);
    }

    [Fact]
    public void NoPrices_AreRefusedByName()
    {
        Assert.Equal("prices", Assert.Throws<ArgumentNullException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kagi(null!, 1)).Build()).ParamName);
    }
}
