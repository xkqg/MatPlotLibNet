// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>A series whose seaborn verb names the axis its values run along where that axis has no label (<c>_add_axis_labels</c>): a count
/// plot 'count', a density curve 'Density'. It says the name and the axis; whether the name is drawn is the theme's
/// (<see cref="Styling.SeabornVerbLook.NamesValueAxis"/>), read by the one title rule (<c>AxesTitleExtensions.TitleOf</c>).</summary>
internal interface INamesValueAxis
{
    /// <summary>The name seaborn's verb gives the axis its values run along.</summary>
    string ValueAxisName { get; }

    /// <summary>Whether its values run along X (a count plot lying down), else along Y.</summary>
    bool ValuesAlongX { get; }
}
