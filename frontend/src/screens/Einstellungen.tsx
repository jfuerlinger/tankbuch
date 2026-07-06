import { useStore } from '../store';
import type { Theme } from '../lib/types';
import { IconDownload, IconUpload, IconLogout } from '../ui/icons';
import { card, h1 } from '../ui/styles';

export function Einstellungen() {
  const { theme, email, tenantName } = useStore();
  const setTheme = useStore((s) => s.setTheme);
  const exportCsv = useStore((s) => s.exportCsv);
  const importCsv = useStore((s) => s.importCsv);
  const logout = useStore((s) => s.logout);

  const themes: [Theme, string][] = [['system', 'System'], ['light', 'Hell'], ['dark', 'Dunkel']];

  return (
    <div style={{ animation: 'tb-in .25s ease', display: 'flex', flexDirection: 'column', gap: 14, maxWidth: 620 }}>
      <h1 style={h1}>Einstellungen</h1>

      <div style={{ ...card, padding: 17, display: 'flex', flexDirection: 'column', gap: 12 }}>
        <div style={{ fontSize: 14, fontWeight: 700 }}>Darstellung</div>
        <div style={{ display: 'flex', background: 'var(--card2)', borderRadius: 11, padding: 3, gap: 3, maxWidth: 340 }}>
          {themes.map(([k, label]) => {
            const active = theme === k;
            return (
              <button key={k} onClick={() => setTheme(k)} style={{ flex: 1, padding: '8px 10px', border: 'none', borderRadius: 9, fontFamily: 'inherit', fontSize: 13, fontWeight: 600, cursor: 'pointer', background: active ? 'var(--card)' : 'transparent', color: active ? 'var(--text)' : 'var(--text2)', boxShadow: active ? 'var(--shadow)' : 'none', transition: 'background .15s' }}>{label}</button>
            );
          })}
        </div>
        <div style={{ fontSize: 12.5, color: 'var(--text3)' }}>„System“ folgt automatisch dem Hell-/Dunkelmodus deines Geräts.</div>
      </div>

      <div style={{ ...card, padding: 17, display: 'flex', flexDirection: 'column', gap: 12 }}>
        <div style={{ fontSize: 14, fontWeight: 700 }}>Daten</div>
        <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
          <button className="tb-accent" onClick={exportCsv} style={{ display: 'flex', alignItems: 'center', gap: 8, background: 'var(--accent)', color: 'var(--accent-ink)', border: 'none', borderRadius: 11, padding: '11px 16px', fontSize: 13.5, fontWeight: 700, cursor: 'pointer', fontFamily: 'inherit' }}>
            <IconDownload size={15} /> CSV exportieren
          </button>
          <label className="tb-soft" style={{ display: 'flex', alignItems: 'center', gap: 8, background: 'var(--card2)', border: '1px solid var(--border)', borderRadius: 11, padding: '11px 16px', fontSize: 13.5, fontWeight: 600, cursor: 'pointer', boxSizing: 'border-box' }}>
            <input type="file" accept=".csv,text/csv" onChange={(e) => { const f = e.target.files?.[0]; if (f) importCsv(f); e.target.value = ''; }} style={{ display: 'none' }} />
            <IconUpload size={15} /> CSV importieren
          </label>
        </div>
        <div style={{ fontSize: 12.5, color: 'var(--text3)' }}>Semikolon-getrennt, Dezimal-Komma, UTF-8. Der Export dient zugleich als Backup – der Import stellt Daten wieder her bzw. führt sie zusammen.</div>
      </div>

      <div style={{ ...card, padding: 17, display: 'flex', flexDirection: 'column', gap: 11 }}>
        <div style={{ fontSize: 14, fontWeight: 700 }}>Konto</div>
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, fontSize: 13.5 }}>
          <span style={{ color: 'var(--text2)' }}>Angemeldet als</span>
          <b style={{ fontWeight: 600, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{email}</b>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, fontSize: 13.5, borderBottom: '1px solid var(--border)', paddingBottom: 11 }}>
          <span style={{ color: 'var(--text2)' }}>Mandant</span><b style={{ fontWeight: 600 }}>{tenantName}</b>
        </div>
        <button className="tb-danger-outline" onClick={logout} style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 9, background: 'none', border: '1px solid var(--border)', borderRadius: 11, padding: 11, fontFamily: 'inherit', fontSize: 13.5, fontWeight: 600, color: 'var(--bad)', cursor: 'pointer' }}>
          <IconLogout size={15} /> Abmelden
        </button>
      </div>
    </div>
  );
}
