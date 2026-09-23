// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Verso.Abstractions;

namespace MatPlotLibNet.Verso;

/// <summary>
/// Draws a MatPlotLibNet chart inside a Verso notebook cell.
/// </summary>
/// <remarks>
/// <para>This package references nothing but <c>Verso.Abstractions</c>, and that is not tidiness — it is the
/// only arrangement that works. Verso installs an extension by copying the package <em>and its dependencies</em>
/// into one folder, and loads that folder in its own collectible <c>AssemblyLoadContext</c>. A charting assembly
/// shipped alongside would therefore be loaded a second time, and the <c>Figure</c> the formatter knows would be
/// a different type from the <c>Figure</c> the cell made — the same name, a different identity, and a
/// <c>typeof</c> comparison that is false forever. So the match is by name and the rendering verb is resolved
/// on the value's own assembly.</para>
/// <para>Nothing is cached across calls: no type, no <see cref="MethodInfo"/>, no assembly reference. Holding
/// one would keep the notebook kernel's load context alive after the cell that made it is gone.</para>
/// </remarks>
[VersoExtension]
public sealed class FigureFormatter : IDataFormatter
{
    private const string LibraryAssembly = "MatPlotLibNet";
    private const string FigureType = "MatPlotLibNet.Models.Figure";
    private const string FigureBuilderType = "MatPlotLibNet.FigureBuilder";
    private const string MosaicBuilderType = "MatPlotLibNet.Builders.MosaicFigureBuilder";
    private const string ExtensionsType = "MatPlotLibNet.FigureExtensions";
    private const string RenderVerb = "ToSvg";

    private const string Html = "text/html";
    private const string Svg = "image/svg+xml";

    /// <summary>Anything, because a value from the notebook's own load context can be no other type here.</summary>
    /// <remarks>The resolver uses this only to pre-filter before it asks <see cref="CanFormat"/>, and a type
    /// token from this assembly would exclude the very values this formatter exists for.</remarks>
    public IReadOnlyList<Type> SupportedTypes { get; } = [typeof(object)];

    /// <summary>Above everything Verso ships, so a chart is drawn as a chart.</summary>
    /// <remarks>Verso's own formatters run from 0 to 50; the two that would otherwise take a figure are the
    /// object formatter at 5 and the HTML one at 20. Forty sits clear of all of them without claiming to
    /// outrank the error formatter.</remarks>
    public int Priority => 40;

    /// <inheritdoc />
    public string ExtensionId => "io.github.xkqg.matplotlibnet.verso";

    /// <inheritdoc />
    public string Name => "MatPlotLibNet";

    /// <inheritdoc />
    /// <remarks>The version the build stamped, without the commit the SDK appends after a <c>+</c>. There is no
    /// fallback behind it on purpose: the stamp comes from <c>&lt;Version&gt;</c>, every package here carries
    /// one, and a fallback would be a branch no test could ever reach. The stamp itself is asserted instead.</remarks>
    public string Version =>
        typeof(FigureFormatter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion.Split('+')[0];

    /// <inheritdoc />
    public string Author => "H.P. Gansevoort";

    /// <inheritdoc />
    public string Description => "Draws MatPlotLibNet charts inline in a notebook cell, as SVG.";

    /// <inheritdoc />
    public bool CanFormat(object value, IFormatterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (value is null || !IsSomethingWeCanDraw(context.MimeType))
        {
            return false;
        }

        var type = value.GetType();

        return type.Assembly.GetName().Name == LibraryAssembly
            && type.FullName is FigureType or FigureBuilderType or MosaicBuilderType;
    }

    /// <inheritdoc />
    public Task<CellOutput> FormatAsync(object value, IFormatterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.CancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<CellOutput>(context.CancellationToken);
        }

        if (!CanFormat(value, context))
        {
            return Task.FromResult(Refusal(
                "MatPlotLibNet draws a figure or the builder that makes one, as HTML or SVG. "
                + $"This cell handed over {Describe(value)}."));
        }

        try
        {
            string svg = Render(value);

            return Task.FromResult(string.Equals(context.MimeType, Svg, StringComparison.OrdinalIgnoreCase)
                ? new CellOutput(Svg, svg)
                : new CellOutput(Html, FitToCell(svg, context.MaxWidth, context.MaxHeight)));
        }
        catch (Exception failure)
        {
            // Reflection wraps whatever the renderer threw, and the wrapper says nothing a reader can act on.
            while (failure is TargetInvocationException { InnerException: { } inner })
            {
                failure = inner;
            }

            return Task.FromResult(Refusal(
                $"MatPlotLibNet could not draw this chart. {failure.Message}",
                failure.GetType().FullName,
                failure.StackTrace));
        }
    }

