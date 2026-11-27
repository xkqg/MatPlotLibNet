// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Samples.Console;

/// <summary>The package icon, drawn by the library it stands for: a pcolormesh under viridis with one bold trace over it, the two
/// things this library is for, in the one colour map that says "scientific plotting" without a word. No text: at the 16 pixels a
/// package list shows, a word is a smudge. Six cells to a side is as fine as it can go and still read there. The sample program
/// writes it to <c>icon.png</c> at the repository root, <c>Directory.Build.props</c> packs that file into every package, and a test
/// of the Skia suite holds the committed file to this drawing.</summary>
internal static class PackageIcon
{
    private const int Cells = 6;

    /// <summary>A value of the drawing cut to nine decimals. Exp and Sin come from the platform's maths library, and two of them
    /// may differ in the last bit; that bit must not reach a pixel of a file that is compared byte for byte.</summary>
    private static double Settled(double value) => Math.Round(value, 9);

    /// <summary>The icon's figure: 128 x 128 pixels, a dark ground, no margins and no gaps, so the mark fills the square edge to edge
    /// and reads as a mark rather than as a tiny chart with a frame around it.</summary>
    public static Figure Build()
    {
        double[] edges = [.. Enumerable.Range(0, Cells + 1).Select(i => (double)i)];

        // A diagonal ramp with a soft peak up in the bright corner, so the map runs its whole length.
        var field = new double[Cells, Cells];
        for (int row = 0; row < Cells; row++)
        {
            for (int col = 0; col < Cells; col++)
            {
                double u = (col + 0.5) / Cells;
                double v = (row + 0.5) / Cells;
                field[row, col] = Settled((0.55 * u) + (0.45 * v)
                    + (0.35 * Math.Exp(-(((u - 0.72) * (u - 0.72)) + ((v - 0.7) * (v - 0.7))) / 0.06)));
            }
        }

        double[] traceX = [.. Enumerable.Range(0, 193).Select(i => i * Cells / 192.0)];
        double[] traceY = [.. traceX.Select(t => Settled((Cells * 0.5) + (Cells * 0.26 * Math.Sin(t / Cells * Math.PI * 1.5))))];

        return Plt.Create()
            .WithSize(128, 128)
            .WithTheme(Theme.CreateFrom(Theme.Default).WithBackground(Color.FromHex("#0B0F14")).Build())
            .AddSubPlot(1, 1, 1, ax => ax
                .Pcolormesh(edges, edges, field, s => s.ColorMap = ColorMaps.Viridis)
                .Plot(traceX, traceY, s => { s.Color = Color.FromHex("#FFFFFF"); s.LineWidth = 6.0; })
                .HideAllAxes()
                .WithGrid(g => g with { Visible = false })
                .WithLegend(visible: false))
            .WithSubPlotSpacing(sp => sp with
            {
                MarginLeft = 0, MarginRight = 0, MarginTop = 0, MarginBottom = 0,
                HorizontalGap = 0, VerticalGap = 0,
            })
            .Build();
    }
}
