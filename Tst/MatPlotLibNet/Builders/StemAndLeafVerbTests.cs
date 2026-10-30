// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>StemAndLeaf</c> verb: the rows themselves are worked out by <see cref="Numerics.StemAndLeafExtensions"/> (held to
/// hand-worked vectors in <c>StemAndLeafTests</c>); this holds what the verb draws with them. A table with no border and no fill
/// whose stem column is aligned to the right and whose leaves have a column each, so the k-th leaf of every row stands at one
/// x in any font, and a key that says how to read it.</summary>
public class StemAndLeafVerbTests
{
    private static readonly double[] Ages = [12, 15, 21, 23, 23, 38, 41, 44, 47, 52];

    private static StemAndLeafParts PartsOf(double[] sample, double? leafUnit = null)
    {
        StemAndLeafParts parts = default;
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StemAndLeaf(sample, leafUnit, p => parts = p)).Build();
        return parts;
    }

    private static List<(string Text, Point At, TextAlignment Alignment)> Texts(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return [.. recording.OfKind("DrawText").Select(call => (dynamic)call.Args!).Select(args => ((string)args.text, (Point)args.position, (TextAlignment)args.alignment))];
    }

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.StemAndLeafByHand(false).ToSvg(), ComposedChartShapes.Named("stem-and-leaf").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.StemAndLeafByHand(true).ToSvg(), ComposedChartShapes.Named("stem-and-leaf").Build(true).ToSvg());
    }

    [Fact]
    public void TheTable_HasARowForEachStem_AndAColumnForEachLeafOfTheFullestRow()
    {
        var table = PartsOf(Ages).Table;

        Assert.Equal(
            [["1", "|", "2", "5", ""], ["2", "|", "1", "3", "3"], ["3", "|", "8", "", ""], ["4", "|", "1", "4", "7"], ["5", "|", "2", "", ""]],
            table.CellData);
        Assert.Equal([TextAlignment.Right, TextAlignment.Center, TextAlignment.Center, TextAlignment.Center, TextAlignment.Center], table.ColumnAlignments!);
        Assert.Equal([2.0, 0.5, 1, 1, 1], table.ColumnWidths!);
    }

    [Fact]
    public void TheStems_AreAlignedToTheRightOnOneX_AndTheKthLeafOfEveryRow_StandsOnOneX()
    {
        var texts = Texts(Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax.StemAndLeaf([.. Ages, 105, 112])).Build());

        // A stem of one digit and a stem of three: aligned to the right edge, both end at the same x.
        var stems = texts.Where(text => text.Text is "1" or "10" or "11" && text.Alignment == TextAlignment.Right).ToList();
        Assert.NotEmpty(stems);
        Assert.Single(stems.Select(stem => Math.Round(stem.At.X, 9)).Distinct());

        var leaves = texts.Where(text => text.Alignment == TextAlignment.Center && text.Text.Length == 1 && text.Text != "|").ToList();
        // The leaves of one column share an x whatever their row: every x holds one column, and there are three columns.
        var columns = leaves.Select(leaf => Math.Round(leaf.At.X, 9)).Distinct().Order().ToList();
        Assert.True(columns.Count >= 3, "at least one column for each leaf of the fullest row");
        Assert.All(leaves, leaf => Assert.Contains(Math.Round(leaf.At.X, 9), columns));
    }

    [Fact]
    public void TheKey_SaysHowToReadTheDisplay()
    {
        Assert.Equal("Key: 1 | 2 = 12", PartsOf(Ages).Key.Text);
        Assert.Equal("Key: 1 | 2 = 1.2", PartsOf([1.2, 1.5, 2.1, 2.3], 0.1).Key.Text);
        Assert.Equal("Key: 1 | 2 = 120", PartsOf([120, 150, 210, 230], 10).Key.Text);
        Assert.Equal("Key: -1 | 5 = -15", PartsOf([-15, -12, -3, 4, 17]).Key.Text);
    }

    [Fact]
    public void TheKey_IsPinnedToThePlotAtItsLowerLeft()
    {
        var key = PartsOf(Ages).Key;

        Assert.Equal(AnnotationCoordinates.AxesFraction, key.Coordinates);
        Assert.Equal(TextAlignment.Left, key.Alignment);
        Assert.Equal(TextVerticalAlignment.Bottom, key.VerticalAlignment);
    }

    [Fact]
    public void TheTable_HasNeitherBorderNorFill_AndTheParts_AreTheSeriesAndTheAnnotationOnTheAxes()
    {
        StemAndLeafParts parts = default;
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StemAndLeaf(Ages, null, p => parts = p)).Build().SubPlots[0];

        Assert.Equal(Colors.Transparent, parts.Table.CellColor);
        Assert.Equal(Colors.Transparent, parts.Table.BorderColor);
        Assert.Same(parts.Table, Assert.Single(axes.Series));
        Assert.Contains(parts.Key, axes.Annotations);
    }

    [Fact]
    public void WhatTheCallbackSets_WinsOverWhatTheVerbDid()
    {
        StemAndLeafParts parts = default;
        Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StemAndLeaf(Ages, null, p => { p.Table.FontSize = 16; p.Key.Color = Colors.Red; parts = p; })).Build();

        Assert.Equal(16.0, parts.Table.FontSize);
        Assert.Equal(Colors.Red, parts.Key.Color);
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax.StemAndLeaf(Ages)).Build();

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void ASampleWithNothingToPut_IsRefusedByName()
    {
        Assert.Equal("sample", Assert.Throws<ArgumentNullException>(() => PartsOf(null!)).ParamName);
        Assert.Equal("sample", Assert.Throws<ArgumentException>(() => PartsOf([double.NaN])).ParamName);
        Assert.Equal("leafUnit", Assert.Throws<ArgumentOutOfRangeException>(() => PartsOf(Ages, 2)).ParamName);
    }
}
