// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Configures the margins and gaps between subplots in a figure.</summary>
public sealed record SubPlotSpacing
{
    public double MarginLeft { get; init; } = 60;

    public double MarginRight { get; init; } = 20;

    public double MarginTop { get; init; } = 40;

    public double MarginBottom { get; init; } = 50;

    public double HorizontalGap { get; init; } = 40;

    public double VerticalGap { get; init; } = 40;

    public bool TightLayout { get; init; }

    public bool ConstrainedLayout { get; init; }
}
