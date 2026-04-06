// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Serialization;

/// <summary>Series that can produce their own serialization DTO.</summary>
public interface ISeriesSerializable
{
    /// <summary>Creates a serialization DTO representing this series.</summary>
    SeriesDto ToSeriesDto();
}
