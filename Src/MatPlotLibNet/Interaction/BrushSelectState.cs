// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Snapshot of a rubber-band rectangle in progress. Pixel coordinates are relative to
/// the control's top-left corner. Used by native controls to draw the selection overlay.</summary>
public readonly record struct BrushSelectState(
    double StartPixelX, double StartPixelY,
    double CurrentPixelX, double CurrentPixelY,
    int AxesIndex);
