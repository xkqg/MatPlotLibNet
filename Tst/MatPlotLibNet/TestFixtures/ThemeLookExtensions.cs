// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests;

/// <summary>The ways a theme travels in the tests, so that a look the theme carries is read off the same theme after each: made again
/// from itself, through its data transfer object, and through the JSON of a figure that wears it.</summary>
public static class ThemeLookExtensions
{
    extension(Theme theme)
    {
        /// <summary>The theme made again from itself, as an application makes its own from a built-in one.</summary>
        public Theme Cloned() => new ThemeBuilder(theme).Build();

        /// <summary>The theme written to its data transfer object and read back.</summary>
        public Theme ThroughDto() => ThemeDto.From(theme).ToTheme();

        /// <summary>The theme read back from the JSON of a figure that wears it.</summary>
        public Theme ThroughJson() => Plt.Create().WithTheme(theme).Build().RoundTripped().Theme;
    }
}
