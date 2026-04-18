// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Jint;
using Jint.Native;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Wraps a C# array as a Jint <see cref="JsArray"/> so JS can call
/// <c>.forEach</c> / <c>.map</c> / <c>.length</c> on the result of
/// <c>querySelectorAll</c>. Without this, scripts get a <c>ClrArray</c> which lacks
/// the JS Array prototype and <c>els.forEach(fn)</c> throws "is not a function".</summary>
internal static class JsArrayWrap
{
    public static object Wrap(Engine? engine, object[] items)
    {
        if (engine is null) return items;       // pre-attach (very early), array is fine for tests
        var jsItems = items.Select(it => JsValue.FromObject(engine, it)).ToArray();
        return new JsArray(engine, jsItems);
    }
}
