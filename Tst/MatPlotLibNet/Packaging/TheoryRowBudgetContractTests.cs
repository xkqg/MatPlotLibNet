// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The budget of rows of one theory. A rule that holds for every property of a type is one case per type that names
/// every property it finds wrong, not one case per property that builds the same figure again; a sweep over generated values is
/// one case with a loop; a product of independent dimensions is tested one dimension at a time. A theory over the budget is
/// therefore a theory to fold, unless it is named in <see cref="Declared"/> with the reason each of its rows has to be a case of
/// its own. Every suite links this file and holds its own theories to the budget.</summary>
public class TheoryRowBudgetContractTests
{
    /// <summary>The number of rows one theory may hold.</summary>
    internal const int RowBudget = 200;

    /// <summary>A theory allowed past the budget: the assembly that holds it, its class and method, and why each row is a case of
    /// its own.</summary>
    /// <param name="Assembly">The name of the test assembly.</param>
    /// <param name="Theory">The theory as <c>Class.Method</c>.</param>
    /// <param name="Reason">Why its rows cannot be folded.</param>
    internal readonly record struct DeclaredTheory(string Assembly, string Theory, string Reason);

    /// <summary>The theories allowed past the budget. A name stays only while its theory exists and is still over the budget.</summary>
    private static readonly DeclaredTheory[] Declared =
    [
        new("MatPlotLibNet.Tests", "OneOwnerSourceContractTests.ARule_ReadsItsProbesAsTheySay_AndIsSpelledInItsOwnersAndNowhereElse",
            "one row per declared rule of the rule tables: each row is a rule with its own probes and its own owners, read over the tree on its own, and a red row names the rule"),
    ];

    /// <summary>A theory of a suite and the number of rows its data gives.</summary>
    /// <param name="Name">The theory as <c>Class.Method</c>.</param>
    /// <param name="Rows">The number of rows.</param>
    internal readonly record struct TheoryRows(string Name, int Rows);

    /// <summary>The number of rows <paramref name="data"/> gives for <paramref name="method"/>, inline and member data alike.</summary>
    internal static async Task<int> RowsOf(MethodInfo method, IEnumerable<IDataAttribute> data)
    {
        await using var tracker = new DisposalTracker();
        int rows = 0;
        foreach (var source in data)
        {
            // A member named without its type is the test class's own, which the runner sets before it reads the data.
            if (source is MemberDataAttributeBase { MemberType: null } member)
            {
                member.MemberType = method.ReflectedType;
            }

            rows += (await source.GetData(method, tracker)).Count;
        }

        return rows;
    }

    /// <summary>Every theory of the public classes among <paramref name="types"/> with the rows its data gives.</summary>
    internal static async Task<IReadOnlyList<TheoryRows>> TheoriesOf(IEnumerable<Type> types)
    {
        var theories = new List<TheoryRows>();
        foreach (var type in types.Where(t => t is { IsClass: true, IsAbstract: false } && (t.IsPublic || t.IsNestedPublic)))
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                         .Where(m => m.IsDefined(typeof(TheoryAttribute), inherit: true)))
            {
                theories.Add(new($"{type.Name}.{method.Name}", await RowsOf(method, method.GetCustomAttributes().OfType<IDataAttribute>())));
            }
        }

        return theories;
    }

    /// <summary>The theories among <paramref name="theories"/> over the budget that <paramref name="declared"/> does not name.</summary>
    internal static string[] OverTheBudget(IEnumerable<TheoryRows> theories, params string[] declared) =>
        [.. theories.Where(t => t.Rows > RowBudget).Select(t => t.Name).Except(declared, StringComparer.Ordinal)
            .Select(name => $"{name}: {theories.First(t => t.Name == name).Rows} rows")];

    private static Assembly Suite => typeof(TheoryRowBudgetContractTests).Assembly;

    private static IEnumerable<DeclaredTheory> DeclaredHere => Declared.Where(d => d.Assembly == Suite.GetName().Name);

    [Fact]
    public async Task NoTheoryOfThisSuite_HoldsMoreRowsThanTheBudget_UnlessNamedWithItsReason()
    {
        var over = OverTheBudget(await TheoriesOf(Suite.GetTypes()), [.. DeclaredHere.Select(d => d.Theory)]);

        Assert.True(over.Length == 0,
            $"{over.Length} theories hold more than {RowBudget} rows: make each one case per subject that names every failing item, "
            + "or name it in Declared with the reason each row is a case of its own:\n" + string.Join('\n', over));
    }

    [Fact]
    public async Task EveryTheoryNamedOverTheBudget_ExistsAndIsStillOverIt_WithItsReason()
    {
        var theories = await TheoriesOf(Suite.GetTypes());

        Assert.All(DeclaredHere, declared =>
        {
            Assert.False(string.IsNullOrWhiteSpace(declared.Reason), $"{declared.Theory} is named without a reason.");
            Assert.Contains(theories, t => t.Name == declared.Theory && t.Rows > RowBudget);
        });
    }

    /// <summary>The probe of the guard: a theory one row over the budget is found, named with its rows, and passes once declared.</summary>
    [Fact]
    public async Task ATheoryOneRowOverTheBudget_IsFound_AndPassesOnceDeclared()
    {
        var probe = typeof(OverBudgetProbe).GetMethod(nameof(OverBudgetProbe.OneRowTooMany))!;
        TheoryRows[] rows = [new("OverBudgetProbe.OneRowTooMany", await RowsOf(probe, new IDataAttribute[] { new MemberDataAttribute(nameof(OverBudgetProbe.Rows)) })), new("Within", RowBudget)];

        Assert.Equal(new[] { "OverBudgetProbe.OneRowTooMany: 201 rows" }, OverTheBudget(rows));
        Assert.Empty(OverTheBudget(rows, "OverBudgetProbe.OneRowTooMany"));
    }

    /// <summary>The data of a theory one row over the budget, and a method to read it for.</summary>
    private static class OverBudgetProbe
    {
        public static TheoryData<int> Rows { get; } = new(Enumerable.Range(0, RowBudget + 1));

        public static void OneRowTooMany(int row) => _ = row;
    }
}
