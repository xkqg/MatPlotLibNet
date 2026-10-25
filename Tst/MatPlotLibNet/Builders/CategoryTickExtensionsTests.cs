// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The categories of an axis written as its ticks: one tick on the centre of each category's slot, reading that
/// category's name. The locator and the formatter are made as one pair, so their positions and names cannot disagree, and
/// both are built-ins, so the pair survives JSON.</summary>
public class CategoryTickExtensionsTests
{
    private static readonly string[] Names = ["first", "second", "third"];

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static string[] TicksAsRead(Axis axis) =>
        [.. axis.TickLocator!.Locate(0, Names.Length).Select(axis.TickFormatter!.Format)];

    /// <summary>Along X a tick stands on the centre of each category's slot, the first category at the left.</summary>
    [Fact]
    public void AlongX_EachCategoryIsNamedOnItsSlotCentre_FromTheLeft()
    {
        var axis = AxesOf(ax => ax.SetXCategoryTicks(Names)).XAxis;

        Assert.IsType<FixedLocator>(axis.TickLocator);
        Assert.IsType<CategoryFormatter>(axis.TickFormatter);
        Assert.Equal(Enumerable.Range(0, 3).Select(k => CategorySlot.Of(k).Centre), axis.TickLocator!.Locate(0, 3));
        Assert.Equal(Names, TicksAsRead(axis));
    }

    /// <summary>Along Y the first category is at the bottom, where the axis starts counting.</summary>
    [Fact]
    public void AlongY_TheFirstCategoryIsAtTheBottom()
    {
        var axis = AxesOf(ax => ax.SetYCategoryTicks(Names)).YAxis;

        Assert.Equal([0.5, 1.5, 2.5], axis.TickLocator!.Locate(0, 3));
        Assert.Equal(Names, TicksAsRead(axis));
    }

    /// <summary>Drawn top-down (the rows of a heatmap, row 0 at the top), the first category is named at the top tick and
    /// the last at the bottom one.</summary>
    [Fact]
    public void AlongY_DrawnTopDown_TheFirstCategoryIsAtTheTop()
    {
        var axis = AxesOf(ax => ax.SetYCategoryTicks(Names, topDown: true)).YAxis;

        Assert.Equal([0.5, 1.5, 2.5], axis.TickLocator!.Locate(0, 3));
        Assert.Equal(["third", "second", "first"], TicksAsRead(axis));
    }

