// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>How the tests say a number is finite: neither a missing value (NaN) nor an infinity. A sequence, a point and a rectangle
/// name the index of the first number that is not. The text a drawing wrote is held to the same: it carries no <c>NaN</c> and no
/// <c>Infinity</c>. A test that expects a missing value on purpose, a gap, says <c>double.IsNaN</c> itself and gives the reason.</summary>
public static class FiniteAssertions
{
    /// <summary>A run of text, up to the quote or the tag around it, that names a missing value or an infinity.</summary>
    private static readonly Regex Tokens = new(@"[^""<>]*(?:NaN|Infinity)[^""<>]*", RegexOptions.CultureInvariant);

    extension(double value)
    {
        /// <summary>Asserts that the number is finite, naming it when it is not.</summary>
        /// <param name="what">What the number is, named in the failure; <see langword="null"/> names only the value.</param>
        public void AssertFinite(string? what = null)
        {
            if (!double.IsFinite(value))
            {
                Assert.Fail($"{(what is null ? "" : what + ": ")}{value.ToString("R", CultureInfo.InvariantCulture)} is not finite");
            }
        }
    }

    extension(IEnumerable<double> values)
    {
        /// <summary>Asserts that every number of the sequence is finite, naming the index and the value of the first that is not.</summary>
        /// <param name="what">What the sequence is, named in the failure before the index; <see langword="null"/> names the index alone.</param>
        public void AssertAllFinite(string? what = null)
        {
            int index = 0;
            foreach (double value in values)
            {
                if (!double.IsFinite(value)) value.AssertFinite($"{what}[{index}]");
                index++;
            }
        }
    }

    extension(Point point)
    {
        /// <summary>Asserts that both coordinates of the point are finite (the index is 0 for X, 1 for Y).</summary>
        /// <param name="what">What the point is, named in the failure before the index.</param>
        public void AssertFinite(string? what = null) => new[] { point.X, point.Y }.AssertAllFinite(what);
    }

    extension(Rect rect)
    {
        /// <summary>Asserts that the corner and the size of the rectangle are finite (the index is 0 for X, 1 for Y, 2 for the width and 3
        /// for the height).</summary>
        /// <param name="what">What the rectangle is, named in the failure before the index.</param>
        public void AssertFinite(string? what = null) => new[] { rect.X, rect.Y, rect.Width, rect.Height }.AssertAllFinite(what);
    }

    extension(string text)
    {
        /// <summary>Asserts that the text a drawing wrote (the markup of a figure, a label, a JSON) carries no missing value and no
        /// infinity: no <c>NaN</c> and no <c>Infinity</c> token, the scripts of a figure left out. Names the first runs of text that do.</summary>
        /// <param name="what">What the text is, named in the failure; <see langword="null"/> names it "the text".</param>
        public void AssertNoNonFiniteNumber(string? what = null)
        {
            string[] tokens = [.. Tokens.Matches(text.WithoutScripts()).Select(match => match.Value).Take(5)];
            Assert.True(tokens.Length == 0, $"{what ?? "the text"} carries a missing value or an infinity: {string.Join(" | ", tokens)}");
        }
    }

    extension(IEnumerable<string> texts)
    {
        /// <summary>Asserts that every text of the sequence carries no missing value and no infinity, naming the index of the first that does.</summary>
        /// <param name="what">What the sequence is, named in the failure before the index.</param>
        public void AssertNoNonFiniteNumber(string? what = null)
        {
            int index = 0;
            foreach (string text in texts)
            {
                text.AssertNoNonFiniteNumber($"{what}[{index++}]");
            }
        }
    }
}
