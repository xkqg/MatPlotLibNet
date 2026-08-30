// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>On a DATE axis a threshold's label must be drawn by <c>AxHLine</c>, whose label is anchored at the
/// plot area's right edge — <c>Threshold(…, label)</c> anchors its label at x = 0 in DATA coordinates, which on
/// an OADate axis is the year 1899, far off the canvas (council M9-F3, verified M12#7).</summary>
public class ReferenceLineOnDateAxisTests
{
    private static readonly DateTime Now = new(2026, 8, 30, 16, 0, 0, DateTimeKind.Utc);

    private static double LabelX(Action<AxesBuilder> threshold)
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
        var m = Regex.Match(svg, "<text[^>]*x=\"([-0-9.]+)\"[^>]*>target 6 µs<");
        Assert.True(m.Success, "the threshold label is drawn");
        return double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    [Fact]
    public void AnAxHLineLabel_SitsInsideTheCanvas()
    {
        double x = LabelX(ax => ax.AxHLine(6, r => r.Label = "target 6 µs"));

        Assert.InRange(x, 0, 800);
    }

    [Fact]
    public void AThresholdLabel_LandsOffTheCanvas_WhichIsWhyItIsNotUsedOnATimeAxis()
    {
        double x = LabelX(ax => ax.Threshold(6, Orientation.Horizontal, ThresholdBreach.Above, label: "target 6 µs"));

        Assert.True(x < 0, $"a data-x of 0 on an OADate axis is the year 1899, was {x}");
    }
}
