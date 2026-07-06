import { useStore } from '../store';
import { IconClose } from '../ui/icons';
import { fieldLabel } from '../ui/styles';
import { overlay, modalCard, headerRow, closeBtn, footerRow, cancelBtn, saveBtn } from './EditModal';

const modalInput: React.CSSProperties = {
  width: '100%', boxSizing: 'border-box', padding: '10px 12px', borderRadius: 10, border: '1px solid var(--border)',
  background: 'var(--bg)', color: 'var(--text)', fontFamily: 'inherit', fontSize: 14.5, outline: 'none',
};
const SWATCHES = ['#3B82F6', '#2DD4BF', '#F59E0B', '#A78BFA', '#F472B6', '#34D399', '#FB923C', '#94A3B8'];
const FUELS = ['Diesel', 'Super 95', 'Super Plus 98', 'Premium Diesel', 'LPG', 'CNG'];

export function VehicleModal() {
  const { vehModal, vehId, vehForm, vehConfirm } = useStore();
  const s = useStore();
  if (!vehModal || !vehForm) return null;
  const f = vehForm;

  return (
    <div onClick={(e) => { if (e.target === e.currentTarget) s.closeVehModal(); }} style={overlay}>
      <div style={modalCard(440)}>
        <div style={headerRow}>
          <div style={{ fontSize: 16, fontWeight: 700 }}>{vehId ? 'Fahrzeug bearbeiten' : 'Fahrzeug anlegen'}</div>
          <button className="tb-icon" onClick={s.closeVehModal} aria-label="Schließen" style={closeBtn}><IconClose size={16} /></button>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 11 }}>
          <div>
            <div style={{ marginBottom: 5 }}><label style={fieldLabel}>Name</label></div>
            <input className="tb-input" value={f.name} onChange={(e) => s.setVehForm({ name: e.target.value })} placeholder="z. B. Škoda Octavia Combi" style={modalInput} />
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 11 }}>
            <div>
              <div style={{ marginBottom: 5 }}><label style={fieldLabel}>Kennzeichen</label></div>
              <input className="tb-input" value={f.kz} onChange={(e) => s.setVehForm({ kz: e.target.value })} placeholder="z. B. W-123 AB" style={modalInput} />
            </div>
            <div>
              <div style={{ marginBottom: 5 }}><label style={fieldLabel}>Kraftstoffart</label></div>
              <select className="tb-input" value={f.fuel} onChange={(e) => s.setVehForm({ fuel: e.target.value })} style={modalInput}>
                {FUELS.map((x) => <option key={x} value={x}>{x}</option>)}
              </select>
            </div>
          </div>
          <div>
            <div style={{ marginBottom: 5 }}><label style={fieldLabel}>Anfangs-Kilometerstand</label></div>
            <input className="tb-input" value={f.startKm} onChange={(e) => s.setVehForm({ startKm: e.target.value })} inputMode="numeric" placeholder="z. B. 41.250" style={{ ...modalInput, fontVariantNumeric: 'tabular-nums' }} />
          </div>
          <div>
            <div style={{ marginBottom: 7 }}><label style={fieldLabel}>Farbe</label></div>
            <div style={{ display: 'flex', gap: 9, flexWrap: 'wrap' }}>
              {SWATCHES.map((c) => (
                <button key={c} onClick={() => s.setVehForm({ color: c })} aria-label="Farbe wählen"
                  style={{ width: 30, height: 30, borderRadius: '50%', background: c, border: 'none', cursor: 'pointer', boxShadow: f.color === c ? `0 0 0 2.5px var(--card), 0 0 0 5px ${c}` : '0 0 0 1px var(--border)' }} />
              ))}
            </div>
          </div>
        </div>

        {vehId && (
          <div style={{ marginTop: 16, borderTop: '1px solid var(--border)', paddingTop: 13 }}>
            {!vehConfirm ? (
              <button onClick={s.askVehDel} style={{ background: 'none', border: 'none', color: 'var(--bad)', fontSize: 13, fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit', padding: '2px 0' }}>Fahrzeug löschen …</button>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 9 }}>
                <div style={{ fontSize: 13, color: 'var(--text2)' }}>Wirklich löschen? Alle Tankvorgänge dieses Fahrzeugs werden entfernt.</div>
                <div style={{ display: 'flex', gap: 8 }}>
                  <button onClick={s.deleteVeh} style={{ border: 'none', borderRadius: 9, background: 'var(--bad)', color: '#FFFFFF', fontSize: 13, fontWeight: 700, padding: '9px 14px', cursor: 'pointer', fontFamily: 'inherit' }}>Ja, löschen</button>
                  <button onClick={s.cancelVehDel} style={{ border: '1px solid var(--border)', borderRadius: 9, background: 'none', color: 'var(--text2)', fontSize: 13, fontWeight: 600, padding: '9px 14px', cursor: 'pointer', fontFamily: 'inherit' }}>Abbrechen</button>
                </div>
              </div>
            )}
          </div>
        )}

        <div style={footerRow}>
          <button onClick={s.closeVehModal} style={cancelBtn}>Abbrechen</button>
          <button className="tb-accent" onClick={s.saveVeh} style={saveBtn}>Speichern</button>
        </div>
      </div>
    </div>
  );
}
