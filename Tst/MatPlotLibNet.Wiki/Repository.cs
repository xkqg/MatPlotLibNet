// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>The repository this suite is built from: where it stands, and the version its packages carry.</summary>
internal static class Repository
{
    /// <summary>The repository's root: the first folder above the suite's output that holds the changelog.</summary>
    public static string Root { get; } = FindRoot();

    /// <summary>The version the live packages carry, as the core package's project file declares it.</summary>
    public static Version PackageVersion =>
        Version.Parse(XDocument.Load(Path.Join(Root, "Src", "MatPlotLibNet", "MatPlotLibNet.csproj")).Descendants("Version").First().Value.Trim());

    private static string FindRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null && !File.Exists(Path.Join(folder.FullName, "CHANGELOG.md")))
        {
            folder = folder.Parent;
        }

        return folder?.FullName ?? throw new InvalidOperationException($"No folder above {AppContext.BaseDirectory} holds CHANGELOG.md.");
    }
}
