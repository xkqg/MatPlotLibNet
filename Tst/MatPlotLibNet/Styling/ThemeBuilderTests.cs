// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>
/// Verifies <see cref="ThemeBuilder"/> constructs customised themes from a base without
/// mutating the source theme. Ships since v1.0 with no direct coverage; existing theme
/// tests target the pre-built themes, not the builder.
/// </summary>
public class ThemeBuilderTests
{
    [Fact]
    public void CreateFrom_ReturnsBuilderSeededFromBaseTheme()
    {
        var built = Theme.CreateFrom(Theme.Default).Build();

        Assert.Equal(Theme.Default.Background, built.Background);
        Assert.Equal(Theme.Default.ForegroundText, built.ForegroundText);
        Assert.Equal(Theme.Default.CycleColors, built.CycleColors);
    }

    [Fact]
    public void Build_DoesNotMutateBaseTheme()
    {
        var originalBg = Theme.Default.Background;
        _ = Theme.CreateFrom(Theme.Default)
            .WithBackground(new Color(1, 2, 3))
            .Build();

        Assert.Equal(originalBg, Theme.Default.Background);
    }

    [Fact]
    public void WithBackground_OverridesOnlyBackground()
    {
        var custom = new Color(245, 245, 245);
        var built = Theme.CreateFrom(Theme.Default).WithBackground(custom).Build();

        Assert.Equal(custom, built.Background);
        Assert.Equal(Theme.Default.ForegroundText, built.ForegroundText);
    }

    [Fact]
    public void WithForegroundText_OverridesOnlyForeground()
    {
        var custom = new Color(20, 30, 40);
        var built = Theme.CreateFrom(Theme.Default).WithForegroundText(custom).Build();

        Assert.Equal(custom, built.ForegroundText);
        Assert.Equal(Theme.Default.Background, built.Background);
    }

    [Fact]
    public void WithCycleColors_ReplacesEntireCycle()
    {
        Color[] palette = [Colors.Red, Colors.Green, Colors.Blue];
        var built = Theme.CreateFrom(Theme.Default).WithCycleColors(palette).Build();

        Assert.Equal(palette, built.CycleColors);
    }

    [Fact]
    public void WithCycleColors_CopiesInput_SoLaterMutationDoesNotAffectTheme()
    {
        Color[] palette = [Colors.Red, Colors.Green];
        var built = Theme.CreateFrom(Theme.Default).WithCycleColors(palette).Build();
        palette[0] = Colors.Black;

        Assert.Equal(Colors.Red, built.CycleColors[0]);
    }

    [Fact]
    public void WithGrid_TransformReceivesBaseGridAndReturnsModifiedCopy()
    {
        var built = Theme.CreateFrom(Theme.Default)
            .WithGrid(g => g with { Visible = !g.Visible })
            .Build();

        Assert.NotEqual(Theme.Default.DefaultGrid.Visible, built.DefaultGrid.Visible);
    }

    [Fact]
    public void WithFont_TransformReceivesBaseFontAndReturnsModifiedCopy()
    {
        var built = Theme.CreateFrom(Theme.Default)
            .WithFont(f => f with { Size = f.Size + 6 })
            .Build();

        Assert.Equal(Theme.Default.DefaultFont.Size + 6, built.DefaultFont.Size);
    }

    [Fact]
    public void Build_Name_IsPrefixedWithCustom()
    {
        var built = Theme.CreateFrom(Theme.Default).Build();
        Assert.StartsWith("custom-", built.Name);
    }

    [Fact]
    public void WithPropCycler_NullClearsToBaseCycleColors()
    {
        var built = Theme.CreateFrom(Theme.Default).WithPropCycler(null).Build();
        Assert.Null(built.PropCycler);
    }
}
