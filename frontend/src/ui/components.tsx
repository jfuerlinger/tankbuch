import type { ReactNode } from 'react';
import { useStore } from '../store';
import { card, chip } from './styles';

export function VehChips({ sel, onSelect }: { sel: string; onSelect: (id: string) => void }) {
  const vehicles = useStore((s) => s.vehicles);
  const chips = [{ id: 'all', label: 'Alle Fahrzeuge', color: null as string | null },
    ...vehicles.map((v) => ({ id: v.id, label: v.name, color: v.farbe }))];
  return (
    <div className="tb-noscroll" style={{ display: 'flex', gap: 8, overflowX: 'auto', paddingBottom: 2 }}>
      {chips.map((c) => {
        const st = chip(sel === c.id, c.color);
        return (
          <button key={c.id} onClick={() => onSelect(c.id)} style={{
            display: 'flex', alignItems: 'center', gap: 7, whiteSpace: 'nowrap', padding: '8px 14px',
            borderRadius: 999, fontSize: 13.5, fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit',
            background: st.bg, color: st.fg, border: `1px solid ${st.bd}`, flexShrink: 0,
          }}>
            {c.color && <span style={{ width: 9, height: 9, borderRadius: '50%', background: c.color, display: 'inline-block' }} />}
            {c.label}
          </button>
        );
      })}
    </div>
  );
}

export function RangeChips({ sel, onSelect }: { sel: string; onSelect: (v: string) => void }) {
  const ranges: [string, string][] = [['all', 'Gesamter Zeitraum'], ['30', '30 Tage'], ['90', '3 Monate'], ['365', '12 Monate']];
  return (
    <div className="tb-noscroll" style={{ display: 'flex', gap: 8, overflowX: 'auto' }}>
      {ranges.map(([k, label]) => {
        const st = chip(sel === k, null);
        return (
          <button key={k} onClick={() => onSelect(k)} style={{
            whiteSpace: 'nowrap', padding: '6px 12px', borderRadius: 999, fontSize: 12.5, fontWeight: 600,
            cursor: 'pointer', fontFamily: 'inherit', background: st.bg, color: st.fg, border: `1px solid ${st.bd}`, flexShrink: 0,
          }}>{label}</button>
        );
      })}
    </div>
  );
}

export function KpiCard({ label, value, unit, sub, big = 23 }: { label: string; value: string; unit: string; sub?: string | null; big?: number }) {
  return (
    <div style={{ ...card, padding: '14px 16px', minWidth: 0 }}>
      <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text2)' }}>{label}</div>
      <div style={{ marginTop: 3, fontSize: big, fontWeight: 700, fontVariantNumeric: 'tabular-nums', letterSpacing: '-0.01em', whiteSpace: 'nowrap' }}>
        {value}<span style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--text3)', marginLeft: 4 }}>{unit}</span>
      </div>
      {sub && <div style={{ fontSize: 12, color: 'var(--text3)', marginTop: 2, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{sub}</div>}
    </div>
  );
}

export function ChartCard({ title, unit, children, legend }: { title: string; unit: string; children: ReactNode; legend?: ReactNode }) {
  return (
    <div style={{ ...card, padding: '16px 18px 12px', minWidth: 0 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 8 }}>
        <div style={{ fontSize: 14, fontWeight: 700 }}>{title}</div>
        <div style={{ fontSize: 12, color: 'var(--text3)' }}>{unit}</div>
      </div>
      <div style={{ marginTop: 6 }}>{children}</div>
      {legend}
    </div>
  );
}

export function Legend({ items }: { items: { color: string; label: string }[] }) {
  return (
    <div style={{ display: 'flex', gap: 14, flexWrap: 'wrap', marginTop: 4 }}>
      {items.map((lg, i) => (
        <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, color: 'var(--text2)' }}>
          <span style={{ width: 12, height: 3, borderRadius: 2, background: lg.color, display: 'inline-block' }} />{lg.label}
        </div>
      ))}
    </div>
  );
}

export const KPI_GRID: React.CSSProperties = { display: 'grid', gridTemplateColumns: 'repeat(auto-fit,minmax(158px,1fr))', gap: 12 };
export const CHART_GRID: React.CSSProperties = { display: 'grid', gridTemplateColumns: 'repeat(auto-fit,minmax(min(100%,310px),1fr))', gap: 14 };
export const EMPTY_CARD: React.CSSProperties = {
  background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 18, boxShadow: 'var(--shadow)',
  padding: '40px 24px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, textAlign: 'center',
};
