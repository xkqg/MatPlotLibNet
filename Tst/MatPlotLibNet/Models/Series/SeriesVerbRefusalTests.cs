// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A verb whose arrays belong together refuses arrays that do not, when it builds the series: the figure is not
/// made, instead of a figure that reads back and breaks when it is drawn. Every verb is called with arrays that pair
/// (accepted) and with one array a place short (refused with the library's one wording for it); the grid verbs with a grid
/// that does not have the size its axes give.</summary>
public class SeriesVerbRefusalTests
{
    private static readonly double[] Three = [0, 1, 2];
    private static readonly double[] Two = [0, 1];

    private static double[,] Grid(int rows, int columns) => new double[rows, columns];

    /// <summary>The verbs with arrays of equal length, and the same verbs with the LAST array one place short.</summary>
    public static TheoryData<string> PointVerbs =>
    [
        "Scatter3D", "Plot3D", "Trisurf", "Stem3D", "Bar3D", "PlanarBar3D", "Quiver3D", "Barbs", "Tricontour", "Tripcolor",
        "PolarPlot", "PolarScatter", "PolarBar",
    ];

    private static void CallPointVerb(Axes axes, string verb, bool lastOneShort)
    {
        double[] Last() => lastOneShort ? Two : Three;
        switch (verb)
        {
            case "Scatter3D": axes.Scatter3D(Three, Three, Last()); break;
            case "Plot3D": axes.Plot3D(Three, Three, Last()); break;
            case "Trisurf": axes.Trisurf(Three, Three, Last()); break;
            case "Stem3D": axes.Stem3D(Three, Three, Last()); break;
            case "Bar3D": axes.Bar3D(Three, Three, Last()); break;
            case "PlanarBar3D": axes.PlanarBar3D(Three, Three, Last()); break;
            case "Quiver3D": axes.Quiver3D(Three, Three, Three, Three, Three, Last()); break;
            case "Barbs": axes.Barbs(Three, Three, Three, Last()); break;
            case "Tricontour": axes.Tricontour(Three, Three, Last()); break;
            case "Tripcolor": axes.Tripcolor(Three, Three, Last()); break;
            case "PolarPlot": axes.PolarPlot(Three, Last()); break;
            case "PolarScatter": axes.PolarScatter(Three, Last()); break;
            case "PolarBar": axes.PolarBar(Three, Last()); break;
            default: throw new ArgumentOutOfRangeException(nameof(verb), verb, "not a verb of this table");
        }
    }

    [Theory]
    [MemberData(nameof(PointVerbs))]
    public void ArraysThatPair_AreAccepted(string verb) =>
        CallPointVerb(new Axes(), verb, lastOneShort: false);

    [Theory]
    [MemberData(nameof(PointVerbs))]
    public void ArraysThatDoNotPair_AreRefused_InTheLibrariesOneWording(string verb)
    {
        var ex = Assert.Throws<ArgumentException>(() => CallPointVerb(new Axes(), verb, lastOneShort: true));

        Assert.Equal("Array lengths must match. Got 3 and 2.", ex.Message);
    }

    [Theory]
    [MemberData(nameof(PointVerbs))]
    public void ARefusedVerb_LeavesNothingOnTheAxes(string verb)
    {
        var axes = new Axes();

        Assert.ThrowsAny<ArgumentException>(() => CallPointVerb(axes, verb, lastOneShort: true));

        Assert.Empty(axes.Series);
    }

    /// <summary>The grid verbs: values indexed [row, column], one row per y and one column per x.</summary>
    public static TheoryData<string> GridVerbs => ["Surface", "Wireframe", "Contour3D", "Streamplot"];

    private static void CallGridVerb(Axes axes, string verb, int rows, int columns)
    {
        double[] x = Three, y = Two;
        var z = Grid(rows, columns);
        switch (verb)
        {
            case "Surface": axes.Surface(x, y, z); break;
            case "Wireframe": axes.Wireframe(x, y, z); break;
            case "Contour3D": axes.Contour3D(x, y, z); break;
            case "Streamplot": axes.Streamplot(x, y, z, Grid(rows, columns)); break;
            default: throw new ArgumentOutOfRangeException(nameof(verb), verb, "not a verb of this table");
        }
    }

    [Theory]
    [MemberData(nameof(GridVerbs))]
    public void AGridWithOneRowPerYAndOneColumnPerX_IsAccepted(string verb) =>
        CallGridVerb(new Axes(), verb, rows: 2, columns: 3);

    [Theory]
    [MemberData(nameof(GridVerbs))]
    public void AGridWithAColumnTooFew_IsRefused(string verb)
    {
        var ex = Assert.Throws<ArgumentException>(() => CallGridVerb(new Axes(), verb, rows: 2, columns: 2));

        Assert.Equal("Array lengths must match. The values have 2 rows and 2 columns; the axes give 2 y and 3 x values.", ex.Message);
    }

