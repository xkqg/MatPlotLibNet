// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>What the public parse of a label allocates (<see cref="MathTextParser.Parse"/>: the label's tree and its flat view) stays at
/// or under what the flat parser allocated before math was laid out from a tree, on the labels the benchmark parses
/// (<c>MathTextBenchmarks.ParseFiveLabels</c> and <c>ParseAMathLabel</c>): 2,896 bytes for the five, 664 for the fraction. The rows a
/// parse reads and the run of text it joins are written on memory the thread keeps, so a parse allocates its result and nothing else.</summary>
public sealed class MathParseCostTests
{
    /// <summary>The labels of the benchmark: text around math, scripts and an operator, a fraction, a radical, and no math.</summary>
    private static readonly string[] Labels =
    [
        @"Latency ($\mu$s)",
        @"$\alpha_i^2 + \beta_i$",
        @"$\frac{\Delta y}{\Delta x}$",
        @"$\sqrt{x^2 + y^2}$",
        "Plain axis label with no math at all",
    ];

    /// <summary>The bytes one parse of each of <paramref name="labels"/> allocates, as the mean of many once the parse has run.</summary>
    private static long BytesOfAParse(params string[] labels)
    {
        const int Parses = 200;
        for (int i = 0; i < Parses; i++)
            foreach (var label in labels) _ = MathTextParser.Parse(label);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Parses; i++)
            foreach (var label in labels) _ = MathTextParser.Parse(label);
        return (GC.GetAllocatedBytesForCurrentThread() - before) / Parses;
    }

    [Fact]
    public void TheFiveLabelsOfTheBenchmark_AllocateNoMoreThanTheFlatParserDid()
    {
        long bytes = BytesOfAParse(Labels);

        Assert.True(bytes <= 2896, $"Parsing the five labels allocated {bytes} bytes; the flat parser allocated 2896.");
    }

    [Fact]
    public void AFraction_AllocatesNoMoreThanTheFlatParserDid()
    {
        long bytes = BytesOfAParse(Labels[2]);

        Assert.True(bytes <= 664, $"Parsing the fraction allocated {bytes} bytes; the flat parser allocated 664.");
    }

    /// <summary>A label with no math is its text in one box and one span: nothing is copied and no list is made for it.</summary>
    [Fact]
    public void APlainLabel_AllocatesItsOneBoxAndItsOneSpan()
    {
        long bytes = BytesOfAParse(Labels[4]);

        Assert.True(bytes <= 232, $"Parsing a plain label allocated {bytes} bytes; its box, its span and their two rows take 232.");
    }
}
