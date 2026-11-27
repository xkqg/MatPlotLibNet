// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Packaging;
using CoreColorMaps = MatPlotLibNet.Styling.ColorMaps.ColorMaps;

namespace MatPlotLibNet.Tests.Styling.ColorMapShelves;

/// <summary>The colour maps the library ships are written once: as a static property of the class that declares them, which puts the
/// map it makes on that class's shelf as it makes it. The registry names the seven classes and reads their shelves; it holds no list
/// of maps of its own, so a map that is declared is registered and a map that is registered is declared.</summary>
public class ColorMapShelfTests
{
    private static readonly (Type Class, ColorMapShelf Shelf)[] Classes =
    [
        (typeof(CoreColorMaps), CoreColorMaps.Shelf),
        (typeof(SequentialColorMaps), SequentialColorMaps.Shelf),
        (typeof(DivergingColorMaps), DivergingColorMaps.Shelf),
        (typeof(CyclicColorMaps), CyclicColorMaps.Shelf),
        (typeof(AdditionalColorMaps), AdditionalColorMaps.Shelf),
        (typeof(PerceptualColorMaps2), PerceptualColorMaps2.Shelf),
        (typeof(QualitativeColorMaps), QualitativeColorMaps.Shelf),
    ];

    private static IColorMap[] PropertiesOf(Type type) =>
        [.. type.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => typeof(IColorMap).IsAssignableFrom(property.PropertyType))
            .Select(property => (IColorMap)property.GetValue(null)!)];

    public static TheoryData<string> ClassNames { get; } = new(Classes.Select(c => c.Class.Name));

    [Theory]
    [MemberData(nameof(ClassNames))]
    public void TheShelfOfAClass_HoldsEveryMapItDeclares_InTheOrderItDeclaresThem(string name)
    {
        var (type, shelf) = Classes.Single(c => c.Class.Name == name);

        Assert.Equal(PropertiesOf(type), shelf.Maps);
    }

    [Fact]
    public void TheSevenShelves_HoldTheSeventyFourMapsOfTheLibrary()
    {
        Assert.Equal(74, Classes.Sum(c => c.Shelf.Maps.Count));
    }

    [Fact]
    public void TheRegistry_HoldsEveryMapOfTheShelves_AndTheReverseOfEach_ByTheMapItself()
    {
        foreach (var map in Classes.SelectMany(c => c.Shelf.Maps))
        {
            Assert.Same(map, ColorMapRegistry.Get(map.Name));
            Assert.Equal(map.Name + "_r", ColorMapRegistry.Get(map.Name + "_r")!.Name);
        }

        Assert.Equal(148, Classes.Sum(c => c.Shelf.Maps.Count) * 2);
    }

    [Fact]
    public void EveryMapOnAShelf_IsOneTheRegistryShips_AndItsReverseToo()
    {
        foreach (var map in Classes.SelectMany(c => c.Shelf.Maps))
        {
            Assert.True(ColorMapRegistry.IsShipped(map), map.Name);
            Assert.True(ColorMapRegistry.IsShipped(ColorMapRegistry.Get(map.Name + "_r")!), map.Name + "_r");
        }
    }

    [Fact]
    public void AMapAddedToAShelf_IsHandedBack_SoAPropertyReadsAsItsInitialiser()
    {
        var shelf = new ColorMapShelf();
        var map = new ListedColorMap("shelved", [Colors.Red]);

        Assert.Same(map, shelf.Add(map));
        Assert.Same(map, Assert.Single(shelf.Maps));
    }

    /// <summary>The registry's constructor names a class of maps, never a map: no line of it reads <c>Ship(Class.Map)</c>.</summary>
    [Fact]
    public void TheRegistrysSource_NamesNoMapOfItsOwn()
    {
        var registry = RepositoryTree.SourceFiles().Single(f => f.Path == "Src/MatPlotLibNet/Styling/ColorMaps/ColorMapRegistry.cs");
        var perMap = registry.Lines.Where(line => System.Text.RegularExpressions.Regex.IsMatch(line, @"\bShip\(\w+\.\w+\)")).ToArray();

        Assert.Empty(perMap);
    }

    /// <summary>The generated table index has an entry for every map that is made of a matplotlib table, and the shelves hold exactly
    /// those maps (made by <c>MatplotlibColorTables.Of</c>) next to the ones written by hand: no table without a map, no table map
    /// without a table.</summary>
    [Fact]
    public void EveryTableOfTheGeneratedData_IsTheMapOfAProperty()
    {
        var data = RepositoryTree.SourceFiles().Single(f => f.Path == "Src/MatPlotLibNet/Styling/ColorMaps/MatplotlibColorTables.Data.cs");
        var tables = System.Text.RegularExpressions.Regex.Matches(data.Text, "^\\s*\"(\\w+)\" => Evenly", System.Text.RegularExpressions.RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value).Order().ToArray();
        var made = Classes.SelectMany(c => c.Shelf.Maps).OfType<LinearColorMap>()
            .Where(map => tables.Contains(map.Name)).Select(map => map.Name).Order().ToArray();

        Assert.Equal(tables, made);
    }
}
