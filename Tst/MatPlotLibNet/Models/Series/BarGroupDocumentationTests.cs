// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The documentation of <see cref="BarGroup"/> names every public verb that takes one. The type is the named-values
/// type of the grouped bar chart and of the composed verbs, so a reader who lands on it must be able to find them all, and a
/// verb that starts taking a group must be added to its summary: this reads the public API for the verbs and the generated
/// XML documentation for the names, so the next verb turns it red without anyone having to remember.</summary>
public class BarGroupDocumentationTests
{
    /// <summary>A public method that takes a group: the type that declares it and its name (an overload counts once).</summary>
    private readonly record struct Verb(string DeclaringType, string Name);

    private static bool TakesGroups(Type type) =>
        type == typeof(BarGroup)
        || (type.IsArray && type.GetElementType() == typeof(BarGroup))
        || (type.IsGenericType && type.GetGenericArguments().Contains(typeof(BarGroup)));

    /// <summary>A C# 14 <c>extension(T){ }</c> member's reflected <see cref="MethodInfo.DeclaringType"/> is a
    /// compiler-synthesized nested skeleton type (named <c>&lt;G&gt;$&lt;hash&gt;</c>), not the class the block is
    /// written in. The outer, user-written type — the one a doc comment's <c>&lt;see cref&gt;</c> names — is one
    /// <see cref="Type.DeclaringType"/> up.</summary>
    private static Type UserWrittenDeclaringType(MethodInfo method) =>
        method.DeclaringType!.IsNested ? method.DeclaringType.DeclaringType! : method.DeclaringType;

    /// <summary>Every public method of the library that takes a <see cref="BarGroup"/>, alone, in an array or in a list. The
    /// record's own members (its equality) take one too, and are not verbs.</summary>
    private static HashSet<Verb> VerbsThatTakeAGroup() =>
        [.. typeof(AxesBuilder).Assembly.GetExportedTypes()
            .Where(type => type != typeof(BarGroup))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetParameters().Any(parameter => TakesGroups(parameter.ParameterType)))
            .Select(method => new Verb(UserWrittenDeclaringType(method).FullName!, method.Name))];

    /// <summary>The methods the summary of <see cref="BarGroup"/> refers to, as the XML documentation of the library has them.</summary>
    private static HashSet<Verb> VerbsTheSummaryNames()
    {
        var documentation = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MatPlotLibNet.xml"));
        var summary = documentation.Descendants("member")
            .Single(member => (string?)member.Attribute("name") == "T:MatPlotLibNet.Models.Series.BarGroup")
            .Element("summary")!;
        return [.. summary.Descendants("see")
            .Select(see => Regex.Match((string?)see.Attribute("cref") ?? "", @"^M:(?<type>[\w.]+)\.(?<name>\w+)\("))
            .Where(match => match.Success)
            .Select(match => new Verb(match.Groups["type"].Value, match.Groups["name"].Value))];
    }

    /// <summary>Liveness: the reading finds the verbs the pin is about, so it cannot go green by finding nothing.</summary>
    [Fact]
    public void TheVerbsThatTakeAGroup_AreFound()
    {
        var verbs = VerbsThatTakeAGroup();

        Assert.Contains(new Verb("MatPlotLibNet.AxesBuilder", "GroupedBar"), verbs);
        Assert.Contains(new Verb("MatPlotLibNet.CategoricalAxesExtensions", "Dumbbell"), verbs);
        Assert.Contains(new Verb("MatPlotLibNet.XYAxesExtensions", "StreamGraph"), verbs);
        Assert.True(verbs.Count >= 8, $"expected the grouped bar chart and the seven composed verbs, found {verbs.Count}: {string.Join(", ", verbs)}");
    }

    /// <summary>The summary names every verb that takes a group, so none is left for a reader to find by luck.</summary>
    [Fact]
    public void Summary_NamesEveryVerbThatTakesAGroup()
    {
        var missing = VerbsThatTakeAGroup().Except(VerbsTheSummaryNames()).OrderBy(v => v.DeclaringType).ThenBy(v => v.Name).ToArray();

        Assert.True(missing.Length == 0, "BarGroup's summary does not name: " + string.Join(", ", missing.Select(v => $"{v.DeclaringType}.{v.Name}")));
    }

    /// <summary>The summary names nothing that does not take a group any more, so a verb that changes its input does not leave
    /// a stale name behind.</summary>
    [Fact]
    public void Summary_NamesNothingThatTakesNoGroup()
    {
        var stale = VerbsTheSummaryNames().Except(VerbsThatTakeAGroup()).OrderBy(v => v.DeclaringType).ThenBy(v => v.Name).ToArray();

        Assert.True(stale.Length == 0, "BarGroup's summary names verbs that take no group: " + string.Join(", ", stale.Select(v => $"{v.DeclaringType}.{v.Name}")));
    }
}
