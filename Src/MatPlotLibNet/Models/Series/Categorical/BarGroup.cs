// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>A name and one value per place: the named-values type of every verb that draws several named series over the
/// same places. <see cref="AxesBuilder.GroupedBar"/> takes its groups this way (three regions over four quarters is three
/// groups of four values, and the name is what the legend shows). So do <see cref="CategoricalAxesExtensions.Dumbbell"/>,
/// <see cref="CategoricalAxesExtensions.SlopeGraph"/>, <see cref="CategoricalAxesExtensions.DotPlot"/>,
/// <see cref="CategoricalAxesExtensions.ParallelCoordinates"/> and <see cref="CategoricalAxesExtensions.Marimekko"/>, over
/// their categories, dimensions or columns, and <see cref="XYAxesExtensions.BumpChart"/> and
/// <see cref="XYAxesExtensions.StreamGraph"/>, over an array of x positions. What the name stands for is each verb's to
/// say in its own parameter: a legend entry, the class of a row, the end of a dumbbell, a segment of a column.
/// <para>Groups are passed as an ordered LIST rather than a map from name to values, because the order IS the
/// picture: it decides which bar sits where inside every category and which colour each one takes from the
/// theme's cycle. A dictionary has no order to give.</para></summary>
/// <param name="Label">The group's name; each verb that takes groups says what it names (the legend shows it for a
/// series).</param>
/// <param name="Values">One value per place, in the same order as the places (the categories, dimensions, columns or x
/// positions of the verb).</param>
public readonly record struct BarGroup(string Label, double[] Values);
