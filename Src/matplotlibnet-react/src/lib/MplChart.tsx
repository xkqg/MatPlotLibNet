// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import { useMplChart } from './use-mpl-chart';
import { mountSvg } from './mount-svg';

/** Props for the MplChart component. */
export interface MplChartProps {
  /** The URL of the chart SVG endpoint. */
  chartUrl: string;
  /** Optional CSS class to add to the container. */
  cssClass?: string;
}

/**
 * Static chart component that fetches SVG from a MatPlotLibNet endpoint and renders it inline.
 * Mirrors MplChart.razor from MatPlotLibNet.Blazor.
 *
 * Usage:
 * ```tsx
 * <MplChart chartUrl="/api/chart.svg" cssClass="my-chart" />
 * ```
 */
export function MplChart({ chartUrl, cssClass }: MplChartProps) {
  const { svgContent } = useMplChart(chartUrl);
  const host = useRef<HTMLDivElement>(null);

  // Mounted so that the scripts of an interactive chart run (a script that arrives through innerHTML never does).
  useEffect(() => {
    if (host.current) mountSvg(host.current, svgContent);
  }, [svgContent]);

  return (
    <div className={`mpl-chart ${cssClass ?? ''}`}>
      <div ref={host} />
    </div>
  );
}
