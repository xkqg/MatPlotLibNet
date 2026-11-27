// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A setting a document carries as an enum-typed field is written as the camel-case name of its member and read back by
/// the same name rule a text field is (<see cref="EnumWireNames{T}"/>): a member's name in any case, escaped or not, reads as the
/// member, and every other text (a number, an undefined value, two names joined by a comma, a name with a space before it, a text
/// longer than any name, a value that is no text) is refused with a <see cref="JsonException"/>, as an unknown name always was.
/// .NET's own string enum converter made a value of each of those.</summary>
public class EnumTypedWireReadTests
{
    private static readonly double[] X = [1.0, 2, 3, 4];
    private static readonly double[] Y = [2.0, 1, 4, 3];

    private static string Pair(string key, string value) => $"\"{key}\":\"{value}\"";

    /// <summary>A setting a document carries as an enum-typed field.</summary>
    public sealed record TypedSite(string Name, Action<Axes> Build, string Key, string Written, Func<Axes, object> Read, object Member);

    private static readonly TypedSite[] TypedSites =
    [
        new("series marker", a => a.Plot(X, Y).Marker = MarkerStyle.Square, "marker", "square", a => a.Series.OfType<LineSeries>().Single().Marker!, MarkerStyle.Square),
        new("annotation alignment", a => a.Annotate("note", 1, 1).Alignment = TextAlignment.Right, "alignment", "right", a => a.Annotations.Single().Alignment, TextAlignment.Right),
        new("legend position", a => a.Legend = a.Legend with { Position = LegendPosition.UpperLeft }, "position", "upperLeft", a => a.Legend.Position, LegendPosition.UpperLeft),
    ];

    public static TheoryData<string> TypedSiteNames { get; } = new(TypedSites.Select(site => site.Name));

    public static TheoryData<string, string> TypedSitesAndTextsThatNameNoMember { get; } = TypedTexts();

    private static TheoryData<string, string> TypedTexts()
    {
        var data = new TheoryData<string, string>();
        foreach (var site in TypedSites)
        {
            data.Add(site.Name, "\"zigzagzag\"");
            data.Add(site.Name, "\"1\"");
            data.Add(site.Name, "1");
            data.Add(site.Name, "\"99\"");
            data.Add(site.Name, "99");
            data.Add(site.Name, $"\"{site.Written}, {site.Written}\"");
            data.Add(site.Name, $"\" {site.Written}\"");
            data.Add(site.Name, $"\"{new string('x', 300)}\"");
            data.Add(site.Name, "true");
        }

        return data;
    }

    private static string WrittenTyped(TypedSite site)
    {
        var figure = new Figure { Width = 400, Height = 300 };
        site.Build(figure.AddSubPlot(1, 1, 1));
        string json = figure.ToJson();
        Assert.Contains(Pair(site.Key, site.Written), json, StringComparison.Ordinal);
        return json;
    }

    [Theory]
    [MemberData(nameof(TypedSiteNames))]
    public void ATypedField_ReadsItsNameInAnyCase(string name)
    {
        var site = TypedSites.Single(s => s.Name == name);
        string json = WrittenTyped(site).Replace(Pair(site.Key, site.Written), Pair(site.Key, site.Written.ToUpperInvariant()), StringComparison.Ordinal);

        var axes = new ChartSerializer().FromJson(json).SubPlots[0];

        Assert.Equal(site.Member, site.Read(axes));
    }

    /// <summary>The read options write an enum as the writer does, so a copy of them (the MCP reader's) writes what a figure writes.</summary>
    [Fact]
    public void TheReadOptions_WriteAMemberAsTheWriterDoes()
    {
        Assert.Equal("\"upperLeft\"", JsonSerializer.Serialize(LegendPosition.UpperLeft, ChartSerializer.ReadOptions));
        Assert.Equal(LegendPosition.UpperLeft, JsonSerializer.Deserialize<LegendPosition>("\"UPPERLEFT\"", ChartSerializer.ReadOptions));
    }

    [Fact]
    public void ATypedField_ReadsAnEscapedName()
    {
        var site = TypedSites[0];
        string json = WrittenTyped(site).Replace(Pair(site.Key, site.Written), "\"marker\":\"\\u0053quare\"", StringComparison.Ordinal);

        var axes = new ChartSerializer().FromJson(json).SubPlots[0];

        Assert.Equal(MarkerStyle.Square, site.Read(axes));
    }

    [Theory]
    [MemberData(nameof(TypedSitesAndTextsThatNameNoMember))]
    public void ATypedField_RefusesATextThatNamesNoMember_AsItRefusesAnUnknownName(string name, string value)
    {
        var site = TypedSites.Single(s => s.Name == name);
        string json = WrittenTyped(site).Replace(Pair(site.Key, site.Written), $"\"{site.Key}\":{value}", StringComparison.Ordinal);

        Assert.ThrowsAny<JsonException>(() => new ChartSerializer().FromJson(json));
    }
}
