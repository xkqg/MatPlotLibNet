// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The spec reader refuses a misspelt enum value and names the accepted ones, but only for the fields its
/// vocabulary lists. The serializer parses an enum written as text and ignores a miss, so a field the vocabulary does not
/// name passes the reader and is dropped without a word: <c>"conditionSeverity": "critcal"</c> would draw a calm tile.
/// This reflects the enum-valued fields off the series record itself, so the next one added turns this red.</summary>
public class SpecVocabularyTests
{
    /// <summary>The enum a series field holds: its own type, or the item type of a list of them; <see langword="null"/>
    /// for any other field.</summary>
    private static Type? EnumOf(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (type.IsArray)
        {
            type = type.GetElementType()!;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            type = type.GetGenericArguments()[0];
        }

        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsEnum ? type : null;
    }

    private static readonly PropertyInfo[] EnumValuedFields =
        [.. typeof(SeriesDto).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => EnumOf(property) is not null)];

    private static string WireName(PropertyInfo property) => JsonNamingPolicy.CamelCase.ConvertName(property.Name);

    /// <summary>The wire name of every series field that holds an enum.</summary>
    public static TheoryData<string> EnumFields { get; } = new(EnumValuedFields.Select(WireName));

    [Theory]
    [MemberData(nameof(EnumFields))]
    public void EveryEnumValuedSeriesField_NamesTheValuesItAccepts(string field)
    {
        var property = EnumValuedFields.Single(candidate => WireName(candidate) == field);
        var enumType = EnumOf(property)!;

        var rule = new SpecVocabulary().RuleFor(SpecNodeKind.Series, field, seriesType: null);

        Assert.True(rule is not null,
            $"The series field '{field}' holds {enumType.Name}, and the spec vocabulary has no row for it: a misspelt value would pass the reader and be dropped by the serializer without a word.");
        Assert.Equal(Enum.GetNames(enumType), rule.Accepted);
    }

    /// <summary>The reflection finds the enum fields of the record: a refactor that hides them all would leave the
    /// Theory above with nothing to check.</summary>
    [Fact]
    public void TheSeriesRecordHoldsEnumValuedFields_TheTheoryChecks()
    {
        var fields = EnumValuedFields.Select(WireName).ToList();

        Assert.Contains("conditionSeverity", fields);
        Assert.Contains("hatches", fields);
        Assert.Contains("marker", fields);
    }
}
