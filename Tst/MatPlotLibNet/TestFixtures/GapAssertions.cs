// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>How the tests say a line in runs is written in its one form: a gap (a missing value, NaN) separates two runs, so it stands
/// between two values, never first, never last, never next to another gap.</summary>
public static class GapAssertions
{
    extension(IReadOnlyList<double> values)
    {
        /// <summary>Asserts that every gap of the line stands between two values, naming the first gap that does not.</summary>
        /// <param name="what">What the line is, named in the failure.</param>
        public void AssertGapsStandBetweenValues(string? what = null)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (!double.IsNaN(values[i]))
                {
                    continue;
                }

                bool betweenValues = i > 0 && i < values.Count - 1 && !double.IsNaN(values[i - 1]) && !double.IsNaN(values[i + 1]);
                Assert.True(betweenValues, $"{what}: a gap at {i} of {values.Count} stands first, last or next to another gap.");
            }
        }
    }
}
