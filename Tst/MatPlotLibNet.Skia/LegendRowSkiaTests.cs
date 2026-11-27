// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Each row of a legend is laid out at its own text height, as matplotlib stacks the entries of a column (a <c>VPacker</c> of their
/// boxes, <c>labelspacing</c> apart) and stands the columns on the baseline of their first rows (an <c>HPacker</c>, align 'baseline'); each
/// text is boxed at least as tall as 'lp' (a <c>TextArea</c>). The text is measured as Agg measures it under the matplotlib themes, which
/// the package's glyph metrics do, so the rows are matplotlib's own. Printed with matplotlib 3.11.2, 800 x 600 at 100 dpi,
/// <c>ax.legend(loc='upper left')</c>, the top and height of each entry's box (<c>_legend_handle_box</c>'s children) in pixels from the top
/// of the figure.</summary>
public sealed class LegendRowSkiaTests
{
    private static IReadOnlyList<Rect> Rows(Theme theme, int columns, params string[] labels)
    {
        var figure = Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax =>
            {
                foreach (var label in labels) ax.Plot([0.0, 1.0], [0.0, 1.0], s => s.Label = label);
                ax.WithLegend(l => l with { Position = Models.LegendPosition.UpperLeft, NCols = columns });
            })
            .Build();
        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);
        return [.. new ChartRenderer().ComputeLayout(figure, new SkiaRenderContext(canvas)).LegendItems[0].Select(item => item.Bounds)];
    }

    private const string Squared = "$x^2$";

    /// <summary>Default style: '$x^2$' then 'x': rows at 84.500 (16.337 tall) and 107.781 (14.337 tall); 'x' then '$x^2$': rows at 84.500
    /// (14.337) and 105.781 (16.337). The plain row is not as tall as the raised one's, wherever it stands. Classic style: 'x' then '$x^2$':
    /// rows at 78.000 (20.000 tall) and 108.000 (22.805 tall); '$x^2$' then 'x': rows at 78.000 (22.805 tall) and 110.805 (20.000 tall). The
    /// raised row is the math line's ascent (matplotlib's <c>Text._get_layout</c>: 19 less its descent of 1) over the descent of 'lp' (4.805).</summary>
    [Theory]
    [InlineData("v2", new[] { 84.500, 107.781 }, new[] { 16.337, 14.337 }, new[] { 84.500, 105.781 }, new[] { 14.337, 16.337 })]
    [InlineData("classic", new[] { 78.000, 110.805 }, new[] { 22.805, 20.000 }, new[] { 78.000, 108.000 }, new[] { 20.000, 22.805 })]
    public void EveryRowIsAsTallAsItsOwnText(string themeId, double[] tallFirstY, double[] tallFirstHeight, double[] plainFirstY, double[] plainFirstHeight)
    {
        var tallFirst = Rows(themeId.AsTheme(), 1, Squared, "x");
        var plainFirst = Rows(themeId.AsTheme(), 1, "x", Squared);

        Assert.Equal(tallFirstY, tallFirst.Select(r => Math.Round(r.Y, 3)));
        Assert.Equal(tallFirstHeight, tallFirst.Select(r => Math.Round(r.Height, 3)));
        Assert.Equal(plainFirstY, plainFirst.Select(r => Math.Round(r.Y, 3)));
        Assert.Equal(plainFirstHeight, plainFirst.Select(r => Math.Round(r.Height, 3)));
    }

    /// <summary>'x', 'y', '$x^2$', 'q' in two columns ([x, y] and [$x^2$, q]): the first column drops by what '$x^2$' rises above 'x'. Default
    /// style: rows at 86.500, 107.781 | 84.500, 107.781; classic: 80.805, 110.805 | 78.000, 110.805.</summary>
    [Fact]
    public void TheColumnsStandOnTheBaselineOfTheirFirstRows()
    {
        Assert.Equal([86.500, 107.781, 84.500, 107.781], Rows(Theme.MatplotlibV2, 2, "x", "y", Squared, "q").Select(r => Math.Round(r.Y, 3)));
        Assert.Equal([80.805, 110.805, 78.000, 110.805], Rows(Theme.MatplotlibClassic, 2, "x", "y", Squared, "q").Select(r => Math.Round(r.Y, 3)));
    }
}
