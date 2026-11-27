// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>DTO for one <see cref="Models.SankeyNode"/>.</summary>
public sealed record SankeyNodeDto(string Label, Color? Color = null, string? SubLabel = null, Color? SubLabelColor = null, int? Column = null);
