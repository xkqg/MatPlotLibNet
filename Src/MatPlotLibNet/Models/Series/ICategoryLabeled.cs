// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that provides category labels for the X axis.</summary>
public interface ICategoryLabeled
{
    string[]? CategoryLabels { get; }
}
