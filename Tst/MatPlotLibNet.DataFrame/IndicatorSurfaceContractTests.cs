// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Indicators;

namespace MatPlotLibNet.Tests.DataFrame;

/// <summary>What the package adds to the library's indicators is the reading of named columns, one call per shape of bars: an
/// indicator is made from its own class (<c>new Atr(df.Hlc("high", "low", "close"))</c>), so its parameters, its defaults, the shape
/// it reads and its documentation are stated once, in that class. The package description, the text nuget.org prints, names the
/// shapes the package exposes.</summary>
public class IndicatorSurfaceContractTests
{
    private static MethodInfo[] Shapes() =>
        typeof(DataFrameIndicatorExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .ToArray();

    private static string ProjectDescription()
    {
        var csproj = XDocument.Load(Path.Combine(RepositoryRoot.Folder, "Src", "MatPlotLibNet.DataFrame", "MatPlotLibNet.DataFrame.csproj"));
        return csproj.Descendants("Description").FirstOrDefault()?.Value
            ?? throw new InvalidOperationException("MatPlotLibNet.DataFrame.csproj carries no <Description>");
    }

    [Fact]
    public void ThePackage_StatesNothingPerIndicator_OnlyTheBarsOfEachShape()
    {
        var perIndicator = Shapes().Where(m => m.ReturnType != typeof(OhlcvBars)).Select(m => m.Name).Distinct().ToList();

        Assert.True(perIndicator.Count == 0,
            "a call of the DataFrame package that is an indicator of its own; make the indicator from its class over the bars of its "
            + "shape: " + string.Join(", ", perIndicator));
        Assert.Equal(["Cv", "Hl", "Hlc", "Hlcv", "Hlv", "Ohlc"], Shapes().Select(m => m.Name).Distinct().Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ThePackageDescription_NamesEveryShapeThePackageExposes()
    {
        string description = ProjectDescription();
        var missing = Shapes().Select(m => m.Name).Distinct()
            .Where(name => !Regex.IsMatch(description, $@"\b{name}\b"))
            .ToList();

        Assert.True(missing.Count == 0, $"the description does not name: {string.Join(", ", missing)}");
    }
}
