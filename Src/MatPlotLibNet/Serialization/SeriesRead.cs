// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Serialization;

/// <summary>What the reader of a registered series kind made of a DTO: the series, or why its data is unreadable. The registry
/// hands it to the one owner of the report (<c>AxesSerializationExtensions.CreateFromDtoOrReport</c>) and to the MCP server's
/// refusal, so a kind that is known but whose data describes nothing is not reported as a kind nobody knows.</summary>
/// <param name="Series">The series made; <see langword="null"/> when the data is unreadable.</param>
/// <param name="Why">Why the data is unreadable, in plain words; <see langword="null"/> when a series was made.</param>
internal readonly record struct SeriesRead(ISeries? Series, string? Why)
{
    /// <summary>The reason given for a reader that names none: a reader registered through the public
    /// <see cref="SeriesRegistry.Register"/>, which returns a series or nothing.</summary>
    internal const string NothingMade = "its reader made nothing of it";

    /// <summary>The series a reader that gives no reason made, or <see cref="NothingMade"/> when it made none.</summary>
    /// <param name="series">The series the reader made; <see langword="null"/> when it made none.</param>
    /// <returns>The result of the read.</returns>
    internal static SeriesRead Of(ISeries? series) => new(series, series is null ? NothingMade : null);

    /// <summary>No series, for <paramref name="why"/>.</summary>
    /// <param name="why">What is wrong with the data.</param>
    /// <returns>A result that holds no series.</returns>
    internal static SeriesRead Unreadable(string why) => new(null, why);
}
