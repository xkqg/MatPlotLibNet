// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The three series a dumbbell chart is made of, handed to the callback of
/// <see cref="CategoricalAxesExtensions.Dumbbell"/> so each can be styled on its own: the connector that joins the two dots of
/// a category, and the two sets of dots. They are the very series on the axes, so a change made through them is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct DumbbellParts
{
    /// <summary>Makes the parts of one dumbbell chart.</summary>
    /// <param name="connector">The error bar of no error that joins the two dots of each category.</param>
    /// <param name="from">The dots of the first group.</param>
    /// <param name="to">The dots of the second group.</param>
    internal DumbbellParts(ErrorBarSeries connector, ScatterSeries from, ScatterSeries to)
    {
        Connector = connector;
        From = from;
        To = to;
    }

    /// <summary>The connector: an error bar of no error that runs from the lower of a category's two dots to the higher one.
    /// It is drawn behind the dots in a neutral grey, has no legend entry and takes no colour slot.</summary>
    public ErrorBarSeries Connector { get; }

    /// <summary>The dots of the first group (where each category was), labelled with the group's name.</summary>
    public ScatterSeries From { get; }

    /// <summary>The dots of the second group (where each category is now), labelled with the group's name.</summary>
    public ScatterSeries To { get; }
}
