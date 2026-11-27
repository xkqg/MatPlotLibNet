// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>How far a stroke reaches past the line it is drawn along is half its width: the room a tick keeps clear of a spine or of a
/// colour bar's frame, the band a raster's line covers across it and past a projecting end, the tip an arrow head keeps clear of.</summary>
public sealed class StrokeWidthExtensionsTests
{
    [Theory]
    [InlineData(0.8, 0.4)]
    [InlineData(1.0, 0.5)]
    [InlineData(1.3888888888888888, 0.6944444444444444)]
    [InlineData(0.0, 0.0)]
    public void AStroke_ReachesHalfItsWidthPastItsLine(double width, double reach) => Assert.Equal(reach, width.Reach);
}
