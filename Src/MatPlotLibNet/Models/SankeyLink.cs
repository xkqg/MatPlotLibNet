// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Represents a flow link between two nodes in a Sankey diagram.</summary>
public sealed record SankeyLink(int SourceIndex, int TargetIndex, double Value);
