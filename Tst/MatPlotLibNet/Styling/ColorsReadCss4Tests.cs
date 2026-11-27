// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A colour that <see cref="Css4Colors"/> names and <see cref="Colors"/> names too has its value written once, in
/// <c>Css4Colors</c>; <c>Colors</c> reads it. The one name the two spell differently is <c>DarkGray</c>: the library's dark grey is
/// #404040 where CSS4's is #A9A9A9, a real difference of value, so it stays a value of its own.</summary>
public class ColorsReadCss4Tests
{
    private static readonly PropertyInfo[] Named = [.. typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(property => property.PropertyType == typeof(Color))];

    private static PropertyInfo? Css4(PropertyInfo named) =>
        typeof(Css4Colors).GetProperty(named.Name, BindingFlags.Public | BindingFlags.Static);

    [Fact]
    public void TheNamesBothClassesHave_AreTheSameColour_ExceptDarkGray()
    {
        var differing = Named
            .Where(named => Css4(named) is { } css && (Color)named.GetValue(null)! != (Color)css.GetValue(null)!)
            .Select(named => named.Name)
            .ToArray();

        Assert.Equal(["DarkGray"], differing);
    }

    [Fact]
    public void ANameCss4Has_WithTheSameValue_IsReadFromCss4_NotWrittenAgain()
    {
        var colors = RepositoryTree.SourceFiles().Single(f => f.Path == "Src/MatPlotLibNet/Styling/Colors.cs");
        var lines = colors.Lines;
        var literals = new List<string>();

        foreach (var named in Named)
        {
            if (Css4(named) is not { } css || (Color)named.GetValue(null)! != (Color)css.GetValue(null)!)
            {
                continue;
            }

            var declaration = lines.Single(line => Regex.IsMatch(line, $@"public static Color {named.Name}\s*=>"));
            if (!declaration.Contains($"Css4Colors.{named.Name}", StringComparison.Ordinal))
            {
                literals.Add(declaration.Trim());
            }
        }

        Assert.True(literals.Count == 0, "Written again, not read from Css4Colors:\n" + string.Join('\n', literals));
    }

    /// <summary>The eleven basic names that were written as RGB literals beside the same value in <c>Css4Colors</c>.</summary>
    [Theory]
    [InlineData("Red", 255, 0, 0)]
    [InlineData("Green", 0, 128, 0)]
    [InlineData("Blue", 0, 0, 255)]
    [InlineData("White", 255, 255, 255)]
    [InlineData("Black", 0, 0, 0)]
    [InlineData("Yellow", 255, 255, 0)]
    [InlineData("Cyan", 0, 255, 255)]
    [InlineData("Magenta", 255, 0, 255)]
    [InlineData("Orange", 255, 165, 0)]
    [InlineData("Gray", 128, 128, 128)]
    [InlineData("LightGray", 211, 211, 211)]
    public void TheBasicColours_KeepTheirValue(string name, int r, int g, int b)
    {
        var colour = (Color)typeof(Colors).GetProperty(name)!.GetValue(null)!;

        Assert.Equal(new Color((byte)r, (byte)g, (byte)b), colour);
        Assert.Equal(colour, (Color)typeof(Css4Colors).GetProperty(name)!.GetValue(null)!);
    }
}
