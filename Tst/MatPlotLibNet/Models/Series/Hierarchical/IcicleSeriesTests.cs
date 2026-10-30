// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>An icicle chart: one row of cells for each level of the hierarchy under the root (the root is not drawn), the cells of
/// a node side by side under it, each as wide as its share of the node's width, in the order the children are stored. The rows
/// share the plot's height equally; a cell is one row tall wherever it stands.</summary>
public class IcicleSeriesTests
{
    private static readonly Color Red = Color.FromHex("#CC0000");

    /// <summary>Four quarters of equal weight; the second splits into a leaf of 3 and a leaf of 1.</summary>
    private static TreeNode Quarters() => new()
    {
        Label = "Year",
        Children =
        [
            new TreeNode { Label = "Q1", Value = 1 },
            new TreeNode { Label = "Q2", Children = [new TreeNode { Label = "April", Value = 3 }, new TreeNode { Label = "May", Value = 1 }] },
            new TreeNode { Label = "Q3", Value = 1 },
            new TreeNode { Label = "Q4", Value = 1 },
        ],
    };

    private static Figure Of(TreeNode root, Action<IcicleSeries>? configure = null) => Plt.Create().WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax => ax.Icicle(root, configure)).Build();

    private static Rect Plot(Figure figure) => new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];

    /// <summary>The cells as drawn, in the order drawn: the rectangle and the fill each wears.</summary>
    private static List<(Rect Box, Color? Fill)> Cells(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return [.. recording.OfKind("DrawRectangle").Select(call => (dynamic)call.Args!)
            .Where(args => (Color?)args.stroke == Colors.White)
            .Select(args => ((Rect)args.rect, (Color?)args.fill))];
    }

    [Fact]
    public void ChildrenOfEqualWeight_AreEqualFractionsOfTheWidth_InTheStoredOrder()
    {
        var root = new TreeNode { Label = "Year", Children = [.. new[] { "Q1", "Q2", "Q3", "Q4" }.Select(q => new TreeNode { Label = q, Value = 1 })] };
        var figure = Of(root);
        var plot = Plot(figure);

        var cells = Cells(figure);

        Assert.Equal(4, cells.Count);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(plot.X + i * plot.Width / 4, cells[i].Box.X, 9);
            Assert.Equal(plot.Width / 4, cells[i].Box.Width, 9);
            Assert.Equal(plot.Y, cells[i].Box.Y, 9);
            Assert.Equal(plot.Height, cells[i].Box.Height, 9);   // one level: one row, the whole height
        }

        // The third quarter starts two quarters in and is a quarter wide.
        Assert.Equal(plot.X + 2 * plot.Width / 4, cells[2].Box.X, 9);
        Assert.Equal(plot.Width / 4, cells[2].Box.Width, 9);
    }

    [Fact]
    public void TheChildrenOfANode_TileItsWidth()
    {
        var figure = Of(Quarters());
        var cells = Cells(figure);
        var plot = Plot(figure);

        // The first row: four cells that tile the plot's width. The second row: the two months, tiling Q2's width.
        double rowHeight = plot.Height / 2;
        var top = cells.Where(cell => Math.Abs(cell.Box.Y - plot.Y) < 1e-9).OrderBy(cell => cell.Box.X).ToList();
        var months = cells.Where(cell => Math.Abs(cell.Box.Y - (plot.Y + rowHeight)) < 1e-9).OrderBy(cell => cell.Box.X).ToList();

        Assert.Equal(4, top.Count);
        Assert.Equal(plot.X, top[0].Box.X, 9);
        for (int i = 1; i < top.Count; i++) Assert.Equal(top[i - 1].Box.X + top[i - 1].Box.Width, top[i].Box.X, 9);
        Assert.Equal(plot.Width, top.Sum(cell => cell.Box.Width), 9);
        var q2 = top[1].Box;
        Assert.Equal(2, months.Count);
        Assert.Equal(q2.X, months[0].Box.X, 9);
        Assert.Equal(q2.X + q2.Width, months[1].Box.X + months[1].Box.Width, 9);
        Assert.Equal(3.0 / 4.0, months[0].Box.Width / q2.Width, 9);
    }

    [Fact]
    public void TheRows_AreTheLevelsOfTheHierarchy_ShareThePlotHeight_AndALeafIsOneRowTall()
    {
        var figure = Of(Quarters());
        var plot = Plot(figure);

        var cells = Cells(figure);

        Assert.All(cells, cell => Assert.Equal(plot.Height / 2, cell.Box.Height, 9));
        Assert.Equal(new[] { 0.0, 1.0 }, cells.Select(cell => Math.Round((cell.Box.Y - plot.Y) / (plot.Height / 2), 9)).Distinct().Order());
        // Q1 is a leaf on the first row: it does not reach down into the second.
        Assert.Equal(plot.Height / 2, cells.Single(cell => cell.Box.X == plot.X && cell.Box.Y == plot.Y).Box.Height, 9);
    }

    [Fact]
    public void TheRootIsNotDrawn_AndTheSiblingsKeepTheirStoredOrder()
    {
        var root = new TreeNode { Label = "Root", Children = [new TreeNode { Label = "big", Value = 3 }, new TreeNode { Label = "small", Value = 1 }, new TreeNode { Label = "medium", Value = 2 }] };
        var figure = Of(root);
        var plot = Plot(figure);

        var cells = Cells(figure);

        Assert.Equal(3, cells.Count);
        Assert.Equal([3.0 / 6, 1.0 / 6, 2.0 / 6], cells.Select(cell => cell.Box.Width / plot.Width));
        Assert.Equal(plot.X, cells[0].Box.X, 9);
    }

    [Fact]
    public void ANodeColour_Wins_ThenItsValueThroughTheMap_ThenItsPlaceAmongItsSiblings()
    {
        var root = new TreeNode
        {
            Label = "Root",
            Children =
            [
                new TreeNode { Label = "own", Value = 1, Color = Red },
                new TreeNode { Label = "valued", Value = 1, ColorValue = 1 },
                new TreeNode { Label = "plain", Value = 1 },
            ],
        };

        var fills = Cells(Of(root)).Select(cell => cell.Fill).ToList();

        Assert.Equal(Red, fills[0]);
        Assert.NotEqual(fills[1], fills[2]);
        Assert.All(fills, fill => Assert.NotNull(fill));
    }

    [Fact]
    public void ANodeWithNothingToDivide_PlacesNone_AndAnEmptyTreeDrawsNothing()
    {
        Assert.Empty(Cells(Of(new TreeNode { Label = "Root" })));
        Assert.Empty(Cells(Of(new TreeNode { Label = "Root", Children = [new TreeNode { Label = "zero", Value = 0 }, new TreeNode { Label = "nan", Value = double.NaN }] })));
    }

    [Fact]
    public void ALabelIsDrawnInItsCellWhenItFits_AndNotWhenLabelsAreOff()
    {
        var withLabels = new RecordingRenderContext();
        new ChartRenderer().Render(Of(Quarters()), withLabels);
        var without = new RecordingRenderContext();
        new ChartRenderer().Render(Of(Quarters(), s => s.ShowLabels = false), without);

        var texts = withLabels.OfKind("DrawText").Select(call => (string)((dynamic)call.Args!).text).ToList();
        Assert.Contains("Q1", texts);
        Assert.Contains("April", texts);
        Assert.DoesNotContain("Q1", without.OfKind("DrawText").Select(call => (string)((dynamic)call.Args!).text));
    }

    [Fact]
    public void ANodeHatch_IsLaidOverItsCell()
    {
        var root = new TreeNode { Label = "Root", Children = [new TreeNode { Label = "a", Value = 1, Hatch = HatchPattern.Cross, HatchColor = Colors.Black }] };

        // The hatch rides in the ShapeStyle of the cell, which the SVG writes as a pattern.
        Assert.Contains("<pattern", Of(root).ToSvg(), StringComparison.Ordinal);
        Assert.DoesNotContain("<pattern", Of(new TreeNode { Label = "Root", Children = [new TreeNode { Label = "a", Value = 1 }] }).ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheTable_IsTheTreeFlattened_WithTheDepthKept()
    {
        var table = new Axes().Icicle(Quarters()).ToDataTable()!;

        Assert.Equal(["label", "depth", "value"], table.Columns.Select(c => c.Header));
        Assert.Equal(7, table.Rows.Count);
        Assert.Equal(["Year", "Q1", "Q2", "April", "May", "Q3", "Q4"], table.Rows.Select(row => row[0].Text));
        Assert.Equal([0.0, 1, 1, 2, 2, 1, 1], table.Rows.Select(row => row[1].Number));
    }

    [Fact]
    public void TheHierarchyAndItsSettings_SurviveTheJson_AndDrawTheSamePicture()
    {
        var figure = Of(Quarters(), s => { s.ShowLabels = false; s.VMin = 0; s.VMax = 4; });

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        var icicle = Assert.IsType<IcicleSeries>(Assert.Single(back.SubPlots[0].Series));
        Assert.False(icicle.ShowLabels);
        Assert.Equal((0.0, 4.0), (icicle.VMin, icicle.VMax));
        Assert.Equal(4, icicle.Root.Children.Count);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void TheSeries_DrawsWithoutAxes_AndTakesOneColourSlot()
    {
        var icicle = new Axes().Icicle(Quarters());

        Assert.True(icicle.DrawsWithoutAxes);
        Assert.Equal(1, icicle.ColorSlotSpan);
    }
}
