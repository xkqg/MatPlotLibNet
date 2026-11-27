// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Serialization;

/// <summary>DTO for one <see cref="MatPlotLibNet.Models.Series.GraphEdge"/>.</summary>
public sealed record GraphEdgeDto(string From, string To, double Weight, bool IsDirected);
