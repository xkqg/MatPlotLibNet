// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Playground;

/// <summary>A theme the Playground offers, with the name its picker shows.</summary>
/// <param name="Theme">The theme the figure is drawn with.</param>
/// <param name="Label">The name shown in the theme picker.</param>
public readonly record struct ThemeChoice(Theme Theme, string Label);
