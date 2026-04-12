// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.Themes;

/// <summary>
/// Captures the matplotlib font stack used by the Matplotlib* themes — primary CSS family
/// plus the default base, tick label, and title sizes (in points).
/// </summary>
internal readonly record struct MatplotlibFontStack(
    string PrimaryFamily,
    double BaseSize,
    double TickSize,
    double TitleSize);
