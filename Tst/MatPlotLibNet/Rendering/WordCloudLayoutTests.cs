// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A word cloud sets each word in a size that follows its weight and packs the boxes of the words into a canvas without
/// overlap, the heaviest at the centre. The layout is pure: given the words, the weights, the canvas and a source of text
/// metrics it lays the same boxes out on every machine, with no random choice and no transcendental function (the packing walks
/// a rectangular spiral on an integer grid). A word that does not fit is never dropped: every size shrinks together toward the
/// smallest, and a word that still does not fit is reported as not placed.</summary>
public class WordCloudLayoutTests
{
    private static readonly Rect Canvas = new(0, 0, 800, 600);

    /// <summary>The metrics every vector is worked out under: a word is 0.6 of its font size wide for each character and as
    /// high as its font size, whatever the font, so the boxes do not depend on a machine's fonts.</summary>
    private sealed class FakeMetrics : IFontMetrics
    {
        public Size Measure(string text, Font font) => new(text.Length * font.Size * 0.6, font.Size);
    }

    private static WordCloudSpec Spec(string[] words, double[] weights, double min = 12, double max = 60) =>
        new(words, weights, new Range1D(min, max), new Font());

    private static WordCloudResult Lay(string[] words, double[] weights, Rect? canvas = null) =>
        WordCloudLayout.Compute(Spec(words, weights), canvas ?? Canvas, new FakeMetrics());

    private static readonly string[] Four = ["alpha", "beta", "gamma", "delta"];

    // ── W1: sizes ───────────────────────────────────────────────────────────────

    [Fact]
    public void W1_TheSizeFollowsTheSquareRootOfTheWeightBetweenTheSmallestAndTheLargest()
    {
        var result = Lay(Four, [100, 25, 4, 1]);

        // size = min + (max - min) * sqrt((w - wmin) / (wmax - wmin))
        Assert.Equal(60.0, result.Words[0].Size, 1e-4);
        Assert.Equal(35.6336, result.Words[1].Size, 1e-4);
        Assert.Equal(20.3557, result.Words[2].Size, 1e-4);
        Assert.Equal(12.0, result.Words[3].Size, 1e-4);
    }

    [Fact]
    public void W1_OneWordGetsTheMaximumSize_AndEqualWeightsGetEqualSizes()
    {
        Assert.Equal(60.0, Lay(["only"], [3]).Words[0].Size);

        var equal = Lay(["a", "b", "c"], [7, 7, 7]);

        Assert.Single(equal.Words.Select(w => w.Size).Distinct());
        Assert.Equal(60.0, equal.Words[0].Size);
    }

    [Fact]
    public void TheWordsAreReportedInTheOrderTheyWereGiven_WithTheirWeights()
    {
        var result = Lay(["small", "big", "mid"], [1, 100, 25]);

        Assert.Equal(["small", "big", "mid"], result.Words.Select(w => w.Word));
        Assert.Equal([1.0, 100.0, 25.0], result.Words.Select(w => w.Weight));
        Assert.Equal([0, 1, 2], result.Words.Select(w => w.Index));
    }

    // ── W2: the first word is at the centre ─────────────────────────────────────

    [Fact]
    public void W2_TheHeaviestWord_IsCentredOnTheCanvas()
    {
        var result = Lay(["small", "big", "mid"], [1, 100, 25]);

        var box = result.Words[1].Box;
        Assert.Equal(Canvas.X + Canvas.Width / 2, box.X + box.Width / 2, 0.5);
        Assert.Equal(Canvas.Y + Canvas.Height / 2, box.Y + box.Height / 2, 0.5);
    }

    [Fact]
    public void WordsOfEqualWeight_AreSetInOrdinalOrder_SoTheFirstByName_IsTheCentred()
    {
        var result = Lay(["pear", "apple", "Zebra"], [5, 5, 5]);

        // Ordinal string order: "Zebra" (uppercase) comes before "apple" before "pear".
        var centred = result.Words.OrderBy(w => Math.Abs(w.Box.X + w.Box.Width / 2 - 400) + Math.Abs(w.Box.Y + w.Box.Height / 2 - 300)).First();
        Assert.Equal("Zebra", centred.Word);
    }

    // ── W3: no overlap, inside the canvas ───────────────────────────────────────

    [Fact]
    public void W3_NoTwoBoxesOverlap_WithAGapOfTwoPixels_AndEveryBoxIsInsideTheCanvas()
    {
        string[] words = [.. Enumerable.Range(0, 40).Select(i => "word" + i)];
        double[] weights = [.. Enumerable.Range(0, 40).Select(i => 1.0 + (i * 7 % 13))];

        var result = Lay(words, weights);

        var boxes = result.Words.Where(w => w.Placed).Select(w => w.Box).ToArray();
        Assert.Equal(40, boxes.Length);
        for (int i = 0; i < boxes.Length; i++)
        {
            Assert.True(boxes[i].X >= Canvas.X && boxes[i].Y >= Canvas.Y
                        && boxes[i].X + boxes[i].Width <= Canvas.X + Canvas.Width
                        && boxes[i].Y + boxes[i].Height <= Canvas.Y + Canvas.Height, $"box {i} leaves the canvas");
            for (int j = i + 1; j < boxes.Length; j++)
            {
                // A gap of two pixels: each box grown by one pixel on every side still does not touch the other.
                Assert.False(boxes[i].Inflate(1, 1).Intersects(boxes[j].Inflate(1, 1)), $"boxes {i} and {j} are closer than the padding");
            }
        }
    }

