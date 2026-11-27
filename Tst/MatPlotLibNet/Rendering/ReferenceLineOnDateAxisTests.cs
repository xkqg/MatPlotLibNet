// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>On a DATE axis a threshold's label must be drawn by <c>AxHLine</c>, whose label is anchored at the
/// plot area's right edge — <c>Threshold(…, label)</c> anchors its label at x = 0 in DATA coordinates, which on
/// an OADate axis is the year 1899, far off the canvas (council M9-F3, verified M12#7).</summary>
public class ReferenceLineOnDateAxisTests
{
    private static readonly DateTime Now = new(2026, 8, 30, 16, 0, 0, DateTimeKind.Utc);

    private static double? LabelX(Action<AxesBuilder> threshold)
    {
        string svg = Plt.Create().WithSize(800, 300)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([Now.AddMinutes(-3).ToOADate(), Now.ToOADate()], [2.0, 250.0]);
                ax.SetXLim(Now.AddMinutes(-3).ToOADate(), Now.ToOADate());
                ax.SetXDateAxis();
                threshold(ax);
            })
            .ToSvg();
        var label = svg.Elements("text").Where(text => text.Text == "target 6 µs").Select(text => (double?)text.Number("x")).FirstOrDefault();
        return label;
    }

    [Fact]
    public void AnAxHLineLabel_SitsInsideTheCanvas()
    {
        double? x = LabelX(ax => ax.AxHLine(6, r => r.Label = "target 6 µs"));

        Assert.NotNull(x);
        Assert.InRange(x.Value, 0, 800);
    }

    /// <summary>The threshold's label annotates a data x of 0, the year 1899 on an OADate axis, which the axes do not show, so it is not
    /// drawn (matplotlib's <c>Annotation._check_xy</c>): which is why a threshold is not used on a time axis.</summary>
    [Fact]
    public void AThresholdLabel_AnnotatesAPointOffTheAxes_AndIsNotDrawn_WhichIsWhyItIsNotUsedOnATimeAxis()
    {
        Assert.Null(LabelX(ax => ax.Threshold(6, Orientation.Horizontal, ThresholdBreach.Above, label: "target 6 µs")));
    }
}
