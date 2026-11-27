// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary><see cref="InteractionModifier"/> is the one owner of the chart id, the layout, the event sink and the
/// "I claim nothing" answers every modifier used to write out for itself.</summary>
public class InteractionModifierBaseTests
{
    /// <summary>A layout that states a plot area and a range and nothing else.</summary>
    private static TestChartLayout PlainLayout() => new(new Rect(10, 10, 200, 100), new DataRange(0, 100, 0, 50));

    /// <summary>A modifier that does nothing of its own, so every answer it gives is the base class's.</summary>
    private sealed class BareModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : InteractionModifier(chartId, layout, sink)
    {
        public string SeenChartId => ChartId;
        public IChartLayout SeenLayout => Layout;
        public void Send(FigureInteractionEvent e) => Emit(e);
    }

    /// <summary>A modifier built the way a read-only one is: from a layout alone.</summary>
    private sealed class ReadOnlyModifier(IChartLayout layout) : InteractionModifier(layout)
    {
        public string SeenChartId => ChartId;
        public IChartLayout SeenLayout => Layout;
        public void Send(FigureInteractionEvent e) => Emit(e);
    }

    private static readonly PointerInputArgs Press = new(20, 20, PointerButton.Left, ModifierKeys.None);
    private static readonly ScrollInputArgs Scroll = new(20, 20, 0.0, 1.0, ModifierKeys.None);
    private static readonly KeyInputArgs Key = new("a", ModifierKeys.None);

    [Fact]
    public void TheThreeFieldsAreHeldAsGiven()
    {
        var layout = PlainLayout();
        var sent = new List<FigureInteractionEvent>();
        var modifier = new BareModifier("chart-7", layout, sent.Add);
        var e = new HoverEvent("chart-7", 0, 1.0, 2.0);

        modifier.Send(e);

        Assert.Equal("chart-7", modifier.SeenChartId);
        Assert.Same(layout, modifier.SeenLayout);
        Assert.Same(e, Assert.Single(sent));
    }

    /// <summary>A modifier that overrides nothing claims no gesture, sends nothing, does nothing on the moves every modifier is
    /// told of, and shows no rubber band, crosshair or tooltip: the roles a controller asks every modifier about are the base's,
    /// so a modifier written outside the library takes one by overriding it.</summary>
    [Fact]
    public void ByDefaultNothingIsClaimedNothingIsSentAndNothingIsShown()
    {
        var sent = new List<FigureInteractionEvent>();
        InteractionModifier modifier = new BareModifier("c", PlainLayout(), sent.Add);

        Assert.False(modifier.HandlesPointerPressed(Press));
        Assert.False(modifier.HandlesScroll(Scroll));
        Assert.False(modifier.HandlesKeyDown(Key));
        var thrown = Record.Exception(() =>
        {
            modifier.OnPointerPressed(Press);
            modifier.OnPointerMoved(Press);
            modifier.OnPointerTracked(Press);
            modifier.OnPointerReleased(Press);
            modifier.OnScroll(Scroll);
            modifier.OnKeyDown(Key);
        });

        Assert.Null(thrown);
        Assert.Empty(sent);
        Assert.Null(modifier.ActiveBrushSelect);
        Assert.Null(modifier.ActiveCrosshair);
        Assert.Null(modifier.ActiveTooltip);
    }

    [Fact]
    public void AModifierBuiltFromALayoutAloneHasNoChartAndSendsNowhere()
    {
        var layout = PlainLayout();
        var modifier = new ReadOnlyModifier(layout);

        var thrown = Record.Exception(() => modifier.Send(new HoverEvent("c", 0, 1.0, 2.0)));

        Assert.Null(thrown);
        Assert.Equal(string.Empty, modifier.SeenChartId);
        Assert.Same(layout, modifier.SeenLayout);
    }

    [Fact]
    public void EveryModifierOfTheLibraryIsAnInteractionModifier()
    {
        var offenders = typeof(IInteractionModifier).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IInteractionModifier).IsAssignableFrom(t))
            .Where(t => !typeof(InteractionModifier).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(offenders);
    }
}
