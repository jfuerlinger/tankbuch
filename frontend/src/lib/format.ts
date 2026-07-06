// de-AT-Formatierung – 1:1 aus der Design-Vorlage (fmt / fmtDate / parseDe)

export function fmt(n: number | null | undefined, d: number): string {
  if (n == null || !isFinite(n)) return '—';
  return n.toLocaleString('de-AT', { minimumFractionDigits: d, maximumFractionDigits: d });
}

export function fmtDate(iso: string): string {
  const d = new Date(iso + 'T12:00:00');
  return isNaN(d.getTime())
    ? iso
    : d.toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

export function parseDe(s: string | null | undefined): number | null {
  if (s == null) return null;
  let t = String(s).trim();
  if (!t) return null;
  if (t.includes(',')) t = t.replace(/\./g, '').replace(',', '.');
  const n = parseFloat(t);
  return isFinite(n) ? n : null;
}

export function todayISO(): string {
  return new Date().toISOString().slice(0, 10);
}