    /// <summary>The pair is made of the built-in locator and formatter, so a figure keeps its named ticks through JSON.</summary>
    [Fact]
    public void TheNamedTicks_SurviveJson()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXCategoryTicks(Names).SetYCategoryTicks(Names, topDown: true))
            .Build();

        var back = ChartSerializerDefault.Current.FromJson(ChartSerializerDefault.Current.ToJson(figure)).SubPlots[0];

        Assert.Equal(Names, TicksAsRead(back.XAxis));
        Assert.Equal(["third", "second", "first"], TicksAsRead(back.YAxis));
    }

    /// <summary>The pair itself, the one the axes renderer names the categories of bars that lie down with: a tick on each
    /// slot centre, reading the names from the bottom up, or from the top down when asked.</summary>
    [Theory]
    [InlineData(false, new[] { "first", "second", "third" })]
    [InlineData(true, new[] { "third", "second", "first" })]
    public void ThePair_NamesEachSlotCentre(bool topDown, string[] fromTheBottom)
    {
        var ticks = Names.AsCategoryTicks(topDown);

        var positions = ticks.Locator.Locate(double.NegativeInfinity, double.PositiveInfinity);
        Assert.Equal([0.5, 1.5, 2.5], positions);
        Assert.Equal(fromTheBottom, positions.Select(ticks.Formatter.Format));
        Assert.Equal("names", Assert.Throws<ArgumentNullException>(() => ((string[])null!).AsCategoryTicks()).ParamName);
    }

    /// <summary>No names at all is refused by the parameter's name, on either axis.</summary>
    [Fact]
    public void NoNames_AreRefusedByName()
    {
        var alongX = Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.SetXCategoryTicks((string[])null!)));
        var alongY = Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.SetYCategoryTicks(null!)));

        Assert.Equal("names", alongX.ParamName);
        Assert.Equal("names", alongY.ParamName);
    }

    // ---- some slots named -------------------------------------------------------------------------------------------

    /// <summary>Only some slots named (the months over a calendar's week columns): a tick on the centre of each named slot,
    /// reading its name, from the left whatever order the names come in, and no tick on a slot without a name.</summary>
    [Fact]
    public void AlongX_OnlyTheNamedSlots_CarryATick()
    {
        var bySlot = new Dictionary<int, string> { [8] = "Mar", [0] = "Jan", [4] = "Feb" };

        var axis = AxesOf(ax => ax.SetXCategoryTicks(bySlot)).XAxis;

        var positions = axis.TickLocator!.Locate(0, 10);
        Assert.Equal([CategorySlot.Of(0).Centre, CategorySlot.Of(4).Centre, CategorySlot.Of(8).Centre], positions);
        Assert.Equal(["Jan", "Feb", "Mar"], positions.Select(axis.TickFormatter!.Format));
        Assert.Equal(string.Empty, axis.TickFormatter.Format(CategorySlot.Of(2).Centre));
    }

    /// <summary>The ticks on some slots are the same built-in pair, so they survive JSON.</summary>
    [Fact]
    public void TheNamedSlots_SurviveJson()
    {
        var bySlot = new Dictionary<int, string> { [0] = "Jan", [4] = "Feb" };
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetXCategoryTicks(bySlot)).Build();

        var back = ChartSerializerDefault.Current.FromJson(ChartSerializerDefault.Current.ToJson(figure)).SubPlots[0].XAxis;

        var positions = back.TickLocator!.Locate(0, 10);
        Assert.Equal([0.5, 4.5], positions);
        Assert.Equal(["Jan", "Feb"], positions.Select(back.TickFormatter!.Format));
    }

    /// <summary>No slot named is no tick at all.</summary>
    [Fact]
    public void NoNamedSlot_IsNoTick()
    {
        var ticks = new Dictionary<int, string>().AsCategoryTicks();

        Assert.Empty(ticks.Locator.Locate(double.NegativeInfinity, double.PositiveInfinity));
    }

    /// <summary>No map of names at all is refused by the parameter's name.</summary>
    [Fact]
    public void NoNamedSlots_AreRefusedByName()
    {
        var pair = Assert.Throws<ArgumentNullException>(() => ((IReadOnlyDictionary<int, string>)null!).AsCategoryTicks());
        var alongX = Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.SetXCategoryTicks((IReadOnlyDictionary<int, string>)null!)));

        Assert.Equal("namesBySlot", pair.ParamName);
        Assert.Equal("namesBySlot", alongX.ParamName);
    }

    // ---- names at positions of their own ------------------------------------------------------------------------------

    private static readonly double[] Centres = [12.5, 47.25, 88.0];

    /// <summary>Names at positions of their own (the centres of a Marimekko chart's columns, whose widths are shares): a tick
    /// on each position, reading its name, and nothing between them, though two of the names would share no slot.</summary>
    [Fact]
    public void AlongX_AtPositions_EachNameIsReadAtItsPosition()
    {
        var axis = AxesOf(ax => ax.SetXCategoryTicks(Names, Centres)).XAxis;

        Assert.IsType<FixedLocator>(axis.TickLocator);
        Assert.IsType<CategoryFormatter>(axis.TickFormatter);
        var positions = axis.TickLocator!.Locate(0, 100);
        Assert.Equal(Centres, positions);
        Assert.Equal(Names, positions.Select(axis.TickFormatter!.Format));
        Assert.Equal(string.Empty, axis.TickFormatter.Format(CategorySlot.Of(1).Centre));
    }

    /// <summary>The pair at positions is the same built-in locator and formatter, so it survives JSON.</summary>
    [Fact]
    public void TheNamesAtPositions_SurviveJson()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetXCategoryTicks(Names, Centres)).Build();

        var back = ChartSerializerDefault.Current.FromJson(ChartSerializerDefault.Current.ToJson(figure)).SubPlots[0].XAxis;

        var positions = back.TickLocator!.Locate(0, 100);
        Assert.Equal(Centres, positions);
        Assert.Equal(Names, positions.Select(back.TickFormatter!.Format));
    }

    /// <summary>Names and positions that do not pair up, or are not there, are refused with the parameter named, before the
    /// axis is touched.</summary>
    [Fact]
    public void NamesAndPositionsThatDoNotPairUp_AreRefusedByName()
    {
        Assert.Equal("names", Assert.Throws<ArgumentNullException>(() => ((string[])null!).AsCategoryTicks(Centres)).ParamName);
        Assert.Equal("positions", Assert.Throws<ArgumentNullException>(() => Names.AsCategoryTicks((double[])null!)).ParamName);
        Assert.Equal("positions", Assert.Throws<ArgumentException>(() => Names.AsCategoryTicks([1.0, 2.0])).ParamName);

        var axes = AxesOf(ax => Assert.Equal("positions",
            Assert.Throws<ArgumentException>(() => ax.SetXCategoryTicks(Names, [1.0])).ParamName));
        Assert.Null(axes.XAxis.TickLocator);
        Assert.Null(axes.XAxis.TickFormatter);
    }
}
