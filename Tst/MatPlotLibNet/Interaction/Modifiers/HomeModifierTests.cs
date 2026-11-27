// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>Verifies <see cref="HomeModifier"/>: a press handed to it on a layout that holds no axes sends no reset.</summary>
public class HomeModifierTests
{
    [Fact]
    public void APressOnALayoutWithNoAxes_SendsNoReset()
    {
        var events = new List<FigureInteractionEvent>();
        var home = new HomeModifier("c1", ChartLayout.Create(Plt.Create().Build(), Array.Empty<Rect>()), events.Add);

        home.OnPointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None, ClickCount: 2));

        Assert.Empty(events);
    }
}
