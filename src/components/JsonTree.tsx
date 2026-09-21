"use client";

export default function JsonTree({ value, name }: { value: unknown; name?: string }) {
  if (value === null || typeof value !== "object") {
    const text = typeof value === "string" ? `"${value}"` : String(value);
    return (
      <div className="json-row">
        {name !== undefined && <span className="json-key">{name}: </span>}
        <span className="json-leaf">{text}</span>
      </div>
    );
  }

  const entries = Array.isArray(value)
    ? value.map((v, i) => [String(i), v] as const)
    : Object.entries(value);

  return (
    <details className="json-block" open>
      <summary>
        {name !== undefined ? <span className="json-key">{name}</span> : null}
        <span className="json-meta">{Array.isArray(value) ? `Array(${value.length})` : "Object"}</span>
      </summary>
      <div className="json-children">
        {entries.map(([k, v]) => (
          <JsonTree key={k} name={k} value={v} />
        ))}
      </div>
    </details>
  );
}
