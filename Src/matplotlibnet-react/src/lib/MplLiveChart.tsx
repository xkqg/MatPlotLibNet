// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import { useMplLiveChart } from './use-mpl-live-chart';
import { mountSvg } from './mount-svg';

/** Props for the MplLiveChart component. */
export interface MplLiveChartProps {
  /** The identifier of the chart to subscribe to. */
  chartId: string;
  /** The SignalR hub URL (defaults to "/charts-hub"). */
  hubUrl?: string;
  /** Optional CSS class to add to the container. */
  cssClass?: string;
  /** Optional initial SVG to display before the first update. */
  initialSvg?: string;
}

/**
 * Live chart component that receives real-time updates via SignalR.
 * Mirrors MplLiveChart.razor from MatPlotLibNet.Blazor.
 *
 * Usage:
 * ```tsx
 * <MplLiveChart chartId="sensor-1" hubUrl="/charts-hub" cssClass="live" />
 * ```
 */
export function MplLiveChart({ chartId, hubUrl = '/charts-hub', cssClass, initialSvg }: MplLiveChartProps) {
  const { svgContent, hub } = useMplLiveChart(chartId, hubUrl);
  const displaySvg = svgContent || initialSvg || '';
  const host = useRef<HTMLDivElement>(null);

  // Mounted by hand, not through dangerouslySetInnerHTML: the interaction script of a server-interactive chart travels
  // inside the SVG, and a script that arrives through innerHTML never runs. The host also carries the connection.
  useEffect(() => {
    if (host.current) mountSvg(host.current, displaySvg, hub ?? undefined);
  }, [displaySvg, hub]);

  return (
    <div className={`mpl-chart mpl-live ${cssClass ?? ''}`}>
      <div ref={host} />
    </div>
  );
}
