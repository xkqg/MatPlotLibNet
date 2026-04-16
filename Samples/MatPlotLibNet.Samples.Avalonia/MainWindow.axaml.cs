// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MplColors = MatPlotLibNet.Styling.Colors;
using MplTheme = MatPlotLibNet.Styling.Theme;

namespace MatPlotLibNet.Samples.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StaticChart.Figure = CreateStaticFigure();
        InteractiveChart.Figure = CreateInteractiveFigure();
    }

    /// <summary>
    /// Static line chart with math-text title demonstrating R-squared rendering.
    /// </summary>
    private static Figure CreateStaticFigure()
    {
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] y = [2.1, 4.5, 3.2, 6.8, 5.1, 7.3, 6.5, 8.9, 7.2, 9.4];

        return Plt.Create()
            .WithTitle("$R^2 = 0.95$")
            .WithTheme(MplTheme.Seaborn)
            .AddSubPlot(1, 1, 1, ax => ax
                .SetXLabel("Observation")
                .SetYLabel("Value")
                .Plot(x, y, line => { line.Color = MplColors.Tab10Blue; line.Label = "Measurements"; })
                .WithLegend(LegendPosition.UpperLeft))
            .Build();
    }

    /// <summary>
    /// Interactive multi-line chart with legend. Pan/zoom is enabled via
    /// <c>IsInteractive="True"</c> on the <see cref="MatPlotLibNet.Avalonia.MplChartControl"/>.
    /// </summary>
    private static Figure CreateInteractiveFigure()
    {
        double[] x = Enumerable.Range(0, 60).Select(i => i * 0.2).ToArray();

        return Plt.Create()
            .WithTitle("Interactive — Pan / Zoom / Reset")
            .WithTheme(MplTheme.Default)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.SetXLabel("x").SetYLabel("f(x)");
                ax.Plot(x, x.Select(v => Math.Sin(v)).ToArray(),           s => s.Label = "sin(x)");
                ax.Plot(x, x.Select(v => Math.Cos(v)).ToArray(),           s => s.Label = "cos(x)");
                ax.Plot(x, x.Select(v => 0.5 * Math.Sin(2 * v)).ToArray(), s => s.Label = "0.5 sin(2x)");
                ax.WithLegend(LegendPosition.UpperRight);
            })
            .Build();
    }
}
