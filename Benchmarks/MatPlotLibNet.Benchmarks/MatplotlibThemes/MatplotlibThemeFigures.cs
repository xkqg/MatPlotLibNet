// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Benchmarks.MatplotlibThemes;

/// <summary>How a row of the matplotlib-theme benchmarks writes its figure: as SVG text, as a raster picture (the pixel grid of a
/// raster export is part of what the picture rows measure), or as a PDF page (a canvas that is not a grid of pixels, whose render
/// context is asked to put marks on a pixel grid it does not have).</summary>
public enum MatplotlibThemeOutput
{
    /// <summary>The figure is written with <c>ToSvg</c>.</summary>
    Svg,

    /// <summary>The figure is written with <c>ToPng</c>.</summary>
    Png,

    /// <summary>The figure is written with <c>ToPdf</c>.</summary>
    Pdf,
}

/// <summary>One row of the matplotlib-theme benchmarks: the name the benchmark method carries, the figure it draws, and how the
/// figure is written.</summary>
/// <param name="Name">The name of the benchmark method that draws the figure.</param>
/// <param name="Figure">The figure, built once.</param>
/// <param name="Output">Whether the row writes SVG text or a raster picture.</param>
public readonly record struct MatplotlibThemeRow(string Name, Figure Figure, MatplotlibThemeOutput Output);

/// <summary>Makes a figure that is drawn again a figure that changed, as a live chart is.</summary>
public static class MatplotlibThemeFigureExtensions
{
    extension(Figure figure)
    {
        /// <summary>Flips the title of every subplot between two words of the same length and returns the figure. A figure that
        /// is drawn again unchanged is handed its kept drawing, and a subplot that did not change is handed back as it was, which
        /// costs a fraction of a draw; one that changed is drawn, which is what a row measures: every panel of the figure.</summary>
        /// <param name="tick">The count of the draw; its parity picks the word.</param>
        public Figure ChangedFor(int tick)
        {
            var title = (tick & 1) == 0 ? "tick" : "tock";
            foreach (var subplot in figure.SubPlots) subplot.Title = title;
            return figure;
        }
    }
}

/// <summary>The figures the matplotlib themes are judged by: what these themes draw that the library's own do not (a grid under
/// the minor ticks, a colour bar with its extension, a zoom indicator, patches, a figure legend, a classic contour, a 3-D picture
/// that a script turns in the browser, a stem plot, wind barbs, a streamplot, a swarm plot). Each is a figure that a step of the release changes how it is drawn, so each step has the
/// same figure to measure before and after.</summary>
public sealed class MatplotlibThemeFigures
{
    private const int Panels = 9;
    private const int InsetPoints = 10_000;
    private const int PatchCount = 20;
    private const int FigLegendPanels = 7;
    private const int Side = 24;

    /// <summary>Builds every figure.</summary>
    public MatplotlibThemeFigures()
    {
        Rows =
        [
            new("V2Grid3x3", Grid3x3(Theme.MatplotlibV2), MatplotlibThemeOutput.Svg),
            new("V2MinorGridNoTicks3x3", Grid3x3(Theme.MatplotlibV2, minorTicks: false), MatplotlibThemeOutput.Svg),
            new("ClassicGrid3x3", Grid3x3(Theme.MatplotlibClassic), MatplotlibThemeOutput.Svg),
            new("V2LogGrid3x3", LogGrid3x3(), MatplotlibThemeOutput.Svg),
            new("V2HeatmapColorBarExtend", HeatmapWithExtendedColorBar(), MatplotlibThemeOutput.Svg),
            new("V2InsetZoom10k", InsetWithZoomIndicator(), MatplotlibThemeOutput.Svg),
            new("V2Patches20", TwentyPatches(), MatplotlibThemeOutput.Svg),
            new("V2SevenPanelsFigLegend", SevenPanelsWithFigureLegend(), MatplotlibThemeOutput.Svg),
            new("ClassicContour", ClassicContour(), MatplotlibThemeOutput.Svg),
            new("V2Interactive3D", InteractiveSurface(), MatplotlibThemeOutput.Svg),
            new("V2ConstrainedColorBar", ConstrainedColorBar(), MatplotlibThemeOutput.Svg),
            new("V2Stem", Stem(), MatplotlibThemeOutput.Svg),
            new("V2Barbs", Barbs(), MatplotlibThemeOutput.Svg),
            new("V2Streamplot", Streamplot(), MatplotlibThemeOutput.Svg),
            new("V2Swarm", Swarm(), MatplotlibThemeOutput.Svg),
            new("V2Grid3x3", Grid3x3(Theme.MatplotlibV2), MatplotlibThemeOutput.Png),
            new("ClassicGrid3x3", Grid3x3(Theme.MatplotlibClassic), MatplotlibThemeOutput.Png),
            new("V2Grid3x3Pdf", Grid3x3(Theme.MatplotlibV2), MatplotlibThemeOutput.Pdf),
            new("ClassicGrid3x3Pdf", Grid3x3(Theme.MatplotlibClassic), MatplotlibThemeOutput.Pdf),
        ];
    }

