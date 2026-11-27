// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>What a word cloud is made of: the words, the weight of each, the range of sizes the weights are spread over, and the
/// font they are set in (whose size is replaced by each word's own).</summary>
/// <param name="Words">The words, none empty and none twice.</param>
/// <param name="Weights">The weight of each word, a finite number above zero; only the proportions matter.</param>
/// <param name="Sizes">The size of the lightest and of the heaviest word, in pixels.</param>
/// <param name="Font">The font of the words.</param>
internal readonly record struct WordCloudSpec(IReadOnlyList<string> Words, IReadOnlyList<double> Weights, Range1D Sizes, Font Font);

/// <summary>One word of a cloud: the size it is set in and the box it takes, or that it found no room.</summary>
/// <param name="Index">The word's position in the input.</param>
/// <param name="Word">The word.</param>
/// <param name="Weight">Its weight, as given.</param>
/// <param name="Size">Its font size, after any shrinking.</param>
/// <param name="Box">The box the word takes, in the canvas; meaningless when the word is not placed.</param>
/// <param name="Placed">Whether the word found room.</param>
internal readonly record struct WordPlacement(int Index, string Word, double Weight, double Size, Rect Box, bool Placed);

/// <summary>The words of a cloud in the order they were given.</summary>
/// <param name="Words">One placement for each word; none is dropped.</param>
/// <param name="Shrink">How much of the size range the words kept: 1 when they all fit at full size, 0 when they were shrunk to
/// the smallest and still did not all fit.</param>
internal readonly record struct WordCloudResult(IReadOnlyList<WordPlacement> Words, double Shrink)
{
    /// <summary>How many words found no room.</summary>
    public int Unplaced => Words.Count(word => !word.Placed);
}

/// <summary>The one owner of how the words of a cloud are sized and packed, with no drawing, no random choice and no
/// transcendental function, so that the same words lay out the same boxes on every machine. The size follows the square root of
/// the weight between the smallest and the largest. The heaviest word is set first, at the centre; the rest follow in descending
/// weight (ties in ordinal order of the word), each at the first place on a rectangular spiral round the centre, on an integer
/// grid, where its box is inside the canvas and clear of every box already set by the padding. Text is horizontal. When the words
/// do not all fit, every size moves toward the smallest by the same share until they do; a word that still does not fit is marked
/// as not placed and never dropped.</summary>
internal static class WordCloudLayout
{
    /// <summary>The clear space between two boxes, in pixels.</summary>
    public const double Padding = 2;

    /// <summary>The step of the spiral, in pixels: the grid the boxes are set on.</summary>
    private const int GridStep = 2;

    /// <summary>How much of the size range the words keep, tried in turn: all of it, three quarters, half, a quarter, none.</summary>
    private static readonly double[] Shares = [1, 0.75, 0.5, 0.25, 0];

    /// <summary>Lays out a word cloud.</summary>
    /// <param name="spec">The words, their weights, the size range and the font.</param>
    /// <param name="canvas">The room the words are set in.</param>
    /// <param name="metrics">The source of the width and height of each word, the one every other text of the library is measured with.</param>
    /// <returns>One placement for each word, in the order given.</returns>
    /// <exception cref="ArgumentException">The words and weights differ in length, or a word is empty or appears twice (the message
    /// names the word or the place).</exception>
    /// <exception cref="ArgumentOutOfRangeException">A weight is not a finite number above zero; the message names its word.</exception>
    public static WordCloudResult Compute(WordCloudSpec spec, Rect canvas, IFontMetrics metrics)
    {
        Refuse(spec);
        int count = spec.Words.Count;
        if (count == 0) return new WordCloudResult([], 1);

        double lightest = spec.Weights.Min(), heaviest = spec.Weights.Max();
        double[] full = [.. spec.Weights.Select(weight => SizeOf(weight, lightest, heaviest, spec.Sizes))];
        int[] order = [.. Enumerable.Range(0, count).OrderByDescending(i => spec.Weights[i]).ThenBy(i => spec.Words[i], StringComparer.Ordinal)];

        WordCloudResult result = default;
        foreach (double share in Shares)
        {
            double[] sizes = [.. full.Select(size => spec.Sizes.Lo + (size - spec.Sizes.Lo) * share)];
            result = new WordCloudResult(Pack(spec, order, sizes, canvas, metrics), share);
            if (result.Unplaced == 0) break;
        }

        return result;
    }

    /// <summary>The size of a word of this weight: the smallest size for the lightest word, the largest for the heaviest, and
    /// between them the square root of how far up the weights the word stands. One weight, or all the same, is the largest.</summary>
    internal static double SizeOf(double weight, double lightest, double heaviest, Range1D sizes) =>
        heaviest == lightest
            ? sizes.Hi
            : sizes.Lo + (sizes.Hi - sizes.Lo) * Math.Sqrt((weight - lightest) / (heaviest - lightest));

