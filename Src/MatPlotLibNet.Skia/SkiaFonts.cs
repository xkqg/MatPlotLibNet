// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Styling;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace MatPlotLibNet.Skia;

/// <summary>The fonts the Skia backend draws with. Four faces of DejaVu Sans, the Computer Modern faces matplotlib's classic
/// style sets math in (<c>cmr10</c>, <c>cmmi10</c>, <c>cmti10</c>, <c>cmb10</c>, <c>cmsy10</c>, <c>cmex10</c>, <c>cmtt10</c>) and the STIX faces
/// it takes the math characters those faces lack from (<c>STIXGeneral</c> regular, italic and bold, <c>STIXNonUnicode</c> regular and
/// italic), ship inside the package and are read once, the first time a font is resolved; any other font is found by NAME on the machine that renders, which is what
/// <see cref="Font.Family"/> has always done. This class adds the door that was missing: a font FILE, registered once
/// at startup, so a chart drawn in a container without installed fonts, or in a script that needs Devanagari, Thai
/// or Chinese, brings its own.</summary>
/// <remarks>
/// <para><b>Resolution order</b> for a family stack such as <c>"Noto Sans Devanagari, DejaVu Sans, sans-serif"</c>:
/// each name is tried against the registered and bundled fonts first, then against the fonts installed on the
/// machine; the first real match wins. The generic <c>sans-serif</c> (the default family) is the last resort, as in
/// CSS, and it is the bundled DejaVu Sans, as in matplotlib, so default text looks the same on every machine. When no
/// name matches anything, the operating system's default face is used and a <see cref="ChartDiagnostics"/> message
/// names the family that was asked for — once per family, so a misspelled name shows up instead of quietly rendering in
/// Segoe UI. <c>serif</c> and <c>monospace</c> have no bundled face and are left to the machine.</para>
/// <para><b>Ownership.</b> Every typeface and shaper handed out lives for the process. Callers never dispose them.</para>
/// <para><b>Concurrency.</b> Registration replaces the registry and the resolution cache as whole objects, read
/// through <see langword="volatile"/> fields, so a render on another thread sees either the old registry or the new
/// one and never a dictionary mid-change. There is no lock.</para>
/// </remarks>
public static class SkiaFonts
{
    private const string BundledResourcePrefix = "MatPlotLibNet.Skia.Fonts.";

    /// <summary>The generic family the package carries a face for: the default of <see cref="Font.Family"/>.</summary>
    private const string SansSerif = "sans-serif";

    /// <summary>The bundled family that <see cref="SansSerif"/> stands for, as in matplotlib.</summary>
    private const string BundledSansSerif = "DejaVu Sans";

    /// <summary>A registered face: the typeface and whether it came from the package or from the user.</summary>
    private sealed record FontEntry(SKTypeface Typeface, bool Bundled);

    /// <summary>The cache key: exactly what a caller asked for.</summary>
    private readonly record struct ResolutionKey(string? Family, SKFontStyleWeight Weight, SKFontStyleSlant Slant);

    /// <summary>The faces embedded in the package, read once for the life of the process. Never mutated: a registration copies the
    /// registry before it writes.</summary>
    private static readonly Dictionary<string, FontEntry> Bundled = LoadBundled();

    /// <summary>Family-and-style key → face. Replaced whole on every registration; never mutated in place.</summary>
    private static volatile Dictionary<string, FontEntry> _registry = Bundled;

    /// <summary>Every resolution ever made, memoised. Replaced whole on every registration so that a family resolved
    /// before its file was registered is resolved again afterwards.</summary>
    private static volatile ConcurrentDictionary<ResolutionKey, ResolvedTypeface> _resolved = new();

