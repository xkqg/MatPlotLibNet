// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Serialization;

/// <summary>DTO for a single 3D text annotation positioned at (X, Y, Z).</summary>
public sealed record Text3DAnnotationDto(double X, double Y, double Z, string Text);
