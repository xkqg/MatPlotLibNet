// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>What an author changed about a grid, as a value: the properties they set and nothing else, applied over whatever
/// grid the figure is drawn under. It is built from the <c>WithGrid</c> callback by running that callback on two grids that
/// differ in every property, so that a property it sets (to any value, a value the theme happens to draw included) is told from
/// one it leaves alone.</summary>
public class GridChangeTests
{
    private static readonly GridStyle Base = new()
    {
        Visible = true, Color = Colors.Blue, LineStyle = LineStyle.Dotted, LineWidth = 3, Alpha = 0.5, Which = GridWhich.Both, Axis = GridAxis.Y,
    };

    // ── applied over a grid ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyTo_ReplacesOnlyThePropertiesTheChangeSets()
    {
        var change = new GridChange(Color: Colors.Red, Alpha: 0.1);

        var applied = change.ApplyTo(Base);

        Assert.Equal(Base with { Color = Colors.Red, Alpha = 0.1 }, applied);
    }

    [Fact]
    public void ApplyTo_OfAChangeThatSetsNothing_IsTheGridItself() =>
        Assert.Equal(Base, default(GridChange).ApplyTo(Base));

    [Fact]
    public void ApplyTo_SetsEveryProperty_WhenTheChangeNamesAll()
    {
        var change = new GridChange(false, Colors.Green, LineStyle.Dashed, 7, 0.9, GridWhich.Minor, GridAxis.X);

        Assert.Equal(new GridStyle { Visible = false, Color = Colors.Green, LineStyle = LineStyle.Dashed, LineWidth = 7, Alpha = 0.9, Which = GridWhich.Minor, Axis = GridAxis.X },
            change.ApplyTo(Base));
    }

    // ── one change after another ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Then_TheLaterChangeWinsPerProperty_TheEarlierOneKeepsTheRest()
    {
        var first = new GridChange(Visible: true, Color: Colors.Red, LineWidth: 2);
        var second = new GridChange(Color: Colors.Green, Alpha: 0.3);

        var both = first.Then(second);

        Assert.Equal(new GridChange(Visible: true, Color: Colors.Green, LineWidth: 2, Alpha: 0.3), both);
        Assert.Equal(second.ApplyTo(first.ApplyTo(Base)), both.ApplyTo(Base));
    }

    // ── read off the callback ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Of_AChangeThatSetsOneProperty_RecordsThatPropertyAlone()
    {
        Assert.Equal(new GridChange(Alpha: 0.4), GridChange.Of(g => g with { Alpha = 0.4 }));
        Assert.Equal(new GridChange(Color: Colors.Red), GridChange.Of(g => g with { Color = Colors.Red }));
        Assert.Equal(new GridChange(LineStyle: LineStyle.Dashed), GridChange.Of(g => g with { LineStyle = LineStyle.Dashed }));
        Assert.Equal(new GridChange(LineWidth: 2.5), GridChange.Of(g => g with { LineWidth = 2.5 }));
        Assert.Equal(new GridChange(Which: GridWhich.Minor), GridChange.Of(g => g with { Which = GridWhich.Minor }));
        Assert.Equal(new GridChange(Axis: GridAxis.X), GridChange.Of(g => g with { Axis = GridAxis.X }));
    }

    /// <summary>The case a change kept as "what differs from the theme's grid" loses: <c>Visible = true</c> over a theme that
    /// draws a grid equals the theme's value, and is still said.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Of_VisibleSetToEitherValue_IsRecorded(bool visible) =>
        Assert.Equal(new GridChange(Visible: visible), GridChange.Of(g => g with { Visible = visible }));

    [Fact]
    public void Of_AChangeThatSetsSeveralProperties_RecordsThemAll() =>
        Assert.Equal(new GridChange(Visible: true, Color: Colors.Red, LineWidth: 2),
            GridChange.Of(g => g with { Visible = true, Color = Colors.Red, LineWidth = 2 }));

    [Fact]
    public void Of_ACallbackThatReturnsTheGridUnchanged_SetsNothing() =>
        Assert.Equal(default, GridChange.Of(g => g));

    /// <summary>The recorded change applied over a grid gives what the callback gives on that grid, for a callback that sets
    /// properties to values.</summary>
    [Fact]
    public void Of_AppliedOverAGrid_GivesWhatTheCallbackGivesOnIt()
    {
        Func<GridStyle, GridStyle> callback = g => g with { Visible = true, Alpha = 0.25, Which = GridWhich.Both };

        Assert.Equal(callback(Base), GridChange.Of(callback).ApplyTo(Base));
        Assert.Equal(callback(new GridStyle()), GridChange.Of(callback).ApplyTo(new GridStyle()));
    }

    /// <summary>The limit, stated: a callback that computes a property from the grid it is given (half the alpha) has no value
    /// of its own to record, so it is recorded as what it does to a plain grid. No such callback exists in the library, its
    /// samples or its documentation; the change an author means by "the theme's grid, but ..." names values.</summary>
    [Fact]
    public void Of_ACallbackThatComputesFromTheGrid_IsRecordedAsWhatItDoesToAPlainGrid()
    {
        var plain = new GridStyle();

        var change = GridChange.Of(g => g with { Alpha = g.Alpha / 2 });

        Assert.Equal(new GridChange(Alpha: plain.Alpha / 2), change);
    }

    [Fact]
    public void AllOf_NamesEveryPropertyOfTheGrid()
    {
        Assert.Equal(new GridChange(true, Colors.Blue, LineStyle.Dotted, 3, 0.5, GridWhich.Both, GridAxis.Y), GridChange.AllOf(Base));
        Assert.Equal(Base, GridChange.AllOf(Base).ApplyTo(new GridStyle { Visible = false }));
    }

    // ── on the wire ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheJsonForm_CarriesEveryPropertyItSets_AndNoOtherOne()
    {
        var change = new GridChange(Visible: false, Alpha: 0.4, Axis: GridAxis.X);

        var dto = GridChangeDto.From(change);

        Assert.Equal(false, dto.Visible);
        Assert.Equal(0.4, dto.Alpha);
        Assert.Equal(GridAxis.X, dto.Axis);
        Assert.Null(dto.Color);
        Assert.Null(dto.LineStyle);
        Assert.Null(dto.LineWidth);
        Assert.Null(dto.Which);
        Assert.Equal(change, dto.ToGridChange());
    }

    [Fact]
    public void TheJsonForm_OfAChangeThatSetsEveryProperty_ReadsBackWhole()
    {
        var change = GridChange.AllOf(Base);

        Assert.Equal(change, GridChangeDto.From(change).ToGridChange());
    }
}
