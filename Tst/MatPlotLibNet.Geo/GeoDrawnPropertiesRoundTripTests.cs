// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The twin, for the map series, of the core suite's drawn-properties contract: every property of a map that changes the
/// picture is carried by the JSON. A property the series gains has to be added to the table below, or the guard fails, and the
/// table is then held to the round trip: set to a value other than its default it comes back as that value, left alone it comes
/// back at its default.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoDrawnPropertiesRoundTripTests
{
    private static readonly ChartSerializer Serializer = new();

    /// <summary>The properties a caller can set after the series is made, each with the way to set it to a value other than its
    /// default.</summary>
    private static readonly Dictionary<string, Action<GeoPolygonSeries>> Settable = new()
    {
        [nameof(GeoPolygonSeries.Color)] = s => s.Color = Colors.Red,
        [nameof(GeoPolygonSeries.StrokeColor)] = s => s.StrokeColor = Colors.Orange,
        [nameof(GeoPolygonSeries.StrokeWidth)] = s => s.StrokeWidth = 2.5,
        [nameof(GeoPolygonSeries.Label)] = s => s.Label = "a map",
        [nameof(GeoPolygonSeries.Visible)] = s => s.Visible = false,
        [nameof(GeoPolygonSeries.ZOrder)] = s => s.ZOrder = 3,
        [nameof(GeoPolygonSeries.Values)] = s => s.Values = [.. Enumerable.Range(0, s.Features.Count).Select(i => i % 7 == 0 ? double.NaN : i * 0.5)],
        [nameof(GeoPolygonSeries.ColorMap)] = s => s.ColorMap = ColorMaps.Plasma,
        [nameof(GeoPolygonSeries.Normalizer)] = s => s.Normalizer = new LogNormalizer(),
    };

    /// <summary>Whether a property came back as the value it was given: a map and a normaliser come back as new instances, so
    /// they are the same when they are the same kind and, for a map, make the same colours.</summary>
    private static void AssertSame(object? written, object? read)
    {
        switch (written)
        {
            case IColorMap map:
                var back = Assert.IsAssignableFrom<IColorMap>(read);
                Assert.Equal(map.Name, back.Name);
                foreach (double t in new[] { 0.0, 0.25, 0.5, 1.0 }) Assert.Equal(map.GetColor(t), back.GetColor(t));
                break;
            case INormalizer normalizer:
                Assert.IsType(normalizer.GetType(), read);
                break;
            default:
                Assert.Equal(written, read);
                break;
        }
    }

    /// <summary>The property that is set when the series is made.</summary>
    private const string InitOnly = nameof(GeoPolygonSeries.IsRawProjected);

    private static GeoPolygonSeries Series(bool rawProjected = false) =>
        new(NaturalEarth110m.Countries(), GeoProjection.Robinson) { IsRawProjected = rawProjected };

    private static GeoPolygonSeries Back(GeoPolygonSeries series)
    {
        var figure = new Figure();
        figure.AddSubPlot().AddSeries(series);
        return Assert.IsType<GeoPolygonSeries>(Serializer.FromJson(Serializer.ToJson(figure)).SubPlots[0].Series[0]);
    }

    private static object? ValueOf(GeoPolygonSeries series, string property) =>
        typeof(GeoPolygonSeries).GetProperty(property)!.GetValue(series);

    [Fact]
    public void EverySettableProperty_IsInTheTable()
    {
        var settable = typeof(GeoPolygonSeries).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true })
            .Select(p => p.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal([.. Settable.Keys.Append(InitOnly).Order(StringComparer.Ordinal)], settable);
    }

    public static TheoryData<string> Names => [.. Settable.Keys];

    [Theory]
    [MemberData(nameof(Names))]
    public void ASetProperty_ComesBackFromJson_WithTheValueItWasGiven(string property)
    {
        var series = Series();
        Settable[property](series);

        var back = Back(series);

        AssertSame(ValueOf(series, property), ValueOf(back, property));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void APropertyLeftAlone_ComesBackFromJson_AtItsDefault(string property)
    {
        var series = Series();

        var back = Back(series);

        AssertSame(ValueOf(series, property), ValueOf(back, property));
    }

    [Fact]
    public void RawProjection_ComesBackFromJson_WhenSet_AndWhenLeftAlone()
    {
        Assert.True(Back(Series(rawProjected: true)).IsRawProjected);
        Assert.False(Back(Series()).IsRawProjected);
    }

    [Fact]
    public void ADefaultStrokeWidth_IsLeftOutOfTheJson()
    {
        var figure = new Figure();
        figure.AddSubPlot().AddSeries(Series());

        Assert.DoesNotContain("lineWidth", Serializer.ToJson(figure), StringComparison.Ordinal);
    }
}
