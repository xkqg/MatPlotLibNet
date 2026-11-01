// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    [Fact]
    public void TheNetworkGraphLayout_AcceptsTheArcDiagram_InAnySpelling()
    {
        var rule = new SpecVocabulary().RuleFor(SpecNodeKind.Series, "networkGraphLayout", seriesType: null);

        Assert.NotNull(rule);
        Assert.Contains("Arc", rule.Accepted);
        Assert.Equal("Arc", rule.Canonical("arc"));
        Assert.Null(rule.Canonical("arcs"));
    }

    /// <summary>The string-valued fields of the axes and spine records that hold text or a key, never an enum spelling: the
    /// title, the keys a shared axis is looked up by, and the light source's own compound text.</summary>
    private static readonly string[] FreeText = ["title", "key", "shareXKey", "shareYKey", "lightSourceType"];

    /// <summary>The string-valued fields of the axes and spine records, as (node kind, wire name): the serializer writes the
    /// enum ones as text and ignores a miss, so each is either free text or needs a row in the vocabulary.</summary>
    public static TheoryData<string, string> AxesAndSpineStringFields { get; } = AsTheoryData(StringFields());

    private static TheoryData<string, string> AsTheoryData(List<(string Kind, string Field)> fields)
    {
        var data = new TheoryData<string, string>();
        foreach (var (kind, field) in fields)
        {
            data.Add(kind, field);
        }

        return data;
    }

    private static List<(string Kind, string Field)> StringFields()
    {
        var data = new List<(string Kind, string Field)>();
        var assembly = typeof(SeriesDto).Assembly;
        foreach (var (kind, record) in new[] { (SpecNodeKind.Axes, "AxesDto"), (SpecNodeKind.Spine, "SpineConfigDto") })
        {
            var type = assembly.GetType($"MatPlotLibNet.Serialization.{record}")!;
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.PropertyType == typeof(string)))
            {
                string field = WireName(property);
                if (!FreeText.Contains(field))
                {
                    data.Add((kind.ToString(), field));
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AxesAndSpineStringFields))]
    public void EveryTextFieldOfTheAxesAndSpineRecords_IsFreeTextOrNamesTheValuesItAccepts(string kind, string field)
    {
        var rule = new SpecVocabulary().RuleFor(Enum.Parse<SpecNodeKind>(kind), field, seriesType: null);

        Assert.True(rule is not null,
            $"The {kind} field '{field}' is text on the wire and not on the list of free-text fields, and the spec vocabulary has no row for it: a misspelt value would pass the reader and be dropped by the serializer without a word.");
    }

    [Fact]
    public void TheAxesAndSpineRecords_HoldTheEnumFieldsTheTheoryChecks()
    {
        var fields = StringFields().Select(row => $"{row.Kind}.{row.Field}").ToList();

        Assert.Contains("Axes.barMode", fields);
        Assert.Contains("Axes.coordinateSystem", fields);
        Assert.Contains("Spine.position", fields);
    }

    // ---- every enum field of every record the figure JSON reaches -----------------------------------------------------

    private static Type Dto(string name) =>
        typeof(SeriesDto).Assembly.GetType($"MatPlotLibNet.Serialization.{name}")
        ?? throw new InvalidOperationException($"The serializer no longer declares {name}.");

    /// <summary>Where each record that holds an enum field stands in the spec: the node kind the vocabulary keys its rules by. A
    /// record of the figure JSON that holds an enum field and is not named here turns the guard below red, so a new record cannot
    /// carry an enum the reader never sees.</summary>
    private static readonly IReadOnlyDictionary<string, SpecNodeKind> KindOfRecord = new Dictionary<string, SpecNodeKind>(StringComparer.Ordinal)
    {
        ["SeriesDto"] = SpecNodeKind.Series,
        ["AxesDto"] = SpecNodeKind.Axes,
        ["AxisDto"] = SpecNodeKind.Axis,
        ["AnnotationDto"] = SpecNodeKind.Annotation,
        ["ReferenceLineDto"] = SpecNodeKind.ReferenceLine,
        ["SpanRegionDto"] = SpecNodeKind.Span,
        ["TrendlineDto"] = SpecNodeKind.Trendline,
        ["HorizontalLevelDto"] = SpecNodeKind.HorizontalLevel,
        ["AxisBreakDto"] = SpecNodeKind.AxisBreak,
        ["SignalMarkerDto"] = SpecNodeKind.Signal,
        ["LegendDto"] = SpecNodeKind.Legend,
        ["ColorBarDto"] = SpecNodeKind.ColorBar,
        ["GridDto"] = SpecNodeKind.Grid,
        ["GridChangeDto"] = SpecNodeKind.Grid,
        ["SpineConfigDto"] = SpecNodeKind.Spine,
        ["TickConfigDto"] = SpecNodeKind.Ticks,
        ["TextStyleDto"] = SpecNodeKind.TextStyle,
        ["FontDto"] = SpecNodeKind.Font,
        ["FigureTextDto"] = SpecNodeKind.FigureText,
        ["ZoomIndicatorDto"] = SpecNodeKind.ZoomIndicator,
        ["PropCyclerDto"] = SpecNodeKind.PropCycler,
        ["PatchDto"] = SpecNodeKind.Patch,
        ["ArrowDto"] = SpecNodeKind.Patch,
        ["ThemeDto"] = SpecNodeKind.Theme,
        ["TreeNodeDto"] = SpecNodeKind.TreeNode,
    };

    /// <summary>One enum-valued member of a record of the figure JSON.</summary>
    /// <param name="Record">The name of the record that declares the member.</param>
    /// <param name="Wire">The member's name on the wire.</param>
    /// <param name="Enum">The enum it holds, alone or as the item of a list.</param>
    private readonly record struct EnumField(string Record, string Wire, Type Enum);

    /// <summary>Every record of the serializer's namespace the figure JSON reaches from <paramref name="root"/>: through the type of
    /// a member (alone, nullable, in a list or an array) and through the kinds a described family names.</summary>
    private static IReadOnlyList<Type> RecordsReachableFrom(Type root)
    {
        var seen = new List<Type> { root };
        for (int next = 0; next < seen.Count; next++)
        {
            var reached = seen[next].GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(WireMember)
                .SelectMany(property => TypesIn(property.PropertyType))
                .Concat(seen[next].GetCustomAttributes<JsonDerivedTypeAttribute>().Select(derived => derived.DerivedType));
            seen.AddRange(reached.Where(IsRecordOfTheWire).Where(type => !seen.Contains(type)).Distinct());
        }

        return seen;
    }

    private static bool WireMember(PropertyInfo property) =>
        !property.IsDefined(typeof(JsonIgnoreAttribute)) && (property.GetMethod is { IsPublic: true } || property.IsDefined(typeof(JsonIncludeAttribute)));

    private static bool IsRecordOfTheWire(Type type) =>
        type.IsClass && type.Assembly == typeof(SeriesDto).Assembly && type.Namespace == "MatPlotLibNet.Serialization";

    /// <summary>The type, and every type it is made of: the item of a list or an array, the argument of a generic, the type under a
    /// nullable.</summary>
    private static IEnumerable<Type> TypesIn(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        yield return type;
        var inner = type.IsArray ? [type.GetElementType()!] : type.IsGenericType ? type.GetGenericArguments() : [];
        foreach (var part in inner.SelectMany(TypesIn))
        {
            yield return part;
        }
    }

    private static IEnumerable<EnumField> EnumFieldsOf(Type record) =>
        record.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(WireMember)
            .Select(property => (Property: property, Enum: TypesIn(property.PropertyType).FirstOrDefault(type => type.IsEnum)))
            .Where(member => member.Enum is not null)
            .Select(member => new EnumField(record.Name, WireName(member.Property), member.Enum!));

    /// <summary>The enum-typed members of every record the figure JSON reaches, wherever the record stands: the series, the axes and
    /// each overlay they hold, the legend, the colour bar, the grid, the spines, the ticks, the text styles, the figure's texts and
    /// images, the theme and its cycler, and the family of patches. The serializer reads an enum written as text and ignores a miss
    /// in some of them and refuses it without naming what is accepted in the rest, so each needs a row the reader checks it by.</summary>
    [Fact]
    public void EveryEnumFieldReachableFromFigureDto_IsNormalised()
    {
        var vocabulary = new SpecVocabulary();
        var gaps = new List<string>();
        foreach (var field in RecordsReachableFrom(Dto("FigureDto")).SelectMany(EnumFieldsOf))
        {
            if (!KindOfRecord.TryGetValue(field.Record, out var kind))
            {
                gaps.Add($"{field.Record}.{field.Wire} ({field.Enum.Name}): the record has no node kind in this guard's table");
                continue;
            }

            var rule = vocabulary.RuleFor(kind, field.Wire, seriesType: null);
            if (rule is null)
            {
                gaps.Add($"{field.Record}.{field.Wire} ({field.Enum.Name}): the vocabulary has no row for {kind}.{field.Wire}");
            }
            else if (!Enum.GetNames(field.Enum).SequenceEqual(rule.Accepted))
            {
                gaps.Add($"{field.Record}.{field.Wire} ({field.Enum.Name}): the row for {kind}.{field.Wire} accepts [{string.Join(", ", rule.Accepted)}], not the names of the enum");
            }
        }

        Assert.True(gaps.Count == 0,
            "A misspelt value of an enum field passes the reader and is dropped by the serializer, or is refused by it without a word on what is accepted:" + Environment.NewLine
            + string.Join(Environment.NewLine, gaps.Order(StringComparer.Ordinal)));
    }

    /// <summary>Where each described family (a base record closed by <c>JsonDerivedType</c> attributes, read by its <c>type</c>) stands
    /// in the spec: the node kind whose <c>type</c> rule lists its kinds. A family of the figure JSON that is not named here turns the
    /// guard below red, so a family added tomorrow cannot be read by a serializer that skips an unknown kind while the reader never
    /// checks the kind.</summary>
    private static readonly IReadOnlyDictionary<string, SpecNodeKind> KindOfFamily = new Dictionary<string, SpecNodeKind>(StringComparer.Ordinal)
    {
        ["TickLocatorDto"] = SpecNodeKind.TickLocator,
        ["TickFormatterDto"] = SpecNodeKind.TickFormatter,
        ["NormalizerDto"] = SpecNodeKind.Normalizer,
        ["ColorMapDto"] = SpecNodeKind.ColorMap,
        ["PatchDto"] = SpecNodeKind.Patch,
        ["AxisTransformDto"] = SpecNodeKind.AxisLink,
    };

    /// <summary>Every described family the figure JSON reaches has a row of its kinds in the vocabulary, spelt as its attributes
    /// declare them. The serializer reads a kind it does not know as the base record and skips it with a diagnostic, so without the row
    /// a misspelt kind would pass the reader and vanish from the picture.</summary>
    [Fact]
    public void EveryDescribedFamilyReachableFromFigureDto_NamesItsKindsInTheVocabulary()
    {
        var vocabulary = new SpecVocabulary();
        var families = RecordsReachableFrom(Dto("FigureDto")).Where(record => record.IsDefined(typeof(JsonPolymorphicAttribute), inherit: false)).ToList();
        var gaps = new List<string>();
        foreach (var family in families)
        {
            if (!KindOfFamily.TryGetValue(family.Name, out var kind))
            {
                gaps.Add($"{family.Name}: the family has no node kind in this guard's table");
                continue;
            }

            var rule = vocabulary.RuleFor(kind, "type", seriesType: null);
            var declared = family.GetCustomAttributes<JsonDerivedTypeAttribute>().Select(derived => (string)derived.TypeDiscriminator!);
            if (rule is null || !declared.SequenceEqual(rule.Accepted))
            {
                gaps.Add($"{family.Name}: {kind}.type does not list [{string.Join(", ", declared)}]");
            }
        }

        Assert.True(gaps.Count == 0,
            "A kind the serializer does not know is skipped with a diagnostic, and a kind the reader does not check passes it:" + Environment.NewLine
            + string.Join(Environment.NewLine, gaps.Order(StringComparer.Ordinal)));
        Assert.Equal(KindOfFamily.Keys.Order(StringComparer.Ordinal), families.Select(family => family.Name).Order(StringComparer.Ordinal));
    }

    /// <summary>The walk reaches the records the guard is meant to cover: a refactor that stops it at the axes would leave the guard
    /// with nothing to check.</summary>
    [Fact]
    public void TheWalkFromFigureDto_ReachesTheSeriesTheAxesTheirOverlaysThePatchesAndTheAxisLinks()
    {
        var reached = RecordsReachableFrom(Dto("FigureDto")).Select(record => record.Name).ToList();

        foreach (var expected in new[] { "AxesDto", "SeriesDto", "AxisDto", "SecondaryAxisSlotDto", "AnnotationDto", "SpanRegionDto", "LegendDto", "ThemeDto", "PropCyclerDto", "PatchDto", "RectangleDto", "ArcDto", "AxisTransformDto", "LinearAxisTransformDto", "PowerAxisTransformDto", "FigureTextDto" })
        {
            Assert.Contains(expected, reached);
        }
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
