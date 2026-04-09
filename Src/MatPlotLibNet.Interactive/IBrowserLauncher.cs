// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interactive;

/// <summary>Abstraction for opening a URL in a browser. Enables DI and testability.</summary>
public interface IBrowserLauncher
{
    /// <summary>Opens the specified URL in the default browser.</summary>
    Task OpenAsync(string url);
}
