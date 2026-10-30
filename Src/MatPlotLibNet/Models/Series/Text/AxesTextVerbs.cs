// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>The text-family verb of <see cref="Axes"/> (<c>WordCloud</c>), refusing words that cannot be laid out, constructing the
/// series and handing it to <see cref="Axes.AddSeries{T}"/>.</summary>
public static class AxesTextVerbs
{
    extension(Axes axes)
    {
        /// <summary>Adds a word cloud to the axes: each word set in a size that follows its weight.</summary>
        /// <param name="words">The words, none empty and none twice.</param>
        /// <param name="weights">The weight of each word, a finite number above zero.</param>
        /// <returns>The newly created <see cref="WordCloudSeries"/> for further configuration.</returns>
        /// <exception cref="ArgumentException">The words and weights differ in length, a weight is not a finite number above zero,
        /// or a word is empty or appears twice; the message names the word or its position.</exception>
        public WordCloudSeries WordCloud(string[] words, double[] weights)
        {
            WordCloudLayout.Refuse(new WordCloudSpec(words, weights, new Range1D(12, 60), new Font()));
            return axes.AddSeries(new WordCloudSeries(words, weights));
        }
    }
}
