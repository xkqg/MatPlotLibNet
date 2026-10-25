// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A verb runs inside <c>AddSubPlot</c>, before the theme is known to anything that draws: the builder holds no
/// theme, the figure takes it at <c>Build</c>, and a caller can still set it on the built figure or on a figure read back from
/// JSON. A verb that fixes a colour reads it when it runs, so a theme set later must not change what any part of a composed
/// chart does beyond what the theme itself decides. Every shape is drawn under every theme the library ships, with and without
/// a missing value, set four ways, and all four must draw the same picture: the theme given to the builder after the verb,
/// the theme set on the built figure, a figure written to JSON under the theme and read back, and a theme set on a figure read
/// back from JSON that was written without one.</summary>
public class ComposedChartThemeTests
{
    private readonly record struct NamedTheme(string Name, Theme Theme);

    private static readonly NamedTheme[] Shipped =
        [.. typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(Theme))
            .Select(property => new NamedTheme(property.Name, (Theme)property.GetValue(null)!))];

    /// <summary>The shapes whose JSON carries a grid the axes changed over the theme's grid: the dot plot's guide lines. That
    /// grid is code, so it is written as the grid it draws under the theme in force when the JSON is written, and a theme set
    /// on the figure read back does not restyle it (the roadmap row "A grid change that follows the theme"). Measured: 20 of
    /// the 30 shipped themes draw such a dot plot differently in that last route. The other three routes hold for it too.</summary>
    private static readonly HashSet<string> ShapesWithAFrozenGrid = ["dot-plot"];

    /// <summary>Liveness: the reading finds the shipped themes, so the Theory below cannot go green by trying none.</summary>
    [Fact]
    public void TheShippedThemesAreFound() =>
        Assert.True(Shipped.Length >= 30, $"expected the shipped themes, found {Shipped.Length}");

    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void ATheme_SetAfterTheVerb_DrawsTheSamePictureHoweverItIsSet(string name)
    {
        var shape = ComposedChartShapes.Named(name);
        var differences = new List<string>();

        foreach (bool missing in new[] { false, true })
        {
            foreach (var named in Shipped)
            {
                string where = $"{named.Name}{(missing ? " with a missing value" : "")}";
                string inTheBuilder = shape.Build(missing).WithTheme(named.Theme).Build().ToSvg();

                var onTheFigure = shape.Build(missing).Build();
                onTheFigure.Theme = named.Theme;
                if (onTheFigure.ToSvg() != inTheBuilder) differences.Add($"{where}: set on the built figure");

                string writtenUnderIt = ChartSerializerDefault.Current.FromJson(shape.Build(missing).WithTheme(named.Theme).Build().ToJson()).ToSvg();
                if (writtenUnderIt != inTheBuilder) differences.Add($"{where}: written to JSON under it and read back");

                if (!ShapesWithAFrozenGrid.Contains(name))
                {
                    var afterTheRead = ChartSerializerDefault.Current.FromJson(shape.Build(missing).Build().ToJson());
                    afterTheRead.Theme = named.Theme;
                    if (afterTheRead.ToSvg() != inTheBuilder) differences.Add($"{where}: set on a figure read back from JSON");
                }
            }
        }

        Assert.True(differences.Count == 0,
            $"{name}: {differences.Count} way(s) of setting a theme draw another picture than setting it in the builder: {string.Join("; ", differences.Take(6))}");
    }
}
