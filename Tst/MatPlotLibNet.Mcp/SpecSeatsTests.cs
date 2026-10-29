// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A subplot keeps its series and its axes at many seats: the primary pair, the top and right axes, and every Y
/// axis slot of either side (the frozen lists and their overflow lists). A model-authored document passes the reader's
/// checks only if every seat gets them: the chart-type catalog, the text limit and the axis normalisation. The seats are
/// listed here by hand, as the oracle the reader's own seat walk is held against.</summary>
public class SpecSeatsTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);

    private const string Line = """{"type":"line","xData":[1,2,3],"yData":[2,4,3]}""";

    /// <summary>One seat of a subplot: a subplot body that opens every parent the seat needs and ends with
    /// the seat's hole (LINE stands for a valid series, HOLE for the object under test), and the path that names it.</summary>
    /// <param name="Name">The seat as the theory rows show it.</param>
    /// <param name="Subplot">The subplot's JSON members.</param>
    /// <param name="Path">The seat's path inside the subplot, as the refusal names it.</param>
    private sealed record Seat(string Name, string Subplot, string Path);

    private static readonly Seat[] SeriesSeatList =
    [
        new("series", """ "series":[LINE,HOLE] """, "series[1]"),
        new("secondarySeries", """ "series":[LINE],"secondaryYAxis":{},"secondarySeries":[LINE,HOLE] """, "secondarySeries[1]"),
        new("xSecondarySeries", """ "series":[LINE],"secondaryXAxis":{},"xSecondarySeries":[LINE,HOLE] """, "xSecondarySeries[1]"),
        new("extraRightYAxes", """ "series":[LINE],"secondaryYAxis":{},"extraRightYAxes":[{},{"series":[LINE,HOLE]}] """, "extraRightYAxes[1].series[1]"),
        new("leftYAxes", """ "series":[LINE],"leftYAxes":[{},{"series":[LINE,HOLE]}] """, "leftYAxes[1].series[1]"),
        new("rightYAxesOverflow", """ "series":[LINE],"secondaryYAxis":{},"extraRightYAxes":[{}],"rightYAxesOverflow":[{},{"series":[LINE,HOLE]}] """, "rightYAxesOverflow[1].series[1]"),
        new("leftYAxesOverflow", """ "series":[LINE],"leftYAxes":[{}],"leftYAxesOverflow":[{},{"series":[LINE,HOLE]}] """, "leftYAxesOverflow[1].series[1]"),
    ];

    private static readonly Seat[] AxisSeatList =
    [
        new("xAxis", """ "series":[LINE],"xAxis":HOLE """, "xAxis"),
        new("yAxis", """ "series":[LINE],"yAxis":HOLE """, "yAxis"),
        new("zAxis", """ "series":[LINE],"zAxis":HOLE """, "zAxis"),
        new("secondaryYAxis", """ "series":[LINE],"secondaryYAxis":HOLE """, "secondaryYAxis"),
        new("secondaryXAxis", """ "series":[LINE],"secondaryXAxis":HOLE """, "secondaryXAxis"),
        new("extraRightYAxes", """ "series":[LINE],"secondaryYAxis":{},"extraRightYAxes":[{},{"axis":HOLE}] """, "extraRightYAxes[1].axis"),
        new("leftYAxes", """ "series":[LINE],"leftYAxes":[{},{"axis":HOLE}] """, "leftYAxes[1].axis"),
        new("rightYAxesOverflow", """ "series":[LINE],"secondaryYAxis":{},"extraRightYAxes":[{}],"rightYAxesOverflow":[{},{"axis":HOLE}] """, "rightYAxesOverflow[1].axis"),
        new("leftYAxesOverflow", """ "series":[LINE],"leftYAxes":[{}],"leftYAxesOverflow":[{},{"axis":HOLE}] """, "leftYAxesOverflow[1].axis"),
    ];

    /// <summary>The axis each axis seat reads into, by the seat's name: slot <c>n</c> of the right side is the n-th axis out
    /// from the plot, and an entry <c>i</c> of a frozen list is slot <c>i + 1</c>. The overflow lists continue after the one
    /// entry the frozen list holds in the seat's subplot, so overflow entry 1 is slot 3.</summary>
    private static readonly IReadOnlyDictionary<string, Func<Axes, Axis>> AxisOf = new Dictionary<string, Func<Axes, Axis>>
    {
        ["xAxis"] = axes => axes.XAxis,
        ["yAxis"] = axes => axes.YAxis,
        ["zAxis"] = axes => axes.ZAxis,
        ["secondaryYAxis"] = axes => axes.YAxes.Right[0].Axis,
        ["secondaryXAxis"] = axes => axes.SecondaryXAxis!,
        ["extraRightYAxes"] = axes => axes.YAxes.Right[2].Axis,
        ["leftYAxes"] = axes => axes.YAxes.Left[2].Axis,
        ["rightYAxesOverflow"] = axes => axes.YAxes.Right[3].Axis,
        ["leftYAxesOverflow"] = axes => axes.YAxes.Left[3].Axis,
    };

    public static TheoryData<string> SeriesSeats => [.. SeriesSeatList.Select(seat => seat.Name)];

    public static TheoryData<string> AxisSeats => [.. AxisSeatList.Select(seat => seat.Name)];

    private static Seat SeriesSeat(string name) => SeriesSeatList.Single(seat => seat.Name == name);

    private static Seat AxisSeat(string name) => AxisSeatList.Single(seat => seat.Name == name);

    private static string Spec(Seat seat, string hole, bool withoutPrimarySeries = false)
    {
        string subplot = seat.Subplot.Replace("LINE", Line).Replace("HOLE", hole);
        if (withoutPrimarySeries)
        {
            subplot = subplot.Replace($"\"series\":[{Line}],", "");
        }

        return $$"""{"width":400,"height":300,"subPlots":[{{{subplot}}}]}""";
    }

    // ---- the chart-type catalog: every series seat ---------------------------------------------------------------

    [Theory]
    [MemberData(nameof(SeriesSeats))]
    public void AnUnlistedTypeOnEverySeriesSeat_IsRefusedWithItsPath(string name)
    {
        var seat = SeriesSeat(name);

        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec(seat, """{"type":"lien","xData":[1,2],"yData":[1,2]}"""))));

        Assert.Contains("'lien'", refusal.Message);
        Assert.Contains($"$.subPlots[0].{seat.Path}.type", refusal.Message);
        Assert.Contains("'line'", refusal.Message);
    }

    [Theory]
    [MemberData(nameof(SeriesSeats))]
    public void ASeriesWithOnlyHalfItsPairOnEverySeriesSeat_IsRefusedWithItsPath(string name)
    {
        var seat = SeriesSeat(name);

        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec(seat, """{"type":"line","xData":[1,2,3]}"""))));

        Assert.Contains($"$.subPlots[0].{seat.Path}", refusal.Message);
        Assert.Contains("Array lengths must match", refusal.Message);
    }

    [Theory]
    [MemberData(nameof(SeriesSeats))]
    public void AMisspelledEnumOnEverySeriesSeat_IsRefusedWithItsPath(string name)
    {
        var seat = SeriesSeat(name);

        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec(seat, """{"type":"line","xData":[1,2],"yData":[1,2],"labelPlacement":"lineEnnd"}"""))));

        Assert.Contains("'lineEnnd'", refusal.Message);
        Assert.Contains($"$.subPlots[0].{seat.Path}.labelPlacement", refusal.Message);
    }

    [Theory]
    [MemberData(nameof(SeriesSeats))]
    public void AListedTypeOnEverySeriesSeat_IsRead(string name)
    {
        var seat = SeriesSeat(name);

        var figure = Reader.Read(new ChartSpec(Spec(seat, Line)));

        Assert.Single(figure.SubPlots);
    }

    /// <summary>Counting what a subplot draws is the same walk: a spec whose only series stands on a Y axis slot, or on the
    /// top axis, draws that series, so it is not "nothing to draw".</summary>
    [Theory]
    [MemberData(nameof(SeriesSeats))]
    public void ASpecWhoseOnlySeriesStandsOnOneSeat_IsNotRefusedAsNothingToDraw(string name)
    {
        var seat = SeriesSeat(name);

        var figure = Reader.Read(new ChartSpec(Spec(seat, Line, withoutPrimarySeries: true)));

        Assert.Single(figure.SubPlots);
    }

    // ---- the text limit: every seat ------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(SeriesSeats))]
    public void ATextPastTheLimitOnEverySeriesSeat_IsRefused(string name)
    {
        var seat = SeriesSeat(name);
        string label = new('A', RenderLimits.Default.MaxTextLength + 1);

        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec(seat, $$"""{"type":"line","xData":[1,2],"yData":[1,2],"label":"{{label}}"}"""))));

        Assert.Contains("'label'", refusal.Message);
        Assert.Contains($"at $.subPlots[0].{seat.Path} ", refusal.Message);
        Assert.Contains(RenderLimits.Default.MaxTextLength.ToString(), refusal.Message);
    }

    [Theory]
    [MemberData(nameof(AxisSeats))]
    public void ATextPastTheLimitOnEverySeat_IsRefused(string name)
    {
        var seat = AxisSeat(name);
        string label = new('A', RenderLimits.Default.MaxTextLength + 1);

        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec(seat, $$"""{"label":"{{label}}"}"""))));

        Assert.Contains("'label'", refusal.Message);
        Assert.Contains($"at $.subPlots[0].{seat.Path} ", refusal.Message);
        Assert.Contains(RenderLimits.Default.MaxTextLength.ToString(), refusal.Message);
    }

    // ---- the axis normalisation: every axis seat -----------------------------------------------------------------

    [Theory]
    [MemberData(nameof(AxisSeats))]
    public void AnAxisWithABadScaleOnEverySeat_IsRefusedWithItsPath(string name)
    {
        var seat = AxisSeat(name);

        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec(Spec(seat, """{"scale":"lgo"}"""))));

        Assert.Contains("'lgo'", refusal.Message);
        Assert.Contains($"$.subPlots[0].{seat.Path}.scale", refusal.Message);
        Assert.Contains("Log", refusal.Message);
    }

    /// <summary>The serializer reads a described value (a tick locator) by its kind only when <c>type</c> stands first, and
    /// reads a tick direction by its exact name or not at all. The reader moves the kind first and spells both as the
    /// serializer reads them, so an axis written the way a model writes it reads the way the primary's does.</summary>
    [Theory]
    [MemberData(nameof(AxisSeats))]
    public void AnAxisOnEverySlotSeat_IsNormalised(string name)
    {
        var seat = AxisSeat(name);

        var figure = Reader.Read(new ChartSpec(Spec(seat,
            """{"scale":"LOG","tickLocator":{"base":5,"type":"MULTIPLE"},"majorTicks":{"direction":"IN"}}""")));

        var axis = AxisOf[name](figure.SubPlots[0]);
        Assert.Equal(AxisScale.Log, axis.Scale);
        Assert.IsType<MultipleLocator>(axis.TickLocator);
        Assert.Equal(TickDirection.In, axis.MajorTicks.Direction);
    }

    // ---- an entry that is not an object --------------------------------------------------------------------------

    [Theory]
    [InlineData(""" "series":[null] """, "series[0]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"leftYAxes":[null] """, "leftYAxes[0]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"leftYAxes":[{"series":[null]}] """, "leftYAxes[0].series[0]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"secondaryYAxis":{},"extraRightYAxes":[{},null] """, "extraRightYAxes[1]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"leftYAxes":[{}],"leftYAxesOverflow":[null] """, "leftYAxesOverflow[0]")]
    public void ANullEntryOnASeatList_IsRefusedWithItsPath(string subplot, string path)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec($$"""{"width":400,"height":300,"subPlots":[{{{subplot}}}]}""")));

        Assert.Contains($"$.subPlots[0].{path}", refusal.Message);
        Assert.Contains("null", refusal.Message);
    }

    /// <summary>The same refusal for every OTHER list of objects in a spec (subplots, insets, annotations, reference
    /// lines, spans, trendlines, levels, breaks): a <c>null</c> entry used to surface as a bare
    /// <c>NullReferenceException</c> from the strict pass or from core's restore.</summary>
    [Theory]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"annotations":[null] """, "$.subPlots[0].annotations[0]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"referenceLines":[null] """, "$.subPlots[0].referenceLines[0]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"spans":[null] """, "$.subPlots[0].spans[0]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"trendlines":[null] """, "$.subPlots[0].trendlines[0]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"horizontalLevels":[null] """, "$.subPlots[0].horizontalLevels[0]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"xBreaks":[null] """, "$.subPlots[0].xBreaks[0]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"yBreaks":[null] """, "$.subPlots[0].yBreaks[0]")]
    [InlineData(""" "series":[{"type":"line","xData":[1],"yData":[1]}],"insets":[null] """, "$.subPlots[0].insets[0]")]
    public void ANullEntryOnAnyOtherObjectList_IsRefusedWithItsPath(string subplot, string path)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec($$"""{"width":400,"height":300,"subPlots":[{{{subplot}}}]}""")));

        Assert.Contains(path, refusal.Message);
        Assert.Contains("null", refusal.Message);
    }

    [Fact]
    public void ANullSubplot_IsRefusedWithItsPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Reader.Read(new ChartSpec("""{"width":400,"height":300,"subPlots":[null]}""")));

        Assert.Contains("$.subPlots[0]", refusal.Message);
        Assert.Contains("null", refusal.Message);
    }

    // ---- the overflow seats are part of the spec ------------------------------------------------------------------

    private const string ManyAxes = """
        {
          "width": 600, "height": 400,
          "subPlots": [{
            "series": [{"type":"line","label":"P","xData":[1,2,3],"yData":[1,2,3]}],
            "yAxis": {"label": "primary"},
            "secondaryYAxis": {"label": "R0"},
            "secondarySeries": [{"type":"line","label":"R0line","xData":[1,2,3],"yData":[10,20,30]}],
            "extraRightYAxes": [{"axis": {"label": "R1"}, "series": [{"type":"line","label":"R1line","xData":[1,2,3],"yData":[4,5,6]}]}],
            "rightYAxesOverflow": [
              {"axis": {"label": "R2"}, "series": [{"type":"line","label":"R2line","xData":[1,2,3],"yData":[7,8,9]}]},
              {"axis": {"label": "R3"}, "series": [{"type":"line","label":"R3line","xData":[1,2,3],"yData":[1,5,2]}]}
            ],
            "leftYAxes": [{"axis": {"label": "L1"}, "series": [{"type":"line","label":"L1line","xData":[1,2,3],"yData":[3,2,1]}]}],
            "leftYAxesOverflow": [
              {"axis": {"label": "L2"}, "series": [{"type":"line","label":"L2line","xData":[1,2,3],"yData":[2,2,2]}]}
            ],
            "yAxisTitleGap": 9
          }]
        }
        """;

    [Fact]
    public void AnAxisSpecThatNamesTheOverflowSeats_ReadsThem()
    {
        var axes = Reader.Read(new ChartSpec(ManyAxes)).SubPlots[0];

        Assert.Equal(["R0", "R1", "R2", "R3"], axes.YAxes.Right.Select(slot => slot.Axis.Label));
        Assert.Equal(["primary", "L1", "L2"], axes.YAxes.Left.Select(slot => slot.Axis.Label));
        Assert.Equal(["R0line", "R1line", "R2line", "R3line"], axes.YAxes.Right.Select(slot => Assert.Single(slot.Series).Label));
        Assert.Equal(["P", "L1line", "L2line"], axes.YAxes.Left.Select(slot => Assert.Single(slot.Series).Label));
        Assert.Equal(9, axes.YAxes.TitleGap);
    }

    /// <summary>The figure a caller builds with the verbs and saves reads back through the reader whole: every slot in its
    /// place, none merged onto another.</summary>
    [Fact]
    public void AFigureOfManyYAxes_SavedAsJson_ReadsBackThroughTheReaderWithEverySlot()
    {
        string json = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [1.0, 2, 3], line => line.Label = "P")
                .WithAddRightYAxis(slot => slot.SetYLabel("R0").Plot([1.0, 2, 3], [4.0, 5, 6]))
                .WithAddRightYAxis(slot => slot.SetYLabel("R1").Plot([1.0, 2, 3], [7.0, 8, 9]))
                .WithAddRightYAxis(slot => slot.SetYLabel("R2").Plot([1.0, 2, 3], [1.0, 5, 2]))
                .WithAddRightYAxis(slot => slot.SetYLabel("R3").Plot([1.0, 2, 3], [2.0, 2, 2]))
                .WithAddLeftYAxis(slot => slot.SetYLabel("L1").Plot([1.0, 2, 3], [3.0, 2, 1]))
                .WithAddLeftYAxis(slot => slot.SetYLabel("L2").Plot([1.0, 2, 3], [5.0, 1, 5]))
                .WithAddLeftYAxis(slot => slot.SetYLabel("L3").Plot([1.0, 2, 3], [9.0, 8, 9]))
                .WithYAxisTitleGap(7))
            .ToJson();

        var axes = Reader.Read(new ChartSpec(json)).SubPlots[0];

        Assert.Equal(["R0", "R1", "R2", "R3"], axes.YAxes.Right.Select(slot => slot.Axis.Label));
        Assert.Equal([null, "L1", "L2", "L3"], axes.YAxes.Left.Select(slot => slot.Axis.Label));
        Assert.All(axes.YAxes.Right.Concat(axes.YAxes.Left), slot => Assert.Single(slot.Series));
        Assert.Equal(7, axes.YAxes.TitleGap);
    }
}
