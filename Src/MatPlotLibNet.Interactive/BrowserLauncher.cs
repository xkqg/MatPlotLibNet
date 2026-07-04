// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MatPlotLibNet.Interactive;

/// <summary>Default <see cref="IBrowserLauncher"/> that opens URLs via the OS shell.</summary>
/// <remarks>Excluded from coverage: calls <see cref="Process.Start(ProcessStartInfo)"/> with
/// <c>UseShellExecute=true</c> — the launch contract can only be tested end-to-end with a
/// real desktop environment. CI has the <c>ShowAsync_OpensBrowser</c> integration test SKIPped
/// for this reason. Mocking <c>Process.Start</c> here would test the mock, not the launcher.</remarks>
[ExcludeFromCodeCoverage]
public sealed class BrowserLauncher : IBrowserLauncher
{
    /// <inheritdoc/>
    /// <remarks>Failure observability: if the OS cannot launch a browser (no default handler
    /// for the URL scheme, shell error, …) <see cref="Process.Start(ProcessStartInfo)"/> throws
    /// (typically <see cref="System.ComponentModel.Win32Exception"/>), so the caller of
    /// <see cref="InteractiveExtensions.ShowAsync"/> receives the failure rather than a silent
    /// no-op. A <see langword="null"/> return is normal for shell-launched URLs (an already-running
    /// browser handles the request without a new process) and is not an error; the returned handle,
    /// when present, is disposed immediately as it is not needed to keep the browser open.</remarks>
    public Task OpenAsync(string url)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
        return Task.CompletedTask;
    }
}
