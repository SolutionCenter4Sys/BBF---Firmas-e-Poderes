import "./globals.css";
import type { Metadata } from "next";
import Sidebar from "@/components/Sidebar";

export const metadata: Metadata = {
  title: "BBF Firmas e Poderes — Orquestrador",
  description: "Plataforma de Validação de Firmas e Poderes — Bradesco / BBF (MVP mock)"
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="pt-BR">
      <body>
        <a href="#main-content" className="skip-link">Pular para conteúdo principal</a>
        <div className="app">
          <Sidebar />
          <main id="main-content" className="main" tabIndex={-1}>{children}</main>
        </div>
      </body>
    </html>
  );
}
