// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.IO.Enumeration;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The source tree of the repository a test is run from: the C# files of the library and of its tests, and its documents.</summary>
internal static class RepositoryTree
{
    private static readonly string[] SkippedFolders = ["node_modules", "bin", "obj", "dist", ".git"];

    // Beside the build output, the folders of a working copy that hold no document of the repository: the coverage reports, the
    // records of the work (ignored by git), the site docfx writes, the benchmarks' reports, the editor's and the tools' own settings.
    private static readonly string[] SkippedDocumentFolders = [.. SkippedFolders, "out", ".path-manifests", "_site", ".vscode", ".claude", "TestResults", "BenchmarkDotNet.Artifacts"];

    private static readonly ConcurrentDictionary<string, Lazy<TreeWalk>> Walks = new(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<FilesOf, Lazy<SourceFile[]>> Lists = new();

    private static readonly ConcurrentDictionary<string, string> Texts = new(StringComparer.Ordinal);

    private static readonly Lazy<SourceFile[]> Builds = new(() =>
        [.. Files("", "*.csproj"), .. Files("", "Directory.Build.*"), .. Files(".github/workflows", "*.yml")]);

    private static readonly Lazy<SourceFile[]> CurrentState = new(() => [.. Documents().Where(file => file.Path != "CHANGELOG.md")]);

    /// <summary>Every <c>.cs</c> file under <c>Src</c>, build output left out, each with its path from the root.</summary>
    internal static IReadOnlyList<SourceFile> SourceFiles() => Files("Src", "*.cs");

    /// <summary>Every Razor component (<c>.razor</c>) under <c>Src</c>, build output left out, each with its path from the root. A component
    /// is source a rule of the code reads too: <see cref="SourceFiles"/> holds the <c>.cs</c> files only and cannot see one.</summary>
    internal static IReadOnlyList<SourceFile> ComponentFiles() => Files("Src", "*.razor");

    /// <summary>Every <c>.cs</c> file under <c>Tst</c>, of every test project, build output left out, each with its path from the root.</summary>
    internal static IReadOnlyList<SourceFile> TestFiles() => Files("Tst", "*.cs");

    /// <summary>Every Markdown document of the repository (the change log, the read-me files, the architecture map, the docs site's
    /// pages), build output, reports and records left out, each with its path from the root.</summary>
    internal static IReadOnlyList<SourceFile> Documents() => Files("", "*.md");

    /// <summary>The files that build and ship the repository, each with its path from the root: every project file, the shared
    /// <c>Directory.Build</c> properties and targets, and the workflows. Their comments name the tests that hold what they say, so a
    /// sweep for a name that is gone reads them beside the documents.</summary>
    internal static IReadOnlyList<SourceFile> BuildFiles() => Builds.Value;

    /// <summary>The documents that describe what the library is today: every document of <see cref="Documents"/> but the change log,
    /// which records what each release added, at the numbers it had then.</summary>
    internal static IReadOnlyList<SourceFile> CurrentStateDocuments() => CurrentState.Value;

    /// <summary>The files of <paramref name="folder"/> and of every folder below it whose name matches <paramref name="pattern"/>, build
    /// output left out, each with its path from the root and its text. A list is read once per process: the tests that ask for the same
    /// files share one list, and a file is read once whoever asks for it.</summary>
    /// <param name="folder">A folder of the repository, from the root, with forward slashes; empty for the whole repository.</param>
    /// <param name="pattern">A file name pattern: <c>*</c> and <c>?</c> stand for any characters and any one character.</param>
    /// <returns>The files, in the order the walk met them.</returns>
    internal static IReadOnlyList<SourceFile> Files(string folder, string pattern) =>
        Lists.GetOrAdd(new FilesOf(folder, pattern), key => new Lazy<SourceFile[]>(() => Read(key))).Value;

    /// <summary>One file of the repository by its path, whatever the walk skips: a file a test names (a script, a data file, a pinned
    /// list) rather than finds. It is read once per process, like every file of a list.</summary>
    /// <param name="parts">The parts of the path from the root, a folder name each and the file name last.</param>
    /// <returns>The file, with its path from the root written with forward slashes.</returns>
    internal static SourceFile FileAt(params string[] parts)
    {
        string path = string.Join('/', parts);
        return new(path, Texts.GetOrAdd(path, file => File.ReadAllText(Path.Combine(RepositoryRoot.Folder, file))));
    }

    /// <summary>What the walk of a top folder of the repository saw, each path from the root. A top folder is walked once per process.</summary>
    /// <param name="top">The first folder of a path from the root; empty for the whole repository.</param>
    /// <returns>The files and the folders of the walk.</returns>
    internal static TreeWalk WalkOf(string top) => Walks.GetOrAdd(top, key => new Lazy<TreeWalk>(() => Walk(key))).Value;

    private static SourceFile[] Read(FilesOf request)
    {
        string prefix = request.Folder.Length == 0 ? string.Empty : request.Folder.TrimEnd('/') + "/";
        return [.. WalkOf(request.Folder.Split('/')[0]).Files
            .Where(path => path.StartsWith(prefix, StringComparison.Ordinal)
                && FileSystemName.MatchesSimpleExpression(request.Pattern, Path.GetFileName(path), ignoreCase: !OperatingSystem.IsLinux()))
            .Select(path => FileAt(path))];
    }

    private static TreeWalk Walk(string top)
    {
        var walk = TreeWalk.Of(Path.Combine(RepositoryRoot.Folder, top), top is "Src" or "Tst" ? SkippedFolders : SkippedDocumentFolders);
        string prefix = top.Length == 0 ? string.Empty : top + "/";
        return new([.. walk.Files.Select(path => prefix + path)], [.. walk.Folders.Select(path => prefix + path)]);
    }

    /// <summary>A request for the files of a folder that match a pattern.</summary>
    private readonly record struct FilesOf(string Folder, string Pattern);
}
