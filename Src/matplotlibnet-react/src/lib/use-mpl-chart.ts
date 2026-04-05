// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';

/** Return type for the useMplChart hook. */
export interface UseMplChartResult {
  svgContent: string;
  loading: boolean;
  error: Error | null;
}

/**
 * React hook that fetches chart SVG from a MatPlotLibNet endpoint.
 * Re-fetches whenever the chartUrl changes.
 *
 * @param chartUrl - The URL of the chart SVG endpoint.
 * @returns The SVG content, loading state, and any error.
 */
export function useMplChart(chartUrl: string): UseMplChartResult {
  const [svgContent, setSvgContent] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    if (!chartUrl) return;

    const controller = new AbortController();
    setLoading(true);
    setError(null);

    fetch(chartUrl, { signal: controller.signal })
      .then(res => res.text())
      .then(svg => {
        setSvgContent(svg);
        setLoading(false);
      })
      .catch(err => {
        if (err.name !== 'AbortError') {
          setError(err);
          setLoading(false);
        }
      });

    return () => controller.abort();
  }, [chartUrl]);

  return { svgContent, loading, error };
}
