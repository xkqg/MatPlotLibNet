// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>The rules that read <see cref="SaveOptions"/>, held in one place so every export that needs one asks the
/// same question.</summary>
internal static class SaveOptionsExtensions
{
    /// <summary>The resolution a raster export draws <paramref name="figure"/> at when saved with these options: the
    /// resolution of the options when they ask for one (anything but the default of 96), else the figure's own.
    /// A figure with <c>WithDpi(300)</c> saved with <c>new SaveOptions { Title = "x" }</c> therefore stays at 300.</summary>
    public static double DpiFor(this SaveOptions options, Figure figure) =>
        options.Dpi == Figure.ReferenceDpi ? figure.Dpi : options.Dpi;

    /// <summary>The title the options ask a file to carry, or <see langword="null"/> when they ask for none: an empty
    /// title is none, in every format.</summary>
    public static string? TitleOrNull(this SaveOptions options) =>
        string.IsNullOrEmpty(options.Title) ? null : options.Title;

    /// <summary>The author the options ask a file to carry, or <see langword="null"/> when they ask for none: an empty
    /// author is none, in every format.</summary>
    public static string? AuthorOrNull(this SaveOptions options) =>
        string.IsNullOrEmpty(options.Author) ? null : options.Author;
}
