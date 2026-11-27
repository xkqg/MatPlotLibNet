// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Lighting;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>The vertex data a 3-D scene writes for the browser to turn it with (<see cref="Rendering.ThreeDAxesRenderer.SceneVertexWriter"/>): every point
/// normalised and written in six significant digits, separated by a space, and nothing at all when the scene writes no vertex
/// data.</summary>
public sealed class SceneVertexWriterTests
{
    private static readonly Projection3D Proj = new(30, -60, new Rect(0, 0, 400, 400), -3, 3, -2, 2, 0, 10);

    /// <summary>The wire as it was written one string per number: the oracle the writer must match byte for byte.</summary>
    private static string Oracle(params Vec3[] points)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < points.Length; i++)
        {
            var n = Proj.Normalize(points[i].X, points[i].Y, points[i].Z);
            if (i > 0) sb.Append(' ');
            sb.Append(n.Nx.ToString("G6", CultureInfo.InvariantCulture)).Append(',')
              .Append(n.Ny.ToString("G6", CultureInfo.InvariantCulture)).Append(',')
              .Append(n.Nz.ToString("G6", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    public static TheoryData<Vec3[]> Points => new()
    {
        { [new Vec3(1.25, -0.5, 7)] },
        { [new Vec3(-3, -2, 0), new Vec3(3, 2, 10)] },
        // Values a hair from the centre normalise to tiny numbers, which G6 writes with an exponent.
        { [new Vec3(1e-7, -2e-8, 5.0000001), new Vec3(-2.999999, 1.999999, 9.99999), new Vec3(0, 0, 5), new Vec3(-1e-9, 3e-9, 5)] },
    };

    [Theory]
    [MemberData(nameof(Points))]
    public void TheWire_IsTheOneStringPerNumberWire_ByteForByte(Vec3[] points)
    {
        var writer = new ThreeDAxesRenderer.SceneVertexWriter(new RecordingRenderContext(), Proj, writes: true);

        Assert.Equal(Oracle(points), writer.Wire(points));
    }

    [Fact]
    public void TheWire_OfASecondElement_DoesNotCarryTheFirst()
    {
        var writer = new ThreeDAxesRenderer.SceneVertexWriter(new RecordingRenderContext(), Proj, writes: true);
        _ = writer.Wire(new Vec3(1, 1, 1), new Vec3(2, 2, 2));

        Assert.Equal(Oracle(new Vec3(-1, 0, 3)), writer.Wire(new Vec3(-1, 0, 3)));
    }

    [Fact]
    public void Vertices_SetTheVertexDataAndThePinnedComponents_OfTheNextElement()
    {
        var ctx = new RecordingRenderContext();
        var writer = new ThreeDAxesRenderer.SceneVertexWriter(ctx, Proj, writes: true);

        writer.Vertices(ThreeDAxesRenderer.PinnedAxes.Y | ThreeDAxesRenderer.PinnedAxes.Z, new Vec3(1, 1, 1), new Vec3(2, 1, 1));
        writer.Vertices(ThreeDAxesRenderer.PinnedAxes.None, new Vec3(0, 0, 0));

        var data = ctx.Of<SetNextElementDataCall>().Select(a => (a.Key, a.Value))
            .ToList();
        Assert.Equal(
            [("v3d", Oracle(new Vec3(1, 1, 1), new Vec3(2, 1, 1))), ("v3d-pinned", ThreeDAxesRenderer.ToWire((ThreeDAxesRenderer.PinnedAxes.Y | ThreeDAxesRenderer.PinnedAxes.Z))),
             ("v3d", Oracle(new Vec3(0, 0, 0)))],
            data);
    }

    [Fact]
    public void ATextAnchor_CarriesItsPointThenTheEdgeThenThePad()
    {
        var ctx = new RecordingRenderContext();
        var writer = new ThreeDAxesRenderer.SceneVertexWriter(ctx, Proj, writes: true);

        writer.TextAnchor(new Vec3(1, 2, 3), "edge wire", "12.5", ThreeDAxesRenderer.PinnedAxes.X);

        string[] keys = [.. ctx.Of<SetNextElementDataCall>().Select(c => c.Key)];
        Assert.Equal(["v3d", "v3d-pinned", "v3d-edge", "pad"], keys);
    }

    [Fact]
    public void AScene_ThatWritesNoVertexData_SetsNothing()
    {
        var ctx = new RecordingRenderContext();
        var writer = new ThreeDAxesRenderer.SceneVertexWriter(ctx, Proj, writes: false);

        writer.Vertices(ThreeDAxesRenderer.PinnedAxes.X, new Vec3(1, 1, 1));
        writer.TextAnchor(new Vec3(1, 2, 3), "edge wire", "12.5", ThreeDAxesRenderer.PinnedAxes.X);

        Assert.False(writer.Writes);
        Assert.Empty(ctx.Of<SetNextElementDataCall>());
    }
}
