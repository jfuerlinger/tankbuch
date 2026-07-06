import { useStore } from '../store';
import { fmt } from '../lib/format';
import { IconPlus, IconCar, IconEdit } from '../ui/icons';
import { accentBtn, h1 } from '../ui/styles';

export function Fahrzeuge() {
  const vehicles = useStore((s) => s.vehicles);
  const openVehModal = useStore((s) => s.openVehModal);

  const miniStat = (label: string, value: string) => (
    <div style={{ background: 'var(--card2)', borderRadius: 10, padding: '9px 12px' }}>
      <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--text3)' }}>{label}</div>
      <div style={{ fontSize: 14.5, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>{value}</div>
    </div>
  );

  return (
    <div style={{ animation: 'tb-in .25s ease', display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
        <h1 style={h1}>Fahrzeuge</h1>
        <button className="tb-accent" onClick={() => openVehModal(null)} style={{ ...accentBtn, borderRadius: 11, padding: '10px 16px', fontSize: 14 }}>
          <IconPlus size={15} /> Fahrzeug anlegen
        </button>
      </div>

      {vehicles.length === 0 && (
        <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 18, boxShadow: 'var(--shadow)', padding: '40px 24px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, textAlign: 'center' }}>
          <div style={{ fontSize: 15, fontWeight: 700 }}>Noch keine Fahrzeuge</div>
          <div style={{ fontSize: 13.5, color: 'var(--text2)' }}>Lege dein erstes Fahrzeug an, um Tankvorgänge zu erfassen.</div>
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit,minmax(min(100%,290px),1fr))', gap: 14 }}>
        {vehicles.map((v) => (
          <div key={v.id} style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 16, boxShadow: 'var(--shadow)', padding: 17, display: 'flex', flexDirection: 'column', gap: 13 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <div style={{ width: 44, height: 44, borderRadius: 12, background: `color-mix(in srgb, ${v.farbe} 16%, var(--card))`, color: v.farbe, display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                <IconCar size={24} />
              </div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontWeight: 700, fontSize: 15.5, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{v.name}</div>
                <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginTop: 3, flexWrap: 'wrap' }}>
                  <span style={{ border: '1.5px solid var(--text3)', color: 'var(--text2)', borderRadius: 5, padding: '0 7px', fontWeight: 700, fontSize: 11.5, letterSpacing: '.07em' }}>{v.kennzeichen || '—'}</span>
                  <span style={{ fontSize: 12, color: 'var(--text3)' }}>{v.kraftstoffart}</span>
                </div>
              </div>
              <button className="tb-icon" onClick={() => openVehModal(v.id)} aria-label="Fahrzeug bearbeiten" style={{ width: 32, height: 32, border: 'none', borderRadius: 8, background: 'none', color: 'var(--text3)', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <IconEdit size={16} />
              </button>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 9 }}>
              {miniStat('Kilometerstand', fmt(v.aktuellerKilometerstand, 0) + ' km')}
              {miniStat('Ø Verbrauch', v.durchschnittsVerbrauch != null ? fmt(v.durchschnittsVerbrauch, 1) + ' l/100 km' : '—')}
              {miniStat('Gesamtkosten', fmt(v.gesamtkosten, 2) + ' €')}
              {miniStat('Tankvorgänge', String(v.anzahlTankvorgaenge))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
