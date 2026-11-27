// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Serialization;

/// <summary>What the figure JSON carries of one colour map: its name when it travels by its name, or its description when it is built
/// in code, never both. A series, a colour bar, a theme, a pair grid and an image on the figure all keep the two halves in two fields of
/// their own record, so this value is the one place the two are told apart when a map is written
/// (<see cref="ColorMapDescriptionExtensions.ColorMapOnTheWire(IColorMap?)"/>) and put together again when it is read (<see cref="Read"/>).</summary>
/// <param name="Name">The name of a colour map that travels by its name (a map the library ships, or one registered under its name);
/// absent for a map built in code and for none.</param>
/// <param name="Description">The description of a colour map built in code; absent for a map that travels by its name and for none.</param>
internal readonly record struct ColorMapWire(string? Name, ColorMapDto? Description)
{
    /// <summary>Whether the document carries a map: a name or a description. A document that carries none leaves the owner of the map
    /// on the map it was created with.</summary>
    internal bool IsWritten => Name is not null || Description is not null;

    /// <summary>The map the two fields make together: the description's map when there is one, otherwise the map registered under the
    /// name (none when nobody registered it), otherwise none.</summary>
    /// <returns>The map, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">The description cannot make a map; the exception names the field.</exception>
    internal IColorMap? Read() => (Description ?? (Name is null ? null : new NamedColorMapDto(Name)))?.Restore();
}
