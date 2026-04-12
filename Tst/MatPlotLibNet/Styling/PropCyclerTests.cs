// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="PropCycler"/> modulo indexing, multi-property lockstep cycling,
/// builder fluent API, and Theme integration.</summary>
public class PropCyclerTests
{
    [Fact]
    public void PropCycler_SingleColor_IndexZero_ReturnsThatColor()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).Build();
        Assert.Equal(Colors.Red, cycler[0].Color);
    }

    [Fact]
    public void PropCycler_ModuloWraps()
    {
        var cycler = new PropCyclerBuilder()
            .WithColors(Colors.Red, Colors.Green, Colors.Blue)
            .Build();

        Assert.Equal(cycler[0].Color, cycler[3].Color);
        Assert.Equal(cycler[1].Color, cycler[4].Color);
        Assert.Equal(cycler[2].Color, cycler[5].Color);
    }

    [Fact]
    public void PropCycler_MultiProperty_Lockstep()
    {
        var cycler = new PropCyclerBuilder()
            .WithColors(Colors.Red, Colors.Blue)
            .WithLineStyles(LineStyle.Solid, LineStyle.Dashed, LineStyle.Dotted)
            .Build();

        Assert.Equal(Colors.Red,       cycler[0].Color);
        Assert.Equal(LineStyle.Solid,  cycler[0].LineStyle);

        Assert.Equal(Colors.Blue,      cycler[1].Color);
        Assert.Equal(LineStyle.Dashed, cycler[1].LineStyle);

        // Colors wrap at 2, line styles continue to index 2
        Assert.Equal(Colors.Red,       cycler[2].Color);
        Assert.Equal(LineStyle.Dotted, cycler[2].LineStyle);
    }

    [Fact]
    public void Builder_AllProperties_Builds()
    {
        var cycler = new PropCyclerBuilder()
            .WithColors(Colors.Red)
            .WithLineStyles(LineStyle.Dashed)
            .WithMarkerStyles(MarkerStyle.Circle)
            .WithLineWidths(2.0)
            .Build();

        var props = cycler[0];
        Assert.Equal(Colors.Red,         props.Color);
        Assert.Equal(LineStyle.Dashed,   props.LineStyle);
        Assert.Equal(MarkerStyle.Circle, props.MarkerStyle);
        Assert.Equal(2.0,                props.LineWidth);
    }

    [Fact]
    public void PropCycler_NoLineStyles_DefaultsToSolid()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).Build();
        Assert.Equal(LineStyle.Solid, cycler[0].LineStyle);
    }

    [Fact]
    public void PropCycler_NoMarkerStyles_DefaultsToNone()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).Build();
        Assert.Equal(MarkerStyle.None, cycler[0].MarkerStyle);
    }

    [Fact]
    public void PropCycler_NoLineWidths_DefaultsTo1Point5()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).Build();
        Assert.Equal(1.5, cycler[0].LineWidth);
    }

    [Fact]
    public void PropCycler_EmptyColors_DefaultsToTab10Blue()
    {
        var cycler = new PropCyclerBuilder().Build();
        Assert.Equal(Colors.Tab10Blue, cycler[0].Color);
    }

    [Fact]
    public void PropCycler_Length_IsLcmOfPropertyArrayLengths()
    {
        // 3 colors, 2 line styles → LCM(3, 2) = 6
        var cycler = new PropCyclerBuilder()
            .WithColors(Colors.Red, Colors.Green, Colors.Blue)
            .WithLineStyles(LineStyle.Solid, LineStyle.Dashed)
            .Build();
        Assert.Equal(6, cycler.Length);
    }

    [Fact]
    public void PropCycler_SingleLength_IsOne()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).Build();
        Assert.Equal(1, cycler.Length);
    }

    [Fact]
    public void Builder_Chaining_ReturnsSameBuilder()
    {
        var builder = new PropCyclerBuilder();
        Assert.Same(builder, builder.WithColors(Colors.Red));
        Assert.Same(builder, builder.WithLineStyles(LineStyle.Solid));
        Assert.Same(builder, builder.WithMarkerStyles(MarkerStyle.Circle));
        Assert.Same(builder, builder.WithLineWidths(1.5));
    }

    [Fact]
    public void Theme_Default_PropCyclerIsNull()
    {
        Assert.Null(Theme.Default.PropCycler);
    }

    [Fact]
    public void Theme_WithPropCycler_StoresIt()
    {
        var cycler = new PropCyclerBuilder()
            .WithColors(Colors.Red, Colors.Blue)
            .Build();
        var theme = Theme.CreateFrom(Theme.Default)
            .WithPropCycler(cycler)
            .Build();

        Assert.NotNull(theme.PropCycler);
        Assert.Equal(Colors.Red,  theme.PropCycler![0].Color);
        Assert.Equal(Colors.Blue, theme.PropCycler![1].Color);
    }

    [Fact]
    public void Theme_WithPropCycler_Null_ClearsIt()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).Build();
        var themeWithCycler  = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var themeNoCycler    = Theme.CreateFrom(themeWithCycler).WithPropCycler(null).Build();

        Assert.Null(themeNoCycler.PropCycler);
    }

    [Fact]
    public void Theme_Null_PropCycler_CycleColors_StillPresent()
    {
        // Backward-compat guarantee: CycleColors is the fallback when PropCycler is null
        Assert.Null(Theme.Default.PropCycler);
        Assert.NotEmpty(Theme.Default.CycleColors);
    }
}
