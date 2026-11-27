// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The verb of the axes builder that adds a word cloud: it adds the series through the axes verb of the same name
/// (<see cref="AxesTextVerbs"/>) and hands it to the caller's callback.</summary>
public static class AxesBuilderTextVerbs
{
    extension(AxesBuilder axes)
    {
        /// <summary>Adds a word cloud to the axes: each word set in a size that follows its weight.</summary>
        public AxesBuilder WordCloud(string[] words, double[] weights, Action<WordCloudSeries>? configure = null)
            => axes.Configured(axes.Axes.WordCloud(words, weights), configure);
    }
}
