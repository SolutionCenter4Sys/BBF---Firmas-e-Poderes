"use client";

import { useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import {
  AUTH_UNAUTHORIZED_EVENT,
  clearAccessToken,
  getAccessToken
} from "@/lib/auth";
import Sidebar from "@/components/Sidebar";

export default function AuthShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const isLogin = pathname === "/login";
  const [ready, setReady] = useState(false);
  const [authed, setAuthed] = useState(false);

  useEffect(() => {
    const sync = () => {
      const has = Boolean(getAccessToken());
      setAuthed(has);
      setReady(true);
      if (!has && !isLogin) {
        router.replace("/login");
        return;
      }
      if (has && isLogin) {
        router.replace("/");
      }
    };

    sync();
    const onUnauthorized = () => {
      clearAccessToken();
      setAuthed(false);
      if (!isLogin) router.replace("/login");
    };
    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
    return () => window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
  }, [isLogin, router]);

  if (isLogin) {
    return <>{children}</>;
  }

  if (!ready || !authed) {
    return (
      <div className="login-boot">
        <p>Redirecionando para o login…</p>
      </div>
    );
  }

  return (
    <div className="app">
      <Sidebar />
      <main id="main-content" className="main" tabIndex={-1}>
        {children}
      </main>
    </div>
  );
}
