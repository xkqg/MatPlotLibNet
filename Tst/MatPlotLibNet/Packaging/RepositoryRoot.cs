// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>Where the repository a test is run from stands. This is the one place that walks up from a test assembly to find it:
/// every suite that reads a file of the repository takes the folder from here, and the other test projects link this very file.</summary>
internal static class RepositoryRoot
{
    /// <summary>The first folder above the test assembly that holds the changelog: the root of the repository.</summary>
    internal static string Folder { get; } = Find();

    private static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException($"No folder above {AppContext.BaseDirectory} holds CHANGELOG.md.");
    }
}
