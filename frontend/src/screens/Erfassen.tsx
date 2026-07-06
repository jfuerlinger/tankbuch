import type { ReactNode } from 'react';
import { useStore } from '../store';
import { chip, inputStyle, fieldLabel, calcPill, h1, Toggle } from '../ui/styles';
import { IconPump, IconGauge, IconCheck } from '../ui/icons';

function ScanCard(props: {
  title: string; subtitleNote?: string; subtitle: string; icon: ReactNode;
  imgUrl: string | null; demoBox: boolean; idle: boolean; scanning: boolean; done: boolean;
  scanLabel: string; onFile: (f: File | null) => void; onDemo: () => void;
}) {
  const striped = 'repeating-linear-gradient(-45deg, var(--card2), var(--card2) 10px, color-mix(in srgb, var(--text3) 8%, var(--card2)) 10px, color-mix(in srgb, var(--text3) 8%, var(--card2)) 20px)';
  return (
    <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 16, boxShadow: 'var(--shadow)', padding: 16, display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
        <span style={{ color: 'var(--accent-text)' }}>{props.icon}</span>
        <div>
          <div style={{ fontSize: 14, fontWeight: 700 }}>{props.title} {props.subtitleNote && <span style={{ fontWeight: 500, color: 'var(--text3)' }}>{props.subtitleNote}</span>}</div>
          <div style={{ fontSize: 12, color: 'var(--text3)' }}>{props.subtitle}</div>
        </div>
      </div>
      <div style={{ position: 'relative', borderRadius: 12, overflow: 'hidden', background: 'var(--card2)', aspectRatio: '16/9', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        {props.imgUrl && <img src={props.imgUrl} alt="" style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', objectFit: 'cover' }} />}
        {props.demoBox && <div style={{ position: 'absolute', inset: 0, background: striped, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text3)', fontSize: 12, fontWeight: 600 }}>Demo-Foto</div>}
        {props.idle && (
          <div style={{ color: 'var(--text3)', fontSize: 12.5, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6 }}>
            <span style={{ opacity: 0.9 }}>{props.icon}</span><span>Noch kein Foto</span>
          </div>
        )}
        {props.scanning && (
          <>
            <div style={{ position: 'absolute', inset: 0, background: 'color-mix(in srgb, var(--accent) 10%, transparent)' }} />
            <div style={{ position: 'absolute', left: 0, right: 0, height: 3, background: 'var(--accent)', boxShadow: '0 0 14px var(--accent)', animation: 'tb-scan 1.6s ease-in-out infinite', top: 2 }} />
            <div style={{ position: 'absolute', left: '50%', bottom: 10, transform: 'translateX(-50%)', background: 'var(--text)', color: 'var(--bg)', fontSize: 12, fontWeight: 600, padding: '5px 12px', borderRadius: 999, animation: 'tb-pulse 1.2s infinite', whiteSpace: 'nowrap' }}>{props.scanLabel}</div>
          </>
        )}
        {props.done && (
          <div style={{ position: 'absolute', top: 8, right: 8, background: 'var(--good)', color: '#0B1B10', fontSize: 11.5, fontWeight: 700, padding: '4px 10px', borderRadius: 999, display: 'flex', alignItems: 'center', gap: 5 }}>
            <IconCheck size={12} /> Erkannt
          </div>
        )}
      </div>
      <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
        <label className="tb-soft" style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 7, background: 'var(--card2)', border: '1px solid var(--border)', borderRadius: 10, padding: '10px 12px', fontSize: 13.5, fontWeight: 600, cursor: 'pointer', boxSizing: 'border-box' }}>
          <input type="file" accept="image/*" capture="environment" onChange={(e) => { props.onFile(e.target.files?.[0] ?? null); e.target.value = ''; }} style={{ display: 'none' }} />
          <IconPump size={15} /> Foto aufnehmen
        </label>
        <button onClick={props.onDemo} style={{ background: 'none', border: 'none', color: 'var(--accent-text)', fontSize: 13, fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit', padding: '10px 6px' }}>Demo</button>
      </div>
    </div>
  );
}

function Field({ label, note, pill, children }: { label: string; note?: string; pill?: boolean; children: ReactNode }) {
  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 7, marginBottom: 6 }}>
        <label style={fieldLabel}>{label} {note && <span style={{ fontWeight: 500, color: 'var(--text3)' }}>{note}</span>}</label>
        {pill && <span style={calcPill}>berechnet</span>}
      </div>
      {children}
    </div>
  );
}

export function Erfassen() {
  const { vehicles, form, autoCalc, derived, pumpImg, pumpDemo, tachoImg, tachoDemo, scanning, rec } = useStore();
  const s = useStore();

  if (vehicles.length === 0) {
    return (
      <div style={{ animation: 'tb-in .25s ease', display: 'flex', flexDirection: 'column', gap: 16, maxWidth: 880 }}>
        <div>
          <h1 style={h1}>Tankvorgang erfassen</h1>
          <div style={{ color: 'var(--text2)', fontSize: 13.5, marginTop: 2 }}>Per Foto oder manuell – Werte werden vor dem Speichern immer geprüft.</div>
        </div>
        <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 16, boxShadow: 'var(--shadow)', padding: '28px 22px', display: 'flex', flexDirection: 'column', alignItems: 'flex-start', gap: 10 }}>
          <div style={{ fontSize: 15, fontWeight: 700 }}>Zuerst ein Fahrzeug anlegen</div>
          <div style={{ fontSize: 13.5, color: 'var(--text2)' }}>Tankvorgänge werden immer einem Fahrzeug zugeordnet.</div>
          <button className="tb-accent" onClick={() => s.go('fahrzeuge')} style={{ background: 'var(--accent)', color: 'var(--accent-ink)', border: 'none', borderRadius: 11, padding: '11px 16px', fontSize: 14, fontWeight: 700, cursor: 'pointer', fontFamily: 'inherit' }}>Fahrzeug anlegen</button>
        </div>
      </div>
    );
  }

  const inp = (value: string, on: (v: string) => void, placeholder: string, mode: 'decimal' | 'numeric' | 'text' = 'text') => (
    <input className="tb-input" value={value} onChange={(e) => on(e.target.value)} inputMode={mode === 'text' ? undefined : mode} placeholder={placeholder} style={inputStyle} />
  );

  return (
    <div style={{ animation: 'tb-in .25s ease', display: 'flex', flexDirection: 'column', gap: 16, maxWidth: 880 }}>
      <div>
        <h1 style={h1}>Tankvorgang erfassen</h1>
        <div style={{ color: 'var(--text2)', fontSize: 13.5, marginTop: 2 }}>Per Foto oder manuell – Werte werden vor dem Speichern immer geprüft.</div>
      </div>

      <div>
        <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text2)', marginBottom: 7 }}>Fahrzeug</div>
        <div className="tb-noscroll" style={{ display: 'flex', gap: 8, overflowX: 'auto' }}>
          {vehicles.map((v) => {
            const st = chip(form.fahrzeugId === v.id, v.farbe);
            return (
              <button key={v.id} onClick={() => s.setFormVehicle(v.id)} style={{ display: 'flex', alignItems: 'center', gap: 7, whiteSpace: 'nowrap', padding: '8px 14px', borderRadius: 999, fontSize: 13.5, fontWeight: 600, cursor: 'pointer', fontFamily: 'inherit', background: st.bg, color: st.fg, border: `1px solid ${st.bd}`, flexShrink: 0 }}>
                <span style={{ width: 9, height: 9, borderRadius: '50%', background: v.farbe, display: 'inline-block' }} />{v.name}
              </button>
            );
          })}
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit,minmax(min(100%,255px),1fr))', gap: 14 }}>
        <ScanCard title="Zapfsäule scannen" subtitle="Erkennt Liter & Gesamtpreis" icon={<IconPump size={22} />}
          imgUrl={pumpImg} demoBox={pumpDemo} idle={!pumpImg && !pumpDemo} scanning={scanning === 'pump'} done={rec.pump && scanning !== 'pump'}
          scanLabel="Werte werden erkannt …" onFile={(f) => s.scan('pump', f)} onDemo={() => s.scan('pump', null)} />
        <ScanCard title="Tacho scannen" subtitleNote="(optional)" subtitle="Erkennt den Kilometerstand" icon={<IconGauge size={22} />}
          imgUrl={tachoImg} demoBox={tachoDemo} idle={!tachoImg && !tachoDemo} scanning={scanning === 'tacho'} done={rec.tacho && scanning !== 'tacho'}
          scanLabel="Kilometerstand wird erkannt …" onFile={(f) => s.scan('tacho', f)} onDemo={() => s.scan('tacho', null)} />
      </div>

      <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 16, boxShadow: 'var(--shadow)', padding: 18, display: 'flex', flexDirection: 'column', gap: 14 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
          <div style={{ fontSize: 15, fontWeight: 700 }}>Details</div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
            <span style={{ fontSize: 12.5, color: 'var(--text2)', fontWeight: 600 }}>Auto-Berechnung</span>
            <Toggle on={autoCalc} onClick={s.toggleAutoCalc} label="Auto-Berechnung umschalten" />
          </div>
        </div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit,minmax(min(100%,205px),1fr))', gap: 12 }}>
          <Field label="Getankte Liter" pill={autoCalc && derived === 'liter'}>{inp(form.liter, (v) => s.onTrio('liter', v), 'z. B. 45,50', 'decimal')}</Field>
          <Field label="Preis pro Liter (€/l)" pill={autoCalc && derived === 'ppl'}>{inp(form.ppl, (v) => s.onTrio('ppl', v), 'z. B. 1,589', 'decimal')}</Field>
          <Field label="Gesamtpreis (€)" pill={autoCalc && derived === 'total'}>{inp(form.total, (v) => s.onTrio('total', v), 'z. B. 72,30', 'decimal')}</Field>
          <Field label="Kilometerstand (km)">{inp(form.km, (v) => s.setForm({ km: v }), 'z. B. 48.230', 'numeric')}</Field>
          <Field label="Datum"><input className="tb-input" type="date" value={form.datum} onChange={(e) => s.setForm({ datum: e.target.value })} style={{ ...inputStyle, padding: '10.5px 12px' }} /></Field>
          <Field label="Tankstelle" note="(optional)">{inp(form.tankstelle, (v) => s.setForm({ tankstelle: v }), 'z. B. OMV Wien Nord')}</Field>
        </div>
        <Field label="Notiz" note="(optional)">{inp(form.notiz, (v) => s.setForm({ notiz: v }), 'z. B. Fahrt in die Steiermark')}</Field>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap', borderTop: '1px solid var(--border)', paddingTop: 15 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 11 }}>
            <Toggle on={form.voll} onClick={s.toggleVoll} label="Volltankung umschalten" />
            <div>
              <div style={{ fontSize: 14, fontWeight: 600 }}>Volltankung</div>
              <div style={{ fontSize: 12, color: 'var(--text3)' }}>Basis für die Verbrauchsberechnung</div>
            </div>
          </div>
          <button className="tb-accent" onClick={s.saveEntry} style={{ display: 'flex', alignItems: 'center', gap: 8, background: 'var(--accent)', color: 'var(--accent-ink)', border: 'none', borderRadius: 12, padding: '12px 22px', fontSize: 14.5, fontWeight: 700, cursor: 'pointer', fontFamily: 'inherit', boxShadow: 'var(--shadow)' }}>
            <IconCheck size={15} /> Speichern
          </button>
        </div>
      </div>
    </div>
  );
}
