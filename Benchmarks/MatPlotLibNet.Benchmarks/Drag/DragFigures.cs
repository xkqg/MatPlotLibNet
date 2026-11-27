// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Benchmarks.Drag;

/// <summary>The figures a drag turns: a lit 3-D surface alone, and the same surface with three line panels beside it, the way a
/// dashboard shows it. Both are built with rotation in the browser, so the surface is the axes the drag moves.</summary>
public sealed class DragFigures
{
    private const int Side = 40;
    private const int PanelPoints = 360;

    /// <summary>The surface on its own, 800 by 600.</summary>
    public Figure Surface()
    {
        var x = Axis();
        return Plt.Create().WithSize(800, 600).WithTheme(Theme.OpsNight).WithTitle("Live surface")
            .With3DRotation().Surface(x, x, Ripple(x)).WithLighting(0.5, 0.5, 1.0).Build();
    }

    /// <summary>The surface and three panels of three lines each, in a two by two grid, 1200 by 900.</summary>
    public Figure SurfaceAndPanels()
    {
        var x = Axis();
        var z = Ripple(x);
        var t = Enumerable.Range(0, PanelPoints).Select(i => (double)i).ToArray();
        var builder = Plt.Create().WithSize(1200, 900).WithTheme(Theme.OpsNight).WithTitle("Wall")
            .AddSubPlot(2, 2, 1, ax => ax.Surface(x, x, z).WithLighting(0.5, 0.5, 1.0));
        for (var panel = 2; panel <= 4; panel++)
        {
            var phase = panel;
            builder = builder.AddSubPlot(2, 2, panel, ax =>
            {
                for (var line = 0; line < 3; line++)
                {
                    var shift = line + phase;
                    ax.Plot(t, t.Select(v => Math.Sin((v / 20) + shift)).ToArray());
                }
                ax.SetXLabel("t").SetYLabel("v").WithGrid(g => g);
            });
        }
        return builder.With3DRotation().Build();
    }

    /// <summary>The grid coordinates along each side: forty points, centred on zero, half a unit apart.</summary>
    private static double[] Axis()
    {
        var axis = new double[Side];
        var half = (Side - 1) / 2.0;
        for (var i = 0; i < Side; i++) axis[i] = (i - half) / 2;
        return axis;
    }

    /// <summary>A ripple running outward from the centre, z = sin(r) / (1 + r/4).</summary>
    private static double[,] Ripple(double[] axis)
    {
        var z = new double[axis.Length, axis.Length];
        for (var i = 0; i < axis.Length; i++)
        {
            for (var j = 0; j < axis.Length; j++)
            {
                var r = Math.Sqrt((axis[i] * axis[i]) + (axis[j] * axis[j]));
                z[i, j] = Math.Sin(r) / (1 + (r / 4));
            }
        }
        return z;
    }
}
