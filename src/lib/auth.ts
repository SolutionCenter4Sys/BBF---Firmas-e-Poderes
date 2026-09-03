const ACCESS_TOKEN_KEY = "bbf.access_token";
const PROFILE_KEY = "bbf.session_profile";

export const AUTH_UNAUTHORIZED_EVENT = "bbf:unauthorized";
export const AUTH_SESSION_EVENT = "bbf:session";

export interface AuthProfile {
  name: string;
  role: string;
  roleLabel: string;
  email: string;
}

function storage(): Storage | null {
  if (typeof window === "undefined") return null;
  return window.sessionStorage;
}

function notifySession(): void {
  if (typeof window === "undefined") return;
  window.dispatchEvent(new CustomEvent(AUTH_SESSION_EVENT));
}

export function getAccessToken(): string | null {
  const raw = storage()?.getItem(ACCESS_TOKEN_KEY);
  if (!raw) return null;
  const token = raw.trim();
  return token.length > 0 ? token : null;
}

export function setAccessToken(
  token: string,
  profile?: { email?: string; role?: string; name?: string }
): void {
  const value = token.trim();
  if (!value) {
    clearAccessToken();
    return;
  }
  storage()?.setItem(ACCESS_TOKEN_KEY, value);
  const resolved = resolveProfile(value, profile);
  storage()?.setItem(PROFILE_KEY, JSON.stringify(resolved));
  notifySession();
}

export function clearAccessToken(): void {
  storage()?.removeItem(ACCESS_TOKEN_KEY);
  storage()?.removeItem(PROFILE_KEY);
  notifySession();
}

export function getAuthProfile(): AuthProfile | null {
  if (!getAccessToken()) return null;
  const raw = storage()?.getItem(PROFILE_KEY);
  if (raw) {
    try {
      const parsed = JSON.parse(raw) as Partial<AuthProfile>;
      if (typeof parsed.name === "string" && typeof parsed.roleLabel === "string") {
        return {
          name: parsed.name,
          role: typeof parsed.role === "string" ? parsed.role : "",
          roleLabel: parsed.roleLabel,
          email: typeof parsed.email === "string" ? parsed.email : ""
        };
      }
    } catch {
      /* parse JWT abaixo */
    }
  }
  const token = getAccessToken();
  return token ? resolveProfile(token) : null;
}

function resolveProfile(
  token: string,
  hint?: { email?: string; role?: string; name?: string }
): AuthProfile {
  const claims = decodeJwtPayload(token);
  const email = firstString(
    hint?.email,
    claims.email,
    claims.unique_name,
    claims.sub,
    claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"]
  );
  const role = firstString(
    hint?.role,
    claims.role,
    claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
  );
  const name = firstString(
    hint?.name,
    claims.name,
    claims.given_name,
    claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
    displayNameFromEmail(email)
  ) || "Usuário";
  return {
    name,
    role,
    roleLabel: roleLabel(role),
    email
  };
}

function roleLabel(role: string): string {
  const key = role.trim().toLowerCase();
  if (key === "operador" || key === "analista") return "Operador";
  if (key === "auditor") return "Auditor";
  if (key === "admin") return "Administrador";
  if (key === "consumer") return "Consumidor";
  return role.trim() ? role : "Perfil desconhecido";
}

function displayNameFromEmail(email: string): string {
  const local = email.split("@")[0]?.trim() ?? "";
  if (!local) return "";
  return local
    .split(/[._-]+/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function firstString(...values: unknown[]): string {
  for (const value of values) {
    if (typeof value === "string" && value.trim()) return value.trim();
    if (Array.isArray(value) && typeof value[0] === "string" && value[0].trim()) {
      return value[0].trim();
    }
  }
  return "";
}

function decodeJwtPayload(token: string): Record<string, unknown> {
  const parts = token.split(".");
  if (parts.length < 2) return {};
  try {
    const base64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64 + "=".repeat((4 - (base64.length % 4)) % 4);
    const json = atob(padded);
    const parsed = JSON.parse(json) as unknown;
    return parsed && typeof parsed === "object" ? (parsed as Record<string, unknown>) : {};
  } catch {
    return {};
  }
}