    // ── W4: determinism ─────────────────────────────────────────────────────────

    [Fact]
    public void W4_TheSameInputLaysOutTheSameBoxes_EveryTime()
    {
        string[] words = [.. Enumerable.Range(0, 25).Select(i => "w" + i)];
        double[] weights = [.. Enumerable.Range(0, 25).Select(i => 1.0 + i % 6)];

        var first = Lay(words, weights);
        var second = Lay(words, weights);

        Assert.Equal(first.Words, second.Words);
        Assert.Equal(first.Shrink, second.Shrink);
    }

    [Fact]
    public void ThePackingIsFastEnough_ForAHundredWords()
    {
        string[] words = [.. Enumerable.Range(0, 100).Select(i => "term" + i)];
        double[] weights = [.. Enumerable.Range(0, 100).Select(i => 1.0 + (i * 31 % 17))];
        var watch = Stopwatch.StartNew();

        var result = Lay(words, weights);

        // About half a second here, and ten times that under the coverage collector the CI job runs this suite with, so the bound is
        // one only a blow-up reaches (a layout that grew with the cube of the words would take minutes), not one a slow runner does.
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30), $"100 words took {watch.Elapsed}");
        Assert.Equal(100, result.Words.Count);
    }

    // ── W5: refusals ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void W5_AWeightThatIsNotAPositiveNumber_IsRefusedByItsWord(double weight)
    {
        var refusal = Assert.Throws<ArgumentException>(() => Lay(["fine", "broken"], [3, weight]));

        Assert.Contains("broken", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("weight", refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void W5_AnEmptyWord_IsRefusedByItsPosition(string word)
    {
        var refusal = Assert.Throws<ArgumentException>(() => Lay(["fine", word], [3, 4]));

        Assert.Contains("word 1", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void W5_ADuplicateWord_IsRefusedByName()
    {
        var refusal = Assert.Throws<ArgumentException>(() => Lay(["fine", "twice", "twice"], [3, 4, 5]));

        Assert.Contains("twice", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("more than once", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WordsAndWeightsOfDifferentLengths_AreRefused()
    {
        Assert.Throws<ArgumentException>(() => Lay(["a", "b"], [1]));
    }

    [Fact]
    public void NoWords_LayOutNothing_AsASeriesWithNoDataIsAnEmptyPlot()
    {
        var result = Lay([], []);

        Assert.Empty(result.Words);
        Assert.Equal(0, result.Unplaced);
    }

    [Theory]
    [InlineData(0.0, 10.0)]
    [InlineData(20.0, 10.0)]
    [InlineData(double.NaN, 10.0)]
    public void ASizeRangeThatCannotBeSet_IsRefused(double min, double max)
    {
        Assert.Throws<ArgumentException>(() => WordCloudLayout.Compute(Spec(["a"], [1], min, max), Canvas, new FakeMetrics()));
    }

    // ── W6: nothing is dropped ──────────────────────────────────────────────────

    [Fact]
    public void W6_WhenTheWordsDoNotFit_EverySizeShrinksTogetherTowardTheSmallest()
    {
        string[] words = [.. Enumerable.Range(0, 14).Select(i => "token" + i)];
        double[] weights = [.. Enumerable.Range(0, 14).Select(i => 1.0 + i)];
        var small = new Rect(0, 0, 330, 150);

        var result = Lay(words, weights, small);

        Assert.True(result.Shrink < 1, "this canvas cannot hold the words at full size");
        Assert.Equal(0, result.Unplaced);
        Assert.All(result.Words, w => Assert.True(w.Placed));
        // Every size moved toward the smallest by the same share: the order of the sizes is kept.
        var full = Lay(words, weights, Canvas);
        Assert.Equal(1.0, full.Shrink);
        for (int i = 0; i < words.Length; i++)
        {
            Assert.Equal(12 + (full.Words[i].Size - 12) * result.Shrink, result.Words[i].Size, 1e-9);
        }
    }

    [Fact]
    public void W6_ACanvasTooSmallForAllTheWords_KeepsEveryRow_AndMarksTheWordsThatDidNotFit()
    {
        string[] words = [.. Enumerable.Range(0, 30).Select(i => "longerword" + i)];
        double[] weights = [.. Enumerable.Range(0, 30).Select(i => 1.0 + i)];

        var result = Lay(words, weights, new Rect(0, 0, 120, 60));

        Assert.Equal(30, result.Words.Count);                       // none is dropped
        Assert.True(result.Unplaced > 0);
        Assert.Equal(result.Unplaced, result.Words.Count(w => !w.Placed));
        Assert.Contains(result.Words, w => w.Placed);
        Assert.Equal(0.0, result.Shrink);                           // shrunk as far as it goes
    }

    [Fact]
    public void AWordWiderThanTheCanvas_IsMarkedNotPlaced_NotClipped()
    {
        var result = Lay(["extraordinarilylongwordindeed"], [1], new Rect(0, 0, 50, 50));

        Assert.False(result.Words[0].Placed);
        Assert.Equal(1, result.Unplaced);
    }
}
