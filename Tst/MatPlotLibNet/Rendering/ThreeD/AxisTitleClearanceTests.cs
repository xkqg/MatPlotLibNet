// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>
/// A 3-D axis TITLE must never land on top of that axis' own tick labels. Reported on
/// <see href="https://github.com/xkqg/MatPlotLibNet/issues/18">issue #18</see> after the v1.14.1
/// frame fix: at azimuth −145 the X title printed straight through the "150" tick label. The title
/// pad used to be a constant (42 px for X, 60 px for Y/Z) while the tick labels sit at
/// <c>tickLength + pad + 14</c> — barely a line height apart, so any label wider than the gap
/// collides regardless of camera.
/// </summary>
public sealed class AxisTitleClearanceTests
{
    private const string XTitle = "x phase (deg)";
    private const string YTitle = "Y period";
    private const string ZTitle = "Z amplitude";

    // <text x=".." y=".." font-family=".." font-size=".." …>content</text>
    private static readonly Regex TextRe = new(
        "<text x=\"(?<x>[-\\d.eE]+)\" y=\"(?<y>[-\\d.eE]+)\"[^>]*font-size=\"(?<fs>[-\\d.eE]+)\"[^>]*>(?<t>[^<]*)</text>",
        RegexOptions.Compiled);

    private readonly record struct Label(double X, double Y, double FontSize, string Text)
    {
        /// <summary>The text's screen box. The SVG anchor is the CENTRED baseline, so the box runs
        /// half a width either side and from the ascent above the baseline to the descent below.</summary>
        internal (double L, double R, double T, double B) Box()
        {
            var size = ChartServices.FontMetrics.Measure(Text,
                new Font { Family = "DejaVu Sans, sans-serif", Size = FontSize });
            return (X - size.Width / 2, X + size.Width / 2, Y - size.Height * 0.8, Y + size.Height * 0.2);
        }
    }

    private static IReadOnlyList<Label> LabelsOf(string svg) =>
        TextRe.Matches(svg).Select(m => new Label(
            double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture),
            double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture),
            double.Parse(m.Groups["fs"].Value, CultureInfo.InvariantCulture),
            m.Groups["t"].Value)).ToList();

    /// <summary>
    /// Verifies the clearance is measured from the labels ACTUALLY drawn: a larger tick font and a
    /// custom formatter both widen the band, and the title has to move with it. A constant pad —
    /// or a pad measured from the default font — puts the title back on top of the labels.
    /// </summary>
    [Fact]
    public void AxisTitles_ClearWiderTickLabels_FromFontSizeAndFormatter()
    {
        var svg = Render(-145, postBuild: fig =>
        {
            var ax = fig.SubPlots[0];
            ax.XAxis.MajorTicks = ax.XAxis.MajorTicks with { LabelSize = 20 };
            ax.YAxis.MajorTicks = ax.YAxis.MajorTicks with { LabelSize = 20 };
            ax.ZAxis.MajorTicks = ax.ZAxis.MajorTicks with { LabelSize = 20 };
            ax.XAxis.TickFormatter = new global::MatPlotLibNet.Rendering.TickFormatters.NumericTickFormatter();
        });
        var labels = LabelsOf(svg);

        var titles = labels.Where(l => l.Text is XTitle or YTitle or ZTitle).ToList();
        Assert.Equal(3, titles.Count);
        var ticks = labels
            .Where(l => double.TryParse(l.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            .ToList();
        Assert.NotEmpty(ticks);

        foreach (var title in titles)
        {
            var box = title.Box();
            var hit = ticks.FirstOrDefault(t => Overlaps(box, t.Box()));
            Assert.True(hit.Text is null,
                $"title '{title.Text}' overlaps tick label '{hit.Text}' once the tick font grows to 20px");
        }
    }

    private static string Render(double azimuth, double elevation = 30,
        Action<global::MatPlotLibNet.Models.Figure>? postBuild = null)
    {
        const int bins = 24, rows = 12;
        var xs = Enumerable.Range(0, bins).Select(i => i * 15.0).ToArray();
        var rnd = new Random(12345);
        var figure = Plt.Create()
            .WithSize(940, 600)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithCamera(elevation: elevation, azimuth: azimuth)
                  .SetXLabel(XTitle).SetYLabel(YTitle).SetZLabel(ZTitle)
                  .SetXLim(-10, 370)
                  .SetZLim(0, 60);
                for (int r = 0; r < rows; r++)
                {
                    var zs = Enumerable.Range(0, bins)
                        .Select(_ => rnd.NextDouble() < 0.15 ? 8 + rnd.NextDouble() * 47 : 0.2).ToArray();
                    ax.PlanarBar3D(xs, Enumerable.Repeat((double)r, bins).ToArray(), zs, s => s.BarWidth = 12.0);
                }
            })
            .Build();
        postBuild?.Invoke(figure);
        return figure.ToSvg();
    }

    private static bool Overlaps((double L, double R, double T, double B) a, (double L, double R, double T, double B) b) =>
        a.L < b.R && b.L < a.R && a.T < b.B && b.T < a.B;

    /// <summary>
    /// Verifies each axis title clears every numeric tick label, at cameras inside AND outside the
    /// historical default quadrant. The reporter's camera is the −145 row.
    /// </summary>
    [Theory]
    [InlineData(-60)]
    [InlineData(-45)]
    [InlineData(-145)]
    [InlineData(45)]
    [InlineData(135)]
    public void AxisTitles_NeverOverlapTheirOwnTickLabels(double azimuth)
    {
        var labels = LabelsOf(Render(azimuth));
        Assert.NotEmpty(labels);

        var titles = labels.Where(l => l.Text is XTitle or YTitle or ZTitle).ToList();
        Assert.Equal(3, titles.Count);

        var ticks = labels
            .Where(l => double.TryParse(l.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            .ToList();
        Assert.NotEmpty(ticks);

        foreach (var title in titles)
        {
            var titleBox = title.Box();
            var hit = ticks.FirstOrDefault(t => Overlaps(titleBox, t.Box()));
            Assert.True(hit.Text is null,
                $"azimuth {azimuth}: title '{title.Text}' at ({title.X:F1},{title.Y:F1}) overlaps tick label " +
                $"'{hit.Text}' at ({hit.X:F1},{hit.Y:F1}) — the title pad must clear the tick-label band.");
        }
    }
}