    private static WordPlacement[] Pack(WordCloudSpec spec, int[] order, double[] sizes, Rect canvas, IFontMetrics metrics)
    {
        var blocked = new List<Rect>(order.Length);   // each box set, with the padding round it, so a probe compares two rectangles and makes none
        var words = new WordPlacement[order.Length];
        double free = canvas.Width * canvas.Height;   // the boxes set never overlap, so what they leave is exactly this much
        double centreX = canvas.Center.X, centreY = canvas.Center.Y;

        foreach (int i in order)
        {
            var measured = metrics.ExtentOf(spec.Words[i], spec.Font with { Size = sizes[i] }).Box;
            var box = measured.Width * measured.Height > free ? null : Find(measured, centreX, centreY, canvas, blocked);
            if (box is { } found)
            {
                blocked.Add(found.Inflate(Padding / 2, Padding / 2));
                free -= found.Width * found.Height;
            }
            words[i] = new WordPlacement(i, spec.Words[i], spec.Weights[i], sizes[i], box ?? default, box is not null);
        }

        return words;
    }

    /// <summary>The first place for a box of this size on the spiral round the centre: inside the canvas and clear of the boxes
    /// set. <see langword="null"/> when there is none, which is when the spiral has left every place the box could stand in. The spiral
    /// is rectangular, on the integer grid of <see cref="GridStep"/>: the origin, then right one, up one, left two, down two, right
    /// three, up three, and so on, out to the ring the room round the box allows.</summary>
    /// <param name="size">The size of the box to set.</param>
    /// <param name="centreX">The X of the centre of the canvas, where the spiral begins.</param>
    /// <param name="centreY">The Y of the centre of the canvas, where the spiral begins.</param>
    /// <param name="canvas">The room the box has to stand in.</param>
    /// <param name="blocked">The boxes already set, each grown by half the padding on every side.</param>
    private static Rect? Find(Size size, double centreX, double centreY, Rect canvas, List<Rect> blocked)
    {
        // The box can only be moved as far from the centre as the room round it allows.
        double reachX = (canvas.Width - size.Width) / 2 / GridStep, reachY = (canvas.Height - size.Height) / 2 / GridStep;
        if (reachX < 0 || reachY < 0) return null;
        double radius = Math.Max(reachX, reachY);

        int x = 0, y = 0;
        if (At(x, y) is { } origin) return origin;
        for (int leg = 1, direction = 0; Math.Max(Math.Abs(x), Math.Abs(y)) <= radius + 1; leg++)
        {
            for (int pass = 0; pass < 2; pass++, direction++)
            {
                for (int step = 0; step < leg; step++)
                {
                    switch (direction % 4)
                    {
                        case 0: x++; break;
                        case 1: y--; break;
                        case 2: x--; break;
                        default: y++; break;
                    }

                    if (At(x, y) is { } box) return box;
                }
            }
        }

        return null;

        // The box a step (stepX, stepY) from the centre, when it is within reach, inside the canvas and clear of the boxes set.
        Rect? At(int stepX, int stepY)
        {
            if (Math.Abs(stepX) > reachX || Math.Abs(stepY) > reachY) return null;

            var box = new Rect(Range1D.Around(centreX, size.Width).Lo + stepX * GridStep, Range1D.Around(centreY, size.Height).Lo + stepY * GridStep, size.Width, size.Height);
            if (box.X < canvas.X || box.Y < canvas.Y || box.X + box.Width > canvas.X + canvas.Width || box.Y + box.Height > canvas.Y + canvas.Height)
                return null;

            return Touches(box.Inflate(Padding / 2, Padding / 2), blocked) ? null : box;
        }
    }

    /// <summary>Whether a box touches any of the boxes already set.</summary>
    /// <param name="box">The box, with its padding.</param>
    /// <param name="blocked">The boxes already set, each with its padding.</param>
    private static bool Touches(Rect box, List<Rect> blocked)
    {
        foreach (var other in blocked)
        {
            if (box.Intersects(other)) return true;
        }

        return false;
    }

    /// <summary>Refuses a cloud that cannot be laid out: words and weights of different lengths, a weight that is not a finite
    /// number above zero, an empty or repeated word. Compute does it first; the verb of the series does it before a series exists.
    /// The size range is not checked here: a spec takes it from <c>WordCloudSeries.Sizes</c>, whose setter refuses a range that is
    /// not above zero, or from the verb's resting range.</summary>
    /// <param name="spec">The cloud.</param>
    /// <exception cref="ArgumentException">The cloud cannot be laid out; the message names the word or the place.</exception>
    internal static void Refuse(WordCloudSpec spec)
    {
        static string Weight(string word) => $"The weight of \"{word}\"";
        spec.Weights.Count.ThrowIfMisaligned(spec.Words.Count, nameof(spec));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < spec.Words.Count; i++)
        {
            string word = spec.Words[i];
            if (string.IsNullOrWhiteSpace(word))
                throw new ArgumentException(string.Invariant($"A word cloud has an empty word: word {i} is empty, and a word is text."), nameof(spec));
            if (!seen.Add(word))
                throw new ArgumentException($"The word \"{word}\" is given more than once; give each word once, with its total weight.", nameof(spec));
            spec.Weights[i].ThrowIfNotFinite(nameof(spec), word, Weight).ThrowIfNotAbove(0, nameof(spec), word, Weight);
        }
    }
}
