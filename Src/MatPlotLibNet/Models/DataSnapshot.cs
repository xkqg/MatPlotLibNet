// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Models;

/// <summary>A copy of the data an object holds in collections, taken when it is drawn, that tells the next time whether any
/// of it changed. A value changed inside an array or a list passes no setter, so no change mark (<see cref="ChangeClock"/>)
/// sees it; this does. Every collection among the object's fields is copied: an array of numbers, of colours or of any other
/// plain value as its bytes, compared bit for bit (a NaN against a NaN is no change, 0 against -0 is one); any other array,
/// list or dictionary as its items, each compared as it is (a string, a record, a value) or, for an object of the library
/// with collections of its own (a tree node and its children), by being that same object holding the same data. A library
/// object a field holds directly (the root of a tree) is followed the same way. A field marked
/// <see cref="RenderCacheAttribute"/> is no part of the data.</summary>
internal sealed class DataSnapshot
{
    /// <summary>How deep the copy follows objects inside objects: past it, a nested object counts as changed, which draws
    /// it again rather than risk a stale picture.</summary>
    private const int MaxDepth = 32;

    private static readonly ConcurrentDictionary<Type, FieldInfo[]> Plans = new();
    private static readonly ConcurrentDictionary<Type, int> PlainValueSizes = new();
    private static readonly MethodInfo SizeOfMethod = typeof(Unsafe).GetMethod(nameof(Unsafe.SizeOf))!;
    private static readonly MethodInfo HasReferencesMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences))!;
    private static readonly Assembly Library = typeof(DataSnapshot).Assembly;

    private readonly Type _type;
    private readonly object?[] _copies;

    private DataSnapshot(object owner, int depth)
    {
        _type = owner.GetType();
        var plan = Plan(_type);
        _copies = new object?[plan.Length];
        for (int i = 0; i < plan.Length; i++) _copies[i] = Copy(plan[i].GetValue(owner), depth);
    }

    /// <summary>A copy of the collections <paramref name="owner"/> holds now. An object that keeps its copy
    /// (<see cref="IKeepsDataCopy"/>: a series that keeps an estimate, <see cref="KeptEstimate{T}"/>) is handed the copy it kept
    /// while it still holds the same data, otherwise a new one it keeps from then on, so the kept drawing of its subplot and its
    /// kept estimate share one copy a draw; any other object is copied afresh. A copy is never changed once made and is put in the
    /// field in one write, so a draw that reads the field reads one whole copy.</summary>
    internal static DataSnapshot Of(object owner) => owner is IKeepsDataCopy keeper ? KeptIn(ref keeper.DataCopy, owner) : new(owner, 0);

    private static DataSnapshot KeptIn(ref DataSnapshot? slot, object owner)
    {
        var kept = slot;
        if (kept is not null && kept.Matches(owner)) return kept;
        return slot = new(owner, 0);
    }
    /// <summary>Whether <paramref name="owner"/> is of the type this copy was taken of and holds the same data in every
    /// collection.</summary>
    internal bool Matches(object owner) => Matches(owner, 0);

    private bool Matches(object owner, int depth)
    {
        if (owner.GetType() != _type) return false;
        var plan = Plan(_type);
        for (int i = 0; i < plan.Length; i++)
            if (!Same(_copies[i], plan[i].GetValue(owner), depth)) return false;
        return true;
    }

    /// <summary>The fields of <paramref name="type"/> (its own and inherited ones) a copy is made of: every array, every other
    /// collection and every object of the library that holds collections of its own; never a render cache.</summary>
    /// <param name="type">The type whose fields are listed.</param>
    /// <returns>The fields that are copied, in the order the type declares them.</returns>
    internal static FieldInfo[] Plan(Type type) =>
        Plans.TryGetValue(type, out var plan) ? plan : Plans.GetOrAdd(type, BuildPlan(type, []));

    /// <summary>The plan of <paramref name="type"/>, with the types whose plan is being built further up in
    /// <paramref name="visiting"/>: a type that holds itself (a node with a parent) counts as holding data, so the walk ends.</summary>
    private static FieldInfo[] BuildPlan(Type type, HashSet<Type> visiting)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        visiting.Add(type);
        var fields = new List<FieldInfo>();
        for (var level = type; level is not null && level != typeof(object); level = level.BaseType)
            foreach (var field in level.GetFields(flags))
                if (field.GetCustomAttribute<RenderCacheAttribute>() is null && HoldsData(field.FieldType, visiting))
                    fields.Add(field);
        visiting.Remove(type);
        return [.. fields];
    }

    /// <summary>Whether a field of <paramref name="type"/> can hold data that changes without a setter.</summary>
    private static bool HoldsData(Type type, HashSet<Type> visiting) =>
        type.IsArray
        || (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
        || (IsLibraryClass(type) && (visiting.Contains(type) || (Plans.TryGetValue(type, out var known) ? known : BuildPlan(type, visiting)).Length > 0));

    /// <summary>A concrete class of the library that does not report its own changes.</summary>
    private static bool IsLibraryClass(Type type) =>
        type.Assembly == Library && type.IsClass && !type.IsAbstract && !typeof(Delegate).IsAssignableFrom(type)
        && !typeof(IChangeTracked).IsAssignableFrom(type) && type != typeof(DataSnapshot);

    /// <summary>A concrete class of the library, not a part that reports its own changes, with collections of its own.</summary>
    private static bool IsLibraryObjectWithData(Type type) => IsLibraryClass(type) && Plan(type).Length > 0;

    private static object? Copy(object? value, int depth) => value switch
    {
        null => null,
        string => value,
        Array array when PlainValueSize(array.GetType().GetElementType()!) is int size and > 0 => new BytesCopy(array, size),
        IEnumerable items => new ItemsCopy(value.GetType(), [.. ItemsOf(items, depth)]),
        _ when IsLibraryObjectWithData(value.GetType()) =>
            depth < MaxDepth ? new ObjectCopy(value, new DataSnapshot(value, depth + 1)) : TooDeep.Instance,
        _ => value,
    };

    private static IEnumerable<object?> ItemsOf(IEnumerable items, int depth)
    {
        foreach (var item in items) yield return Copy(item, depth + 1);
    }

    private static bool Same(object? copy, object? now, int depth) => copy switch
    {
        null => now is null,
        BytesCopy bytes => now is Array array && bytes.Matches(array),
        ItemsCopy items => now is IEnumerable sequence && now is not string && now.GetType() == items.Type && SameItems(items.Items, sequence, depth),
        ObjectCopy obj => ReferenceEquals(obj.Object, now) && obj.Data.Matches(now, depth + 1),
        TooDeep => false,
        _ => Equals(copy, now),
    };

    private static bool SameItems(object?[] copies, IEnumerable now, int depth)
    {
        int i = 0;
        foreach (var item in now)
        {
            if (i >= copies.Length || !Same(copies[i], item, depth + 1)) return false;
            i++;
        }

        return i == copies.Length;
    }

    /// <summary>The size of one element of <paramref name="type"/> when it is a plain value (a number, a flag, a colour, an
    /// enum: no reference anywhere inside), so an array of it is compared as bytes; 0 otherwise.</summary>
    private static int PlainValueSize(Type type) => PlainValueSizes.GetOrAdd(type, static t =>
        t.IsValueType && !(bool)HasReferencesMethod.MakeGenericMethod(t).Invoke(null, null)!
            ? (int)SizeOfMethod.MakeGenericMethod(t).Invoke(null, null)!
            : 0);

    /// <summary>An array of plain values, as its type, its shape and its bytes.</summary>
    private sealed class BytesCopy
    {
        private readonly Type _type;
        private readonly int[] _lengths;
        private readonly byte[] _bytes;

        internal BytesCopy(Array array, int elementSize)
        {
            _type = array.GetType();
            _lengths = LengthsOf(array);
            _bytes = BytesOf(array, elementSize).ToArray();
        }

        internal bool Matches(Array array)
        {
            if (array.GetType() != _type || array.Length * (long)SizeOf() != _bytes.Length) return false;
            for (int d = 0; d < _lengths.Length; d++)
                if (array.GetLength(d) != _lengths[d]) return false;
            return BytesOf(array, SizeOf()).SequenceEqual(_bytes);
        }

        private int SizeOf() => PlainValueSize(_type.GetElementType()!);

        private static int[] LengthsOf(Array array)
        {
            var lengths = new int[array.Rank];
            for (int d = 0; d < lengths.Length; d++) lengths[d] = array.GetLength(d);
            return lengths;
        }

        private static ReadOnlySpan<byte> BytesOf(Array array, int elementSize) =>
            MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(array), checked(array.Length * elementSize));
    }

    /// <summary>A collection of other items, as its type and a copy of each item.</summary>
    private sealed record ItemsCopy(Type Type, object?[] Items);

    /// <summary>A library object with collections of its own: the object itself and a copy of its data.</summary>
    private sealed record ObjectCopy(object Object, DataSnapshot Data);

    /// <summary>A nested object past <see cref="MaxDepth"/>: it never matches.</summary>
    private sealed class TooDeep
    {
        internal static readonly TooDeep Instance = new();
    }
}
