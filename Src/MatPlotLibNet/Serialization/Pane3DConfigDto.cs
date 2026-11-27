// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for <see cref="Pane3DConfig"/>: the 3-D axes' pane colours and visibility, written only when the
/// axes changed them.</summary>
internal sealed record Pane3DConfigDto
{
    public Color? FloorColor { get; init; }
    public Color? LeftWallColor { get; init; }
    public Color? RightWallColor { get; init; }
    public double Alpha { get; init; } = 1.0;
    public bool Visible { get; init; } = true;

    private static readonly Pane3DConfig Rest = new();

    /// <summary>The JSON's form of <paramref name="pane"/>; <see langword="null"/> for a pane at its defaults, which writes no
    /// field.</summary>
    /// <param name="pane">The pane configuration.</param>
    internal static Pane3DConfigDto? From(Pane3DConfig pane) => pane.DescribedUnlessAtRest(Rest, Describe);

    /// <summary>The JSON's form of a pane away from its defaults.</summary>
    private static Pane3DConfigDto Describe(Pane3DConfig pane) => new()
    {
        FloorColor = pane.FloorColor,
        LeftWallColor = pane.LeftWallColor,
        RightWallColor = pane.RightWallColor,
        Alpha = pane.Alpha,
        Visible = pane.Visible,
    };

    /// <summary>Sets the pane configuration this record describes on <paramref name="axes"/>.</summary>
    /// <param name="axes">The axes being read.</param>
    internal void AddTo(Axes axes) => axes.Pane3D = new Pane3DConfig
    {
        FloorColor = FloorColor,
        LeftWallColor = LeftWallColor,
        RightWallColor = RightWallColor,
        Alpha = Alpha,
        Visible = Visible,
    };
}
