import "./globals.css";
import type { Metadata } from "next";
import ApiHealthProbe from "@/components/ApiHealthProbe";
import AuthShell from "@/components/AuthShell";

export const metadata: Metadata = {
  title: "BBF Firmas e Poderes — Orquestrador",
  description: "Plataforma de Validação de Firmas e Poderes — Bradesco / BBF (MVP mock)"
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="pt-BR">
      <body>
        <ApiHealthProbe />
        <a href="#main-content" className="skip-link">Pular para conteúdo principal</a>
        <AuthShell>{children}</AuthShell>
      </body>
    </html>
  );
}
