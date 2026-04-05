// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { useMplChart } from './use-mpl-chart';

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

  return (
    <div className={`mpl-chart ${cssClass ?? ''}`}>
      <div dangerouslySetInnerHTML={{ __html: svgContent }} />
    </div>
  );
}
