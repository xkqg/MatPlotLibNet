// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>The checked-in files that pin the bytes of the wire: one place that knows where they live, how one is read, and how one is
/// written when <c>MPL_REGEN_GOLDEN=1</c> says the bytes are meant to change. Every byte-compared corpus
/// (<see cref="ChartSerializerGoldenCorpusTests"/>, <see cref="GivenSeriesCorpusTests"/>, <see cref="GivenAxesCorpusTests"/>,
/// <see cref="ThemeCorpusTests"/>) reads its pin here, so none of them has a way of its own to regenerate, to trim or to go green
/// on a file that is missing.
/// <para>The files are copied next to the test assembly at build time (the csproj's <c>Content</c> item over
/// <c>Serialization\Golden\**\*.json</c>), under the folder the corpus names; the folder of the first corpus is the root.</para></summary>
internal static class BytePins
{
    /// <summary>The environment variable that makes <see cref="Pinned"/> write the bytes it is given instead of reading them.</summary>
    internal const string RegenerateVariable = "MPL_REGEN_GOLDEN";

    private static string DirectoryOf(string folder) => Path.Combine(AppContext.BaseDirectory, "Serialization", "Golden", folder);

    private static string PathOf(string folder, string name) => Path.Combine(DirectoryOf(folder), $"{name}.json");

    /// <summary>The pinned bytes of <paramref name="name"/> under <paramref name="folder"/>, without the line end the file closes with.
    /// With <c>MPL_REGEN_GOLDEN=1</c> the file is written from <paramref name="actual"/> instead and <paramref name="actual"/> is
    /// returned.</summary>
    /// <exception cref="Xunit.Sdk.XunitException">There is no file, and the regeneration is not asked for.</exception>
    internal static string Pinned(string folder, string name, string actual)
    {
        string path = PathOf(folder, name);
        if (Environment.GetEnvironmentVariable(RegenerateVariable) == "1")
        {
            Directory.CreateDirectory(DirectoryOf(folder));
            File.WriteAllText(path, actual);
            return actual;
        }

        Assert.True(File.Exists(path),
            $"The pin for '{name}' is missing: {path}. Regenerate it with the {RegenerateVariable}=1 environment variable.");
        return File.ReadAllText(path).TrimEnd('\r', '\n');
    }

    /// <summary>The names of the files pinned under <paramref name="folder"/> (the root when it is empty), in ordinal order.</summary>
    internal static IReadOnlyList<string> Names(string folder) =>
        Directory.Exists(DirectoryOf(folder))
            ? [.. Directory.EnumerateFiles(DirectoryOf(folder), "*.json", SearchOption.TopDirectoryOnly)
                .Select(file => Path.GetFileNameWithoutExtension(file)!).Order(StringComparer.Ordinal)]
            : [];
}
