"use client";

import { useEffect } from "react";
import { getHealthLive } from "@/lib/api";

/**
 * WF-13: probe silencioso em dev. Sem UI. Sidebar e telas intactos.
 * GET {NEXT_PUBLIC_API_URL}/health/live — anônimo, espera 200.
 */
export default function ApiHealthProbe() {
  useEffect(() => {
    if (process.env.NODE_ENV !== "development") return;
    void getHealthLive().catch(() => undefined);
  }, []);

  return null;
}
