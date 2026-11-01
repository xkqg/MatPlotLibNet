// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The figure JSON carries the link of an axis (<see cref="Axis.Link"/>, matplotlib's <c>secondary_xaxis</c> and
/// <c>secondary_yaxis</c>): the <c>link</c> field of an axis, one record per named transform told apart by its <c>type</c>
/// (<c>linear</c>, <c>reciprocal</c>, <c>logarithm</c>, <c>power</c>), written with what the transform was made with. A figure with
/// a linked top or side axis draws the same picture after a round trip; a figure without a link writes no field and so keeps the
/// bytes it wrote before the link reached the JSON. A transform made from a pair of functions (<see cref="AxisTransform.Of"/>) is
/// code, which a JSON document cannot carry: it is left out, and the axis reads back unlinked. A kind this build does not know is
/// skipped with a diagnostic, the figure still loads, and a number the transform refuses is refused when it is read.
/// <para>Tagged into the <c>ChartSerializerGlobalState</c> collection: the figures read back through the process-global series
/// registry, which <see cref="SeriesRegistryTests"/> briefly rewrites, and the unknown-kind rows listen to the process-global
/// <see cref="ChartDiagnostics"/> channel.</para></summary>
[Collection("ChartSerializerGlobalState")]
public class LinkedAxisSerializationTests
{
    private static readonly ChartSerializer Serializer = new();

    private static readonly double[] X = [1, 2, 3, 4, 5];
    private static readonly double[] Y = [2, 4, 3, 5, 4];
    private static readonly double[] Other = [10, 30, 20, 40, 25];

    private static Figure RoundTrip(Figure figure) => Serializer.FromJson(Serializer.ToJson(figure));

    /// <summary>The four transforms the JSON names, each made with a value that is not what its factory defaults to, over a primary
    /// range (1..5) every one of them can place.</summary>
    private static readonly IReadOnlyDictionary<string, AxisTransform> Named = new Dictionary<string, AxisTransform>(StringComparer.Ordinal)
    {
        ["linear"] = AxisTransform.Linear(1.8, 32),
        ["reciprocal"] = AxisTransform.Reciprocal(2),
        ["logarithm"] = AxisTransform.Logarithm(2),
        ["power"] = AxisTransform.Power(2),
    };

    /// <summary>The kinds the JSON names a transform by.</summary>
    public static TheoryData<string> Kinds { get; } = new(Named.Keys);

    /// <summary>The seats a link can stand at: the top X axis, and the Y axes stacked past the primary on either side, one for each
    /// place the JSON keeps a slot (right slot 0, the one slot the frozen list holds, a slot past it, on both sides).</summary>
    private static readonly string[] Seats = ["top", "right0", "right1", "right2", "left1", "left2"];

    /// <summary>Every kind at every seat.</summary>
    public static TheoryData<string, string> KindsAtSeats { get; } = new(Named.Keys.SelectMany(kind => Seats.Select(seat => (kind, seat))));

    /// <summary>The seats.</summary>
    public static TheoryData<string> SeatNames { get; } = new(Seats);

    /// <summary>A plot with one line, and at <paramref name="seat"/> an axis with the label "linked" and no series, linked through
    /// <paramref name="link"/> when there is one. The axes the seat stands behind on its side hold a line of their own, so each seat
    /// the JSON keeps a slot in is the slot that is linked.</summary>
    private static Figure Draw(string seat, AxisTransform? link) => Plt.Create().WithSize(640, 480).AddSubPlot(1, 1, 1, ax =>
    {
        ax.Plot(X, Y);
        void Linked(SecondaryXAxisBuilder top) => top.SetXLabel("linked");
        void LinkedSlot(YAxisSlotBuilder slot) => slot.SetYLabel("linked");
        switch (seat)
        {
            case "top":
                ax.WithSecondaryXAxis(top => { Linked(top); if (link is not null) top.Link(link); });
                break;
            case "right0": case "right1": case "right2":
                for (int i = 0; i <= seat[^1] - '0'; i++)
                {
                    bool linkedOne = i == seat[^1] - '0';
                    ax.WithAddRightYAxis(slot => { if (linkedOne) { LinkedSlot(slot); if (link is not null) slot.Link(link); } else slot.Plot(X, Other); });
                }

                break;
            case "left1": case "left2":
                for (int i = 1; i <= seat[^1] - '0'; i++)
                {
                    bool linkedOne = i == seat[^1] - '0';
                    ax.WithAddLeftYAxis(slot => { if (linkedOne) { LinkedSlot(slot); if (link is not null) slot.Link(link); } else slot.Plot(X, Other); });
                }

                break;
            default:
                throw new ArgumentException($"No seat is called '{seat}'.", nameof(seat));
        }
    }).Build();

