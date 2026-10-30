// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The token of a registration that cannot be taken back: disposing it does nothing.</summary>
internal sealed class NullSubscription : IDisposable
{
    /// <summary>The one instance; it holds no state.</summary>
    internal static readonly NullSubscription Instance = new();

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
