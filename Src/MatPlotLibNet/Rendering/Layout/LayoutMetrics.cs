// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>Holds the minimum pixel margin requirements computed for a single subplot.</summary>
internal sealed record LayoutMetrics(
    double LeftNeeded,
    double BottomNeeded,
    double TopNeeded,
    double RightNeeded);
