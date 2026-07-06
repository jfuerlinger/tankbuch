import { useMemo } from 'react';
import { useStore } from '../store';
import { fmt, fmtDate } from '../lib/format';
import { VehChips, RangeChips } from '../ui/components';
import { IconEdit, IconTrash } from '../ui/icons';
import { h1 } from '../ui/styles';

export function Verlauf() {
  const { vehicles, entries, histVeh, histRange, delId } = useStore();
  const s = useStore();

  const rows = useMemo(() => {
    const veh = (id: string) => vehicles.find((v) => v.id === id);
    const cutoff = histRange === 'all' ? null : new Date(Date.now() - parseInt(histRange, 10) * 86400000).toISOString().slice(0, 10);
    return entries
      .filter((e) => histVeh === 'all' || e.fahrzeugId === histVeh)
      .filter((e) => !cutoff || e.datum >= cutoff)
      .sort((a, b) => (a.datum > b.datum ? -1 : a.datum < b.datum ? 1 : b.kilometerstand - a.kilometerstand))
      .map((e) => {
        const v = veh(e.fahrzeugId);
        const avg = v?.durchschnittsVerbrauch ?? null;
        let verbColor = 'var(--text)';
        if (e.verbrauch != null && avg) {
          if (e.verbrauch <= avg * 0.97) verbColor = 'var(--good)';
          else if (e.verbrauch >= avg * 1.06) verbColor = 'var(--bad)';
        }
        const meta = [e.tankstelle, e.volltankung ? 'Volltankung' : 'Teilbetankung', e.notiz].filter(Boolean).join(' · ');
        return { e, dot: v?.farbe ?? 'var(--text3)', vehName: v?.name ?? '?', verbColor, meta };
      });
  }, [vehicles, entries, histVeh, histRange]);

  return (
    <div style={{ animation: 'tb-in .25s ease', display: 'flex', flexDirection: 'column', gap: 14 }}>
      <h1 style={h1}>Verlauf</h1>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        <VehChips sel={histVeh} onSelect={(id) => s.setSel('histVeh', id)} />
        <RangeChips sel={histRange} onSelect={(v) => s.setSel('histRange', v)} />
      </div>

      {rows.length === 0 && (
        <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 18, boxShadow: 'var(--shadow)', padding: '40px 24px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, textAlign: 'center' }}>
          <div style={{ fontSize: 15, fontWeight: 700 }}>Keine Tankvorgänge gefunden</div>
          <div style={{ fontSize: 13.5, color: 'var(--text2)' }}>Passe die Filter an oder erfasse einen neuen Tankvorgang.</div>
        </div>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {rows.map(({ e, dot, vehName, verbColor, meta }) => (
          <div key={e.id} style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 14, boxShadow: 'var(--shadow)', padding: '13px 16px', display: 'flex', flexDirection: 'column', gap: 7 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 9, flexWrap: 'wrap' }}>
              <span style={{ width: 10, height: 10, borderRadius: '50%', background: dot, flexShrink: 0, display: 'inline-block' }} />
              <span style={{ fontWeight: 700, fontSize: 14.5, fontVariantNumeric: 'tabular-nums' }}>{fmtDate(e.datum)}</span>
              <span style={{ color: 'var(--text3)', fontSize: 13 }}>{vehName}</span>
              <span style={{ flex: 1 }} />
              {e.verbrauch != null && (
                <span style={{ fontSize: 12.5, fontWeight: 700, color: verbColor, background: 'var(--card2)', padding: '3px 10px', borderRadius: 999, fontVariantNumeric: 'tabular-nums', whiteSpace: 'nowrap' }}>{fmt(e.verbrauch, 1)} l/100 km</span>
              )}
              {delId !== e.id ? (
                <div style={{ display: 'flex', gap: 2 }}>
                  <button className="tb-icon" onClick={() => s.openEdit(e)} aria-label="Bearbeiten" title="Bearbeiten" style={iconBtn}><IconEdit size={15} /></button>
                  <button className="tb-icon-danger" onClick={() => s.askDel(e.id)} aria-label="Löschen" title="Löschen" style={iconBtn}><IconTrash size={15} /></button>
                </div>
              ) : (
                <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                  <span style={{ fontSize: 12.5, fontWeight: 600, color: 'var(--text2)' }}>Löschen?</span>
                  <button onClick={() => s.deleteEntry(e.id)} style={{ border: 'none', borderRadius: 8, background: 'var(--bad)', color: '#FFFFFF', fontSize: 12.5, fontWeight: 700, padding: '6px 12px', cursor: 'pointer', fontFamily: 'inherit' }}>Ja</button>
                  <button onClick={s.cancelDel} style={{ border: '1px solid var(--border)', borderRadius: 8, background: 'var(--card)', color: 'var(--text2)', fontSize: 12.5, fontWeight: 600, padding: '6px 12px', cursor: 'pointer', fontFamily: 'inherit' }}>Nein</button>
                </div>
              )}
            </div>
            <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', fontSize: 13.5, color: 'var(--text2)', fontVariantNumeric: 'tabular-nums' }}>
              <span><b style={{ color: 'var(--text)', fontWeight: 600 }}>{fmt(e.liter, 2)}</b> l</span>
              <span><b style={{ color: 'var(--text)', fontWeight: 600 }}>{fmt(e.gesamtpreis, 2)}</b> €</span>
              <span><b style={{ color: 'var(--text)', fontWeight: 600 }}>{fmt(e.preisProLiter, 3)}</b> €/l</span>
              <span><b style={{ color: 'var(--text)', fontWeight: 600 }}>{fmt(e.kilometerstand, 0)}</b> km</span>
              {meta && <span style={{ color: 'var(--text3)' }}>{meta}</span>}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

const iconBtn: React.CSSProperties = {
  width: 30, height: 30, border: 'none', borderRadius: 8, background: 'none', color: 'var(--text3)',
  cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
};
