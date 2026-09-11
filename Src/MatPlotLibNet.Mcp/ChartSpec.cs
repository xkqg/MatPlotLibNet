// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace MatPlotLibNet.Mcp;

/// <summary>A chart specification exactly as the model wrote it: the library's figure JSON, still unvalidated.
/// A value type of its own, so a spec can never be swapped with a path or a format on a tool signature.</summary>
internal readonly record struct ChartSpec(string Json)
{
    /// <summary>Wraps the raw JSON of a tool argument.</summary>
    public static ChartSpec From(JsonElement element) => new(element.GetRawText());
}
