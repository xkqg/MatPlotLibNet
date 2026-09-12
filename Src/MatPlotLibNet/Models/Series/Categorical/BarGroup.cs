// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>One group of a grouped bar chart: a name and one value per category. Three regions over four
/// quarters is three groups of four values, and the name is what the legend shows.
/// <para>Groups are passed as an ordered LIST rather than a map from name to values, because the order IS the
/// picture: it decides which bar sits where inside every category and which colour each one takes from the
/// theme's cycle. A dictionary has no order to give.</para></summary>
/// <param name="Label">The group's name, as the legend shows it.</param>
/// <param name="Values">One value per category, in the same order as the categories.</param>
public readonly record struct BarGroup(string Label, double[] Values);
