// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="StyleSheetRegistry"/> lookup and registration.</summary>
public class StyleSheetRegistryTests
{
    /// <summary>A registered name gives its sheet; a name no sheet was registered under gives none.</summary>
    [Theory]
    [InlineData("default", true)]
    [InlineData("dark", true)]
    [InlineData("no-such-style-xyzzy-9999", false)]
    public void Get_GivesTheSheetOfARegisteredName_AndNoneElse(string name, bool found)
    {
        Assert.Equal(found, StyleSheetRegistry.Get(name) is not null);
    }

    [Fact]
    public void Get_CaseInsensitive()
    {
        var lower = StyleSheetRegistry.Get("seaborn");
        var upper = StyleSheetRegistry.Get("SEABORN");
        Assert.NotNull(lower);
        Assert.Same(lower, upper);
    }

    [Fact]
    public void Register_Custom_IsRetrievable()
    {
        var custom = new StyleSheet("custom-test-xyz", new Dictionary<string, object>());
        StyleSheetRegistry.Register("custom-test-xyz", custom);
        var retrieved = StyleSheetRegistry.Get("custom-test-xyz");
        Assert.Same(custom, retrieved);
    }

    [Fact]
    public void Names_ContainsBuiltIns()
    {
        var names = StyleSheetRegistry.Names.Select(n => n.ToLowerInvariant()).ToArray();
        Assert.Contains("default", names);
        Assert.Contains("dark", names);
    }
}