    /// <summary>Every row: the SVG rows, then the picture rows, then the PDF rows.</summary>
    public IReadOnlyList<MatplotlibThemeRow> Rows { get; }

    /// <summary>The figure of the row of this name and output.</summary>
    /// <exception cref="InvalidOperationException">No row has that name and output.</exception>
    public Figure Of(string name, MatplotlibThemeOutput output) =>
        Rows.Single(row => row.Name == name && row.Output == output).Figure;

    private static double[] Axis(int count) => [.. Enumerable.Range(0, count).Select(i => (double)i)];

    /// <summary>Nine panels (lines, bars and a scatter), each with a grid that asks for the major and the minor lines of both axes:
    /// with minor ticks on, the grid stands on the major and the minor ticks; with them off, it stands on the major ticks alone
    /// under a matplotlib theme.</summary>
    private static Figure Grid3x3(Theme theme, bool minorTicks = true)
    {
        var x = Axis(100);
        var y = x.Select(v => Math.Sin(v * 0.1) * 50 + 50).ToArray();
        string[] categories = ["A", "B", "C", "D"];
        var builder = Plt.Create().WithSize(900, 700).WithTheme(theme).WithTitle("3x3");
        for (var panel = 1; panel <= Panels; panel++)
        {
            var cell = panel;
            builder = builder.AddSubPlot(3, 3, cell, ax =>
            {
                switch (cell % 3)
                {
                    case 0: ax.Bar(categories, [10.0, 25, 15 + cell, 30]); break;
                    case 1: ax.Plot(x, y); break;
                    default: ax.Scatter(x.Take(40).ToArray(), y.Take(40).ToArray()); break;
                }
                ax.SetXLabel("x").SetYLabel("y").WithMinorTicks(minorTicks)
                    .WithGrid(g => g with { Visible = true, Axis = GridAxis.Both, Which = GridWhich.Both });
            });
        }
        return builder.Build();
    }

    /// <summary>Nine panels of a line over four powers of ten on a log Y axis, each with a grid that asks for the major and the minor
    /// lines of the Y axis and no call that switches the minor ticks on: a log axis places its own minor ticks under the matplotlib
    /// themes, as matplotlib's log scale does, so the grid and the marks stand on them.</summary>
    private static Figure LogGrid3x3()
    {
        var x = Axis(100);
        var builder = Plt.Create().WithSize(900, 700).WithTheme(Theme.MatplotlibV2).WithTitle("log 3x3");
        for (var panel = 1; panel <= Panels; panel++)
        {
            var reach = 2 + (panel % 3);
            var y = x.Select(v => Math.Pow(10, (v / 99) * reach) + 1).ToArray();
            builder = builder.AddSubPlot(3, 3, panel, ax => ax.Plot(x, y).SetYScale(AxisScale.Log).SetXLabel("x").SetYLabel("y")
                .WithGrid(g => g with { Visible = true, Axis = GridAxis.Y, Which = GridWhich.Both }));
        }
        return builder.Build();
    }

