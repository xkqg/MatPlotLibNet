// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The compiler can say a public member has no documentation (CS1591), but this repository switches that
/// warning off for the whole library, so nothing else would. A composed-chart verb is the first thing a reader of the API
/// meets, so each carries the five parts the documentation promises: a summary, one <c>param</c> for every parameter, a
/// <c>returns</c>, an <c>exception</c> for the arguments it refuses, and an <c>example</c> that calls it. The types the
/// verbs introduce (the parts a callback receives, the named data they take) carry a summary on the type and on every
/// public property. This reads the public API and the generated XML documentation, so a verb or type added tomorrow is
/// held to the same without an edit here.</summary>
public class ComposedVerbDocumentationTests
{
    private static IReadOnlyList<Type> Families => VerbFamilies.All;

    /// <summary>The types the verbs introduce: the parts a callback receives and the named data they take, found through the
    /// verbs' parameters, and the result of the log-rank test, which a verb hands out inside the risk table's parts.</summary>
    private static readonly Type[] VerbTypes = [.. VerbFamilies.TypesTheVerbsIntroduce(), typeof(LogRankResult)];

    [Fact]
    public void TheTypesItChecks_AreEveryTypeTheVerbsIntroduce()
    {
        var missing = VerbFamilies.TypesTheVerbsIntroduce().Except(VerbTypes).Select(type => type.Name).ToArray();

        Assert.True(missing.Length == 0, $"The documentation check leaves out types the verbs introduce: {string.Join(", ", missing)}.");
    }

    private static readonly XDocument Documentation = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MatPlotLibNet.xml"));