    /// <inheritdoc />
    public Task OnLoadedAsync(IExtensionHostContext context) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OnUnloadedAsync() => Task.CompletedTask;

    private static bool IsSomethingWeCanDraw(string mimeType) =>
        string.Equals(mimeType, Html, StringComparison.OrdinalIgnoreCase)
        || string.Equals(mimeType, Svg, StringComparison.OrdinalIgnoreCase);

    private static CellOutput Refusal(string message, string? errorName = null, string? stackTrace = null) =>
        new("text/plain", message, IsError: true, ErrorName: errorName, ErrorStackTrace: stackTrace);

    private static string Describe(object? value) =>
        value is null ? "nothing" : $"a {value.GetType().FullName}";

    /// <summary>Renders through the library the VALUE came from, never through a type this assembly names.</summary>
    private static string Render(object value)
    {
        var type = value.GetType();

        // A builder renders itself; a figure is rendered by an extension method, which reflection sees as a
        // static on the class that declares it.
        var (method, target, arguments) = type.FullName == FigureType
            ? (type.Assembly.GetType(ExtensionsType, throwOnError: true)!
                   .GetMethod(RenderVerb, BindingFlags.Public | BindingFlags.Static, binder: null, [type], modifiers: null),
               (object?)null,
               new[] { value })
            : (type.GetMethod(RenderVerb, BindingFlags.Public | BindingFlags.Instance, binder: null, Type.EmptyTypes, modifiers: null),
               value,
               []);

        if (method is null || method.ReturnType != typeof(string))
        {
            throw new MissingMethodException($"{type.FullName} has no {RenderVerb}() that returns an SVG string.");
        }

        return method.Invoke(target, arguments) as string is { Length: > 0 } svg
            ? svg
            : throw new InvalidOperationException("The renderer returned no SVG.");
    }

    /// <summary>Hands the chart over as the cell can hold it.</summary>
    /// <remarks>
    /// The width needs nothing: the library's SVG already carries a <c>viewBox</c> and <c>width:100%</c>, so it
    /// fills the cell and keeps its proportions. Only a figure taller than the cell needs a decision, and the
    /// honest one is a scroll box — resizing the figure would re-run its layout and hand back a chart drawn at a
    /// size nobody chose, with the labels of twelve rows squeezed into four.
    /// </remarks>
    private static string FitToCell(string svg, double maxWidth, double maxHeight)
    {
        if (maxWidth <= 0 || maxHeight <= 0 || !FitsWithin(svg, maxWidth, maxHeight))
        {
            return maxHeight > 0 && maxWidth > 0
                ? $"<div style=\"max-height:{maxHeight.ToString("0.##", CultureInfo.InvariantCulture)}px;overflow-y:auto;\">{svg}</div>"
                : svg;
        }

        return svg;
    }

    /// <summary>Whether the chart, scaled to the cell's width, still fits its height.</summary>
    private static bool FitsWithin(string svg, double maxWidth, double maxHeight)
    {
        double width = RootAttribute(svg, "width");
        double height = RootAttribute(svg, "height");

        // An SVG whose root says nothing about its size scales freely; there is nothing to overflow.
        return width <= 0 || height <= 0 || maxWidth * (height / width) <= maxHeight;
    }

    /// <summary>A numeric attribute of the root element, or zero when it is absent or not a number.</summary>
    private static double RootAttribute(string svg, string name)
    {
        int end = svg.IndexOf('>');
        string root = end < 0 ? svg : svg[..end];

        int at = root.IndexOf($" {name}=\"", StringComparison.Ordinal);
        if (at < 0)
        {
            return 0;
        }

        int from = at + name.Length + 3;
        int to = root.IndexOf('"', from);

        return to > from && double.TryParse(root[from..to], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value
            : 0;
    }
}
