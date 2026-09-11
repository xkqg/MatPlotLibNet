// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp;

/// <summary>Decides where a model-chosen path is allowed to land. The library's file writer truncates whatever it
/// is handed, and a path from a model is a guess — so every write is resolved against ONE root the operator names
/// (<c>MATPLOTLIBNET_MCP_OUTPUT_ROOT</c>, the system temp directory by default) and anything that leaves that root
/// is refused. A relative path is resolved against the root as well, never against the working directory the host
/// happened to start the server in.</summary>
internal sealed class OutputPathResolver
{
    /// <summary>The environment variable an operator sets to move the output root.</summary>
    public const string RootVariable = "MATPLOTLIBNET_MCP_OUTPUT_ROOT";

    /// <summary>Creates a resolver rooted at <paramref name="root"/>.</summary>
    public OutputPathResolver(string root) => Root = Path.GetFullPath(root);

    /// <summary>The only directory this server writes under.</summary>
    public string Root { get; }

    /// <summary>The resolver for a configured root, falling back to the system temp directory.</summary>
    public static OutputPathResolver FromEnvironment(string? configuredRoot) =>
        new(string.IsNullOrWhiteSpace(configuredRoot) ? Path.GetTempPath() : configuredRoot);

    /// <summary>The absolute path <paramref name="path"/> names inside the root, with its directory created; a
    /// <see cref="ToolRefusalException"/> when it names anywhere else.</summary>
    public string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ToolRefusalException($"No file name given. Pass a name or a relative path; it is written under {Root}.");
        }

        string full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(Root, path));
        string root = Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new ToolRefusalException(
                $"'{path}' resolves to {full}, outside the output root {Root}. This server writes under that root only; "
                + $"set {RootVariable} to move it.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        return full;
    }
}
