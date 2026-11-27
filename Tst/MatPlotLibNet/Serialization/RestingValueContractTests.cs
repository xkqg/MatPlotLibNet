// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A resting value is one number, named <c>RestingX</c> beside the property <c>X</c> it is the start of, on a series, a part
/// of the axes or the figure, or a settings record a part is made of: the property starts at it, the writer leaves the property out
/// of the JSON while it holds it, the reader gives it back when the JSON names none, and every verb that makes the subject takes it as
/// its default. A constant that names a number the series does not
/// start from, or a property that starts from a number typed again beside its constant, would let the writer and the reader
/// disagree with the series about what rest is, and a document would then come back drawn another way.</summary>
public class RestingValueContractTests
{
    private const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

    private static object Normal(object? value) => value switch
    {
        null => "null",
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        Enum e => Convert.ToDouble(e, CultureInfo.InvariantCulture),
        _ => value,
    };

    /// <summary>The settings a series starts from, by the constant that names each: the resting constants of the type, with the
    /// property they are the start of.</summary>
    private static IEnumerable<(FieldInfo Constant, PropertyInfo Property)> RestingSettingsOf(Type type) =>
        type.GetFields(AnyStatic)
            .Where(field => field.IsLiteral && field.Name.StartsWith("Resting", StringComparison.Ordinal) && field.Name.Length > "Resting".Length)
            .Select(field => (Constant: field, Property: type.GetProperty(field.Name["Resting".Length..], BindingFlags.Public | BindingFlags.Instance)))
            .Where(pair => pair.Property is { CanRead: true, CanWrite: true } property && property.GetIndexParameters().Length == 0)
            .Select(pair => (pair.Constant, pair.Property!));

    /// <summary>A subject told nothing (a series type, a part of the axes or the figure, a settings record a part is made of: the one
    /// roster of <see cref="RestingSubjects"/>) starts each setting from the constant that names its start.</summary>
    [Theory]
    [MemberData(nameof(RestingSubjects.Names), MemberType = typeof(RestingSubjects))]
    public void ASubjectThatIsToldNothing_StartsFromEachRestingConstantItNames(string name)
    {
        var subject = RestingSubjects.Subject(name);
        object atRest = subject.AtRest();

        var apart = RestingSettingsOf(subject.Type)
            .Where(pair => !Equals(Normal(pair.Property.GetValue(atRest)), Normal(pair.Constant.GetRawConstantValue())))
            .Select(pair => $"{name}: {subject.Type.Name}.{pair.Property.Name} starts at {pair.Property.GetValue(atRest)}, {pair.Constant.Name} is {pair.Constant.GetRawConstantValue()}")
            .ToList();

        Assert.True(apart.Count == 0, string.Join('\n', apart));
    }

    /// <summary>A subject made by its constructor and told nothing starts from the constant that names what the constructor's argument
    /// is when it is left out: a streaming series' capacity, a signal's rate, a point-and-figure chart's reversal, a break's style.</summary>
    [Theory]
    [MemberData(nameof(RestingSubjects.Names), MemberType = typeof(RestingSubjects))]
    public void ASubjectsConstructorThatIsToldNothing_TakesEachRestingConstantItNames(string name)
    {
        var type = RestingSubjects.Subject(name).Type;
        string typeName = type.Name;

        var apart = type.GetConstructors().SelectMany(constructor => constructor.GetParameters()
                .Where(parameter => parameter.HasDefaultValue && parameter.DefaultValue is not null)
                .Select(parameter => (parameter, constant: type.GetField("Resting" + char.ToUpperInvariant(parameter.Name![0]) + parameter.Name[1..], AnyStatic)))
                .Where(pair => pair.constant is { IsLiteral: true })
                .Where(pair => !Equals(Normal(pair.parameter.DefaultValue), Normal(pair.constant!.GetRawConstantValue())))
                .Select(pair => $"{typeName}({pair.parameter.Name} = {pair.parameter.DefaultValue}), {pair.constant!.Name} is {pair.constant.GetRawConstantValue()}"))
            .ToList();

        Assert.True(apart.Count == 0, string.Join('\n', apart));
    }

