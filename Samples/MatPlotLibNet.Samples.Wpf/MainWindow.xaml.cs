// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using MatPlotLibNet;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Samples.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        BuildLine();
    }

    private void OnLineClicked(object sender, RoutedEventArgs e)    => BuildLine();
    private void OnBarClicked(object sender, RoutedEventArgs e)     => BuildBar();
    private void OnScatterClicked(object sender, RoutedEventArgs e) => BuildScatter();
    private void OnSurfaceClicked(object sender, RoutedEventArgs e) => BuildSurface();
    private void OnInteractiveChanged(object sender, RoutedEventArgs e)
        => Chart.IsInteractive = InteractiveToggle.IsChecked == true;

    private void BuildLine()
    {
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] y = [2.1, 4.5, 3.2, 6.8, 5.1, 7.3, 6.5, 8.9, 7.2, 9.4];
        Chart.Figure = Plt.Create()
            .WithTitle("Line chart — WPF sample")
            .WithSize(800, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(x, y, s =>
            {
                s.Color = Colors.Blue;
                s.LineWidth = 2;
                s.Label = "Revenue";
            }))
            .Build();
    }

    private void BuildBar()
    {
        string[] cats = ["Q1", "Q2", "Q3", "Q4"];
        double[] vals = [23, 45, 12, 67];
        Chart.Figure = Plt.Create()
            .WithTitle("Bar chart — WPF sample")
            .WithSize(800, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Bar(cats, vals).WithBarLabels())
            .Build();
    }

    private void BuildScatter()
    {
        var rng = new Random(42);
        double[] x = Enumerable.Range(0, 50).Select(_ => rng.NextDouble() * 10).ToArray();
        double[] y = x.Select(v => v * 0.8 + rng.NextDouble() * 3).ToArray();
        Chart.Figure = Plt.Create()
            .WithTitle("Scatter plot — WPF sample")
            .WithSize(800, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter(x, y, s =>
            {
                s.Color = Colors.CornflowerBlue;
                s.Marker = MarkerStyle.Circle;
                s.MarkerSize = 7;
            }))
            .Build();
    }

    private void BuildSurface()
    {
        const int N = 30;
        double[] xs = Enumerable.Range(0, N).Select(i => -3 + 6.0 * i / (N - 1)).ToArray();
        double[] ys = (double[])xs.Clone();
        double[,] z = new double[N, N];
        for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
                z[i, j] = Math.Sin(Math.Sqrt(xs[i] * xs[i] + ys[j] * ys[j]));
        Chart.Figure = Plt.Create()
            .WithTitle("3D Surface — drag to rotate, scroll to zoom")
            .WithSize(800, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 20, azimuth: -60)
                .Surface(xs, ys, z, s => s.ColorMap = ColorMaps.Plasma))
            .Build();
    }
}
