// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Records one merge step in a hierarchical clustering dendrogram.</summary>
/// <param name="Left">Index of the left cluster (negative = leaf index negated-minus-1).</param>
/// <param name="Right">Index of the right cluster.</param>
/// <param name="Distance">Ward linkage distance at this merge.</param>
/// <param name="Size">Total number of leaves in the merged cluster.</param>
public sealed record DendrogramNode(int Left, int Right, double Distance, int Size);

/// <summary>Represents the output of a hierarchical clustering run.</summary>
/// <param name="Merges">Ordered merge steps from closest to farthest.</param>
/// <param name="LeafOrder">Leaf indices reordered to minimize dendrogram crossings.</param>
public sealed record Dendrogram(DendrogramNode[] Merges, int[] LeafOrder);

/// <summary>Hierarchical (agglomerative) clustering using Ward's method. Suitable for N&lt;1000 observations.</summary>
public static class HierarchicalClustering
{
    /// <summary>Clusters observations using Ward's linkage over a pairwise distance matrix.</summary>
    /// <param name="distanceMatrix">Symmetric N×N matrix where entry (i,j) is the distance between observations i and j.</param>
    /// <returns>A <see cref="Dendrogram"/> with merge steps and a leaf ordering that minimizes dendrogram crossings.</returns>
    public static Dendrogram Cluster(double[,] distanceMatrix)
    {
        int n = distanceMatrix.GetLength(0);
        if (n == 0) return new Dendrogram([], []);
        if (n == 1) return new Dendrogram([], [0]);

        // Current clusters: initially each observation is its own cluster.
        // cluster sizes and representative indices for Ward linkage.
        var merges = new List<DendrogramNode>(n - 1);
        var sizes = new int[2 * n - 1];
        var clusterLeaves = new List<List<int>>(2 * n - 1);

        for (int i = 0; i < n; i++)
        {
            sizes[i] = 1;
            clusterLeaves.Add([i]);
        }

        // Active set of cluster indices
        var active = new List<int>(n);
        for (int i = 0; i < n; i++) active.Add(i);

        // Distance between clusters (using Lance-Williams update for Ward's method)
        int totalClusters = 2 * n - 1;
        var dist = new double[totalClusters, totalClusters];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                dist[i, j] = distanceMatrix[i, j];

        int nextCluster = n;

        while (active.Count > 1)
        {
            // Find the closest pair
            double minDist = double.MaxValue;
            int minA = -1, minB = -1;
            for (int ai = 0; ai < active.Count; ai++)
            for (int bi = ai + 1; bi < active.Count; bi++)
            {
                int a = active[ai], b = active[bi];
                if (dist[a, b] < minDist)
                {
                    minDist = dist[a, b]; minA = a; minB = b;
                }
            }

            int newCluster = nextCluster++;
            int sizeA = sizes[minA], sizeB = sizes[minB];
            int sizeNew = sizeA + sizeB;
            sizes[newCluster] = sizeNew;

            var newLeaves = new List<int>(clusterLeaves[minA]);
            newLeaves.AddRange(clusterLeaves[minB]);
            clusterLeaves.Add(newLeaves);

            merges.Add(new DendrogramNode(minA, minB, minDist, sizeNew));

            // Remove minA and minB from active, add newCluster
            active.Remove(minA);
            active.Remove(minB);

            // Ward's linkage update: dist(new, k) for each remaining cluster k
            foreach (int k in active)
            {
                int sizeK = sizes[k];
                double dAk = dist[minA, k];
                double dBk = dist[minB, k];
                double dAB = dist[minA, minB];

                // Lance-Williams Ward's formula
                double newDist = ((sizeA + sizeK) * dAk
                                + (sizeB + sizeK) * dBk
                                - sizeK * dAB)
                               / (double)(sizeA + sizeB + sizeK);

                dist[newCluster, k] = newDist;
                dist[k, newCluster] = newDist;
            }

            active.Add(newCluster);
        }

        // Compute leaf order by in-order traversal of the merge tree
        int rootCluster = active[0];
        var leafOrder = new List<int>(n);
        InOrderTraversal(rootCluster, n, merges, clusterLeaves, leafOrder);

        return new Dendrogram([.. merges], [.. leafOrder]);
    }

    private static void InOrderTraversal(int clusterIdx, int n, List<DendrogramNode> merges,
        List<List<int>> clusterLeaves, List<int> result)
    {
        if (clusterIdx < n)
        {
            result.Add(clusterIdx);
            return;
        }

        int mergeIdx = clusterIdx - n;
        if (mergeIdx >= merges.Count) return;

        var merge = merges[mergeIdx];
        InOrderTraversal(merge.Left, n, merges, clusterLeaves, result);
        InOrderTraversal(merge.Right, n, merges, clusterLeaves, result);
    }
}
