// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The one place the verb families are found: the tests that need them ask <see cref="VerbFamilies"/>, so a family
/// added tomorrow joins every check without an edit in three files.</summary>
public class VerbFamiliesTests
{
    [Fact]
    public void TheFamilies_AreEveryPublicClassThatHoldsVerbsOfTheBuilder()
    {
        Assert.Equal(
            ["CategoricalAxesExtensions", "DistributionAxesExtensions", "GridAxesExtensions", "PriceAxesExtensions", "SignalAxesExtensions", "XYAxesExtensions"],
            VerbFamilies.All.Select(family => family.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TheVerbs_AreOnTheBuilder_AndTheirCountIsThePinnedOne()
    {
        Assert.Equal(38, VerbFamilies.AllVerbs.Length);
        Assert.All(VerbFamilies.AllVerbs, verb => Assert.Equal(typeof(AxesBuilder), verb.GetParameters()[0].ParameterType));
    }

    [Fact]
    public void TheVerbsOfAFamily_AreItsPublicExtensionMethods_InOrdinalOrder()
    {
        var names = VerbFamilies.VerbsOf(typeof(GridAxesExtensions)).Select(verb => verb.Name).ToArray();

        Assert.Contains("CalendarHeatmap", names);
        Assert.Contains("Spy", names);
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
    }

    [Fact]
    public void TheTypesTheVerbsIntroduce_AreTheirPinnedCount_AndIncludeThePartsOfEveryFamily()
    {
        var names = VerbFamilies.TypesTheVerbsIntroduce().Select(type => type.Name).ToArray();

        Assert.Equal(23, names.Length);
        Assert.Contains("DumbbellParts", names);
        Assert.Contains("HorizonParts", names);
        Assert.Contains("StairsParts", names);
        Assert.Contains("CorrelationParts", names);
        Assert.Contains("SpectralOptions", names);
        Assert.Contains("SpectrumOptions", names);
        Assert.Contains("CorrelationOptions", names);
        Assert.Contains("BarGroup", names);
    }
}
