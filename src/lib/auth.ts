const ACCESS_TOKEN_KEY = "bbf.access_token";

function storage(): Storage | null {
  if (typeof window === "undefined") return null;
  return window.sessionStorage;
}

export function getAccessToken(): string | null {
  const raw = storage()?.getItem(ACCESS_TOKEN_KEY);
  if (!raw) return null;
  const token = raw.trim();
  return token.length > 0 ? token : null;
}

export function setAccessToken(token: string): void {
  const value = token.trim();
  if (!value) {
    clearAccessToken();
    return;
  }
  storage()?.setItem(ACCESS_TOKEN_KEY, value);
}

export function clearAccessToken(): void {
  storage()?.removeItem(ACCESS_TOKEN_KEY);
}

export const AUTH_UNAUTHORIZED_EVENT = "bbf:unauthorized";
