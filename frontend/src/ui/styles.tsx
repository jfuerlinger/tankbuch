import type { CSSProperties } from 'react';

export const card: CSSProperties = {
  background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 16, boxShadow: 'var(--shadow)',
};

export const inputStyle: CSSProperties = {
  width: '100%', boxSizing: 'border-box', padding: '11px 12px', borderRadius: 10,
  border: '1px solid var(--border)', background: 'var(--bg)', color: 'var(--text)',
  fontFamily: 'inherit', fontSize: 15, outline: 'none', fontVariantNumeric: 'tabular-nums',
};

export const fieldLabel: CSSProperties = { fontSize: 12, fontWeight: 600, color: 'var(--text2)' };

export const accentBtn: CSSProperties = {
  display: 'flex', alignItems: 'center', gap: 8, background: 'var(--accent)', color: 'var(--accent-ink)',
  border: 'none', borderRadius: 12, padding: '12px 18px', fontSize: 14.5, fontWeight: 700,
  cursor: 'pointer', boxShadow: 'var(--shadow)', fontFamily: 'inherit',
};

export const h1: CSSProperties = { margin: 0, fontSize: 24, fontWeight: 700, letterSpacing: '-0.01em' };

export const calcPill: CSSProperties = {
  fontSize: 10.5, fontWeight: 700, color: 'var(--accent-text)',
  background: 'color-mix(in srgb, var(--accent) 15%, transparent)', padding: '2px 7px', borderRadius: 999,
};

export function chip(active: boolean, color?: string | null) {
  const c = color || 'var(--accent)';
  return active
    ? { bg: `color-mix(in srgb, ${c} 16%, var(--card))`, fg: 'var(--text)', bd: c }
    : { bg: 'var(--card)', fg: 'var(--text2)', bd: 'var(--border)' };
}

export function sw(on: boolean) {
  return on
    ? { bg: 'var(--accent)', bd: 'var(--accent)', x: '21px' }
    : { bg: 'var(--card2)', bd: 'var(--border)', x: '2.5px' };
}

export function navSt(active: boolean) {
  return active
    ? { c: 'var(--text)', bg: 'color-mix(in srgb, var(--accent) 15%, transparent)' }
    : { c: 'var(--text2)', bg: 'transparent' };
}

// Umschalter (Volltankung / Auto-Berechnung)
export function Toggle({ on, onClick, label }: { on: boolean; onClick: () => void; label: string }) {
  const s = sw(on);
  return (
    <button onClick={onClick} aria-label={label} style={{
      width: 44, height: 26, borderRadius: 13, border: `1px solid ${s.bd}`, background: s.bg,
      position: 'relative', cursor: 'pointer', padding: 0, flexShrink: 0,
    }}>
      <span style={{
        position: 'absolute', top: 2.5, left: s.x, width: 19, height: 19, borderRadius: '50%',
        background: '#FFFFFF', boxShadow: '0 1px 3px rgba(0,0,0,.35)', transition: 'left .18s',
      }} />
    </button>
  );
}
