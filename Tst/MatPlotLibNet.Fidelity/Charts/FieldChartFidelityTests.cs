// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Field-family fidelity tests (quiver, streamplot, barbs, stem).</summary>
public class FieldChartFidelityTests : FidelityTest
{
    private static double[] Linspace(double start, double end, int n)
    {
        var arr = new double[n];
        double step = (end - start) / (n - 1);
        for (int i = 0; i < n; i++) arr[i] = start + i * step;
        return arr;
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 14, Ssim = 0.97, DeltaE = 3, V2DeltaE = 4)]   // the arrows are as long, as pointing, as shaped and as black as the reference's are, the key's label stands where its text does; what is left is antialiasing and the text's blend
    public void Quiver_RotationalField_MatchesMatplotlib(string themeId)
    {
        var xs = Linspace(-2, 2, 10);
        var ys = Linspace(-2, 2, 10);
        var (x, y) = Meshgrid(xs, ys);
        double[] u = [.. y.Select(value => -value)];
        double[] v = x;
        AssertFidelity(themeId, "quiver", "Quiver — rotational field", ax => ax
            .Quiver(x, y, u, v)   // black, as matplotlib's arrows are in both styles
            .QuiverKey(0.9, 0.9, 1, "1 unit", s => s.LabelPosition = QuiverKeyLabelPosition.Right));   // the reference's ax.quiverkey(q, 0.9, 0.9, 1, "1 unit", labelpos="E")
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 9, Ssim = 0.98, DeltaE = 3, V2Rms = 10)]   // the streamlines are laid out by matplotlib's StreamMask and drawn with its arrows; antialiasing remains
    public void Streamplot_VectorField_MatchesMatplotlib(string themeId)
    {
        int n = 40;
        var x = Linspace(-3, 3, n);
        var y = Linspace(-3, 3, n);
        var u = new double[n, n];
        var v = new double[n, n];
        for (int iy = 0; iy < n; iy++)
            for (int ix = 0; ix < n; ix++)
            {
                u[iy, ix] = -1 - x[ix] * x[ix] + y[iy];
                v[iy, ix] = 1 + x[ix] - y[iy] * y[iy];
            }
        AssertFidelity(themeId, "streamplot", "Streamplot", ax => ax
            .Streamplot(x, y, u, v));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 15, Ssim = 0.97, DeltaE = 3, V2Rms = 17, V2Ssim = 0.96, V2DeltaE = 4)]   // matplotlib's barb polygons; its joins are round where ours are mitred, and antialiasing
    public void Barbs_WindField_MatchesMatplotlib(string themeId)
    {
        var xs = Linspace(-2, 2, 8);
        var ys = Linspace(-2, 2, 8);
        var (x, y) = Meshgrid(xs, ys);
        var speed = new double[x.Length];
        var direction = new double[x.Length];
        for (int k = 0; k < x.Length; k++)
        {
            double u = Math.Sin(x[k]) * 20;
            double vv = Math.Cos(y[k]) * 20;
            speed[k] = Math.Sqrt(u * u + vv * vv);
            // matplotlib's staff points against (u, v), where the wind comes from (_make_barbs turns it by arctan2(v, u) + pi / 2
            // from straight down); the series names that direction in degrees clockwise from north.
            direction[k] = 90 - (Math.Atan2(-vv, -u) * 180.0 / Math.PI);
        }
        var figure = FidelityFigure(themeId)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Barbs — wind field")
                .Barbs(x, y, speed, direction))   // black, as matplotlib's barbs are in both styles
            .Build();
        AssertFidelity(figure, "barbs");
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 10, Ssim = 0.98, DeltaE = 2, V2Rms = 13, V2DeltaE = 3)]   // matplotlib's stems, heads and red baseline; antialiasing and the text's blend remain
    public void Stem_Sine_MatchesMatplotlib(string themeId)
    {
        int n = 20;
        var x = Linspace(0, 2 * Math.PI, n);
        var y = x.Select(Math.Sin).ToArray();
        AssertFidelity(themeId, "stem", "Stem — sin(x)", ax => ax
            .Stem(x, y));
    }
}
