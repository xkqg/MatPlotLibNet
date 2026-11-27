// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A series' JSON carries every property the series draws. A property the JSON drops is a picture that
/// changes the moment a figure is sent over the wire, handed to the MCP server or saved and opened again: the marker
/// that vanishes from a line, the stem that turns from the colour it was given to the cycle colour, the connector
/// that jumps from behind the dots to in front of them.
/// <para>One case per series type (and seat), the properties reflected off the type, every property it finds wrong named at once:
/// set to a value that is not its default, a property must come back with that value. And a property left alone must come back
/// at its default, which is what a JSON written before the field existed reads as.</para></summary>
public class SeriesDrawnPropertiesRoundTripTests
{
    private static readonly ChartSerializer Serializer = new();

    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.Seats), MemberType = typeof(DrawnSeriesFixtures))]
    public void EveryDrawnProperty_ComesBackFromJson_WithTheValueItWasGiven(string type, SeriesSeat seat)
    {
        var figure = DrawnSeriesFixtures.Build(type, everyPropertySet: true, seat);

        DrawnSeriesFixtures.AssertKept(seat is SeriesSeat.Primary ? type : $"{type} on {seat}",
            DrawnSeriesFixtures.ValuesOf(DrawnSeriesFixtures.TheSeries(figure, seat)),
            DrawnSeriesFixtures.ValuesOf(DrawnSeriesFixtures.TheSeries(figure.RoundTripped(), seat)), "from JSON");
    }

    [Theory]
    [MemberData(nameof(DrawnSeriesFixtures.TypeNames), MemberType = typeof(DrawnSeriesFixtures))]
    public void EveryDrawnPropertyLeftAlone_ComesBackFromJson_AtItsDefault(string type)
    {
        var figure = DrawnSeriesFixtures.Build(type, everyPropertySet: false);

        DrawnSeriesFixtures.AssertKept(type, DrawnSeriesFixtures.ValuesOf(DrawnSeriesFixtures.TheSeries(figure)),
            DrawnSeriesFixtures.ValuesOf(DrawnSeriesFixtures.TheSeries(figure.RoundTripped())), "from JSON");
    }

    /// <summary>Every row of the exemption list names a property that exists, so the list cannot outlive the property
    /// it excuses.</summary>
    [Fact]
    public void EveryPropertyThatNoJsonCarries_IsARealSettableProperty()
    {
        foreach (var name in DrawnSeriesFixtures.NotCarried.Keys)
        {
            string[] parts = name.Split('.');
            var type = DrawnSeriesFixtures.DrawnTypes.Single(t => t.Name == parts[0]);

            Assert.True(type.GetProperty(parts[1])?.SetMethod is { IsPublic: true }, $"{name} is not a public settable property.");
        }
    }

    /// <summary>Only code is not carried: a property named in <see cref="DrawnSeriesFixtures.NotCarried"/> is a delegate, which no JSON
    /// document can hold. Any other property has a way into the document, so it is a row of the contract above, and a row that
    /// excuses a hierarchy, a number or a colour turns this red the day it is written.</summary>
    [Fact]
    public void APropertyNamedAsNotCarried_IsADelegate_BecauseOnlyCodeIsNotCarried()
    {
        foreach (var name in DrawnSeriesFixtures.NotCarried.Keys)
        {
            string[] parts = name.Split('.');
            var property = DrawnSeriesFixtures.DrawnTypes.Single(t => t.Name == parts[0]).GetProperty(parts[1])!;

            Assert.True(typeof(Delegate).IsAssignableFrom(property.PropertyType),
                $"{name} is named as not carried but is a {property.PropertyType.Name}, not code: carry it in the DTO and take the row out.");
        }
    }

    /// <summary>Every series type the library ships is under the contract or named in the exemption list with the
    /// reason it is not. A series added tomorrow turns this red until it is one or the other: the omission the contract
    /// exists to catch cannot start with the contract not knowing the type.</summary>
    [Fact]
    public void EverySeriesTypeTheLibraryShips_IsUnderTheContract_OrNamedWithTheReasonItIsNot()
    {
        var shipped = typeof(ISeries).Assembly.ConcretePublicSubtypesOf(typeof(ISeries));

        var unaccounted = shipped.Except(DrawnSeriesFixtures.DrawnTypes).Except(DrawnSeriesFixtures.NotUnderContract.Keys)
            .Select(t => t.Name).Order(StringComparer.Ordinal).ToList();

        Assert.True(unaccounted.Count == 0,
            $"{unaccounted.Count} shipped series type(s) are neither under the drawn-property contract nor named with a reason: {string.Join(", ", unaccounted)}.");
    }

    /// <summary>A type is under the contract or exempt from it, never both, and an exemption says why.</summary>
    [Fact]
    public void ASeriesTypeIsNeverBothUnderTheContractAndExempt_AndEveryExemptionStatesItsReason()
    {
        var both = DrawnSeriesFixtures.DrawnTypes.Intersect(DrawnSeriesFixtures.NotUnderContract.Keys).Select(t => t.Name).ToList();
        var unexplained = DrawnSeriesFixtures.NotUnderContract.Where(pair => string.IsNullOrWhiteSpace(pair.Value)).Select(pair => pair.Key.Name).ToList();

        Assert.True(both.Count == 0, $"Under the contract and exempt at once: {string.Join(", ", both)}.");
        Assert.True(unexplained.Count == 0, $"Exempt without a reason: {string.Join(", ", unexplained)}.");
    }

    /// <summary>Every series type the library ships comes back from JSON as its own type: the registry has a reader for each
    /// discriminator a series writes, a live series included, so no type is left out of the contract and the exemption list stays
    /// empty. A type that cannot come back would be named there with the reason; this row turns red the day one is.</summary>
    [Fact]
    public void EverySeriesTypeTheLibraryShips_ComesBackFromJsonAsItsOwnType_SoNoneIsExempt()
    {
        Assert.Empty(DrawnSeriesFixtures.NotUnderContract);
        foreach (var type in DrawnSeriesFixtures.DrawnTypes)
        {
            var figure = DrawnSeriesFixtures.Build(type.Name, everyPropertySet: false);

            Assert.IsType(type, DrawnSeriesFixtures.TheSeries(figure.RoundTripped()));
        }
    }

    /// <summary>The fixture asks for a seat only a line or a scatter can take.</summary>
    [Fact]
    public void ASeriesTypeWithoutASecondarySeat_IsRefusedByTheFixture()
    {
        Assert.Throws<ArgumentException>(() => DrawnSeriesFixtures.Build(nameof(BarSeries), false, SeriesSeat.SecondaryY));
        Assert.Throws<ArgumentException>(() => DrawnSeriesFixtures.Build("NoSuchSeries", false));
    }

    // ---- the base path: the fields every series shares -------------------------------------------------------------

    /// <summary>An area is drawn behind the other series by default (its z-order rests at -1); a z-order the user puts
    /// back to 0 is a change, and JSON keeps it.</summary>
    [Fact]
    public void AnAreaBroughtForwardToZOrderZero_StaysThereThroughJson()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.FillBetween([1.0, 2], [1.0, 2], null, s => s.ZOrder = 0)).Build();

        var read = figure.RoundTripped().SubPlots[0].Series.Single();

        Assert.Equal(0, read.ZOrder);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  A series at its resting z-order writes no z-order at all, whatever that rest is: 0 for a line, -1 for an area. That is what
        // keeps every JSON written before the field existed byte-identical.
        new DrawnToken("ASeriesAtItsRestingZOrder_WritesNoZOrder", () => Serializer.ToJson(Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2], [1.0, 2])
            .FillBetween([1.0, 2], [1.0, 2])).Build())) { Absent = ["zOrder"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>A series that is not a <see cref="ChartSeries"/> rests at z-order 0, which <see cref="ISeries"/> itself
    /// defaults to: 0 is left out, anything else is written.</summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(3, true)]
    public void ASeriesOutsideTheChartSeriesFamily_WritesItsZOrderAgainstZero(int zOrder, bool written)
    {
        var figure = new Figure();
        figure.AddSubPlot().AddSeries(new ForeignSeries(new(0, 1, 0, 1)) { ZOrder = zOrder });

        string json = Serializer.ToJson(figure);

        Assert.Equal(written, json.Contains("\"zOrder\":3", StringComparison.Ordinal));
        Assert.Equal(written, json.Contains("zOrder", StringComparison.Ordinal));
    }

    /// <summary>A bar on numeric x positions (the MACD histogram is one) comes back on those positions; positions that
    /// do not pair up with the values are refused rather than drawn out of range, in the length owner's sentence, naming the array
    /// that does not pair up: the positions, or the categories.</summary>
    [Theory]
    [InlineData("""["a","b"]""", "[1,2]", "[1,2,3]", "XCoordinate", 3)]
    [InlineData("""["a","b","c"]""", "[1,2]", "[1,2]", "categories", 3)]
    public void ABarWhosePositionsDoNotPairWithItsValues_IsRefused(string categories, string values, string positions, string blamed, int length)
    {
        string json = $$"""{"width":400,"height":300,"subPlots":[{"series":[{"type":"bar","categories":{{categories}},"values":{{values}},"xCoordinate":{{positions}}}]}]}""";

        var refusal = Assert.Throws<ArgumentException>(() => Serializer.FromJson(json));

        Assert.Equal(blamed, refusal.ParamName);
        Assert.StartsWith(length.RefusalAgainst(2), refusal.Message);
    }
}
