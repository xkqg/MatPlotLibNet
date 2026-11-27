// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="SignalMarker"/> behavior.</summary>
public class SignalMarkerTests
{

    /// <summary>Verifies that AddSignal supports fluent chaining via the builder API.</summary>
    [Fact]
    public void AxesBuilder_AddSignal_FluentChaining()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0, 3.0], [10, 20, 15])
                .AddSignal(1.0, 10.0, SignalDirection.Buy)
                .AddSignal(2.0, 20.0, SignalDirection.Sell))
            .Build();
        Assert.Equal(2, figure.SubPlots[0].Signals.Count);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  Verifies that SVG output contains a polygon element for a signal marker.
        new DrawnToken("SvgOutput_ContainsPolygonForSignal", () =>
        {
            var figure = new Figure();
            var axes = figure.AddSubPlot();
            axes.Plot([1.0, 2.0, 3.0], [10, 20, 15]);
            axes.AddSignal(1.0, 10.0, SignalDirection.Buy);
            return SvgRendererDefault.Current.Render(figure);
        }, "<polygon"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}

