// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for Sankey hover emphasis — hovering a node
/// dims every link that isn't reachable upstream or downstream from that node, mirroring
/// ECharts' <c>focus: adjacency</c> behaviour.</summary>
internal static class SvgSankeyHoverScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that installs hover listeners on every Sankey
    /// node rectangle (identified by <c>data-sankey-node-id</c>) and every link path
    /// (identified by <c>data-sankey-link-source</c>/<c>data-sankey-link-target</c>).
    /// On mouseenter the script walks the link topology from the hovered node in both
    /// directions (upstream via target→source traversal, downstream via source→target
    /// traversal), collects the set of reachable nodes and links, then reduces the
    /// opacity of everything else to ~0.15 so the reader's eye is drawn to the selected
    /// flow chain. On mouseleave every element returns to its stored base opacity.
    /// </summary>
    /// <remarks>
    /// <para>Keyboard accessibility: each node rect also receives <c>tabindex="0"</c>, and
    /// focus events mirror the hover behaviour so keyboard users get the same emphasis.</para>
    ///
    /// <para>The script reads opacity directly from the element's stored <c>data-base-opacity</c>
    /// attribute (set on first interaction) so restoring the non-hover state can't drift if
    /// the caller mutates opacities via CSS or other scripts.</para>
    /// </remarks>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            // Per-chart isolation (Phase 2): scope queries to THIS script's owning <svg>.
            var svg = (document.currentScript && document.currentScript.parentNode) || document;
            var nodes = svg.querySelectorAll('[data-sankey-node-id]');
            if (nodes.length === 0) return;
            var links = svg.querySelectorAll('[data-sankey-link-source]');
            if (links.length === 0) return;

            // Build source→links and target→links adjacency tables for O(1) traversal.
            var outgoing = {}, incoming = {};
            links.forEach(function(link) {
                var src = link.getAttribute('data-sankey-link-source');
                var tgt = link.getAttribute('data-sankey-link-target');
                (outgoing[src] = outgoing[src] || []).push({ link: link, tgt: tgt });
                (incoming[tgt] = incoming[tgt] || []).push({ link: link, src: src });
                // Store base opacity once so restore is idempotent.
                if (!link.hasAttribute('data-base-opacity')) {
                    var current = link.getAttribute('fill-opacity');
                    link.setAttribute('data-base-opacity', current !== null ? current : '1');
                }
            });
            nodes.forEach(function(node) {
                if (!node.hasAttribute('data-base-opacity')) {
                    node.setAttribute('data-base-opacity', '1');
                }
            });

            function reachable(startId) {
                var reachableLinks = new Set();
                var reachableNodes = new Set();
                reachableNodes.add(startId);
                // Downstream BFS.
                var frontier = [startId];
                while (frontier.length) {
                    var cur = frontier.shift();
                    (outgoing[cur] || []).forEach(function(edge) {
                        reachableLinks.add(edge.link);
                        if (!reachableNodes.has(edge.tgt)) {
                            reachableNodes.add(edge.tgt);
                            frontier.push(edge.tgt);
                        }
                    });
                }
                // Upstream BFS.
                frontier = [startId];
                while (frontier.length) {
                    var cur = frontier.shift();
                    (incoming[cur] || []).forEach(function(edge) {
                        reachableLinks.add(edge.link);
                        if (!reachableNodes.has(edge.src)) {
                            reachableNodes.add(edge.src);
                            frontier.push(edge.src);
                        }
                    });
                }
                return { links: reachableLinks, nodes: reachableNodes };
            }

            function highlight(nodeId) {
                var r = reachable(nodeId);
                links.forEach(function(link) {
                    var inSet = r.links.has(link);
                    link.setAttribute('fill-opacity', inSet ? link.getAttribute('data-base-opacity') : '0.08');
                });
                nodes.forEach(function(node) {
                    var inSet = r.nodes.has(node.getAttribute('data-sankey-node-id'));
                    node.setAttribute('fill-opacity', inSet ? node.getAttribute('data-base-opacity') : '0.25');
                });
            }

            function restore() {
                links.forEach(function(link) {
                    link.setAttribute('fill-opacity', link.getAttribute('data-base-opacity'));
                });
                nodes.forEach(function(node) {
                    node.setAttribute('fill-opacity', node.getAttribute('data-base-opacity'));
                });
            }

            nodes.forEach(function(node) {
                node.style.cursor = 'pointer';
                node.setAttribute('tabindex', '0');
                var id = node.getAttribute('data-sankey-node-id');
                node.addEventListener('mouseenter', function() { highlight(id); });
                node.addEventListener('mouseleave', restore);
                node.addEventListener('focus', function() { highlight(id); });
                node.addEventListener('blur', restore);
            });
        })();
        ]]></script>
        """;
}
