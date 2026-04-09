// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive;

namespace MatPlotLibNet.Notebooks;

/// <summary>
/// Polyglot Notebooks kernel extension. Registers <see cref="MatPlotLibNet.Models.Figure"/> as an inline SVG
/// display type so that <c>figure.Display()</c> or returning a <c>Figure</c> from a cell renders it in-place.
/// </summary>
/// <remarks>
/// Load in a notebook cell: <c>#r "nuget: MatPlotLibNet.Notebooks"</c>
/// The extension is auto-discovered by the Polyglot Notebooks runtime via the
/// <see cref="IKernelExtension"/> interface.
/// </remarks>
public sealed class NotebookExtension : IKernelExtension
{
    /// <inheritdoc />
    public Task OnLoadAsync(Kernel kernel)
    {
        FigureFormatter.Register();
        return Task.CompletedTask;
    }
}
