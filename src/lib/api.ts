import { AUTH_UNAUTHORIZED_EVENT, clearAccessToken, getAccessToken } from "@/lib/auth";

export const DEFAULT_API_URL = "http://localhost:8080";

export class ApiError extends Error {
  readonly status: number;
  readonly body: unknown;

  constructor(status: number, body: unknown, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.body = body;
  }
}

export function getApiBaseUrl(): string {
  const raw = process.env.NEXT_PUBLIC_API_URL ?? DEFAULT_API_URL;
  return raw.replace(/\/+$/, "") || DEFAULT_API_URL;
}

export type ApiRequestOptions = RequestInit & {
  /** Default true. Health probes and login stay false. */
  auth?: boolean;
};

function newCorrelationId(): string {
  return `corr_${crypto.randomUUID()}`;
}

function notifyUnauthorized(): void {
  clearAccessToken();
  if (typeof window === "undefined") return;
  window.dispatchEvent(new CustomEvent(AUTH_UNAUTHORIZED_EVENT));
}

export async function apiFetch(path: string, options: ApiRequestOptions = {}): Promise<Response> {
  const { auth = true, headers, ...rest } = options;
  const suffix = path.startsWith("/") ? path : `/${path}`;
  const url = `${getApiBaseUrl()}${suffix}`;

  const nextHeaders = new Headers(headers);
  if (!nextHeaders.has("X-Correlation-Id")) {
    nextHeaders.set("X-Correlation-Id", newCorrelationId());
  }

  if (auth) {
    const token = getAccessToken();
    if (token) {
      nextHeaders.set("Authorization", `Bearer ${token}`);
    }
  }

  const response = await fetch(url, { ...rest, headers: nextHeaders });

  if (response.status === 401) {
    notifyUnauthorized();
  }

  return response;
}

export async function getHealthLive(): Promise<{ status: number; ok: boolean; body: unknown }> {
  const response = await apiFetch("/health/live", { method: "GET", auth: false, cache: "no-store" });
  let body: unknown = null;
  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    body = await response.json();
  } else {
    const text = await response.text();
    body = text.length > 0 ? text : null;
  }

  if (!response.ok) {
    throw new ApiError(response.status, body, `GET /health/live → ${response.status}`);
  }

  return { status: response.status, ok: true, body };
}

export async function readApiBody(response: Response): Promise<unknown> {
  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("application/json") || contentType.includes("application/problem+json")) {
    return response.json().catch(() => null);
  }
  const text = await response.text();
  return text.length > 0 ? text : null;
}

export async function loginWithPassword(
  email: string,
  password: string
): Promise<{ accessToken: string; role: string; email: string }> {
  const response = await apiFetch("/v1/auth/login", {
    method: "POST",
    auth: false,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  });
  const body = await readApiBody(response);
  if (!response.ok) {
    throw new ApiError(response.status, body, parseApiProblem(body, `POST /v1/auth/login → ${response.status}`));
  }
  const rec = body && typeof body === "object" ? (body as Record<string, unknown>) : {};
  const accessToken = typeof rec.accessToken === "string" ? rec.accessToken : "";
  if (!accessToken) {
    throw new ApiError(response.status, body, "Login sem accessToken.");
  }
  return {
    accessToken,
    role: typeof rec.role === "string" ? rec.role : "",
    email: typeof rec.email === "string" ? rec.email : email
  };
}

export function parseApiProblem(body: unknown, fallback: string): string {
  if (body && typeof body === "object") {
    const rec = body as { detail?: unknown; title?: unknown; message?: unknown };
    if (typeof rec.detail === "string" && rec.detail.trim()) return rec.detail;
    if (typeof rec.message === "string" && rec.message.trim()) return rec.message;
    if (typeof rec.title === "string" && rec.title.trim()) return rec.title;
  }
  return fallback;
}