    /// <summary>The axis at <paramref name="seat"/> of the first subplot of <paramref name="figure"/>.</summary>
    private static Axis AxisAt(Figure figure, string seat)
    {
        var axes = figure.SubPlots[0];
        return seat switch
        {
            "top" => axes.SecondaryXAxis!,
            _ when seat.StartsWith("right", StringComparison.Ordinal) => axes.YAxes.Right[seat[^1] - '0'].Axis,
            _ => axes.YAxes.Left[seat[^1] - '0'].Axis,
        };
    }

    private static JsonElement WrittenAxis(Figure figure, string seat)
    {
        using var json = JsonDocument.Parse(Serializer.ToJson(figure));
        var subplot = json.RootElement.GetProperty("subPlots")[0];
        return seat switch
        {
            "top" => subplot.GetProperty("secondaryXAxis").Clone(),
            "right0" => subplot.GetProperty("secondaryYAxis").Clone(),
            "right1" => subplot.GetProperty("extraRightYAxes")[0].GetProperty("axis").Clone(),
            "right2" => subplot.GetProperty("rightYAxesOverflow")[0].GetProperty("axis").Clone(),
            "left1" => subplot.GetProperty("leftYAxes")[0].GetProperty("axis").Clone(),
            _ => subplot.GetProperty("leftYAxesOverflow")[0].GetProperty("axis").Clone(),
        };
    }

    private static List<ChartDiagnostic> Announced(Action act)
    {
        var seen = new List<ChartDiagnostic>();
        void Collect(ChartDiagnostic d) => seen.Add(d);
        ChartDiagnostics.Emitted += Collect;
        try
        {
            act();
        }
        finally
        {
            ChartDiagnostics.Emitted -= Collect;
        }

        return [.. seen.Where(d => d.Message.Contains("link", StringComparison.OrdinalIgnoreCase))];
    }

    // ---- a named link, at every seat ------------------------------------------------------------------------------

    /// <summary>Each named transform on the top axis and on a Y axis of either side draws the same picture after a round trip, and
    /// maps the same values both ways. The picture is not the one without the link: the linked axis really is drawn.</summary>
    [Theory]
    [MemberData(nameof(KindsAtSeats))]
    public void ANamedLink_DrawsTheSameAfterARoundTrip_AndMapsTheSameValues(string kind, string seat)
    {
        var figure = Draw(seat, Named[kind]);
        string drawn = figure.ToSvg();

        var read = RoundTrip(figure);

        Assert.NotNull(AxisAt(read, seat).Link);
        Assert.True(DrawnSeriesFixtures.SameValue(Named[kind], AxisAt(read, seat).Link),
            $"{kind} at {seat}: set to {DrawnSeriesFixtures.Show(Named[kind])}, read back as {DrawnSeriesFixtures.Show(AxisAt(read, seat).Link)}.");
        Assert.NotEqual(Draw(seat, link: null).ToSvg(), drawn);
        Assert.Equal(drawn, read.ToSvg());
    }

