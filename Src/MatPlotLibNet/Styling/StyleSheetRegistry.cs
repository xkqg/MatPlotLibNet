// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace MatPlotLibNet.Styling;

/// <summary>Thread-safe registry for <see cref="StyleSheet"/> lookup by name. All built-in sheets are pre-registered.</summary>
public static class StyleSheetRegistry
{
    private static readonly ConcurrentDictionary<string, StyleSheet> Sheets
        = new(StringComparer.OrdinalIgnoreCase);

    static StyleSheetRegistry()
    {
        Register(StyleSheet.Default.Name,  StyleSheet.Default);
        Register(StyleSheet.Dark.Name,     StyleSheet.Dark);
        Register(StyleSheet.Seaborn.Name,  StyleSheet.Seaborn);
        Register(StyleSheet.Ggplot.Name,   StyleSheet.Ggplot);
    }

    /// <summary>Registers a style sheet under the given name.</summary>
    public static void Register(string name, StyleSheet sheet)
        => Sheets[name] = sheet;

    /// <summary>Gets a style sheet by name (case-insensitive), or <see langword="null"/> if not found.</summary>
    public static StyleSheet? Get(string name)
        => Sheets.TryGetValue(name, out var sheet) ? sheet : null;

    /// <summary>Gets all registered style sheet names.</summary>
    public static IEnumerable<string> Names => Sheets.Keys;
}
