// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Serialization;

/// <summary>A look of a theme that owns its keys in the theme's record of the figure JSON (<see cref="ThemeDto"/>): it writes each of
/// its values under its own flat key and reads them back, so the record only declares the keys (and with that the order they are
/// written in), and the look alone says what stands under them, which value is left out at the library's, and what a key that is
/// absent reads as.</summary>
/// <typeparam name="TLook">The look as a theme holds it: a look a theme may lack is held as <see cref="Nullable{T}"/>.</typeparam>
internal interface IThemeKeys<TLook>
{
    /// <summary>Writes this look's values under its keys of <paramref name="keys"/>; a value the library's look holds is left unset.</summary>
    /// <param name="keys">The theme's record being written.</param>
    void WriteKeys(ThemeDto keys);

    /// <summary>The look <paramref name="keys"/> describe: each value from its own key, the library's where the key is absent.</summary>
    /// <param name="keys">The theme's record being read.</param>
    /// <returns>The look.</returns>
    static abstract TLook ReadKeys(ThemeDto keys);
}
