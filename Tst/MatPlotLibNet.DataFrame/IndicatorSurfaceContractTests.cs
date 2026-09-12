// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MatPlotLibNet.Tests.DataFrame;

/// <summary>
/// The package description is the text nuget.org prints on the package page, and it counts the indicators this
/// package exposes. Nothing read that number: the core's indicator count is pinned against the core assembly,
/// and this package has a surface of its own over it, so the core's pin can never catch a stale number here.
/// It was stale — the description said 52 where the class carries 51 methods.
/// </summary>
public class IndicatorSurfaceContractTests
{
    /// <summary>An indicator here is one thing a caller can CALL: a public extension method on a DataFrame.
    /// Counting methods rather than concepts is what can be measured without judgment — <c>Adx</c> and
    /// <c>AdxFull</c> compute the same indicator and are two calls, and the number claims calls.</summary>
    private static string[] IndicatorMethods() =>
        [.. typeof(DataFrameIndicatorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)];

    private static string ProjectDescription()
    {
        string root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "MatPlotLibNet.slnx")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("no repository root above " + AppContext.BaseDirectory);
        }

        var csproj = XDocument.Load(Path.Combine(root, "Src", "MatPlotLibNet.DataFrame", "MatPlotLibNet.DataFrame.csproj"));
        return csproj.Descendants("Description").FirstOrDefault()?.Value
            ?? throw new InvalidOperationException("MatPlotLibNet.DataFrame.csproj carries no <Description>");
    }

    [Fact]
    public void ThePackageDescription_CountsTheIndicatorsThisPackageActuallyExposes()
    {
        var counted = Regex.Match(ProjectDescription(), @"(\d+) financial / signal-processing indicators");

        Assert.True(counted.Success,
            "the description no longer counts the indicators in the phrasing this test pins; update both together");

        Assert.True(int.Parse(counted.Groups[1].Value, CultureInfo.InvariantCulture) == IndicatorMethods().Length,
            $"the description says \"{counted.Value}\"; the package exposes {IndicatorMethods().Length}: "
            + string.Join(", ", IndicatorMethods()));
    }
}
