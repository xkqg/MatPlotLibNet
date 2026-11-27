// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Xml.Linq;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The XML documentation of a public member is what IntelliSense shows a caller and what the API reference is built from, and a caller
/// cannot reach a type or a member the packages keep internal: a reference to one names what the reader can neither call nor look up.
/// This reads the documentation file the build wrote beside each package, where the compiler has resolved every reference to the member
/// it names, and holds the documentation of every public or protected member to references the packages publish. A constant is the one
/// exception: a reference to it names the one place a value is written (a resting value, a default), which the documentation would
/// otherwise write a second time.
/// </summary>
public sealed class PublicDocumentationTests
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>How many documented members the files hold at least: a reader that finds fewer reads the wrong files. Measured, not chosen.</summary>
    private const int FewestDocumented = 17_000;

    private static readonly Lazy<IReadOnlyDictionary<string, Type>> TypesById = new(() =>
        PackageSurface.Loaded.Types.GroupBy(IdOf, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal));

    [Fact]
    public void TheDocumentationOfAPublicMember_NamesNothingThePackagesKeepInternal()
    {
        var documented = PackageSurface.Loaded.Assemblies
            .Select(assembly => Path.ChangeExtension(assembly.Location, ".xml"))
            .Where(File.Exists)
            .SelectMany(file => XDocument.Load(file).Descendants("member"))
            .ToList();

        var findings = documented
            .Where(member => IsPublished((string?)member.Attribute("name") ?? string.Empty, asTarget: false))
            .SelectMany(member => member.Descendants()
                .Select(element => (string?)element.Attribute("cref"))
                .OfType<string>()
                .Where(target => !IsPublished(target, asTarget: true))
                .Distinct(StringComparer.Ordinal)
                .Select(target => $"{(string?)member.Attribute("name")} names {target}"))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(documented.Count >= FewestDocumented, $"the documentation files hold {documented.Count} members, fewer than the {FewestDocumented} measured");
        Assert.True(findings.Count == 0, $"{findings.Count} references to what the packages keep internal:\n" + string.Join('\n', findings));
    }

    [Theory]
    [InlineData("T:MatPlotLibNet.Plt", true)]
    [InlineData("M:MatPlotLibNet.Plt.Create", true)]
    [InlineData("P:MatPlotLibNet.Models.Axis.Label", true)]
    [InlineData("T:MatPlotLibNet.Rendering.ChartRenderer.FigureFrame", false)]
    [InlineData("M:MatPlotLibNet.Models.Axis.MajorTicksUnder(MatPlotLibNet.Styling.Theme)", false)]
    [InlineData("F:MatPlotLibNet.Models.Figure.ReferenceDpi", true)]
    [InlineData("F:MatPlotLibNet.Models.Axes._xSecondarySeries", false)]
    [InlineData("M:MatPlotLibNet.Rendering.DepthQueue3D.Add(System.Double,System.Int32,System.Int32)", false)]
    [InlineData("T:System.String", true)]
    [InlineData("!:NotResolved", true)]
    public void AReference_IsPublishedWhenACallerCanReachIt(string id, bool published) => Assert.Equal(published, IsPublished(id, asTarget: true));

    /// <summary>An internal constant is a target a public member's documentation may name, and is no public member itself: its own
    /// documentation is not held to the rule.</summary>
    [Fact]
    public void AnInternalConstant_IsATargetAndNoPublishedMember()
    {
        Assert.True(IsPublished("F:MatPlotLibNet.Models.Figure.ReferenceDpi", asTarget: true));
        Assert.False(IsPublished("F:MatPlotLibNet.Models.Figure.ReferenceDpi", asTarget: false));
    }

    // Whether a documentation id names what a caller of the packages can reach: a type outside the packages, a public type, or a public or
    // protected member of one; as the target of a reference, a constant too. An id the compiler could not resolve (`!:`) is the compiler's
    // to report.
    private static bool IsPublished(string id, bool asTarget)
    {
        if (id.Length < 2 || id[1] != ':' || id[0] == '!')
        {
            return true;
        }

        string kind = id[..1];
        string path = id[2..].Split('(')[0];

        if (kind == "T" || kind == "N")
        {
            return !TypesById.Value.TryGetValue(path, out var type) || type.IsVisible;
        }

        int dot = path.LastIndexOf('.');
        if (dot < 0 || !TypesById.Value.TryGetValue(path[..dot], out var owner))
        {
            return true;
        }

        string[] generic = path[(dot + 1)..].Replace('#', '.').Split("``", StringSplitOptions.None);
        string name = generic[0];
        int? parameters = kind == "M" ? ParameterCountOf(id) : null;
        int arity = generic.Length > 1 ? int.Parse(generic[1], System.Globalization.CultureInfo.InvariantCulture) : 0;
        var members = owner.GetMembers(Declared)
            .Where(member => NameOf(member) == name || member.Name.EndsWith("." + name, StringComparison.Ordinal))
            .Where(member => member is not MethodBase method || parameters is null
                || (method.GetParameters().Length == parameters && (method.IsGenericMethod ? method.GetGenericArguments().Length : 0) == arity))
            .ToArray();

        // A constant is a value, and the reference is to the one place it is written: the documentation names the owner of the value
        // instead of writing the number a second time, which would drift from it (a resting value, a default, a precision).
        if (asTarget && kind == "F" && owner.GetField(name, Declared) is { } field && IsConstant(field))
        {
            return true;
        }

        return owner.IsVisible && (members.Length == 0 || members.Any(IsReachable));
    }

    // The number of parameters a method's documentation id lists: the arguments between its parentheses, cut at the commas outside the
    // braces of a generic type and the brackets of an array.
    private static int ParameterCountOf(string id)
    {
        int open = id.IndexOf('(', StringComparison.Ordinal);
        if (open < 0)
        {
            return 0;
        }

        int depth = 0, count = 1;
        foreach (char letter in id[(open + 1)..id.LastIndexOf(')')])
        {
            depth += letter is '{' or '[' ? 1 : letter is '}' or ']' ? -1 : 0;
            count += letter == ',' && depth == 0 ? 1 : 0;
        }

        return count;
    }

    private static bool IsConstant(MemberInfo member) => member is FieldInfo { IsLiteral: true } or FieldInfo { IsStatic: true, IsInitOnly: true };

    private static string NameOf(MemberInfo member) => member is ConstructorInfo ? (member.Name == ".cctor" ? ".cctor" : ".ctor") : member.Name;

    private static bool IsReachable(MemberInfo member) => member switch
    {
        MethodBase method => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly,
        FieldInfo field => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly,
        PropertyInfo property => property.GetAccessors(nonPublic: true).Any(IsReachable),
        EventInfo @event => @event.AddMethod is { } add && IsReachable(add),
        Type nested => nested.IsVisible,
        _ => true,
    };

    // The documentation id of a type: its namespace and its names, a nested type after its outer type with a dot, the arity kept.
    private static string IdOf(Type type) => (type.FullName ?? type.Name).Replace('+', '.');
}
