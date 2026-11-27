// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A text a series writes in a cell it paints (a table's cell, a heat map's square, a funnel's stage, a progress bar, a state's
/// segment, a Sankey node) is centred on the cell across its line by one rule, the box's (<c>TextBox</c>): its baseline 0.35 of its
/// ascent below the cell's middle, the ascent being the font size under the library's own measure. No kind drops its text by a
/// number of its own (four pixels, a third of the size, three tenths of the height) or not at all.</summary>
public sealed class TextCentredInItsCellTests
{
    public static TheoryData<string> Kinds { get; } = ["table", "heatmap", "funnel", "progress", "timeline", "sankey"];

    private static Figure Of(string kind) => Plt.Create().WithSize(640, 480).AddSubPlot(1, 1, 1, ax => _ = kind switch
    {
        // In the plot: below the axes (the matplotlib themes' place) the X axis' tick labels stand over the header cells.
        "table" => ax.Table([["a", "b"], ["c", "d"]], t => { t.ColumnHeaders = ["X", "Y"]; t.RowHeaders = ["r1", "r2"]; t.Location = TableLocation.InPlot; }),
        "heatmap" => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }, h => h.ShowLabels = true),
        "funnel" => ax.Funnel(["Seen", "Tried", "Kept"], [100, 60, 20]),
        "progress" => ax.ProgressBar(0.62),
        "timeline" => ax.StateTimeline([new StateSegment(0, 4, "idle", Colors.Tab10Blue), new StateSegment(4, 10, "busy", Colors.Tab10Orange)]),
        "sankey" => ax.Sankey([new SankeyNode("From"), new SankeyNode("To")], [new SankeyLink(0, 1, 5)], s => s.InsideLabels = true),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    }).Build();

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ATextWrittenInACell_IsCentredOnTheCell_ByTheBox(string kind)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(Of(kind), recording);

        int checkedTexts = 0;
        var cells = new List<Rect>();
        var written = new HashSet<string>();
        foreach (var call in recording.Calls)
        {
            if (call is DrawRectangleCall { Rect: var rect })
            {
                if (rect != new Rect(0, 0, 640, 480)) cells.Add(rect);   // the figure's ground is no cell
                continue;
            }
            if (call is not DrawTextCall args) continue;

            var at = args.Position;
            var font = args.Font;
            var holding = cells.Where(r => r.Width > 0 && r.Height > 0 && at.X >= r.X - 1e-9 && at.X <= r.X + r.Width + 1e-9
                && at.Y >= r.Y && at.Y <= r.Y + r.Height).Select(r => (Rect?)r).LastOrDefault();
            if (holding is not { } cell) continue;   // a text outside every cell (a tick label) is no cell's text
            if (!written.Add(args.Text)) continue;   // the legend repeats a name after the series wrote it

            Assert.Equal(cell.Y + cell.Height / 2 + 0.35 * font.Size, at.Y, 9);
            checkedTexts++;
        }

        Assert.True(checkedTexts > 0, $"no text of the {kind} stands in a cell it paints");
    }
}
