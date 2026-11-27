// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO.Enumeration;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>What one walk of a folder saw: the files below it and the folders it went into, each written from the folder it started in,
/// with forward slashes. The walk decides which folders it enters, so a folder it skips is never listed, never opened and costs nothing.</summary>
/// <param name="Files">Every file below the start, in the folders the walk entered.</param>
/// <param name="Folders">Every folder the walk entered.</param>
internal readonly record struct TreeWalk(string[] Files, string[] Folders)
{
    /// <summary>Every folder below the start, hidden ones too, and a folder that cannot be read is an error, not a silent gap in a guard.</summary>
    private static readonly EnumerationOptions Everything = new()
    {
        RecurseSubdirectories = true,
        AttributesToSkip = 0,
        IgnoreInaccessible = false,
    };

    /// <summary>Walks <paramref name="start"/>, entering every folder whose name is not in <paramref name="skipped"/> (at any depth).</summary>
    /// <param name="start">The folder to walk.</param>
    /// <param name="skipped">The names of the folders the walk does not enter.</param>
    /// <returns>What the walk saw; empty when <paramref name="start"/> does not exist.</returns>
    internal static TreeWalk Of(string start, string[] skipped)
    {
        if (!Directory.Exists(start))
        {
            return new([], []);
        }

        bool Skips(ref FileSystemEntry entry) => entry.IsDirectory && skipped.Contains(entry.FileName.ToString(), StringComparer.Ordinal);
        var seen = new FileSystemEnumerable<Seen>(start,
            (ref FileSystemEntry entry) => new Seen(Path.GetRelativePath(start, entry.ToFullPath()).Replace(Path.DirectorySeparatorChar, '/'), entry.IsDirectory),
            Everything)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) => !Skips(ref entry),
            ShouldRecursePredicate = (ref FileSystemEntry entry) => !Skips(ref entry),
        }.ToArray();

        return new([.. seen.Where(entry => !entry.IsFolder).Select(entry => entry.Path)], [.. seen.Where(entry => entry.IsFolder).Select(entry => entry.Path)]);
    }

    /// <summary>One file or folder the walk met.</summary>
    private readonly record struct Seen(string Path, bool IsFolder);
}
