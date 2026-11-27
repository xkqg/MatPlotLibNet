// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A number the library writes as text a reader sees (a label in the picture, the text of a hover, a colour a page reads) is
/// written the same on every machine: a culture that writes its decimals with a comma, or its percent sign after a space, changes none
/// of them. Each row is a text that was written in the culture of the machine.</summary>
public class NumbersInEveryCultureTests
{
    public static TheoryData<string, string> Rows { get; } = new()
    {
        { "nl-NL", "a level of a Fibonacci retracement" },
        { "de-DE", "a level of a Fibonacci retracement" },
        { "nl-NL", "the value of a progress bar" },
        { "", "the value of a progress bar" },
        { "nl-NL", "the hover text of a point" },
        { "nl-NL", "a colour with an alpha, as CSS" },
    };

    [Theory]
    [MemberData(nameof(Rows))]
    public void ANumberWrittenAsText_ReadsTheSameInEveryCulture(string culture, string text)
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture);
            switch (text)
            {
                case "a level of a Fibonacci retracement":
                    Assert.Contains(">61.8%  4.29<", Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [2.0, 8.0])
                        .AddFibonacci(priceHigh: 8.0, priceLow: 2.0)).ToSvg());
                    break;
                case "the value of a progress bar":
                    Assert.Contains(">62%<", Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ProgressBar(0.62)).ToSvg());
                    break;
                case "the hover text of a point":
                    Assert.Equal("load: (1, 2.5)", HoverTextOverTheMiddlePoint());
                    break;
                default:
                    Assert.Equal("rgba(255,128,0,0.50)", new Color(255, 128, 0, 128).ToRgbaString());
                    break;
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    /// <summary>The hover text of the middle of three points, the pointer on it.</summary>
    private static string HoverTextOverTheMiddlePoint()
    {
        var figure = Plt.Create().Plot([0.0, 1.0, 2.0], [0.0, 2.5, 10.0], s => s.Label = "load").Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 2;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        var controller = InteractionController.CreateLocal(figure, ChartLayout.Create(figure, [new Rect(0, 0, 200, 100)]));

        controller.HandlePointerMoved(new PointerInputArgs(100, 75, PointerButton.None, ModifierKeys.None));

        return Assert.IsType<HoverTooltipContent>(controller.ActiveTooltip).Text;
    }
}
