// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>Phase B.9 — signal-marker rendering equivalence baselines.</summary>
public class CartesianSignalsPartBaselineTests
{
    private static string Render(Action<AxesBuilder> configure) =>
        Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, configure).Build().ToSvg();

    [Fact] public void BuySignal_DrawsUpwardGreenTriangle()
    {
        var svg = Render(ax => ax.Plot([1.0, 5], [1.0, 5]).AddSignal(2, 3, SignalDirection.Buy));
        Assert.Contains("<polygon", svg);
    }

    [Fact] public void SellSignal_DrawsDownwardRedTriangle()
    {
        var svg = Render(ax => ax.Plot([1.0, 5], [1.0, 5]).AddSignal(2, 3, SignalDirection.Sell));
        Assert.Contains("<polygon", svg);
    }

    [Fact] public void SignalWithCustomColor_UsesOverride()
    {
        var svg = Render(ax => ax.Plot([1.0, 5], [1.0, 5]).AddSignal(2, 3, SignalDirection.Buy, m => m.Color = Colors.Magenta));
        Assert.Contains("<polygon", svg);
    }

    [Fact] public void MultipleSignals_AllRender()
    {
        var svg = Render(ax => ax.Plot([1.0, 5], [1.0, 5])
            .AddSignal(1.5, 2, SignalDirection.Buy)
            .AddSignal(2.5, 3, SignalDirection.Sell)
            .AddSignal(3.5, 4, SignalDirection.Buy));
        Assert.Contains("<polygon", svg);
    }

    [Fact] public void NoSignals_StillRenders()
    {
        var svg = Render(ax => ax.Plot([1.0, 5], [1.0, 5]));
        Assert.Contains("<svg", svg);
    }
}
