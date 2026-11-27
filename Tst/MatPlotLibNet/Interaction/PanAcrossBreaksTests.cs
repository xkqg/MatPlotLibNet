// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>A pan is a shift in the space the axis is drawn in, and on an axis with breaks that space has the breaks cut out
/// of it: the pointer measures its move there (<see cref="PanModifier"/>), so <see cref="PanEvent"/> moves both ends there
/// too, past every break they cross. It used to shift each end through the scale alone, so an end carried across a break
/// landed inside it and the view jumped by the break's width.</summary>
public class PanAcrossBreaksTests
{
    // The library's own theme, which cuts a break to nothing: one drawn unit per pixel over a plot as wide as the range less its breaks.
    private static Figure Broken(Action<AxesBuilder> configure) =>
        Plt.Create().WithTheme(Theme.Default).AddSubPlot(1, 1, 1, ax => configure(ax.Plot([0.0, 150.0], [0.0, 1.0]).SetYLim(0, 1))).Build();

    private static Axis X(Figure figure) => figure.SubPlots[0].XAxis;

    /// <summary>Each row is a range, a break and a pan, with the range that results. The breaks are cut out of the space the pan is made in, so a
    /// pan carries an end past every break it crosses.</summary>
    [Theory]
    // 0..150 less (30, 80) is drawn over 0..100; 40 further is 40..140 drawn, 90..190 in data: a low end carried across a break lands past it
    [InlineData(0, 150, 30, 80, 40, 90, 190)]
    // 90..190 stands at 40..140 drawn; 70 back is -30..70 drawn, -30..120 in data: a high end carried back lands before it
    [InlineData(90, 190, 30, 80, -70, -30, 120)]
    // (120, 150) lies past 0..100, so it hides nothing on screen yet; 40 further carries the high end across it
    [InlineData(0, 100, 120, 150, 40, 40, 170)]
    // 40 lies in (30, 80): on screen the axis begins where the break is cut, so 10 further is 10 past the break's end
    [InlineData(40, 150, 30, 80, 10, 90, 160)]
    // a reversed axis: 150..0 is drawn over 100..0; 40 further is 140..40 drawn, 190..90 in data
    [InlineData(150, 0, 30, 80, 40, 190, 90)]
    public void AnEndCarriedAcrossABreak_LandsPastIt(double from, double to, double breakFrom, double breakTo, double pan, double min, double max)
    {
        var figure = Broken(ax => ax.SetXLim(from, to).WithXBreak(breakFrom, breakTo));

        new PanEvent("c", 0, pan, 0).ApplyTo(figure);

        Assert.Equal(min, X(figure).Min!.Value, 1e-9);
        Assert.Equal(max, X(figure).Max!.Value, 1e-9);
    }

    [Fact]
    public void OnALogAxis_TheShiftIsInDecades_PastTheBreak()
    {
        // 1..1e6 less (100, 1000) is drawn over 0..5 decades; 2.5 further is 2.5..7.5 drawn, 10^3.5..10^8.5 in data.
        var figure = Broken(ax => ax.SetXScale(AxisScale.Log).SetXLim(1, 1e6).WithXBreak(100, 1000));

        new PanEvent("c", 0, 2.5, 0).ApplyTo(figure);

        Assert.Equal(Math.Pow(10, 3.5), X(figure).Min!.Value, 1e-6);
        Assert.Equal(Math.Pow(10, 8.5), X(figure).Max!.Value, 1e-1);
    }

    [Fact]
    public void OnASymLogAxis_TheShiftIsInItsUnits_PastTheBreak()
    {
        // linthresh 1, s = 1 / 0.9 (matplotlib's linear stretch): forward(10) = s + 1, forward(100) = s + 2, forward(1e4) = s + 4;
        // 0..1e4 less (10, 100) is drawn over 0..s + 3. 2.5 further is 2.5..s + 5.5 drawn, 3.5..s + 6.5 with the break put back,
        // 10^(3.5 - s)..10^6.5 in data.
        var figure = Broken(ax => ax.WithSymlogXScale(1).SetXLim(0, 1e4).WithXBreak(10, 100));

        new PanEvent("c", 0, 2.5, 0).ApplyTo(figure);

        Assert.Equal(Math.Pow(10, 3.5 - 1 / 0.9), X(figure).Min!.Value, 1e-9);
        Assert.Equal(Math.Pow(10, 6.5), X(figure).Max!.Value, 1e-3);
    }

    [Fact]
    public void AYAxisWithABreak_PansPastItToo()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 150.0])
            .SetXLim(0, 1).SetYLim(0, 150).WithYBreak(30, 80)).Build();

        new PanEvent("c", 0, 0, 40).ApplyTo(figure);

        Assert.Equal(90.0, figure.SubPlots[0].YAxis.Min!.Value, 1e-9);
        Assert.Equal(190.0, figure.SubPlots[0].YAxis.Max!.Value, 1e-9);
    }

    [Fact]
    public void ADragAcrossABreak_KeepsTheValueUnderThePointerUnderIt()
    {
        // 0..150 less (30, 80) over a 100-pixel plot: one pixel is one drawn unit, and pixel 50 shows 100.
        var figure = Broken(ax => ax.SetXLim(0, 150).WithXBreak(30, 80));
        var events = new List<FigureInteractionEvent>();
        var pan = new PanModifier("c", ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]), events.Add);

        pan.OnPointerPressed(new PointerInputArgs(50, 25, PointerButton.Left, ModifierKeys.None));
        pan.OnPointerMoved(new PointerInputArgs(10, 25, PointerButton.Left, ModifierKeys.None));
        Assert.Single(events).ApplyTo(figure);

        Assert.Equal(90.0, X(figure).Min!.Value, 1e-9);
        Assert.Equal(190.0, X(figure).Max!.Value, 1e-9);
        var after = ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]);
        Assert.Equal(100.0, after.PixelToData(10, 25, 0)!.Value.DataX, 1e-9);
    }
    /// <summary>Under the matplotlib themes the parts of a broken axis stand a room apart, a share of their mean span; a drag within a
    /// part moves the data by the part's own scale, so the value under the pointer stays under it while the break stays in view.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_ADragWithinAPart_KeepsTheValueUnderThePointerUnderIt()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 150.0], [0.0, 1.0]).SetYLim(0, 1).SetXLim(0, 150).WithXBreak(30, 80)).Build();
        double before = ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]).PixelToData(80, 25, 0)!.Value.DataX;
        var events = new List<FigureInteractionEvent>();
        var pan = new PanModifier("c", ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]), events.Add);

        pan.OnPointerPressed(new PointerInputArgs(80, 25, PointerButton.Left, ModifierKeys.None));
        pan.OnPointerMoved(new PointerInputArgs(70, 25, PointerButton.Left, ModifierKeys.None));
        Assert.Single(events).ApplyTo(figure);

        Assert.Equal(128.0, before, 1e-9);   // drawn 88 of 0..110 (two parts of 30 and 70, a room of 10): past the break by its 50 less 10
        Assert.Equal(before, ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]).PixelToData(70, 25, 0)!.Value.DataX, 1e-9);
    }
}