    private static readonly MethodInfo[] Verbs =
        [.. Families
            .SelectMany(family => family.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false))
            .OrderBy(method => method.Name, StringComparer.Ordinal)];

    /// <summary>A C# 14 <c>extension(T){ }</c> member's reflected <see cref="MethodInfo.DeclaringType"/> is a
    /// compiler-synthesized nested skeleton type (named <c>&lt;G&gt;$&lt;hash&gt;</c>), not the class the block is
    /// written in — <c>XYAxesExtensions.BumpChart</c> reflects as declared on
    /// <c>XYAxesExtensions+&lt;G&gt;$70B89E00D34DD57ECEA311265D89A0A1</c>. The outer, user-written type is one
    /// <see cref="Type.DeclaringType"/> up.</summary>
    private static Type UserWrittenDeclaringType(MethodInfo method) =>
        method.DeclaringType!.IsNested ? method.DeclaringType.DeclaringType! : method.DeclaringType;

    /// <summary>The generated XML documentation mirrors the same split: the member under the user-written type's
    /// name is a shim carrying only <c>&lt;inheritdoc cref="..."/&gt;</c> to the skeleton-type member that holds the
    /// real summary/param/returns/exception/example content. Follows that redirect when present.</summary>
    private static XElement ResolveInheritedContent(XElement member)
    {
        var redirect = member.Elements().SingleOrDefault() is { Name.LocalName: "inheritdoc" } only
            ? (string?)only.Attribute("cref")
            : null;
        if (redirect is null) return member;

        return Documentation.Descendants("member").Single(candidate => (string?)candidate.Attribute("name") == redirect);
    }

    /// <summary>The verb names as a Theory carries them.</summary>
    public static TheoryData<string> VerbNames { get; } = new(Verbs.Select(verb => verb.Name));

    /// <summary>The names of the types the verbs introduce, as a Theory carries them.</summary>
    public static TheoryData<string> TypeNames { get; } = new(VerbTypes.Select(type => type.Name));

    private static string TextOf(XElement? element) => string.Concat(element?.DescendantNodes().OfType<XText>().Select(text => text.Value) ?? []).Trim();

    private static XElement[] MembersStartingWith(string prefix) =>
        [.. Documentation.Descendants("member").Where(member => ((string?)member.Attribute("name") ?? "").StartsWith(prefix, StringComparison.Ordinal))];

    /// <summary>Liveness: the reading finds the verbs and the documentation file, so the pins cannot go green by finding nothing.</summary>
    [Fact]
    public void TheVerbsAndTheirDocumentationAreFound()
    {
        Assert.True(Verbs.Length >= 15, $"expected the fifteen composed verbs, found {Verbs.Length}: {string.Join(", ", Verbs.Select(v => v.Name))}");
        Assert.NotEmpty(MembersStartingWith("M:MatPlotLibNet.CategoricalAxesExtensions.Dumbbell("));
        Assert.All(Verbs, verb => Assert.Equal(1, Verbs.Count(other => other.Name == verb.Name)));
    }

    [Theory]
    [MemberData(nameof(VerbNames))]
    public void EveryVerb_IsDocumentedWithSummaryParamsReturnsExceptionAndAnExample(string name)
    {
        var verb = Verbs.Single(candidate => candidate.Name == name);
        var shim = Assert.Single(MembersStartingWith($"M:{UserWrittenDeclaringType(verb).FullName}.{name}("));
        var member = ResolveInheritedContent(shim);

        var missing = new List<string>();
        if (TextOf(member.Element("summary")).Length == 0) missing.Add("summary");
        // GetParameters()[0] is the extension receiver (true for both old this-T and new extension(T) syntax);
        // it is documented once on the containing family's class summary, not restated on every member.
        foreach (var parameter in verb.GetParameters().Skip(1))
        {
            var documented = member.Elements("param").FirstOrDefault(param => (string?)param.Attribute("name") == parameter.Name);
            if (TextOf(documented).Length == 0) missing.Add($"param '{parameter.Name}'");
        }
        if (TextOf(member.Element("returns")).Length == 0) missing.Add("returns");
        if (!member.Elements("exception").Any()) missing.Add("exception");
        if (!TextOf(member.Element("example")).Contains($".{name}(", StringComparison.Ordinal)) missing.Add($"an example that calls .{name}(");

        Assert.True(missing.Count == 0, $"{verb.DeclaringType!.Name}.{name} lacks: {string.Join(", ", missing)}.");
    }

    /// <summary>The summary of <c>Ridgeline</c> and its code say the bandwidth is the mean of the groups' own Silverman
    /// bandwidths; the parameter's own documentation said it was Silverman's rule over all the observations, which on three
    /// months of daily highs is 5.13 where the mean of the groups' own is 1.04. The parameter is what IntelliSense shows at
    /// the call, so it must say what the code does.</summary>
    [Fact]
    public void TheRidgelineBandwidthParameter_SaysWhatTheCodeDoesWhenItIsLeftOut()
    {
        var shim = Assert.Single(MembersStartingWith($"M:{typeof(DistributionAxesExtensions).FullName}.Ridgeline("));
        var member = ResolveInheritedContent(shim);
        string text = TextOf(member.Elements("param").Single(param => (string?)param.Attribute("name") == "bandwidth"));

        Assert.Contains("mean of the groups", text, StringComparison.Ordinal);
        Assert.DoesNotContain("over all the observations", text, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(TypeNames))]
    public void EveryTypeTheVerbsIntroduce_IsDocumentedOnTheTypeAndOnEveryPublicProperty(string name)
    {
        var type = VerbTypes.Single(candidate => candidate.Name == name);

        var missing = new List<string>();
        if (TextOf(MembersStartingWith($"T:{type.FullName}").FirstOrDefault(member => (string?)member.Attribute("name") == $"T:{type.FullName}")?.Element("summary")).Length == 0)
        {
            missing.Add("the type's summary");
        }
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                     .Where(property => property.Name != "EqualityContract"))
        {
            var documented = MembersStartingWith($"P:{type.FullName}.{property.Name}").FirstOrDefault(member => (string?)member.Attribute("name") == $"P:{type.FullName}.{property.Name}");
            if (TextOf(documented?.Element("summary")).Length == 0) missing.Add($"property '{property.Name}'");
        }

        Assert.True(missing.Count == 0, $"{type.Name} lacks documentation for: {string.Join(", ", missing)}.");
    }
}
