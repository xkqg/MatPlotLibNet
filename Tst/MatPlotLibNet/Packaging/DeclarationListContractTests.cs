// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A list of declarations the clone detector finds that restates no rule holds only lines of one shape: a list of names, one
/// line per name, where nothing is worked out. One check reads every list, so a line of another shape slipped in among the names is
/// found in one place, whichever folder the list is in.</summary>
public class DeclarationListContractTests
{
    /// <summary>A line of code inside a type: not blank, not a comment, not a brace, and not the line that declares the type.</summary>
    private const string CodeInsideTheType = @"^(?!\s*(?:\{|\}|$))(?!.*\b(?:class|record struct|namespace)\b).+";

    /// <summary>Each row selects the lines of its list (a line of code inside the type), the shape every one of them has, and why the list stays.</summary>
    public static TheoryData<string, string, string, string> Declarations { get; } = new()
    {
        // the interaction folder
        { "Src/MatPlotLibNet/Interaction/HubMethodNames.cs", @"^\s*public const ",
            @"^\s*public const string \w+ = ""\w+"";$",
            "DECLARATION: one published constant per hub method, the name its clients send (the wire contract, a value each), nothing derived" },
        { "Src/MatPlotLibNet/Interaction/InteractionToolbar.cs", @"^\s*public const string ",
            @"^\s*public const string \w+\s*= ""\w+"";$",
            "DECLARATION: one published button id per tool, the id a control's button carries" },
        { "Src/MatPlotLibNet/Styling/RcParamKeys.cs", CodeInsideTheType,
            @"^\s*public const string \w+\s*= ""[\w.]+"";$",
            "DECLARATION: one published constant per rc parameter, matplotlib's own key" },
        { "Src/MatPlotLibNet/Interaction/RectangleZoomState.cs", CodeInsideTheType,
            @"^\s*(?:double \w+, double \w+,|int AxesIndex\);)$",
            "DECLARATION: the published overlay state of the zoom rectangle; the brush's state has the same fields and is another published type (both are data a native control draws, neither is dead)" },
        { "Src/MatPlotLibNet/Interaction/BrushSelectState.cs", CodeInsideTheType,
            @"^\s*(?:double \w+, double \w+,|int AxesIndex\);)$",
            "DECLARATION: the published overlay state of the brush (see RectangleZoomState)" },
        // the theme files
        { "Src/MatPlotLibNet/Styling/Theme.cs", @"^\s*public static Theme \w+ \{ get; \} =",
            @"^\s*public static Theme \w+ \{ get; \} = (?:new\($|(?:Default|Dark)\.Preset\(""[\w-]+""\)|Themes\.MatplotlibThemeFactory\.Create(?:Classic|V2)\(\);$)",
            "DECLARATION: one published property per theme: a root theme (RootThemes), a preset holding its base, its name and the values that differ from the base, or a matplotlib theme made by its factory" },
        { "Src/MatPlotLibNet/Styling/Looks/ThemeLooks.cs", @"^\s*public \w+Look\??\s",
            @"^\s*public \w+Look\?? \w+ \{ get; init; \}( = \w+Look\.Library;)?$",
            "DECLARATION: one look per concern; WriteKeys and ReadKeys name each once more (C# has no way to walk a record's properties without reflection), held to the properties by ThemeKeysContractTests (every look owns its keys and reads back alone)" },
        { "Src/MatPlotLibNet/Styling/StyleSheetRegistry.cs", @"^\s*Register\(",
            @"^\s*Register\(StyleSheet\.\w+\.Name,\s*StyleSheet\.\w+\);$",
            "DECLARATION: the published registry of style sheets, a facade whose every member forwards to NamedTable (the one table of names), as InterpolationRegistry is for engines; a static class cannot share a base" },
        { "Src/MatPlotLibNet/Styling/Themes/MatplotlibThemeFactory.cs", @"^\s*private static readonly double (?:V2|Classic)",
            @"^\s*private static readonly double (?:V2|Classic)\w+ = [\d.]+\.PointsInPixels;$",
            "DATA: matplotlib's printed rcParams of each style, one constant per value" },
    };

    [Theory]
    [MemberData(nameof(Declarations))]
    public void ADeclarationList_HoldsOnlyLinesOfItsShape(string path, string selector, string shape, string reason)
    {
        var file = RepositoryTree.SourceFiles().Single(f => f.Path == path);
        var lines = file.CodeLines.Where(line => Regex.IsMatch(line, selector)).ToList();

        Assert.True(lines.Count >= 2, $"{path} ({reason}): no line of the block is left");
        Assert.True(lines.All(line => Regex.IsMatch(line, shape)),
            $"{path} ({reason}): a line of the block is not of its shape:\n" + string.Join('\n', lines.Where(line => !Regex.IsMatch(line, shape))));
    }
}
