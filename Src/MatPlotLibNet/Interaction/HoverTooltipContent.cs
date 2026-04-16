// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Content for a hover tooltip to be displayed by a native control. Carries the
/// formatted text and the pixel position where the tooltip should appear.</summary>
public sealed record HoverTooltipContent(
    string Text,
    double PixelX, double PixelY);