    [Theory]
    [MemberData(nameof(GridVerbs))]
    public void AGridWithARowTooMany_IsRefused(string verb)
    {
        var ex = Assert.Throws<ArgumentException>(() => CallGridVerb(new Axes(), verb, rows: 3, columns: 3));

        Assert.Equal("Array lengths must match. The values have 3 rows and 3 columns; the axes give 2 y and 3 x values.", ex.Message);
    }

    [Fact]
    public void AGridWithNoValuesAtAll_AndNoAxes_IsAccepted() =>
        new Axes().Surface([], [], Grid(0, 0));

    [Fact]
    public void StreamplotsSecondField_IsHeldToTheSameGrid()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Axes().Streamplot(Three, Two, Grid(2, 3), Grid(2, 2)));

        Assert.Contains("Array lengths must match", ex.Message);
    }

    // ---- a stack: every layer has a value at every x -----------------------------------------------------------------------

    [Fact]
    public void LayersWithAValueAtEveryX_AreAccepted() =>
        new Axes().StackPlot(Three, [[1.0, 2, 3], [3.0, 4, 5]]);

    [Fact]
    public void ALayerAValueShort_IsRefused_AndNothingIsAdded()
    {
        var axes = new Axes();

        var ex = Assert.Throws<ArgumentException>(() => axes.StackPlot(Three, [[1.0, 2, 3], [3.0, 4]]));

        Assert.Equal("Array lengths must match. Got 3 and 2.", ex.Message);
        Assert.Empty(axes.Series);
    }

    [Fact]
    public void LayersWithNoX_AreRefused() =>
        Assert.Throws<ArgumentException>(() => new Axes().StackPlot([], [[1.0, 2], [3.0, 4]]));

    [Fact]
    public void NoLayersAtAll_NeedNoCheck() =>
        new Axes().StackPlot(Three, []);

    // ---- a mesh: edges, one more than the cells ----------------------------------------------------------------------------

    [Fact]
    public void AMeshWithOneEdgeMoreThanItHasCells_IsAccepted() =>
        new Axes().Pcolormesh(new double[] { 0, 1, 2, 3 }, new double[] { 0, 1, 2 }, Grid(2, 3));

    [Fact]
    public void AMeshWithAnEdgeTooFew_IsRefused()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Axes().Pcolormesh(new double[] { 0, 1, 2 }, new double[] { 0, 1, 2 }, Grid(2, 3)));

        Assert.Equal("Array lengths must match. The values have 2 rows and 3 columns; the axes give 2 y and 2 x values.", ex.Message);
    }

    [Fact]
    public void AMeshWithNoCells_IsAccepted() =>
        new Axes().Pcolormesh(Array.Empty<double>(), Array.Empty<double>(), Grid(0, 0));

    // ---- a document is read through the same verbs ---------------------------------------------------------------------------

    /// <summary>A figure read from JSON builds each series through its verb, so a document whose arrays do not pair is refused
    /// when it is read, in the verb's own words, and never reaches a draw.</summary>
    [Theory]
    [InlineData("""{"type":"surface","xData":[0,1,2],"yData":[0,1],"zGridData":[[1,2],[3,4]]}""")]
    [InlineData("""{"type":"line3d","xData":[1,2,3],"yData":[1,2,3],"zData":[1]}""")]
    [InlineData("""{"type":"barbs","xData":[1,2],"yData":[1,2],"speed":[1,2],"direction":[1]}""")]
    [InlineData("""{"type":"polarline","rData":[1,2,3],"thetaData":[1,2]}""")]
    [InlineData("""{"type":"streamplot","xData":[0,1],"yData":[0,1],"heatmapData":[[1,2],[3,4]],"vFieldData":[[1,2]]}""")]
    [InlineData("""{"type":"pcolormesh","xData":[0,1,2],"yData":[0,1,2],"heatmapData":[[1,2,3],[4,5,6]]}""")]
    public void ADocumentWhoseSeriesDoesNotFit_IsRefusedWhenItIsRead(string series)
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            new ChartSerializer().FromJson($$"""{"subPlots":[{"series":[{{series}}]}]}"""));

        Assert.StartsWith("Array lengths must match.", ex.Message);
    }

    // ---- a Sankey: the links name nodes that exist -------------------------------------------------------------------------

    [Fact]
    public void LinksBetweenNodesThatExist_AreAccepted() =>
        new Axes().Sankey([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(0, 1, 5)]);

    [Fact]
    public void ALinkToANodeThatDoesNotExist_IsRefused_NamingTheLinkAndTheNode()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Axes().Sankey([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(0, 1, 5), new SankeyLink(0, 5, 2)]));

        Assert.Equal("Link 1 points at node 5, but there are only 2 nodes.", ex.Message);
    }

    [Fact]
    public void ALinkFromANegativeNode_IsRefused()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new Axes().Sankey([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(-1, 1, 5)]));

        Assert.Equal("Link 0 points at node -1, but there are only 2 nodes.", ex.Message);
    }

    [Fact]
    public void NoLinksAtAll_NeedNoNodes() =>
        new Axes().Sankey([], []);
}
