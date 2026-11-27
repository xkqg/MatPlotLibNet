// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies title/label styling properties and builder overloads (sub-phase 2C).</summary>
public class TitleLabelTests
{
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

    public static TheoryData<Action<AxesBuilder>, Func<Axes, Axis>, string, Func<TextStyle, object?>, object> LabelStyleOverloads => new()
    {
        { ax => ax.SetXLabel("Time", ts => ts with { FontSize = 14 }), axes => axes.XAxis, "Time", style => style.FontSize, 14.0 },
        { ax => ax.SetYLabel("Value", ts => ts with { Color = Colors.Red }), axes => axes.YAxis, "Value", style => style.Color, Colors.Red },
    };

    [Theory]
    [MemberData(nameof(LabelStyleOverloads))]
    public void AxesBuilder_SetALabel_StyleOverload(Action<AxesBuilder> setLabel, Func<Axes, Axis> axisOf, string label, Func<TextStyle, object?> read, object expected)
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, setLabel)
            .Build();
        var axis = axisOf(figure.SubPlots[0]);
        Assert.Equal(label, axis.Label);
        Assert.NotNull(axis.LabelStyle);
        Assert.Equal(expected, read(axis.LabelStyle!));
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
}
