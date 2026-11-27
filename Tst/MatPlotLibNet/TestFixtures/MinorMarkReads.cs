// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>What the tests of the tick marks of a 3-D axis read off its SVG: where each mark of one colour stands along X, and whether the
/// minor marks (red) split each step between the major marks (blue) in equal parts. The SVG is drawn with vertex data.</summary>
internal static class MinorMarkReads
{
    extension(string svg)
    {
        /// <summary>The first normalised X of every line of <paramref name="stroke"/>: the normalised coordinate is linear in the data value, so
        /// it shows which value a tick mark stands on.</summary>
        /// <param name="stroke">The colour of the lines, as hex.</param>
        internal double[] NormalisedXs(string stroke) =>
            [.. svg.Elements("line").Where(line => line["stroke"] == stroke && line["data-v3d"] is not null)
                .Select(line => line.Vertices()[0][0])
                .Order()];

        /// <summary>Checks that the red minor marks split each step between the blue major marks into <paramref name="parts"/> equal parts:
        /// <paramref name="between"/> of them stand between the first and the last major mark, each on a multiple of the part.</summary>
        /// <param name="parts">How many parts a step is split into.</param>
        /// <param name="between">How many minor marks stand between the first and the last major mark.</param>
        internal void AssertMinorMarksSplitEachMajorStepIn(int parts, int between)
        {
            double[] majors = svg.NormalisedXs(Colors.Blue.ToHex());
            double[] minors = svg.NormalisedXs(Colors.Red.ToHex());

            double step = (majors[1] - majors[0]) / parts;
            var inside = minors.Where(m => m > majors[0] && m < majors[^1]).ToArray();
            Assert.Equal(between, inside.Length);
            Assert.All(inside, m =>
            {
                double k = (m - majors[0]) / step;
                Assert.Equal(Math.Round(k), k, 3);
            });
        }
    }
}
