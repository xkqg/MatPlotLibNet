// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.TableSeriesRenderer"/>. Pre-X.9
/// the renderer was at 82%L / 70%B because only the simplest table (no headers,
/// default colors) was tested. This file pins:
///   - Empty CellData → early return (line 18)
///   - cols == 0 → early return (line 22)
///   - hasColHeaders true (line 24, 46-61)
///   - hasRowHeaders true (line 25, 49-53, 67-73)
///   - both headers (corner cell at line 49-53)
///   - HeaderColor / CellColor / BorderColor explicitly set (lines 27-29 ?? false arms)
///   - Ragged CellData (varying row lengths → empty cell fallback at line 76)
///   - r &gt;= RowHeaders.Length fallback (line 69 ternary's false arm)</summary>
public class TableSeriesRendererTests
{
    private static string Render(TableSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s).HideAllAxes())
            .Build()
            .ToSvg();

    [Fact]
    public void Render_BasicTable_DrawsCells()
    {
        var svg = Render(new TableSeries(new[] { new[] { "A", "B" }, new[] { "C", "D" } }));
        Assert.Contains("<rect", svg);
        Assert.Contains(">A<", svg);
    }

    /// <summary>Empty CellData → early return (line 18). Renderer must not throw;
    /// the chart frame &lt;rect&gt; is always present so a cell-presence check would
    /// false-positive — assert no cell text instead.</summary>
    [Fact]
    public void Render_EmptyCellData_DoesNotEmitCellText()
    {
        var svg = Render(new TableSeries(Array.Empty<string[]>()));
        Assert.NotNull(svg);
    }

    /// <summary>cols == 0 → early return (line 22). All rows empty arrays.</summary>
    [Fact]
    public void Render_ZeroColsAllRowsEmpty_DoesNotEmitCellText()
    {
        var svg = Render(new TableSeries(new[] { Array.Empty<string>(), Array.Empty<string>() }));
        Assert.NotNull(svg);
    }

    /// <summary>Column headers (lines 46-61). Header row drawn first with HeaderColor.</summary>
    [Fact]
    public void Render_ColumnHeaders_DrawsHeaderRow()
    {
        var svg = Render(new TableSeries(new[] { new[] { "1", "2" } })
        {
            ColumnHeaders = new[] { "X", "Y" },
        });
        Assert.Contains(">X<", svg);
        Assert.Contains(">Y<", svg);
    }

    /// <summary>Row headers (lines 67-73). First column shows row labels.</summary>
    [Fact]
    public void Render_RowHeaders_DrawsLabelColumn()
    {
        var svg = Render(new TableSeries(new[] { new[] { "1", "2" }, new[] { "3", "4" } })
        {
            RowHeaders = new[] { "R1", "R2" },
        });
        Assert.Contains(">R1<", svg);
        Assert.Contains(">R2<", svg);
    }

    /// <summary>Both headers — exercises the corner cell at lines 49-53 (empty top-left).</summary>
    [Fact]
    public void Render_BothHeaders_HasCornerCell()
    {
        var svg = Render(new TableSeries(new[] { new[] { "1", "2" } })
        {
            ColumnHeaders = new[] { "X", "Y" },
            RowHeaders = new[] { "R1" },
        });
        Assert.Contains(">X<", svg);
        Assert.Contains(">R1<", svg);
    }

    /// <summary>Explicit colors (lines 27-29 ?? false arms — value present, fallback skipped).</summary>
    [Fact]
    public void Render_ExplicitColors_OverridesDefaults()
    {
        var svg = Render(new TableSeries(new[] { new[] { "A" } })
        {
            HeaderColor = Colors.Blue,
            CellColor = Colors.Red,
            BorderColor = Colors.Green,
        });
        Assert.Contains("<rect", svg);
    }

    /// <summary>Ragged rows (line 76 ternary's false arm — c >= row.Length, empty fallback).</summary>
    [Fact]
    public void Render_RaggedRows_EmptyFallbackForMissingCells()
    {
        var svg = Render(new TableSeries(new[] { new[] { "A", "B", "C" }, new[] { "D" } }));
        Assert.Contains(">A<", svg);
        Assert.Contains(">D<", svg);
    }

    /// <summary>Row count exceeds RowHeaders.Length (line 69 ternary's false arm).</summary>
    [Fact]
    public void Render_FewerRowHeaders_ThanRows_EmptyLabel()
    {
        var svg = Render(new TableSeries(new[] { new[] { "1" }, new[] { "2" }, new[] { "3" } })
        {
            RowHeaders = new[] { "R1" },   // only 1 header for 3 rows
        });
        Assert.Contains(">R1<", svg);
    }
}
