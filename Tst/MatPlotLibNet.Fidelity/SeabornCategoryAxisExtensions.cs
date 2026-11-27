// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Fidelity;

/// <summary>The axis seaborn gives a categorical plot, set by the library's own verbs so a check draws the axes its reference draws.</summary>
public static class SeabornCategoryAxisExtensions
{
    extension(AxesBuilder ax)
    {
        /// <summary>Stands the X axis as seaborn's <c>_adjust_cat_axis</c> leaves it: <c>set_xlim(-.5, n - .5)</c> with no margin, and one
        /// tick on every category, 0 to n - 1.</summary>
        /// <param name="count">The number of categories.</param>
        public AxesBuilder OnSeabornCategories(int count) =>
            ax.SetXLim(-0.5, count - 0.5).SetXTickLocator(new FixedLocator([.. Enumerable.Range(0, count).Select(i => (double)i)]));
    }
}