    /// <summary>The link stands on its own axis: the other axes of the subplot read back without one.</summary>
    [Theory]
    [MemberData(nameof(SeatNames))]
    public void ALinkAtOneSeat_IsReadBackAtThatSeatOnly(string seat)
    {
        var read = RoundTrip(Draw(seat, Named["linear"]));
        var axes = read.SubPlots[0];

        var others = new List<Axis> { axes.XAxis, axes.YAxis };
        others.AddRange(axes.YAxes.Right.Select(slot => slot.Axis));
        others.AddRange(axes.YAxes.Left.Select(slot => slot.Axis));
        if (axes.SecondaryXAxis is not null)
        {
            others.Add(axes.SecondaryXAxis);
        }

        Assert.Single(others, axis => axis.Link is not null);
        Assert.NotNull(AxisAt(read, seat).Link);
    }

    // ---- what is written ------------------------------------------------------------------------------------------

    /// <summary>A named link is written as an object whose first field is its kind, then what it was made with, under the names a
    /// reader outside .NET sees.</summary>
    [Theory]
    [InlineData("linear", new[] { "type", "scale", "offset" })]
    [InlineData("reciprocal", new[] { "type", "numerator" })]
    [InlineData("logarithm", new[] { "type", "base" })]
    [InlineData("power", new[] { "type", "exponent" })]
    public void ANamedLink_IsWrittenAsItsKindThenWhatItWasMadeWith(string kind, string[] fields)
    {
        var link = WrittenAxis(Draw("top", Named[kind]), "top").GetProperty("link");

        Assert.Equal(fields, link.EnumerateObject().Select(p => p.Name));
        Assert.Equal(kind, link.GetProperty("type").GetString());
    }

    /// <summary>What each transform was made with travels as the number it was given.</summary>
    [Fact]
    public void TheNumbersOfANamedLink_AreTheOnesItWasMadeWith()
    {
        double Number(string kind, string field) => WrittenAxis(Draw("right0", Named[kind]), "right0").GetProperty("link").GetProperty(field).GetDouble();

        Assert.Equal(1.8, Number("linear", "scale"));
        Assert.Equal(32, Number("linear", "offset"));
        Assert.Equal(2, Number("reciprocal", "numerator"));
        Assert.Equal(2, Number("logarithm", "base"));
        Assert.Equal(2, Number("power", "exponent"));
    }

    /// <summary>The same link written on the side of the container each seat stands at is found where the reader looks for it.</summary>
    [Theory]
    [MemberData(nameof(SeatNames))]
    public void ALinkIsWrittenOnTheAxisOfItsSeat(string seat)
    {
        var axis = WrittenAxis(Draw(seat, Named["linear"]), seat);

        Assert.Equal("linear", axis.GetProperty("link").GetProperty("type").GetString());
        Assert.Equal("linked", axis.GetProperty("label").GetString());
    }

    /// <summary>A figure that links nothing writes no <c>link</c> field, so every figure written before the link reached the JSON
    /// keeps its bytes; the same holds for one with a top axis and Y axes past the primary.</summary>
    [Theory]
    [MemberData(nameof(SeatNames))]
    public void AFigureWithNoLink_WritesNoLinkField(string seat)
    {
        string json = Serializer.ToJson(Draw(seat, link: null));

        Assert.DoesNotContain("\"link\"", json, StringComparison.Ordinal);
    }

