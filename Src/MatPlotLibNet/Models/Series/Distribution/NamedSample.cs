// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>One group of a chart estimated from samples: a name and the group's observations. Three regions measured a
/// different number of times each are three named samples, and the name is how the chart labels the group.
/// <para>A sample is of free length: each group has as many observations as were made, unlike a <see cref="BarGroup"/>,
/// which carries exactly one value per category. A missing observation (NaN) and an infinity are left out of what is
/// estimated.</para>
/// <para>Groups are passed as an ordered LIST rather than a map from name to sample, because the order IS the picture: it
/// decides where each group is drawn and which colour it takes from the theme's cycle. A dictionary has no order to
/// give.</para></summary>
/// <param name="Label">The group's name, as the chart shows it.</param>
/// <param name="Values">The group's observations, in any order.</param>
public readonly record struct NamedSample(string Label, double[] Values);
