// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using MatPlotLibNet;
using MatPlotLibNet.Data;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Benchmarks;

/// <summary>v1.7.0 feature benchmarks — run as tests, output to console.</summary>
public sealed class V170Benchmarks
{
    [Fact]
    public void RingBuffer_AppendThroughput()
    {
        var buf = new DoubleRingBuffer(100_000);
        var sw = Stopwatch.StartNew();
        const int iterations = 1_000_000;
        for (int i = 0; i < iterations; i++)
            buf.Append(i);
        sw.Stop();
        double opsPerSec = iterations / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"RingBuffer.Append: {opsPerSec:N0} ops/sec ({sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0})");
        Assert.True(opsPerSec > 1_000_000, "Should exceed 1M ops/sec");
    }

    [Fact]
    public void RingBuffer_SnapshotThroughput()
    {
        var buf = new DoubleRingBuffer(10_000);
        for (int i = 0; i < 10_000; i++) buf.Append(i);
        var sw = Stopwatch.StartNew();
        const int iterations = 10_000;
        for (int i = 0; i < iterations; i++)
        {
            var arr = buf.ToArray();
            _ = arr.Length;
        }
        sw.Stop();
        double opsPerSec = iterations / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"RingBuffer.ToArray(10K): {opsPerSec:N0} snapshots/sec ({sw.Elapsed.TotalMilliseconds:F2}ms)");
        Assert.True(opsPerSec > 1_000);
    }

    [Fact]
    public void StreamingSeries_AppendAndSnapshot()
    {
        var series = new StreamingLineSeries(10_000);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100_000; i++)
            series.AppendPoint(i, Math.Sin(i * 0.01));
        var snap = series.CreateSnapshot();
        sw.Stop();
        Console.WriteLine($"StreamingLine 100K appends + snapshot: {sw.Elapsed.TotalMilliseconds:F2}ms, snap={snap.XData.Length} pts");
        Assert.True(sw.Elapsed.TotalMilliseconds < 500);
    }

    [Fact]
    public void GeoProjection_AllProjections_Forward()
    {
        IGeoProjection[] projs = [
            GeoProjection.PlateCarree, GeoProjection.Mercator, GeoProjection.Robinson,
            GeoProjection.Orthographic, GeoProjection.LambertConformal,
            GeoProjection.Mollweide, GeoProjection.Sinusoidal, GeoProjection.AlbersEqualArea,
            GeoProjection.AzimuthalEquidistant, GeoProjection.Stereographic,
            GeoProjection.TransverseMercator, GeoProjection.NaturalEarth, GeoProjection.EqualEarth
        ];
        foreach (var proj in projs)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100_000; i++)
                proj.Forward(45, -90 + (i % 180));
            sw.Stop();
            double opsPerSec = 100_000 / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"  {proj.Name,-25} 100K forwards: {sw.Elapsed.TotalMilliseconds:F2}ms ({opsPerSec:N0}/sec)");
        }
        Assert.Equal(13, projs.Length);
    }

    [Fact]
    public void GeoNaturalEarth_LoadCoastlines()
    {
        var sw = Stopwatch.StartNew();
        var coastlines = NaturalEarth110m.Coastlines();
        sw.Stop();
        Console.WriteLine($"NaturalEarth 110m coastlines loaded: {sw.Elapsed.TotalMilliseconds:F2}ms ({coastlines.Count} features)");
        Assert.True(coastlines.Count > 0);
    }

    [Fact]
    public void GeoNaturalEarth_LoadCountries()
    {
        var sw = Stopwatch.StartNew();
        var countries = NaturalEarth110m.Countries();
        sw.Stop();
        Console.WriteLine($"NaturalEarth 110m countries loaded: {sw.Elapsed.TotalMilliseconds:F2}ms ({countries.Count} features)");
        Assert.True(countries.Count > 100);
    }

    [Fact]
    public void MathTextParser_OperatorLimits()
    {
        var sw = Stopwatch.StartNew();
        const int iterations = 10_000;
        for (int i = 0; i < iterations; i++)
        {
            MathTextParser.Parse(@"$\sum_{i=0}^{n} \frac{1}{i!} = e$");
        }
        sw.Stop();
        double opsPerSec = iterations / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"MathTextParser (sum+frac): {opsPerSec:N0} parses/sec ({sw.Elapsed.TotalMilliseconds:F2}ms)");
        Assert.True(opsPerSec > 10_000);
    }

    [Fact]
    public void MathTextParser_Matrix()
    {
        var sw = Stopwatch.StartNew();
        const int iterations = 10_000;
        for (int i = 0; i < iterations; i++)
        {
            MathTextParser.Parse(@"$\begin{pmatrix} a & b \\ c & d \end{pmatrix}$");
        }
        sw.Stop();
        double opsPerSec = iterations / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"MathTextParser (2x2 matrix): {opsPerSec:N0} parses/sec ({sw.Elapsed.TotalMilliseconds:F2}ms)");
        Assert.True(opsPerSec > 10_000);
    }

    [Fact]
    public void ThemeInstantiation_AllPresets()
    {
        var sw = Stopwatch.StartNew();
        Theme[] themes = [
            Theme.Default, Theme.Dark, Theme.Seaborn, Theme.Ggplot, Theme.Bmh,
            Theme.FiveThirtyEight, Theme.ColorBlindSafe, Theme.HighContrast,
            Theme.MatplotlibClassic, Theme.MatplotlibV2,
            Theme.Nord, Theme.Dracula, Theme.Monokai, Theme.Cyberpunk,
            Theme.Catppuccin, Theme.Gruvbox, Theme.OneDark, Theme.GitHub,
            Theme.Solarize, Theme.Grayscale, Theme.Paper, Theme.Presentation,
            Theme.Poster, Theme.Minimal, Theme.Retro, Theme.Neon
        ];
        sw.Stop();
        Console.WriteLine($"26 theme presets loaded: {sw.Elapsed.TotalMicroseconds:F0}µs ({themes.Length} themes)");
        Assert.Equal(26, themes.Length);
    }

    [Fact]
    public void SvgRender_SimpleChart()
    {
        double[] x = Enumerable.Range(0, 1000).Select(i => (double)i).ToArray();
        double[] y = x.Select(v => Math.Sin(v * 0.01)).ToArray();
        var fig = Plt.Create().Plot(x, y).Build();

        var sw = Stopwatch.StartNew();
        const int iterations = 100;
        for (int i = 0; i < iterations; i++)
            fig.ToSvg();
        sw.Stop();
        Console.WriteLine($"SVG render (1K points): {sw.Elapsed.TotalMilliseconds / iterations:F2}ms/render ({iterations} iterations)");
        Assert.True(sw.Elapsed.TotalMilliseconds / iterations < 100);
    }

    [Fact]
    public void SvgRender_With3DRotation()
    {
        var fig = Plt.Create()
            .With3DRotation()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0, 2.0], [0.0, 1.0, 2.0],
                    new double[,] { { 0, 1, 0 }, { 1, 2, 1 }, { 0, 1, 0 } }))
            .Build();

        var sw = Stopwatch.StartNew();
        const int iterations = 50;
        for (int i = 0; i < iterations; i++)
            fig.ToSvg();
        sw.Stop();
        Console.WriteLine($"SVG render (3D surface + rotation script): {sw.Elapsed.TotalMilliseconds / iterations:F2}ms/render");
        Assert.True(sw.Elapsed.TotalMilliseconds / iterations < 500);
    }
}