    /// <summary>An axis whose link is unset writes none, whatever else it holds.</summary>
    [Fact]
    public void AnAxisThatHoldsSeriesAndNoLink_WritesNoLinkField()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y)
            .WithSecondaryXAxis(top => top.PlotXSecondary(X, Other))
            .WithAddRightYAxis(right => right.Plot(X, Other))).Build();

        Assert.DoesNotContain("\"link\"", Serializer.ToJson(figure), StringComparison.Ordinal);
    }

    // ---- a transform of your own is code ---------------------------------------------------------------------------

    /// <summary>A transform made from a pair of functions is left out of the JSON, as every value of the caller's own is (a tick
    /// locator written outside the library, a normaliser). The axis reads back unlinked, and the picture it then draws is pinned
    /// here: the picture of the same axis with no link at all, which is not the picture the link drew.</summary>
    [Theory]
    [MemberData(nameof(SeatNames))]
    public void ATransformMadeOfFunctions_IsLeftOutOfTheJson_AndTheAxisReadsBackUnlinked(string seat)
    {
        var ofFunctions = AxisTransform.Of(celsius => celsius * 1.8 + 32, fahrenheit => (fahrenheit - 32) / 1.8);
        var figure = Draw(seat, ofFunctions);
        string drawn = figure.ToSvg();
        string json = Serializer.ToJson(figure);

        var read = Serializer.FromJson(json);

        Assert.DoesNotContain("\"link\"", json, StringComparison.Ordinal);
        Assert.Null(AxisAt(read, seat).Link);
        Assert.Equal(Draw(seat, link: null).ToSvg(), read.ToSvg());
        Assert.NotEqual(drawn, read.ToSvg());
    }

    // ---- a kind or a number this build cannot read -----------------------------------------------------------------

    /// <summary>A kind this build does not know (JSON from a later release, or a typo), or a link with no kind at all, is skipped and
    /// announced; the axis reads back unlinked and the figure still loads with everything else on it.</summary>
    [Theory]
    [InlineData("""{"type":"cube","exponent":3}""", "secondaryXAxis", "top")]
    [InlineData("""{"scale":1.8,"offset":32}""", "secondaryXAxis", "top")]
    [InlineData("""{"type":"cube","exponent":3}""", "secondaryYAxis", "right0")]
    [InlineData("""{"type":"__tropical__"}""", "secondaryYAxis", "right0")]
    public void AKindOfLinkThisBuildDoesNotKnow_IsSkippedWithADiagnostic_AndTheFigureStillLoads(string link, string axis, string seat)
    {
        string json = $$"""
        {"width":400,"height":300,"subPlots":[{
          "series":[{"type":"line","xData":[1,2,3],"yData":[1,2,3]}],
          "{{axis}}":{"label":"kept","link":{{link}}}
        }]}
        """;

        Figure? read = null;
        var seen = Announced(() => read = Serializer.FromJson(json));

        Assert.Single(read!.SubPlots[0].Series);
        Assert.Equal("kept", AxisAt(read, seat).Label);
        Assert.Null(AxisAt(read, seat).Link);
        var announcement = Assert.Single(seen);
        Assert.Equal("ChartSerializer", announcement.Source);
        Assert.Contains("axis link", announcement.Message, StringComparison.Ordinal);
    }

    /// <summary>A number the transform cannot map with is refused when the JSON is read, by the factory that refuses it when it is
    /// called, and naming the same parameter; reading a link never throws anything else.</summary>
    [Theory]
    [InlineData("""{"type":"linear","scale":0}""", "scale")]
    [InlineData("""{"type":"linear","scale":"NaN","offset":1}""", "scale")]
    [InlineData("""{"type":"linear"}""", "scale")]
    [InlineData("""{"type":"linear","scale":2,"offset":"Infinity"}""", "offset")]
    [InlineData("""{"type":"reciprocal","numerator":0}""", "numerator")]
    [InlineData("""{"type":"logarithm","base":1}""", "base")]
    [InlineData("""{"type":"logarithm","base":-2}""", "base")]
    [InlineData("""{"type":"power","exponent":0}""", "exponent")]
    [InlineData("""{"type":"power"}""", "exponent")]
    public void ANumberTheTransformRefuses_IsRefusedWhenItIsRead_ByTheNameOfTheParameter(string link, string parameter)
    {
        string json = $$$"""{"width":400,"height":300,"subPlots":[{"secondaryXAxis":{"link":{{{link}}}}}]}""";

        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Serializer.FromJson(json));

        Assert.Equal(parameter, refusal.ParamName);
    }

    /// <summary>A description written by hand (an agent's spec, an older or a newer writer) may leave out what has a default: it then
    /// reads as the transform made with it, the same as the factory's own.</summary>
    [Theory]
    [InlineData("""{"type":"linear","scale":2}""", "linear")]
    [InlineData("""{"type":"reciprocal"}""", "reciprocal")]
    [InlineData("""{"type":"logarithm"}""", "logarithm")]
    public void ADescriptionWithItsDefaultsLeftOut_ReadsAsTheTransformMadeWithThem(string link, string kind)
    {
        string json = $$$"""{"width":400,"height":300,"subPlots":[{"secondaryXAxis":{"link":{{{link}}}}}]}""";
        AxisTransform expected = kind switch
        {
            "linear" => AxisTransform.Linear(2),
            "reciprocal" => AxisTransform.Reciprocal(),
            _ => AxisTransform.Logarithm(),
        };

        var read = Serializer.FromJson(json).SubPlots[0].SecondaryXAxis!.Link;

        Assert.True(DrawnSeriesFixtures.SameValue(expected, read),
            $"{kind}: read as {DrawnSeriesFixtures.Show(read)}, expected {DrawnSeriesFixtures.Show(expected)}.");
    }

    // ---- the drawn-property contract --------------------------------------------------------------------------------

    /// <summary>The drawn-property fixtures hold the link on every axis seat now: the list of properties no seat holds is empty, and
    /// every part of type <see cref="Axis"/> has <c>Link</c> among the properties the contract proves come back from JSON.</summary>
    [Fact]
    public void EveryAxisSeatOfTheDrawnPropertyFixtures_ProvesTheLinkIsCarried()
    {
        var seats = DrawnAxesFixtures.Parts.Where(part => part.Type == typeof(Axis)).ToList();

        Assert.Empty(DrawnAxesFixtures.AxisNotCarried);
        Assert.NotEmpty(seats);
        Assert.All(seats, seat => Assert.Contains(nameof(Axis.Link), DrawnAxesFixtures.DrawnPaths(seat)));
    }

    /// <summary>A link on an axis that holds series (which only the builders refuse; a property set directly, or a hand-made document,
    /// can say as much) is read as written and draws, after the round trip, the picture it drew before: drawing a linked axis never
    /// throws.</summary>
    [Fact]
    public void ALinkOnAnAxisThatHoldsSeries_IsKept_AndDrawsTheSameAfterARoundTrip_WithoutThrowing()
    {
        var figure = Plt.Create().WithSize(640, 480).AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y)
            .WithSecondaryXAxis(top => top.PlotXSecondary(X, Other))
            .WithAddRightYAxis(right => right.Plot(X, Other))).Build();
        figure.SubPlots[0].SecondaryXAxis!.Link = Named["linear"];
        figure.SubPlots[0].YAxes.Right[0].Axis.Link = Named["power"];
        string drawn = figure.ToSvg();

        var read = RoundTrip(figure);

        Assert.True(DrawnSeriesFixtures.SameValue(Named["linear"], read.SubPlots[0].SecondaryXAxis!.Link));
        Assert.True(DrawnSeriesFixtures.SameValue(Named["power"], read.SubPlots[0].YAxes.Right[0].Axis.Link));
        Assert.Single(read.SubPlots[0].XSecondarySeries);
        Assert.Single(read.SubPlots[0].YAxes.Right[0].Series);
        Assert.Equal(drawn, read.ToSvg());
    }

    /// <summary>The link of every axis seat the fixtures build comes back as a transform that maps the same values, on the primary
    /// axes and an inset's alike (each axes is read back by the same path).</summary>
    [Fact]
    public void TheLinkOfEverySeatOfTheFixtureFigure_ComesBackFromJson()
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);

        var read = RoundTrip(figure);

        foreach (var seat in DrawnAxesFixtures.Parts.Where(part => part.Type == typeof(Axis)))
        {
            object? written = DrawnAxesFixtures.ValueOf(figure, seat.Name, nameof(Axis.Link));
            object? back = DrawnAxesFixtures.ValueOf(read, seat.Name, nameof(Axis.Link));
            Assert.NotNull(written);
            Assert.True(DrawnSeriesFixtures.SameValue(written, back),
                $"{seat.Name}.Link: set to {DrawnSeriesFixtures.Show(written)}, read back as {DrawnSeriesFixtures.Show(back)}.");
        }
    }
}
