// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies title/label styling properties and builder overloads (sub-phase 2C).</summary>
public class TitleLabelTests
{
    [Fact]
    public void Axes_TitleStyle_DefaultsToNull()
    {
        var axes = new Axes();
        Assert.Null(axes.TitleStyle);
    }

    [Fact]
    public void Axes_TitleLoc_DefaultsToCenter()
    {
        var axes = new Axes();
        Assert.Equal(TitleLocation.Center, axes.TitleLoc);
    }

    [Fact]
    public void Axis_LabelStyle_DefaultsToNull()
    {
        var axis = new Axis();
        Assert.Null(axis.LabelStyle);
    }

    [Fact]
    public void TitleLocation_HasThreeValues()
    {
        var values = Enum.GetValues<TitleLocation>();
        Assert.Equal(3, values.Length);
        Assert.Contains(TitleLocation.Left, values);
        Assert.Contains(TitleLocation.Center, values);
        Assert.Contains(TitleLocation.Right, values);
    }

    [Fact]
    public void AxesBuilder_WithTitle_StyleOverload()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("My Title", ts => ts with { FontSize = 20 }))
            .Build();
        var axes = figure.SubPlots[0];
        Assert.Equal("My Title", axes.Title);
        Assert.NotNull(axes.TitleStyle);
        Assert.Equal(20, axes.TitleStyle!.FontSize);
    }

    [Fact]
    public void AxesBuilder_WithTitle_NoStyle_TitleStyleRemainsNull()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("My Title"))
            .Build();
        var axes = figure.SubPlots[0];
        Assert.Equal("My Title", axes.Title);
        Assert.Null(axes.TitleStyle);
    }

    [Fact]
    public void AxesBuilder_SetXLabel_StyleOverload()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetXLabel("Time", ts => ts with { FontSize = 14 }))
            .Build();
        var axes = figure.SubPlots[0];
        Assert.Equal("Time", axes.XAxis.Label);
        Assert.NotNull(axes.XAxis.LabelStyle);
        Assert.Equal(14, axes.XAxis.LabelStyle!.FontSize);
    }

    [Fact]
    public void AxesBuilder_SetYLabel_StyleOverload()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.SetYLabel("Value", ts => ts with { Color = Colors.Red }))
            .Build();
        var axes = figure.SubPlots[0];
        Assert.Equal("Value", axes.YAxis.Label);
        Assert.NotNull(axes.YAxis.LabelStyle);
        Assert.Equal(Colors.Red, axes.YAxis.LabelStyle!.Color);
    }

    [Fact]
    public void TextStyle_ApplyTo_TitleFont_OverridesSize()
    {
        var baseFont = new Font { Size = 17, Weight = FontWeight.Bold };
        var style = new TextStyle { FontSize = 22 };
        var result = style.ApplyTo(baseFont);
        Assert.Equal(22, result.Size);
        Assert.Equal(FontWeight.Bold, result.Weight);
    }

    [Fact]
    public void Axes_TitleLoc_CanBeSetToLeft()
    {
        var axes = new Axes { TitleLoc = TitleLocation.Left };
        Assert.Equal(TitleLocation.Left, axes.TitleLoc);
    }

    [Fact]
    public void Axes_TitleStyle_CanBeSet()
    {
        var style = new TextStyle { FontSize = 18, Color = Colors.Blue };
        var axes = new Axes { TitleStyle = style };
        Assert.NotNull(axes.TitleStyle);
        Assert.Equal(18, axes.TitleStyle!.FontSize);
    }
}
