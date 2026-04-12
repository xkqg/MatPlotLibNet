// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MplColor = MatPlotLibNet.Styling.Color;

namespace MatPlotLibNet.Maui;

internal static class ColorExtensions
{
    internal static Microsoft.Maui.Graphics.Color ToMauiColor(this MplColor color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
}
