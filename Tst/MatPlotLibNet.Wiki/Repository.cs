// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>The repository this suite is built from: the version its packages carry (where it stands is <see cref="RepositoryRoot"/>).</summary>
internal static class Repository
{
    /// <summary>The version the live packages carry, as the core package's project file declares it.</summary>
    public static Version PackageVersion =>
        Version.Parse(XDocument.Load(Path.Join(RepositoryRoot.Folder, "Src", "MatPlotLibNet", "MatPlotLibNet.csproj")).Descendants("Version").First().Value.Trim());
}
