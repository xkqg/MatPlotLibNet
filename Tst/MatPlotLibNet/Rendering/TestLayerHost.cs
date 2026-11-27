// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The renderer a series pass draws for, in a test that draws one pass or one axis part without a renderer: the colour slots of
/// the axes, what is drawn inside a series' group (by default nothing), and a step that is not a series, which it records.</summary>
/// <param name="colorSlots">The colour slot of each series of the axes in identity order.</param>
/// <param name="insideSeriesGroup">What is drawn inside a series' group, given its identity slot; nothing when <see langword="null"/>.</param>
internal sealed class TestLayerHost(int[] colorSlots, Action<int>? insideSeriesGroup = null) : ILayerHost
{
    /// <summary>Every step that is not a series the pass handed over, in the order it was handed over.</summary>
    internal List<LayerStep> Layers { get; } = [];

    /// <inheritdoc />
    public int[] ColorSlots => colorSlots;

    /// <inheritdoc />
    public void DrawInsideSeriesGroup(int seriesIndex) => insideSeriesGroup?.Invoke(seriesIndex);

    /// <inheritdoc />
    public int DrawLayerAt(in SeriesPass pass, ReadOnlySpan<LayerStep> steps, int at)
    {
        Layers.Add(steps[at]);
        return at;
    }
}
