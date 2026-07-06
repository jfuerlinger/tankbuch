import type { ReactNode } from 'react';
import { useStore } from '../store';
import { IconClose } from '../ui/icons';
import { fieldLabel, Toggle } from '../ui/styles';

const modalInput: React.CSSProperties = {
  width: '100%', boxSizing: 'border-box', padding: '10px 12px', borderRadius: 10, border: '1px solid var(--border)',
  background: 'var(--bg)', color: 'var(--text)', fontFamily: 'inherit', fontSize: 14.5, outline: 'none', fontVariantNumeric: 'tabular-nums',
};
const pill: React.CSSProperties = {
  fontSize: 10, fontWeight: 700, color: 'var(--accent-text)', background: 'color-mix(in srgb, var(--accent) 15%, transparent)', padding: '1px 6px', borderRadius: 999,
};

export function EditModal() {
  const { editId, editForm, autoCalc, derivedE } = useStore();
  const s = useStore();
  if (!editId || !editForm) return null;
  const f = editForm;

  const Field = ({ label, span, pillOn, children }: { label: string; span?: boolean; pillOn?: boolean; children: ReactNode }) => (
    <div style={span ? { gridColumn: '1/-1' } : undefined}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 5 }}>
        <label style={fieldLabel}>{label}</label>{pillOn && <span style={pill}>berechnet</span>}
      </div>
      {children}
    </div>
  );

  return (
    <div onClick={(e) => { if (e.target === e.currentTarget) s.closeEdit(); }} style={overlay}>
      <div style={modalCard(470)}>
        <div style={headerRow}>
          <div style={{ fontSize: 16, fontWeight: 700 }}>Tankvorgang bearbeiten</div>
          <button className="tb-icon" onClick={s.closeEdit} aria-label="Schließen" style={closeBtn}><IconClose size={16} /></button>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 11 }}>
          <Field label="Datum" span>
            <input className="tb-input" type="date" value={f.datum} onChange={(e) => s.setEditForm({ datum: e.target.value })} style={modalInput} />
          </Field>
          <Field label="Liter" pillOn={autoCalc && derivedE === 'liter'}>
            <input className="tb-input" value={f.liter} onChange={(e) => s.onTrioE('liter', e.target.value)} inputMode="decimal" style={modalInput} />
          </Field>
          <Field label="Preis pro Liter (€/l)" pillOn={autoCalc && derivedE === 'ppl'}>
            <input className="tb-input" value={f.ppl} onChange={(e) => s.onTrioE('ppl', e.target.value)} inputMode="decimal" style={modalInput} />
          </Field>
          <Field label="Gesamtpreis (€)" pillOn={autoCalc && derivedE === 'total'}>
            <input className="tb-input" value={f.total} onChange={(e) => s.onTrioE('total', e.target.value)} inputMode="decimal" style={modalInput} />
          </Field>
          <Field label="Kilometerstand">
            <input className="tb-input" value={f.km} onChange={(e) => s.setEditForm({ km: e.target.value })} inputMode="numeric" style={modalInput} />
          </Field>
          <Field label="Tankstelle" span>
            <input className="tb-input" value={f.tankstelle} onChange={(e) => s.setEditForm({ tankstelle: e.target.value })} style={modalInput} />
          </Field>
          <Field label="Notiz" span>
            <input className="tb-input" value={f.notiz} onChange={(e) => s.setEditForm({ notiz: e.target.value })} style={modalInput} />
          </Field>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 11, marginTop: 14 }}>
          <Toggle on={f.voll} onClick={s.toggleEVoll} label="Volltankung umschalten" />
          <div style={{ fontSize: 14, fontWeight: 600 }}>Volltankung</div>
        </div>
        <div style={footerRow}>
          <button onClick={s.closeEdit} style={cancelBtn}>Abbrechen</button>
          <button className="tb-accent" onClick={s.saveEdit} style={saveBtn}>Speichern</button>
        </div>
      </div>
    </div>
  );
}

export const overlay: React.CSSProperties = {
  position: 'fixed', inset: 0, zIndex: 50, background: 'rgba(9,11,15,.52)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 16, boxSizing: 'border-box',
};
export const modalCard = (max: number): React.CSSProperties => ({
  width: '100%', maxWidth: max, maxHeight: '88vh', overflow: 'auto', background: 'var(--card)', border: '1px solid var(--border)',
  borderRadius: 18, boxShadow: 'var(--shadow-lg)', padding: 20, boxSizing: 'border-box', animation: 'tb-in .2s ease',
});
export const headerRow: React.CSSProperties = { display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 10, marginBottom: 15 };
export const closeBtn: React.CSSProperties = { width: 30, height: 30, border: 'none', borderRadius: 8, background: 'none', color: 'var(--text3)', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center' };
export const footerRow: React.CSSProperties = { display: 'flex', justifyContent: 'flex-end', gap: 9, marginTop: 18 };
export const cancelBtn: React.CSSProperties = { border: '1px solid var(--border)', borderRadius: 11, background: 'none', color: 'var(--text2)', fontSize: 14, fontWeight: 600, padding: '11px 16px', cursor: 'pointer', fontFamily: 'inherit' };
export const saveBtn: React.CSSProperties = { border: 'none', borderRadius: 11, background: 'var(--accent)', color: 'var(--accent-ink)', fontSize: 14, fontWeight: 700, padding: '11px 20px', cursor: 'pointer', fontFamily: 'inherit' };
