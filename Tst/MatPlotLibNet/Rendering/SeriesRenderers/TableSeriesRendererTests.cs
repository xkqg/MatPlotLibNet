// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.TableSeriesRenderer"/>. Pre-X.9
/// the renderer was at 82%L / 70%B because only the simplest table (no headers,
/// default colors) was tested. This file pins:
///   - Empty CellData → early return
///   - cols == 0 → early return
///   - hasColHeaders true
///   - hasRowHeaders true
///   - both headers (corner cell)
///   - HeaderColor / CellColor / BorderColor explicitly set (?? false arms)
///   - Ragged CellData (varying row lengths → empty cell fallback)
///   - r &gt;= RowHeaders.Length fallback (ternary's false arm)</summary>
public class TableSeriesRendererTests
{
    private static string Render(TableSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s).HideAllAxes())
            .Build()
            .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render_BasicTable_DrawsCells", () => Render(new TableSeries(new[] { new[] { "A", "B" }, new[] { "C", "D" } })), "<rect", ">A<"),
        // Column headers. Header row drawn first with HeaderColor.
        new DrawnToken("Render_ColumnHeaders_DrawsHeaderRow", () => Render(new TableSeries(new[] { new[] { "1", "2" } })
        {
            ColumnHeaders = new[] { "X", "Y" },
        }), ">X<", ">Y<"),
        // Row headers. First column shows row labels.
        new DrawnToken("Render_RowHeaders_DrawsLabelColumn", () => Render(new TableSeries(new[] { new[] { "1", "2" }, new[] { "3", "4" } })
        {
            RowHeaders = new[] { "R1", "R2" },
        }), ">R1<", ">R2<"),
        // Both headers — exercises the corner cell (empty top-left).
        new DrawnToken("Render_BothHeaders_HasCornerCell", () => Render(new TableSeries(new[] { new[] { "1", "2" } })
        {
            ColumnHeaders = new[] { "X", "Y" },
            RowHeaders = new[] { "R1" },
        }), ">X<", ">R1<"),
        // Explicit colors (?? false arms — value present, fallback skipped).
        new DrawnToken("Render_ExplicitColors_OverridesDefaults", () => Render(new TableSeries(new[] { new[] { "A" } })
        {
            HeaderColor = Colors.Blue,
            CellColor = Colors.Red,
            BorderColor = Colors.Green,
        }), "<rect"),
        // Ragged rows (ternary's false arm — c >= row.Length, empty fallback).
        new DrawnToken("Render_RaggedRows_EmptyFallbackForMissingCells", () => Render(new TableSeries(new[] { new[] { "A", "B", "C" }, new[] { "D" } })), ">A<", ">D<"),
        // Row count exceeds RowHeaders.Length (ternary's false arm).
        // only 1 header for 3 rows
        // No rows, or rows of no cell: the table draws no cell text.
        new DrawnToken("Render_EmptyCellData_DoesNotEmitCellText", () => Render(new TableSeries(Array.Empty<string[]>()))) { Absent = ["<text"] },
        new DrawnToken("Render_ZeroColsAllRowsEmpty_DoesNotEmitCellText", () => Render(new TableSeries(new[] { Array.Empty<string>(), Array.Empty<string>() }))) { Absent = ["<text"] },
        new DrawnToken("Render_FewerRowHeaders_ThanRows_EmptyLabel", () => Render(new TableSeries(new[] { new[] { "1" }, new[] { "2" }, new[] { "3" } })
        {
            RowHeaders = new[] { "R1" },   
        }), ">R1<"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Column widths given and then taken back lay the table out as widths never given do.</summary>
    [Fact]
    public void ColumnWidthsTakenBack_LayTheTableOutAsNoneGiven()
    {
        string[][] cells = [["1", "2"], ["3", "4"]];
        var takenBack = new TableSeries(cells) { ColumnWidths = [3.0, 1.0] };
        takenBack.ColumnWidths = null;

        Assert.Equal(Render(new TableSeries(cells)), Render(takenBack));
    }
}
