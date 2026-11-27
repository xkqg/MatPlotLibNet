// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Serializes every test that writes <see cref="RcParams.Default"/> (through <see cref="Plt.Style.Use(StyleSheet)"/>).
/// A figure built without a theme of its own wears the look those defaults describe, so a test that changed them while the
/// render tests ran would change what those draw. A collection with parallelization disabled runs on its own.</summary>
[CollectionDefinition("RcParamsGlobalState", DisableParallelization = true)]
public sealed class RcParamsGlobalStateCollection
{
}

/// <summary>Takes a copy of every value of <see cref="RcParams.Default"/> and puts each back when disposed.</summary>
internal sealed class RcParamsDefaultsRestorer : IDisposable
{
    private readonly Dictionary<string, object> _saved = [];

    public RcParamsDefaultsRestorer()
    {
        foreach (var key in Keys())
            if (RcParams.Default.ContainsKey(key))
                _saved[key] = RcParams.Default.Get<object>(key);
    }

    public void Dispose()
    {
        foreach (var (key, value) in _saved)
            RcParams.Default.Set(key, value);
    }

    private static IEnumerable<string> Keys() =>
        typeof(RcParamKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);
}
