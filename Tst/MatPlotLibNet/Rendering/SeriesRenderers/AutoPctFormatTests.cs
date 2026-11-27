// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A pie's <c>AutoPct</c> is a .NET format string, and a caller who knows matplotlib hands it the printf form
/// of matplotlib's <c>autopct</c> (<c>"%.1f%%"</c>). A printf form with no .NET placeholder used to print its own text on
/// every wedge; the pie now reads <c>%f</c>, <c>%d</c> and <c>%%</c> as the .NET format they stand for.</summary>
public class AutoPctFormatTests
{
    [Theory]
    [InlineData("%.0f%%", "{0:F0}%")]
    [InlineData("%.1f%%", "{0:F1}%")]
    [InlineData("%1.1f%%", "{0:F1}%")]
    [InlineData("%5.2f %%", "{0:F2} %")]
    [InlineData("%f", "{0:F6}")]
    [InlineData("%d%%", "{0:F0}%")]
    [InlineData("%+.1f%%", "{0:F1}%")]
    [InlineData("share %.0f%% of all", "share {0:F0}% of all")]
    [InlineData("%.1f}", "{0:F1}}}")]
    [InlineData("%.f%%", "{0:F0}%")]
    [InlineData("%i%%", "{0:F0}%")]
    [InlineData("100% of %.0f%%", "100% of {0:F0}%")]
    [InlineData("%.0f%", "{0:F0}%")]
    [InlineData("%-6.1f%%", "{0:F1}%")]
    [InlineData("% .1f%%", "{0:F1}%")]
    [InlineData("%05.2f", "{0:F2}")]
    [InlineData("%#.0f", "{0:F0}")]
    [InlineData("%.2F%%", "{0:F2}%")]
    public void APrintfForm_BecomesTheNetFormatItStandsFor(string printf, string expected) =>
        Assert.Equal(expected, PieSeriesRenderer.ToNetPercentFormat(printf));

    [Theory]
    [InlineData("{0:F1}%")]
    [InlineData("{0:0}")]
    [InlineData("{0:F0}% of {0}")]
    [InlineData("100%")]
    [InlineData("%")]
    [InlineData("%%")]
    [InlineData("%s")]
    [InlineData("")]
    [InlineData("%-")]
    [InlineData("%*d")]
    [InlineData("%,d")]
    [InlineData("%E")]
    [InlineData("%G")]
    [InlineData("%.2e")]
    [InlineData("%g")]
    [InlineData("%x")]
    [InlineData("Done 50%!")]
    [InlineData("50%\tdone")]
    public void ANetFormatOrTextWithNoConversion_IsKeptAsGiven(string text) =>
        Assert.Equal(text, PieSeriesRenderer.ToNetPercentFormat(text));

    [Theory]
    [InlineData("%.0f%%")]
    [InlineData("%.1f%%")]
    [InlineData("%d%%")]
    public void APieWithAPrintfAutoPct_PrintsTheSharesNotTheFormat(string printf)
    {
        string svg = Plt.Create().WithSize(400, 400)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new PieSeries([1.0, 3.0]) { AutoPct = printf }).HideAllAxes())
            .Build().ToSvg();

        Assert.DoesNotContain("%.", svg);
        Assert.DoesNotContain("%d", svg);
        Assert.Contains(svg.Elements("text"), text => text.Text is "25%" or "25.0%");
        Assert.Contains(svg.Elements("text"), text => text.Text is "75%" or "75.0%");
    }
}
