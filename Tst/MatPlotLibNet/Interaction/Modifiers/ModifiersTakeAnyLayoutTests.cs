// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary><see cref="IChartLayout"/> is a public interface a host may implement with nothing but a plot area and a
/// range. The modifiers hold it as that interface, but each of them used to cast it to the library's own
/// <see cref="ChartLayout"/> to turn a pixel into a data point, so a host's layout (or a layout standing in for the
/// current one while a repaint swaps it) threw an <see cref="InvalidCastException"/> on the first pointer move.
/// Converting a pixel is the layout's job, so it is a member of the interface.</summary>
public class ModifiersTakeAnyLayoutTests
{
    /// <summary>A layout that states a plot area and a range and nothing else: the least a host can implement.</summary>
    private sealed class PlainLayout : IChartLayout
    {
        public int AxesCount => 1;
        public Rect GetPlotArea(int axesIndex) => new(10, 10, 200, 100);
        public DataRange GetDataRange(int axesIndex) => new(0, 100, 0, 50);
        public int? HitTestAxes(double pixelX, double pixelY) =>
            pixelX >= 10 && pixelX <= 210 && pixelY >= 10 && pixelY <= 110 ? 0 : null;
        public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex) => null;
    }

    private static Figure NewFigure()
    {
        var figure = Plt.Create().Plot([10.0, 50.0, 90.0], [10.0, 25.0, 40.0]).Build();
        figure.ChartId = "c";
        return figure;
    }

    private static PointerInputArgs At(double x, double y, ModifierKeys mods = ModifierKeys.None) =>
        new(x, y, PointerButton.Left, mods);

    private static PointerInputArgs Over(double x, double y) => new(x, y, PointerButton.None, ModifierKeys.None);

    [Fact]
    public void ThePanModifier_ConvertsThePixelsOfAPlainLayout()
    {
        var events = new List<FigureInteractionEvent>();
        var pan = new PanModifier("c", new PlainLayout(), events.Add);

        var thrown = Record.Exception(() =>
        {
            pan.OnPointerPressed(At(100, 60));
            pan.OnPointerMoved(At(140, 60));
        });

        Assert.Null(thrown);
        var sent = Assert.IsType<PanEvent>(Assert.Single(events));
        Assert.Equal(-20.0, sent.DxData, 9);   // 40 px over a 200 px plot of 100 data units
    }

    [Fact]
    public void TheRectangleZoomModifier_ConvertsThePixelsOfAPlainLayout()
    {
        var events = new List<FigureInteractionEvent>();
        var zoom = new RectangleZoomModifier("c", new PlainLayout(), events.Add);

        var thrown = Record.Exception(() =>
        {
            zoom.OnPointerPressed(At(50, 30, ModifierKeys.Ctrl));
            zoom.OnPointerMoved(At(110, 80, ModifierKeys.Ctrl));
            zoom.OnPointerReleased(At(110, 80, ModifierKeys.Ctrl));
        });

        Assert.Null(thrown);
        Assert.IsType<RectangleZoomEvent>(Assert.Single(events));
    }

    [Fact]
    public void TheBrushSelectModifier_ConvertsThePixelsOfAPlainLayout()
    {
        var events = new List<FigureInteractionEvent>();
        var brush = new BrushSelectModifier("c", new PlainLayout(), events.Add);

        var thrown = Record.Exception(() =>
        {
            brush.OnPointerPressed(At(50, 30, ModifierKeys.Shift));
            brush.OnPointerMoved(At(110, 80, ModifierKeys.Shift));
            brush.OnPointerReleased(At(110, 80, ModifierKeys.Shift));
        });

        Assert.Null(thrown);
        Assert.IsType<BrushSelectEvent>(Assert.Single(events));
    }

    [Fact]
    public void TheSpanSelectModifier_ConvertsThePixelsOfAPlainLayout()
    {
        var events = new List<FigureInteractionEvent>();
        var span = new SpanSelectModifier("c", new PlainLayout(), events.Add);

        var thrown = Record.Exception(() =>
        {
            span.OnPointerPressed(At(50, 30, ModifierKeys.Alt));
            span.OnPointerMoved(At(110, 80, ModifierKeys.Alt));
            span.OnPointerReleased(At(110, 80, ModifierKeys.Alt));
        });

        Assert.Null(thrown);
        Assert.IsType<SpanSelectEvent>(Assert.Single(events));
    }

    [Fact]
    public void TheHoverModifier_ConvertsThePixelsOfAPlainLayout()
    {
        var events = new List<FigureInteractionEvent>();
        var hover = new HoverModifier("c", new PlainLayout(), events.Add);

        var thrown = Record.Exception(() => hover.OnPointerMoved(Over(90, 60)));

        Assert.Null(thrown);
        var sent = Assert.IsType<HoverEvent>(Assert.Single(events));
        Assert.Equal(40.0, sent.X, 9);   // (90 - 10) px of 200 px over 100 data units
    }

    [Fact]
    public void TheDataCursorModifier_ConvertsThePixelsOfAPlainLayout()
    {
        var events = new List<FigureInteractionEvent>();
        var cursor = new DataCursorModifier("c", new PlainLayout(), NewFigure(), events.Add);

        var thrown = Record.Exception(() => cursor.HandlesPointerPressed(At(50, 30)));

        Assert.Null(thrown);
    }

    [Fact]
    public void TheController_ReadsTheHoverPointOfAPlainLayout()
    {
        var controller = InteractionController.CreateLocal(NewFigure(), new PlainLayout());

        var thrown = Record.Exception(() => controller.HandlePointerMoved(Over(90, 60)));

        Assert.Null(thrown);
    }
}
