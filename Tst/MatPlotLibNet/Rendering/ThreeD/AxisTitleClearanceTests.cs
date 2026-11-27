// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
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
    /// <summary>A label far wider than the others, written for one value at an end of an edge.</summary>
    private const string WideLabel = "a label far wider than any label drawn";

    private const string XTitle = "x phase (deg)";
    private const string YTitle = "Y period";
    private const string ZTitle = "Z amplitude";

    private readonly record struct Label(double X, double Y, double FontSize, string Text)
    {
        /// <summary>The text's screen box. The SVG anchor is the CENTRED baseline, so the box runs
        /// half a width either side and from the ascent above the baseline to the descent below.</summary>
        internal (double L, double R, double T, double B) Box()
        {
            var size = FontMetricsDefault.Current.Measure(Text,
                new Font { Family = "DejaVu Sans, sans-serif", Size = FontSize });
            return (X - size.Width / 2, X + size.Width / 2, Y - size.Height * 0.8, Y + size.Height * 0.2);
        }
    }

    private static IReadOnlyList<Label> LabelsOf(string svg) =>
        [.. svg.Elements("text").Where(text => text["x"] is not null && text["y"] is not null && text["font-size"] is not null)
            .Select(text => new Label(text.Number("x"), text.Number("y"), text.Number("font-size"), text.Text))];

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

    /// <summary>A tick on the edge below the lowest bar draws its label, as matplotlib draws every tick in view, so the
    /// title clears that label too.</summary>
    [Fact]
    public void AnAxisTitle_ClearsALabelDrawnBelowTheLowestBar()
    {
        // Bars from x = 0: the cube's margins run below 0, so -0.1 lies on the edge below the lowest bar, and its wide label
        // is drawn.
        static Label TitleWith(double[] ticks)
        {
            var figure = Plt.Create()
                .WithSize(600, 500)
                .AddSubPlot(1, 1, 1, ax => ax.Bar3D([0.0, 1, 2], [0.0, 1, 2], [1.0, 2, 3]).SetXLabel(XTitle))
                .Build();
            var x = figure.SubPlots[0].XAxis;
            x.TickLocator = new global::MatPlotLibNet.Rendering.TickLocators.FixedLocator(ticks);
            x.TickFormatter = new TestTickFormatter(value => value < 0 ? WideLabel : value.ToString("0", CultureInfo.InvariantCulture));
            return LabelsOf(figure.ToSvg()).Single(l => l.Text == XTitle);
        }

        var withoutTheTick = TitleWith([0.0, 1, 2]);
        var withTheTick = TitleWith([-0.1, 0.0, 1, 2]);

        Assert.True(Math.Abs(withoutTheTick.X - withTheTick.X) + Math.Abs(withoutTheTick.Y - withTheTick.Y) > 1,
            $"the title stayed at ({withTheTick.X:F2},{withTheTick.Y:F2}) though the wide label at -0.1 is drawn");
    }

    /// <summary>A tick past the end of its edge draws no label either, so the title does not clear it.</summary>
    [Fact]
    public void AnAxisTitle_ClearsOnlyTheLabelsDrawn_NotOneBeyondTheEdge()
    {
        // The X edge runs from 1 to 3 and the data from 0 to 4: a tick at 4 lies past the high end, and only the edge leaves it
        // out.
        static Label TitleWith(global::MatPlotLibNet.Rendering.TickLocators.ITickLocator locator)
        {
            var figure = Plt.Create()
                .WithSize(600, 500)
                .AddSubPlot(1, 1, 1, ax => ax.Scatter3D([0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4], [0.0, 1, 2, 3, 4])
                    .SetXLabel(XTitle))
                .Build();
            var x = figure.SubPlots[0].XAxis;
            x.Min = 1;
            x.Max = 3;
            x.TickLocator = locator;
            x.TickFormatter = new TestTickFormatter(value => value > 3 ? WideLabel : value.ToString("0", CultureInfo.InvariantCulture));
            return LabelsOf(figure.ToSvg()).Single(l => l.Text == XTitle);
        }

        var onTheEdge = TitleWith(new global::MatPlotLibNet.Rendering.TickLocators.FixedLocator([1.0, 2, 3]));
        var withOneBeyond = TitleWith(new TestTickLocator(1.0, 2, 3, 4));

        Assert.Equal(onTheEdge.X, withOneBeyond.X, 6);
        Assert.Equal(onTheEdge.Y, withOneBeyond.Y, 6);
    }
}
