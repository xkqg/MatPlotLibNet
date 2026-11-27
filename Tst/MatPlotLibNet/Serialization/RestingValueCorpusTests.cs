// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>What every subject a figure draws holds when it is told nothing, pinned in a checked-in file per subject: each setting of a
/// series type, of a part of the axes or the figure and of a settings record a part is made of, with the value it starts from. A
/// setting whose start moves (a constant changed, a property that stops reading its constant, a record whose default is typed again)
/// fails here and names the subject and the setting, so a test of one start (<c>DefaultWhis_Is1Point5</c>) has nothing left to say.
/// <para>The subjects are the one roster of <see cref="RestingSubjects"/>, reflected, so a setting added tomorrow joins its subject's
/// file, and that file is written again in the commit that adds it. The files live under <c>Serialization/Golden/Resting</c>;
/// <c>MPL_REGEN_GOLDEN=1</c> writes them, as it writes the others.</para></summary>
public class RestingValueCorpusTests
{
    private const string Folder = "Resting";

    /// <summary>The values as the file holds them: a JSON object of one setting a line, in ordinal order, with only the backslash and
    /// the double quote escaped, so a value reads in the file as the corpus shows it.</summary>
    private static string TextOf(IReadOnlyDictionary<string, string> values) =>
        "{\n" + string.Join(",\n", values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"  \"{pair.Key}\": \"{pair.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"")) + "\n}";

    [Theory]
    [MemberData(nameof(RestingSubjects.Names), MemberType = typeof(RestingSubjects))]
    public void ASubjectToldNothing_StartsFromTheValuesPinned(string name)
    {
        var values = RestingSubjects.ValuesOf(RestingSubjects.Subject(name));

        var pinned = JsonSerializer.Deserialize<Dictionary<string, string>>(BytePins.Pinned(Folder, name, TextOf(values)))!;

        var apart = pinned.Keys.Union(values.Keys).Order(StringComparer.Ordinal)
            .Where(key => !pinned.TryGetValue(key, out string? was) || !values.TryGetValue(key, out string? now) || was != now)
            .Select(key => $"{name}.{key}: pinned {(pinned.TryGetValue(key, out string? was) ? was : "nothing")}, starts at {(values.TryGetValue(key, out string? now) ? now : "nothing")}")
            .ToList();
        Assert.True(apart.Count == 0, string.Join('\n', apart));
    }

    /// <summary>A new axes and a new figure hold nothing a call adds or sets later: every list of theirs (the series, the overlays, the
    /// insets, the subplots, the texts) is empty, and every partner, place or part the library sets when a call asks for it (the axes a
    /// scale is shared with, the top axis, the inset's bounds, the grid position) is not there yet. Reflected, so a list or a
    /// library-set reference added tomorrow is held too.</summary>
    [Fact]
    public void ANewAxesAndANewFigure_HoldNothingACallAddsLater()
    {
        var held = HeldAtRest(new Axes()).Concat(HeldAtRest(new Figure())).ToList();

        Assert.True(held.Count == 0, string.Join('\n', held));
        Assert.Contains(nameof(Axes.Series), ReadAtRest(typeof(Axes)));
        Assert.Contains(nameof(Axes.ShareXWith), ReadAtRest(typeof(Axes)));
        Assert.Contains(nameof(Figure.Texts), ReadAtRest(typeof(Figure)));
    }

    /// <summary>The properties of <paramref name="type"/> a call fills or sets later: every list it reads out (no public setter), and
    /// every reference only the library sets (a setter that is not public).</summary>
    private static IEnumerable<string> ReadAtRest(Type type) =>
        type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && p.SetMethod is not { IsPublic: true })
            .Where(p => p.PropertyType != typeof(string) && (typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType)
                || (p.SetMethod is not null && (!p.PropertyType.IsValueType || Nullable.GetUnderlyingType(p.PropertyType) is not null))))
            .Select(p => p.Name);

    /// <summary>Each property of <see cref="ReadAtRest"/> that holds something on <paramref name="owner"/>, named.</summary>
    private static IEnumerable<string> HeldAtRest(object owner) =>
        ReadAtRest(owner.GetType()).Select(name => (name, value: owner.GetType().GetProperty(name)!.GetValue(owner)))
            .Where(pair => pair.value is System.Collections.IEnumerable items ? items.Cast<object>().Any() : pair.value is not null)
            .Select(pair => $"A new {owner.GetType().Name}'s {pair.name} holds "
                + (pair.value is System.Collections.IEnumerable items ? $"{items.Cast<object>().Count()} items" : $"a {pair.value!.GetType().Name}"));

    [Fact]
    public void ThePinnedFiles_AreExactlyTheSubjects()
    {
        Assert.Equal(RestingSubjects.All.Select(subject => subject.Name).Order(StringComparer.Ordinal), BytePins.Names(Folder));
    }
}
