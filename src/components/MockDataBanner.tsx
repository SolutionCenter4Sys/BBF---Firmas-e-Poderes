export function MockDataBanner({ detail }: { detail?: string }) {
  return (
    <div className="banner banner--warn" role="status">
      Não persistido{detail ? ` — ${detail}` : " — dados de mock. Sem endpoint .NET."}
    </div>
  );
}