    [Fact]
    public void TheScan_FindsTheSettingsItIsMeantToHold()
    {
        var settings = RestingSubjects.All.Select(subject => subject.Type).Distinct()
            .SelectMany(type => RestingSettingsOf(type).Select(pair => $"{type.Name}.{pair.Property.Name}")).ToList();

        Assert.Contains("HistogramSeries.Bins", settings);
        Assert.Contains("ScatterSeries.MarkerSize", settings);
        Assert.Contains("SankeySeries.NodeWidth", settings);
        Assert.Contains("ColorBar.Width", settings);
        Assert.Contains("Axes.TitleLoc", settings);
        Assert.Contains("SpanRegion.LineWidth", settings);
        Assert.True(settings.Count > 70, $"{settings.Count} settings found");
    }

    /// <summary>Where a series declares <c>RestingX</c> and the property <c>X</c> starts from a value, that value is the constant, not
    /// a number typed beside it.</summary>
    private static IReadOnlyList<string> InitialisersTypedAgain(IEnumerable<SourceFile> sources)
    {
        var found = new List<string>();
        foreach (var file in sources)
        {
            foreach (Match constant in Regex.Matches(file.Text, @"const\s+[\w.]+\s+Resting(?<name>\w+)\s*=", RegexOptions.CultureInvariant))
            {
                string name = constant.Groups["name"].Value;
                foreach (Match initialiser in Regex.Matches(file.Text, $@"\b(?:double|int|bool|[A-Z]\w+)\??\s+{name}\s*\{{[^\n]*\}}\s*=\s*(?<value>[^;]+);", RegexOptions.CultureInvariant))
                {
                    if (!Regex.IsMatch(initialiser.Groups["value"].Value.Trim(), @"^(?:\w+\.)*Resting\w+$"))
                    {
                        found.Add($"{file.Path}: {name} starts from {initialiser.Groups["value"].Value.Trim()}, not from Resting{name}");
                    }
                }
            }
        }

        return found;
    }

    [Fact]
    public void ASeriesWithARestingConstant_StartsItsPropertyFromTheConstant()
    {
        var found = InitialisersTypedAgain(RepositoryTree.SourceFiles().Where(f => f.Path.StartsWith("Src/MatPlotLibNet", StringComparison.Ordinal)));

        Assert.True(found.Count == 0, string.Join('\n', found));
    }

    [Theory]
    [InlineData("private const double RestingAlpha = 1.0;\npublic double Alpha { get; set => Change(ref field, value); } = 1.0;")]
    [InlineData("internal const int RestingBins = 10;\npublic int Bins { get; set => Change(ref field, value); } = 10;")]
    [InlineData("private const bool RestingShowLines = true;\npublic bool ShowLines { get; set => Change(ref field, value); } = true;")]
    public void APropertyThatTypesItsStartBesideItsConstant_IsFound(string text)
    {
        Assert.Single(InitialisersTypedAgain([new("Src/MatPlotLibNet/Any.cs", text)]));
    }

    [Theory]
    [InlineData("private const double RestingAlpha = 1.0;\npublic double Alpha { get; set => Change(ref field, value); } = RestingAlpha;")]
    [InlineData("internal const double RestingMarkerSize = MarkerLook.RestingSize;\npublic double MarkerSize { get; set => Change(ref field, value); } = RestingMarkerSize;")]
    [InlineData("private const double RestingAlpha = 1.0;\npublic double Beta { get; set => Change(ref field, value); } = 2.0;")]
    public void APropertyThatStartsFromItsConstantOrFromAnotherSetting_IsNotFound(string text)
    {
        Assert.Empty(InitialisersTypedAgain([new("Src/MatPlotLibNet/Any.cs", text)]));
    }
}