    /// <summary>The families that were asked for and found nowhere — each named once.</summary>
    private static readonly ConcurrentDictionary<string, byte> _warnedFamilies = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers the font file at <paramref name="path"/> for every chart this process renders from now on,
    /// under the family name the file declares.</summary>
    /// <param name="path">A TrueType or OpenType font file.</param>
    /// <returns>The family, weight and slant the file carries — the family is what goes in <see cref="Font.Family"/>.</returns>
    /// <exception cref="FileNotFoundException">There is no file at <paramref name="path"/>.</exception>
    /// <exception cref="ArgumentException">The file is not a font.</exception>
    /// <exception cref="InvalidOperationException">A font of that family, weight and slant was registered before.
    /// A family is registered once; a bundled DejaVu Sans face is replaced instead.</exception>
    public static RegisteredFont Register(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"There is no font file at '{path}'.", path);
        }

        var typeface = SKTypeface.FromFile(path)
            ?? throw new ArgumentException($"'{path}' is not a font file that SkiaSharp can read.", nameof(path));
        return Register(typeface);
    }

    /// <summary>Registers the font that <paramref name="stream"/> holds, for every chart this process renders from
    /// now on, under the family name the font declares. The stream is read to its end; the caller keeps ownership.</summary>
    /// <param name="stream">A TrueType or OpenType font.</param>
    /// <returns>The family, weight and slant the font carries.</returns>
    /// <exception cref="ArgumentException">The stream does not hold a font.</exception>
    /// <exception cref="InvalidOperationException">A font of that family, weight and slant was registered before.</exception>
    public static RegisteredFont Register(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var typeface = SKTypeface.FromStream(stream)
            ?? throw new ArgumentException("The stream does not hold a font that SkiaSharp can read.", nameof(stream));
        return Register(typeface);
    }

    private static RegisteredFont Register(SKTypeface typeface)
    {
        string key = BuildKey(typeface.FamilyName, typeface.FontStyle);
        var registered = new RegisteredFont(typeface.FamilyName, ToFontWeight(typeface.FontStyle), ToFontSlant(typeface.FontStyle));
        bool replacedBundled;

        // Copy, decide, publish: a second registration racing this one starts over from the registry it lost to.
        while (true)
        {
            var current = _registry;
            replacedBundled = false;
            if (current.TryGetValue(key, out var existing))
            {
                if (!existing.Bundled)
                {
                    throw new InvalidOperationException(
                        $"Font family '{typeface.FamilyName}' ({registered.Weight}, {registered.Slant}) is already registered; a family is registered once.");
                }

                replacedBundled = true;
            }

            var next = new Dictionary<string, FontEntry>(current, StringComparer.OrdinalIgnoreCase)
            {
                [key] = new FontEntry(typeface, Bundled: false),
            };
            if (Interlocked.CompareExchange(ref _registry, next, current) == current)
            {
                break;
            }
        }

        // Every earlier resolution is stale now: a family that fell back to the OS before this file arrived must
        // resolve again. The whole cache goes, not a key — the key is the caller's family stack, which this
        // family may sit anywhere inside.
        _resolved = new ConcurrentDictionary<ResolutionKey, ResolvedTypeface>();
        _warnedFamilies.TryRemove(typeface.FamilyName, out _);

        if (replacedBundled)
        {
            ChartDiagnostics.Emit(new ChartDiagnostic("SkiaFonts",
                $"Font family '{typeface.FamilyName}' ({registered.Weight}, {registered.Slant}) replaces the bundled face of that family.", null));
        }

        return registered;
    }

    /// <summary>The typeface and shaper for a <see cref="Font"/>: its family stack, weight and slant.</summary>
    internal static ResolvedTypeface Resolve(Font font) => Resolve(font.Family,
        font.IsBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
        font.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

    /// <summary>The typeface and shaper for a family stack, weight and slant. Resolved once per distinct request and
    /// memoised; the result is shared and never disposed by a caller.</summary>
    /// <param name="family">The font family stack, <see langword="null"/> for the default family.</param>
    /// <param name="weight">The weight asked for.</param>
    /// <param name="slant">The slant asked for.</param>
    /// <returns>The typeface and the shaper that answer the request.</returns>
    internal static ResolvedTypeface Resolve(string? family, SKFontStyleWeight weight, SKFontStyleSlant slant)
    {
        var key = new ResolutionKey(family, weight, slant);
        var cache = _resolved;
        if (cache.TryGetValue(key, out var hit))
        {
            return hit;
        }

        var (typeface, matched, shared) = ResolveUncached(family, weight, slant);
        var candidate = new ResolvedTypeface(typeface, new SKShaper(typeface), new FaceInk(typeface));
        var winner = cache.GetOrAdd(key, candidate);
        if (!ReferenceEquals(winner.Shaper, candidate.Shaper))
        {
            // Another thread resolved the same key first; this copy is surplus.
            candidate.Shaper.Dispose();
            if (!shared)
            {
                typeface.Dispose();
            }
        }

        // The diagnostic is raised here, outside the cache, and gated by its own set: GetOrAdd does not promise
        // to run a factory once, so "once per family" cannot hang on it.
        if (!matched && !string.IsNullOrWhiteSpace(family) && _warnedFamilies.TryAdd(family, 0))
        {
            ChartDiagnostics.Emit(new ChartDiagnostic("SkiaFonts",
                $"Font family '{family}' is not bundled, registered or installed on this machine; '{winner.Typeface.FamilyName}' is used instead. "
                + "Register the font file with SkiaFonts.Register to use it here.", null));
        }

        return winner;
    }

    /// <summary>The face found for a request.</summary>
    /// <param name="Typeface">The face.</param>
    /// <param name="Matched">Whether some name in the stack was really found (registered, bundled, or installed under that
    /// name) as opposed to the OS handing back its default.</param>
    /// <param name="Shared">Whether the typeface is owned by the registry and must never be disposed.</param>
    private readonly record struct FaceSearch(SKTypeface Typeface, bool Matched, bool Shared);

    /// <summary>Finds the face for a request (<see cref="FaceSearch"/>).</summary>
    private static FaceSearch ResolveUncached(string? family, SKFontStyleWeight weight, SKFontStyleSlant slant)
    {
        if (string.IsNullOrWhiteSpace(family))
        {
            return new FaceSearch(SKTypeface.FromFamilyName(null, weight, SKFontStyleWidth.Normal, slant), Matched: true, Shared: false);
        }

        // SKFontStyle is only a key ingredient here; the returned typefaces never retain it.
        using var style = new SKFontStyle(weight, SKFontStyleWidth.Normal, slant);
        var registry = _registry;
        var candidates = new List<string>();
        bool sansSerif = false;
        foreach (var raw in family.Split(','))
        {
            string name = raw.Trim().Trim('"', '\'');
            if (name.Length == 0)
            {
                continue;
            }

            // The generic family is no installed name: it is the last resort of the stack, below.
            if (string.Equals(name, SansSerif, StringComparison.OrdinalIgnoreCase))
            {
                sansSerif = true;
                continue;
            }

            candidates.Add(name);
            if (registry.TryGetValue(BuildKey(name, style), out var entry))
            {
                return new FaceSearch(entry.Typeface, Matched: true, Shared: true);
            }
        }

        // The machine's own fonts, one name at a time: a real match reports the name it was asked for.
        foreach (string name in candidates)
        {
            var installed = SKTypeface.FromFamilyName(name, weight, SKFontStyleWidth.Normal, slant);
            if (installed is not null && string.Equals(installed.FamilyName, name, StringComparison.OrdinalIgnoreCase))
            {
                return new FaceSearch(installed, Matched: true, Shared: false);
            }

            installed?.Dispose();
        }

        // "sans-serif" is the face the package carries, as matplotlib's is DejaVu Sans, never the machine's default.
        // Every style key of it is present: the four faces load with the package and a registration only replaces one.
        if (sansSerif)
        {
            return new FaceSearch(registry[BuildKey(BundledSansSerif, style)].Typeface, Matched: true, Shared: true);
        }

        return new FaceSearch(SKTypeface.FromFamilyName(family, weight, SKFontStyleWidth.Normal, slant), Matched: false, Shared: false);
    }

    /// <summary>The registry key for a family and style: the bare family for a regular upright face, and the family
    /// with <c>|Bold</c>, <c>|Italic</c> or <c>|BoldItalic</c> otherwise. A face that leans, italic or oblique, fills the italic slot.</summary>
    internal static string BuildKey(string family, SKFontStyle style)
    {
        bool bold = ToFontWeight(style) == FontWeight.Bold;
        bool italic = ToFontSlant(style) != FontSlant.Normal;
        return (bold, italic) switch
        {
            (true, true) => $"{family}|BoldItalic",
            (true, false) => $"{family}|Bold",
            (false, true) => $"{family}|Italic",
            _ => family,
        };
    }

    /// <summary>Reads the faces embedded in this assembly. Runs once, the first time the package resolves a font; a user's
    /// registrations come after it.</summary>
    private static Dictionary<string, FontEntry> LoadBundled()
    {
        var registry = new Dictionary<string, FontEntry>(StringComparer.OrdinalIgnoreCase);
        var assembly = typeof(SkiaFonts).Assembly;
        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(BundledResourcePrefix, StringComparison.Ordinal)
                || !name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(name)!;
            var typeface = SKTypeface.FromStream(stream)!;
            registry[BuildKey(typeface.FamilyName, typeface.FontStyle)] = new FontEntry(typeface, Bundled: true);
        }

        return registry;
    }

    /// <summary>Back to the state after package initialisation: only the bundled faces (the very ones read at the start),
    /// nothing resolved, nothing warned about. For tests that register fonts.</summary>
    internal static void ResetForTests()
    {
        _registry = Bundled;
        _resolved = new ConcurrentDictionary<ResolutionKey, ResolvedTypeface>();
        _warnedFamilies.Clear();
    }

    private static FontWeight ToFontWeight(SKFontStyle style) => style.Weight switch
    {
        >= (int)SKFontStyleWeight.SemiBold => FontWeight.Bold,
        <= (int)SKFontStyleWeight.Light => FontWeight.Light,
        _ => FontWeight.Normal,
    };

    private static FontSlant ToFontSlant(SKFontStyle style) => style.Slant switch
    {
        SKFontStyleSlant.Italic => FontSlant.Italic,
        SKFontStyleSlant.Oblique => FontSlant.Oblique,
        _ => FontSlant.Normal,
    };
}
