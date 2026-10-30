// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Charts of the same data on one page can be linked: a brush on one lights the same rows on the others. The link is a
/// name, <c>SelectionGroup</c>, that the charts share; a chart with no group is linked to nothing. The rows a chart starts with
/// selected, <c>SelectedRows</c>, are the application's own data (the state it restores), carried in the JSON; what the reader
/// brushes afterwards stays in their browser.</summary>
public class LinkedSelectionTests
{
    [Fact]
    public void AFigure_IsLinkedToNothing_AndStartsWithNothingSelected()
    {
        var figure = new Figure();

        Assert.Null(figure.SelectionGroup);
        Assert.Null(figure.SelectedRows);
        Assert.False(figure.HasInteractivity);
    }

    [Fact]
    public void AGroup_MakesTheFigureInteractive_AndIsRefusedWhenBlank()
    {
        var figure = new Figure { SelectionGroup = "orders" };

        Assert.True(figure.HasInteractivity);
        Assert.Throws<ArgumentException>(() => figure.SelectionGroup = " ");
        Assert.Throws<ArgumentException>(() => figure.SelectionGroup = "");
        figure.SelectionGroup = null;
        Assert.False(figure.HasInteractivity);
    }

    [Fact]
    public void TheSelectedRows_AreKeys_NoneBlank_AndNullClearsThem()
    {
        var figure = new Figure { SelectionGroup = "orders" };

        figure.SelectedRows = ["a", "b"];
        Assert.Equal(["a", "b"], figure.SelectedRows);

        Assert.Throws<ArgumentException>(() => figure.SelectedRows = ["a", ""]);
        Assert.Equal(["a", "b"], figure.SelectedRows);   // a refused set changes nothing

        figure.SelectedRows = null;
        Assert.Null(figure.SelectedRows);
    }

    [Fact]
    public void TheBuilder_LinksTheFigure_AndSwitchesTheBrushOn()
    {
        var figure = Plt.Create().LinkSelection("orders", ["b"]).Build();

        Assert.Equal("orders", figure.SelectionGroup);
        Assert.Equal(["b"], figure.SelectedRows);
        Assert.True(figure.EnableSelection);   // the brush is how rows are selected
    }

    [Fact]
    public void TheBuilder_LinksWithNoRowsSelected_ByDefault()
    {
        var figure = Plt.Create().LinkSelection("orders").Build();

        Assert.Null(figure.SelectedRows);
    }

    [Fact]
    public void TheGroupAndTheRows_RoundTripThroughTheJson()
    {
        var figure = new Figure { SelectionGroup = "orders", SelectedRows = ["a", "c"], EnableSelection = true };

        var read = new ChartSerializer().FromJson(figure.ToJson());

        Assert.Equal("orders", read.SelectionGroup);
        Assert.Equal(["a", "c"], read.SelectedRows);
    }

    [Fact]
    public void WithoutAGroup_TheJsonSaysNothingOfIt()
    {
        string json = new Figure().ToJson();

        Assert.DoesNotContain("selectionGroup", json, StringComparison.Ordinal);
        Assert.DoesNotContain("selectedRows", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ADocumentWithABlankGroup_IsRefusedWhenItIsRead()
    {
        string json = new Figure { SelectionGroup = "orders" }.ToJson().Replace("\"orders\"", "\"\"", StringComparison.Ordinal);

        Assert.ThrowsAny<ArgumentException>(() => new ChartSerializer().FromJson(json));
        Assert.NotNull(JsonDocument.Parse(json));
    }
}
