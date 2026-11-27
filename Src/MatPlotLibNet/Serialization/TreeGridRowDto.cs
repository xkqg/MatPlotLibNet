// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>DTO for one <see cref="Models.Series.TreeGridRow"/>.</summary>
public sealed record TreeGridRowDto
{
    /// <summary>The row's name, drawn at the left and indented by its depth
    /// (<see cref="Models.Series.TreeGridRow.Label"/>).</summary>
    public string Label { get; init; } = "";
    /// <summary>The row's values, one per column (<see cref="Models.Series.TreeGridRow.Cells"/>).</summary>
    public List<string>? Cells { get; init; }
    /// <summary>How deep the row sits, 0 being the root (<see cref="Models.Series.TreeGridRow.Depth"/>).</summary>
    public int Depth { get; init; }
    /// <summary>Where the row leads (<see cref="Models.Series.TreeGridRow.Url"/>); absent for a row that is plain
    /// text.</summary>
    public string? Url { get; init; }
    /// <summary>Whether the subtree under the row is shown (<see cref="Models.Series.TreeGridRow.Expanded"/>); absent for a
    /// leaf, which discloses nothing.</summary>
    public bool? Expanded { get; init; }
    /// <summary>The accent colour of the row's text (<see cref="Models.Series.TreeGridRow.Accent"/>); absent for
    /// none.</summary>
    public Color? Accent { get; init; }
}
