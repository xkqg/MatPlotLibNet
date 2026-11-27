// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>How the tests say two numbers are the same to within a tolerance. A tolerance is always given: a test says how close is
/// close enough, and a sequence compares each element and names the one that differs.</summary>
public static class NumberAssertions
{
    extension(IReadOnlyList<double> expected)
    {
        /// <summary>Asserts that <paramref name="actual"/> has as many elements as the sequence and every element is within the
        /// absolute <paramref name="tolerance"/> of its counterpart (a NaN equals a NaN and nothing else), naming the index of the one
        /// that is not.</summary>
        /// <param name="actual">The numbers under test.</param>
        /// <param name="tolerance">How far each element may lie from the expected one, inclusive.</param>
        public void AssertCloseTo(IReadOnlyList<double> actual, double tolerance)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                if (double.IsNaN(expected[i]))
                {
                    Assert.True(double.IsNaN(actual[i]), $"[{i}]: expected NaN, got {actual[i]:R}");
                }
                else
                {
                    Assert.True(Math.Abs(expected[i] - actual[i]) <= tolerance, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
                }
            }
        }
    }

    extension(IReadOnlyList<double> expected)
    {
        /// <summary>Asserts that <paramref name="actual"/> has as many elements as the sequence and each lies within
        /// <paramref name="relative"/> of its counterpart, taken against the larger of the counterpart's size and
        /// <paramref name="floor"/>.</summary>
        /// <param name="actual">The numbers under test.</param>
        /// <param name="relative">The fraction of the scale each element may be off by, inclusive.</param>
        /// <param name="floor">The smallest scale the tolerance is taken against.</param>
        public void AssertCloseTo(IReadOnlyList<double> actual, double relative, double floor)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.True(Math.Abs(expected[i] - actual[i]) <= relative * Math.Max(floor, Math.Abs(expected[i])),
                    $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
            }
        }
    }

    extension(Range1D expected)
    {
        /// <summary>Asserts that both ends of <paramref name="actual"/> are within the absolute <paramref name="tolerance"/> of the range's.</summary>
        /// <param name="actual">The range under test.</param>
        /// <param name="tolerance">How far each end may lie from the expected one, inclusive.</param>
        public void AssertCloseTo(Range1D actual, double tolerance) =>
            Assert.True(Math.Abs(expected.Lo - actual.Lo) <= tolerance && Math.Abs(expected.Hi - actual.Hi) <= tolerance,
                $"expected {expected.Lo:R}..{expected.Hi:R}, got {actual.Lo:R}..{actual.Hi:R}");
    }

    extension(double expected)
    {
        /// <summary>Asserts that <paramref name="actual"/> lies within <paramref name="relative"/> of the number: the tolerance is
        /// <paramref name="relative"/> times the larger of the number's size and <paramref name="floor"/>, so a number near zero is not
        /// held to a tolerance near zero.</summary>
        /// <param name="actual">The number under test.</param>
        /// <param name="relative">The fraction of the scale the number may be off by, inclusive.</param>
        /// <param name="floor">The smallest scale the tolerance is taken against.</param>
        /// <param name="what">What the number is, named in the failure; <see langword="null"/> names only the two values.</param>
        public void AssertCloseTo(double actual, double relative, double floor, string? what = null)
        {
            double tolerance = Math.Max(Math.Abs(expected), floor) * relative;
            Assert.True(Math.Abs(actual - expected) <= tolerance,
                $"{(what is null ? "" : what + ": ")}expected {expected.ToString("R", CultureInfo.InvariantCulture)}, got {actual.ToString("R", CultureInfo.InvariantCulture)}");
        }
    }

    extension(Point expected)
    {
        /// <summary>Whether both coordinates are within the absolute <paramref name="tolerance"/> of <paramref name="actual"/>'s.</summary>
        /// <param name="actual">The point under test.</param>
        /// <param name="tolerance">How far each coordinate may lie from the expected one.</param>
        public bool IsCloseTo(Point actual, double tolerance) =>
            Math.Abs(expected.X - actual.X) <= tolerance && Math.Abs(expected.Y - actual.Y) <= tolerance;

        /// <summary>Asserts that <paramref name="actual"/> is close to the point, naming both when it is not.</summary>
        /// <param name="actual">The point under test.</param>
        /// <param name="tolerance">How far each coordinate may lie from the expected one.</param>
        public void AssertCloseTo(Point actual, double tolerance) =>
            Assert.True(expected.IsCloseTo(actual, tolerance), $"expected {expected}, got {actual} (tolerance {tolerance:R})");
    }

    extension(Rect expected)
    {
        /// <summary>Whether the corner and the size are within the absolute <paramref name="tolerance"/> of <paramref name="actual"/>'s.</summary>
        /// <param name="actual">The rectangle under test.</param>
        /// <param name="tolerance">How far each number may lie from the expected one.</param>
        public bool IsCloseTo(Rect actual, double tolerance) =>
            Math.Abs(expected.X - actual.X) <= tolerance && Math.Abs(expected.Y - actual.Y) <= tolerance
            && Math.Abs(expected.Width - actual.Width) <= tolerance && Math.Abs(expected.Height - actual.Height) <= tolerance;

        /// <summary>Asserts that <paramref name="actual"/> is close to the rectangle, naming both when it is not.</summary>
        /// <param name="actual">The rectangle under test.</param>
        /// <param name="tolerance">How far each number may lie from the expected one.</param>
        public void AssertCloseTo(Rect actual, double tolerance) =>
            Assert.True(expected.IsCloseTo(actual, tolerance), $"expected {expected}, got {actual} (tolerance {tolerance:R})");
    }
}
