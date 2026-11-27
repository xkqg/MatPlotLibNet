// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>Every text the library draws is set as math when it holds a pair of <c>$</c>, as matplotlib sets every text: a legend's title, a
/// colour bar's label, the label of a reference line and of a span, an annotation, a text on the figure, the labels a tick formatter
/// returns and the labels a series writes. Each kind below is drawn as the rich text its markup parses to, and none is drawn as the
/// raw string with its dollar signs (a legend title was measured as math but drawn raw).</summary>
public sealed class LabelsReadMathTests
{
    private const string Markup = "$\\alpha$ rate";

    private static readonly double[] Xs = [1.0, 2.0, 3.0];

    private static readonly double[] Ys = [2.0, 1.0, 3.0];

    private static bool DrawnRaw(RecordingRenderContext recording) =>
        recording.Of<DrawTextCall>().Any(call => call.Text.Contains('$'));

    /// <summary>Whether the alpha of the markup was drawn as a run of the label's layout: its own character, not the command.</summary>
    private static bool DrawnAsMath(RecordingRenderContext recording) =>
        recording.Of<DrawTextCall>().Any(call => call.Text.Contains('α'));

    private static void AssertReadAsMath(Figure figure)
    {
        var recording = figure.Drawn();

        Assert.True(DrawnAsMath(recording), "the label is drawn as the rich text its markup parses to");
        Assert.False(DrawnRaw(recording), "no text is drawn with its dollar signs");
    }

    [Fact]
    public void ALegendTitle_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(Xs, Ys, s => s.Label = "line")
            .WithLegend(l => l with { Title = Markup })).Build());

    [Fact]
    public void AColorBarLabel_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } })
            .WithColorBar(cb => cb with { Label = Markup })).Build());

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AHorizontalOrVerticalReferenceLineLabel_ReadsMath(bool horizontal) =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            var plotted = ax.Plot(Xs, Ys);
            if (horizontal) plotted.AxHLine(1.5, r => r.Label = Markup);
            else plotted.AxVLine(1.5, r => r.Label = Markup);
        }).Build());

    [Fact]
    public void ASpanLabel_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(Xs, Ys)
            .AxVSpan(1.2, 1.8, s => s.Label = Markup)).Build());

    [Fact]
    public void AnAnnotation_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(Xs, Ys)
            .Annotate(Markup, 2, 2)).Build());

    [Fact]
    public void AFigureText_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(Xs, Ys))
            .FigText(Markup, 0.5, 0.5).Build());

    [Fact]
    public void ATickFormatterOutput_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(Xs, Ys)
            .SetXTickFormatter(new TestTickFormatter(value => $"$\\alpha_{{{(int)value}}}$"))).Build());

    [Fact]
    public void ASeriesLabel_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Funnel([Markup, "b", "c"], [100.0, 60.0, 30.0])).Build());

    /// <summary>The labels the label layout places read math as every other text does: it measures them by the same seam that draws them,
    /// so a pie's outer label is not plain while the same pie's inner label is math.</summary>
    [Fact]
    public void APieOuterLabel_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Pie([1.0, 2.0, 3.0], [Markup, "b", "c"])).Build());

    [Fact]
    public void APieInnerLabel_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Pie([1.0, 2.0, 3.0], null, s => s.AutoPct = "%.0f $\\alpha$")).Build());

    [Fact]
    public void ABarValueLabel_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Bar(["a", "b"], [1.0, 2.0], s =>
        {
            s.ShowLabels = true;
            s.LabelFormat = "0'$\\alpha$'";
        })).Build());

    [Fact]
    public void ASunburstLabel_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Sunburst(new TreeNode
        {
            Label = "root",
            Children = [new TreeNode { Label = Markup, Value = 3 }, new TreeNode { Label = "b", Value = 2 }],
        })).Build());

    [Fact]
    public void ASankeyOuterLabel_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(
            [new(Markup, Color.FromHex("#1F77B4")), new("b", Color.FromHex("#FF7F0E"))], [new(0, 1, 10)])).Build());

    [Fact]
    public void AWordOfAWordCloud_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WordCloud([Markup, "rate"], [3.0, 1.0])).Build());

    [Fact]
    public void ATreeGridRowLabel_ReadsMath() =>
        AssertReadAsMath(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.TreeGrid([new TreeGridRow(Markup, ["1"])])).Build());

    /// <summary>A row of a tree grid says how deep it sits (<c>aria-level</c>) whether its label is plain or math, as one text.</summary>
    [Theory]
    [InlineData("Svc", false)]
    [InlineData("$x^{2}$ rate", true)]
    public void ATreeGridRow_CarriesItsLevelWhateverItsLabel(string label, bool math)
    {
        string svg = Plt.Create().WithSize(600, 200).AddSubPlot(1, 1, 1, ax => ax.TreeGrid(
            [new TreeGridRow(label, ["1"]), new TreeGridRow("child", ["2"]) { Depth = 1 }])).ToSvg();

        Assert.Equal(2, svg.AttributeValues("aria-level").Count(level => level is "1" or "2"));
        Assert.Contains("aria-level=\"1\"", svg);
        Assert.Equal(math, svg.Contains("> rate</text>", StringComparison.Ordinal)); // the text after the math, a run of its own
    }

    [Fact]
    public void ADollarSignAlone_IsStillDrawnAsText()
    {
        var recording = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(Xs, Ys)
            .AxHLine(1.5, r => r.Label = "price ($)")).Build().Drawn();

        Assert.Contains(recording.Of<DrawTextCall>(), call => call.Text == "price ($)");
        Assert.False(DrawnAsMath(recording));
    }

    /// <summary>A math text drawn into the collector of the marks a legend keeps clear of leaves a box for each run of its layout where
    /// the run stands: the superscript's box beside the x's and raised from it.</summary>
    [Fact]
    public void AMathLabelInTheMarkCollector_IsABoxForEachRunOfItsLayout()
    {
        var measuring = new RecordingRenderContext();
        var marks = new MarkObstacles.Builder();
        IRenderContext collecting = new MarkCollectingContext(measuring, marks);
        var font = new Font { Size = 12 };

        collecting.Draw(TextLabel.Of("$x^{2}$"), font, new TextPlacement(new Point(50, 50), TextAlignment.Left));

        var boxes = marks.Build().Boxes;
        Assert.Equal(2, boxes.Count);
        Assert.True(boxes[1].X >= boxes[0].X + boxes[0].Width, "the superscript stands right of the x");
        Assert.True(boxes[1].Y + boxes[1].Height < boxes[0].Y + boxes[0].Height, "the superscript stands higher than the x");
    }

    [Fact]
    public void AMathLabelAtAPointThatCannotBeDrawn_LeavesNoBoxInTheMarkCollector()
    {
        var marks = new MarkObstacles.Builder();
        IRenderContext collecting = new MarkCollectingContext(new RecordingRenderContext(), marks);

        collecting.Draw(TextLabel.Of("$x$"), new Font { Size = 12 }, new TextPlacement(new Point(double.NaN, 5), TextAlignment.Left) { Rotation = 30 });

        Assert.Empty(marks.Build().Boxes);
    }
}
