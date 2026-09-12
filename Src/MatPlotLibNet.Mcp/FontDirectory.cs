// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp;

/// <summary>What the <c>MATPLOTLIBNET_FONTS</c> variable expanded to: the font files to register, in the order
/// they were named, and the entries that could not be used.</summary>
/// <param name="Files">Existing <c>.ttf</c> and <c>.otf</c> files, in the order of the variable; a directory
/// contributes its font files in name order.</param>
/// <param name="Problems">One line per entry that names nothing usable: a path that does not exist, or a
/// directory without font files.</param>
public readonly record struct FontDirectoryExpansion(IReadOnlyList<string> Files, IReadOnlyList<string> Problems);

/// <summary>The server's font door. A host starts this process; no code of the user's runs in it, so a font file
/// cannot be registered by a call. It is registered by naming it in <see cref="Variable"/>: files and directories
/// of files, separated by <c>;</c>, the way a PATH is written on every platform.</summary>
public static class FontDirectory
{
    /// <summary>The environment variable: font files (<c>.ttf</c>, <c>.otf</c>) and directories of them, separated
    /// by <c>;</c>.</summary>
    public const string Variable = "MATPLOTLIBNET_FONTS";

    private static readonly string[] FontExtensions = [".ttf", ".otf"];

    /// <summary>Expands the variable's value into the files to register. Nothing here throws: a bad entry becomes a
    /// problem line for the host's log, and the server starts with the fonts it could find.</summary>
    /// <param name="value">The variable's value, or null when it is not set.</param>
    public static FontDirectoryExpansion Expand(string? value)
    {
        var files = new List<string>();
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return new FontDirectoryExpansion(files, problems);
        }

        foreach (string raw in value.Split(';'))
        {
            string entry = raw.Trim();
            if (entry.Length == 0)
            {
                continue;
            }

            if (File.Exists(entry))
            {
                files.Add(Path.GetFullPath(entry));
                continue;
            }

            if (Directory.Exists(entry))
            {
                var found = Directory.EnumerateFiles(entry)
                    .Where(f => FontExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .Select(Path.GetFullPath)
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToList();
                if (found.Count == 0)
                {
                    problems.Add($"{Variable}: the directory '{entry}' holds no .ttf or .otf file.");
                }

                files.AddRange(found);
                continue;
            }

            problems.Add($"{Variable}: '{entry}' is neither a file nor a directory.");
        }

        return new FontDirectoryExpansion(files, problems);
    }
}
