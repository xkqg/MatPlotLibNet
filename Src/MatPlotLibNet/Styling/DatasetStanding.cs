// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Where the datasets of a box plot or a violin plot stand along the axis that carries their positions when their author names
/// no position: the first at <see cref="FirstPosition"/>, the others one unit apart. The one value the box look
/// (<see cref="BoxStanding"/>) and the violin look (<see cref="ViolinLook"/>) both read, so a box and a violin drawn under one theme stand
/// by one rule.</summary>
/// <param name="FirstPosition">Where the first dataset stands.</param>
internal readonly record struct DatasetStanding(double FirstPosition)
{
    /// <summary>The library's own themes: 0, 1, 2 and so on.</summary>
    internal static DatasetStanding Library => new(0);

    /// <summary>matplotlib 3.11.2: <c>Axes.boxplot</c> (through <c>bxp</c>) and <c>Axes.violinplot</c> both stand a dataset given no
    /// position at <c>positions = range(1, N + 1)</c>.</summary>
    internal static DatasetStanding Matplotlib => new(1);
}
