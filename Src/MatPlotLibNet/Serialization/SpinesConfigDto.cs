// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object that groups all four axis-spine configurations for a subplot.</summary>
internal sealed record SpinesConfigDto
{
    public SpineConfigDto? Top { get; init; }
    public SpineConfigDto? Bottom { get; init; }
    public SpineConfigDto? Left { get; init; }
    public SpineConfigDto? Right { get; init; }

    /// <summary>The JSON's form of <paramref name="spines"/>: each of the four sides.</summary>
    internal static SpinesConfigDto Of(SpinesConfig spines) => new()
    {
        Top = SpineConfigDto.Of(spines.Top),
        Bottom = SpineConfigDto.Of(spines.Bottom),
        Left = SpineConfigDto.Of(spines.Left),
        Right = SpineConfigDto.Of(spines.Right)
    };

    /// <summary>The spines this record describes; a side it leaves out is a new spine.</summary>
    internal SpinesConfig ToSpines() => new()
    {
        Top = Top?.ToSpine() ?? new(),
        Bottom = Bottom?.ToSpine() ?? new(),
        Left = Left?.ToSpine() ?? new(),
        Right = Right?.ToSpine() ?? new()
    };
}
