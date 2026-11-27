// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Playground;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Samples;

/// <summary>The themes the Playground offers are named once, by the property of <see cref="Theme"/> that holds each, and that name is
/// both the picker's label and the <c>Theme.X</c> of the snippet a reader copies.</summary>
public class ThemeChoicesTests
{
    /// <summary>A label that is not the property's name would put a theme in the snippet that does not compile.</summary>
    [Fact]
    public void EveryChoiceIsLabelledByThePropertyThatHoldsItsTheme()
    {
        var properties = typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(Theme))
            .ToDictionary(property => property.Name, property => (Theme)property.GetValue(null)!);

        Assert.All(ThemeChoices.All, choice =>
        {
            Assert.True(properties.TryGetValue(choice.Label, out var held), $"Theme has no property {choice.Label}");
            Assert.Same(held, choice.Theme);
        });
    }

    [Fact]
    public void All_HoldsTheLightThenTheDarkThenTheAccessibleChoices_EachOnce()
    {
        Assert.Equal([.. ThemeChoices.Light, .. ThemeChoices.Dark, .. ThemeChoices.Accessibility], ThemeChoices.All);
        Assert.Equal(ThemeChoices.All.Count, ThemeChoices.All.Select(choice => choice.Label).Distinct().Count());
    }

    [Fact]
    public void ATheme_ThatIsNotAChoice_IsWrittenAsTheDefault()
    {
        Assert.Equal("Default", ThemeChoices.CodeNameOf(Theme.CreateFrom(Theme.Dark).WithName("mine").Build()));
    }

    [Fact]
    public void AChoice_IsWrittenByItsLabel()
    {
        Assert.Equal("OneDark", ThemeChoices.CodeNameOf(Theme.OneDark));
    }
}
