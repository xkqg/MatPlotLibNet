// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Verso.Abstractions;

namespace MatPlotLibNet.Verso.Tests;

/// <summary>The three things a formatter reads from its context, and nothing else.</summary>
/// <remarks>
/// Hand-written rather than taken from a testing package, so the abstractions this suite compiles against stay
/// byte-for-byte the ones the shipped package compiles against. Every member the formatter never touches
/// throws, because a stub that silently answers a question nobody asked hides the day somebody starts asking.
/// </remarks>
internal sealed class StubFormatterContext(
    string mimeType,
    double maxWidth = 800,
    double maxHeight = 600,
    CancellationToken cancellationToken = default) : IFormatterContext
{
    public string MimeType { get; } = mimeType;

    public double MaxWidth { get; } = maxWidth;

    public double MaxHeight { get; } = maxHeight;

    public CancellationToken CancellationToken { get; } = cancellationToken;

    public IVariableStore Variables => throw new NotSupportedException();

    public IThemeContext Theme => throw new NotSupportedException();

    public LayoutCapabilities LayoutCapabilities => throw new NotSupportedException();

    public IExtensionHostContext ExtensionHost => throw new NotSupportedException();

    public INotebookMetadata NotebookMetadata => throw new NotSupportedException();

    public INotebookOperations Notebook => throw new NotSupportedException();

    public Task WriteOutputAsync(CellOutput output) => throw new NotSupportedException();
}
