// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>Serializes and deserializes <see cref="Figure"/> instances to and from JSON.</summary>
public interface IChartSerializer
{
    /// <summary>Serializes a figure to its JSON representation.</summary>
    string ToJson(Figure figure, bool indented = false);

    /// <summary>Deserializes a figure from its JSON representation.</summary>
    Figure FromJson(string json);
}
