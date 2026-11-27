// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>What the part unit tests ask of what a part drew, once for every part that draws it: a line across the plot, a label set against
/// its right edge, and the number of calls of one kind.</summary>
internal static class PartDrawnExtensions
{
    extension(RecordingRenderContext ctx)
    {
        /// <summary>Asserts that the part drew exactly one line and that it runs from the left edge of <paramref name="plot"/> to its right
        /// edge at one height.</summary>
        internal void AssertOneLineAcross(Rect plot)
        {
            Assert.Equal(1, ctx.Count<DrawLineCall>());
            var line = ctx.Of<DrawLineCall>()[0];
            Assert.Equal(plot.X, line.P1.X);
            Assert.Equal(plot.X + plot.Width, line.P2.X);
            Assert.Equal(line.P1.Y, line.P2.Y);
        }

        /// <summary>Asserts that the part drew exactly one text and that it is aligned to the right.</summary>
        internal void AssertOneLabelAlignedRight()
        {
            Assert.Equal(1, ctx.Count<DrawTextCall>());
            Assert.Equal(TextAlignment.Right, ctx.Of<DrawTextCall>()[0].Alignment);
        }

        /// <summary>Asserts that the part made <paramref name="expected"/> calls of the kind <paramref name="call"/>.</summary>
        internal void AssertCalls(Type call, int expected) => Assert.Equal(expected, ctx.Calls.Count(call.IsInstanceOfType));
    }
}
