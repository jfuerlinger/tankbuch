import { useStore } from '../store';
import { IconCar, IconSettings, IconChevronRight, IconLogout } from '../ui/icons';

export function Mehr() {
  const { email, tenantName } = useStore();
  const go = useStore((s) => s.go);
  const logout = useStore((s) => s.logout);
  const initial = (email[0] || 'D').toUpperCase();

  const rowStyle: React.CSSProperties = {
    width: '100%', display: 'flex', alignItems: 'center', gap: 12, padding: '15px 16px', border: 'none',
    background: 'none', cursor: 'pointer', fontFamily: 'inherit', fontSize: 14.5, fontWeight: 600, color: 'var(--text)', textAlign: 'left',
  };

  return (
    <div style={{ animation: 'tb-in .25s ease', display: 'flex', flexDirection: 'column', gap: 14, maxWidth: 560 }}>
      <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 16, boxShadow: 'var(--shadow)', padding: 16, display: 'flex', alignItems: 'center', gap: 13 }}>
        <div style={{ width: 44, height: 44, borderRadius: '50%', background: 'color-mix(in srgb, var(--accent) 18%, var(--card))', color: 'var(--accent-text)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 17, flexShrink: 0 }}>{initial}</div>
        <div style={{ minWidth: 0 }}>
          <div style={{ fontWeight: 700, fontSize: 14.5, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{email}</div>
          <div style={{ fontSize: 12.5, color: 'var(--text3)' }}>Mandant: {tenantName}</div>
        </div>
      </div>
      <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 16, boxShadow: 'var(--shadow)', overflow: 'hidden' }}>
        <button className="tb-row" onClick={() => go('fahrzeuge')} style={{ ...rowStyle, borderBottom: '1px solid var(--border)' }}>
          <span style={{ color: 'var(--text2)' }}><IconCar size={19} /></span>
          <span style={{ flex: 1 }}>Fahrzeuge</span>
          <span style={{ color: 'var(--text3)' }}><IconChevronRight size={17} /></span>
        </button>
        <button className="tb-row" onClick={() => go('einstellungen')} style={rowStyle}>
          <span style={{ color: 'var(--text2)' }}><IconSettings size={19} /></span>
          <span style={{ flex: 1 }}>Einstellungen</span>
          <span style={{ color: 'var(--text3)' }}><IconChevronRight size={17} /></span>
        </button>
      </div>
      <button onClick={logout} style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 9, background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 14, boxShadow: 'var(--shadow)', padding: 14, fontFamily: 'inherit', fontSize: 14, fontWeight: 600, color: 'var(--bad)', cursor: 'pointer' }}>
        <IconLogout size={16} /> Abmelden
      </button>
    </div>
  );
}
