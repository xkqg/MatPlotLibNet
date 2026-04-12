// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Maui;

internal static class LineStyleExtensions
{
    internal static float[] ToMauiDashPattern(this LineStyle style)
    {
        var pattern = DashPatterns.GetPattern(style);
        if (pattern.Length == 0) return [];
        var result = new float[pattern.Length];
        for (int i = 0; i < pattern.Length; i++)
            result[i] = (float)pattern[i];
        return result;
    }
}