    /// <summary>A matrix drawn as a heatmap with its colour bar, the bar extended at both ends.</summary>
    private static Figure HeatmapWithExtendedColorBar()
    {
        var z = new double[Side, Side];
        for (var i = 0; i < Side; i++)
        {
            for (var j = 0; j < Side; j++) z[i, j] = Math.Sin(i * 0.3) * Math.Cos(j * 0.3);
        }
        return Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(z).WithColorBar(bar => bar with { Extend = ColorBarExtend.Both }))
            .Build();
    }

    /// <summary>A line of 10 000 points with an inset that shows a stretch of it and the zoom indicator from the stretch to the
    /// inset.</summary>
    private static Figure InsetWithZoomIndicator()
    {
        var x = Axis(InsetPoints);
        var y = x.Select(v => Math.Sin(v * 0.01) * 50 + 50).ToArray();
        return Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(x, y)
                .AddInset(0.55, 0.05, 0.4, 0.4, inset => inset.Plot(x, y).SetXLim(2000, 2500).SetYLim(0, 100).IndicateZoom()))
            .Build();
    }

    /// <summary>Twenty patches of the five kinds over a line that gives the axes their range.</summary>
    private static Figure TwentyPatches()
    {
        var x = Axis(50);
        var y = x.Select(v => v * 0.5).ToArray();
        return Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(x, y);
                for (var i = 0; i < PatchCount; i++)
                {
                    var at = new DataPoint((i * 2.2) + 1, (i * 7 % 20) + 2);
                    Patch patch = (i % 5) switch
                    {
                        0 => new Rectangle(at, 3, 2) { EdgeColor = Colors.Black },
                        1 => new Circle(at, 1.5) { EdgeColor = Colors.Black },
                        2 => new Ellipse(at, 3, 1.5) { EdgeColor = Colors.Black },
                        3 => new Wedge(at, 2, 20, 280) { EdgeColor = Colors.Black },
                        _ => new Polygon([at, new(at.DataX + 3, at.DataY), new(at.DataX + 1.5, at.DataY + 2.5)]) { EdgeColor = Colors.Black },
                    };
                    ax.AddPatch(patch);
                }
            })
            .Build();
    }

    /// <summary>Seven stacked panels of a labelled line each, and one legend for the figure that lists all seven.</summary>
    private static Figure SevenPanelsWithFigureLegend()
    {
        var t = Axis(200);
        var builder = Plt.Create().WithSize(800, 900).WithTheme(Theme.MatplotlibV2);
        for (var panel = 1; panel <= FigLegendPanels; panel++)
        {
            var phase = panel;
            builder = builder.AddSubPlot(FigLegendPanels, 1, panel, ax => ax
                .Plot(t, t.Select(v => Math.Sin((v / 20) + phase)).ToArray(),
                    s => s.Label = "panel " + phase.ToString(CultureInfo.InvariantCulture))
                .SetYLabel("v"));
        }
        return builder.FigLegend().Build();
    }

    /// <summary>A line contour under the classic theme, whose limits round out to the ticks.</summary>
    private static Figure ClassicContour()
    {
        const int Count = 60;
        var x = new double[Count];
        var y = new double[Count];
        for (var i = 0; i < Count; i++)
        {
            x[i] = -3 + (i * 6.0 / (Count - 1));
            y[i] = -3 + (i * 6.0 / (Count - 1));
        }
        var z = new double[Count, Count];
        for (var i = 0; i < Count; i++)
        {
            for (var j = 0; j < Count; j++) z[i, j] = Math.Exp(-((x[j] * x[j]) + (y[i] * y[i])) / 4) * Math.Cos(x[j] * y[i]);
        }
        return Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Contour(x, y, z))
            .Build();
    }

    /// <summary>A 3-D surface whose SVG carries the script that turns it in the browser; the length of that SVG is the row's
    /// other number.</summary>
    private static Figure InteractiveSurface()
    {
        var axis = new double[Side];
        for (var i = 0; i < Side; i++) axis[i] = (i - ((Side - 1) / 2.0)) / 2;
        var z = new double[Side, Side];
        for (var i = 0; i < Side; i++)
        {
            for (var j = 0; j < Side; j++)
            {
                var r = Math.Sqrt((axis[i] * axis[i]) + (axis[j] * axis[j]));
                z[i, j] = Math.Sin(r) / (1 + (r / 4));
            }
        }
        return Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2).With3DRotation().Surface(axis, axis, z).Build();
    }

    /// <summary>Two heatmaps side by side under the constrained layout, each with a labelled colour bar beside it: the layout
    /// keeps room for each bar and its labels.</summary>
    private static Figure ConstrainedColorBar()
    {
        var z = new double[Side, Side];
        for (var i = 0; i < Side; i++)
        {
            for (var j = 0; j < Side; j++) z[i, j] = (i * Side) + j;
        }
        return Plt.Create().WithSize(800, 400).WithTheme(Theme.MatplotlibV2).ConstrainedLayout()
            .AddSubPlot(1, 2, 1, ax => ax.Heatmap(z).SetXLabel("x").WithColorBar(bar => bar with { Label = "value" }))
            .AddSubPlot(1, 2, 2, ax => ax.Heatmap(z).SetXLabel("x").WithColorBar(bar => bar with { Label = "value" }))
            .Build();
    }

    /// <summary>Twenty stems of a sine, the figure of the stem fidelity check.</summary>
    private static Figure Stem()
    {
        const int Count = 20;
        var x = new double[Count];
        var y = new double[Count];
        for (var i = 0; i < Count; i++)
        {
            x[i] = i * 2 * Math.PI / (Count - 1);
            y[i] = Math.Sin(x[i]);
        }
        return Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.Stem(x, y)).Build();
    }

    /// <summary>Sixty-four wind barbs on an eight by eight grid, the field of the barbs fidelity check: some stand for a calm, the
    /// rest carry half and full barbs.</summary>
    private static Figure Barbs()
    {
        const int Count = 8;
        var x = new double[Count * Count];
        var y = new double[Count * Count];
        var speed = new double[Count * Count];
        var direction = new double[Count * Count];
        for (var row = 0; row < Count; row++)
        {
            for (var column = 0; column < Count; column++)
            {
                var k = (row * Count) + column;
                x[k] = -2 + (column * 4.0 / (Count - 1));
                y[k] = -2 + (row * 4.0 / (Count - 1));
                double u = Math.Sin(x[k]) * 20, v = Math.Cos(y[k]) * 20;
                speed[k] = Math.Sqrt((u * u) + (v * v));
                direction[k] = 90 - (Math.Atan2(-v, -u) * 180 / Math.PI);
            }
        }
        return Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Barbs(x, y, speed, direction))
            .Build();
    }

    /// <summary>A streamplot of a 40 by 40 field, the field of the streamplot fidelity check.</summary>
    private static Figure Streamplot()
    {
        const int Count = 40;
        var axis = new double[Count];
        for (var i = 0; i < Count; i++) axis[i] = -3 + (i * 6.0 / (Count - 1));
        var u = new double[Count, Count];
        var v = new double[Count, Count];
        for (var row = 0; row < Count; row++)
        {
            for (var column = 0; column < Count; column++)
            {
                double x = axis[column], y = axis[row];
                u[row, column] = -1 - (x * x) + y;
                v[row, column] = 1 + x - (y * y);
            }
        }
        return Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Streamplot(axis, axis, u, v))
            .Build();
    }

    /// <summary>A swarm plot of three groups of a hundred dots each, drawn from a fixed seed: the dots of a group crowd the middle of the
    /// plot, so the placement has neighbours to avoid.</summary>
    private static Figure Swarm()
    {
        const int Groups = 3, Dots = 100;
        var random = new Random(42);
        var data = new double[Groups][];
        for (var g = 0; g < Groups; g++)
        {
            data[g] = new double[Dots];
            for (var i = 0; i < Dots; i++)
            {
                // The sum of two draws: a bell-shaped group, as the fidelity check's normal samples are.
                data[g][i] = g + ((random.NextDouble() + random.NextDouble() - 1) * 1.5);
            }
        }
        return Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.Swarmplot(data)).Build();
    }
}
