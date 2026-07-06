import { useEffect, type ReactNode } from 'react';
import { useStore } from './store';
import { navSt } from './ui/styles';
import { Logo } from './ui/Logo';
import { IconHome, IconClock, IconBars, IconCar, IconSettings, IconPlus, IconLogout, IconChevronLeft, IconDots } from './ui/icons';
import { Login } from './screens/Login';
import { Dashboard } from './screens/Dashboard';
import { Erfassen } from './screens/Erfassen';
import { Verlauf } from './screens/Verlauf';
import { Statistik } from './screens/Statistik';
import { Fahrzeuge } from './screens/Fahrzeuge';
import { Mehr } from './screens/Mehr';
import { Einstellungen } from './screens/Einstellungen';
import { EditModal } from './modals/EditModal';
import { VehicleModal } from './modals/VehicleModal';
import type { Screen } from './lib/types';

const ACCENT = '#F59E0B';
const TITLES: Record<string, string> = {
  dashboard: 'Tankbuch', erfassen: 'Erfassen', verlauf: 'Verlauf', statistik: 'Statistiken',
  fahrzeuge: 'Fahrzeuge', einstellungen: 'Einstellungen', mehr: 'Mehr',
};

function NavItem({ active, onClick, icon, label }: { active: boolean; onClick: () => void; icon: ReactNode; label: string }) {
  const st = navSt(active);
  return (
    <button className="tb-nav" onClick={onClick} style={{ display: 'flex', alignItems: 'center', gap: 11, padding: '10px 12px', borderRadius: 10, border: 'none', cursor: 'pointer', fontFamily: 'inherit', fontSize: 14, fontWeight: 600, textAlign: 'left', color: st.c, background: st.bg }}>
      {icon}{label}
    </button>
  );
}

function TabItem({ active, onClick, icon, label }: { active: boolean; onClick: () => void; icon: ReactNode; label: string }) {
  return (
    <button onClick={onClick} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 3, background: 'none', border: 'none', cursor: 'pointer', color: active ? 'var(--accent-text)' : 'var(--text3)', fontSize: 10.5, fontWeight: 600, padding: '6px 0', fontFamily: 'inherit' }}>
      {icon}{label}
    </button>
  );
}

function ScreenView({ name }: { name: Screen }) {
  switch (name) {
    case 'dashboard': return <Dashboard />;
    case 'erfassen': return <Erfassen />;
    case 'verlauf': return <Verlauf />;
    case 'statistik': return <Statistik />;
    case 'fahrzeuge': return <Fahrzeuge />;
    case 'einstellungen': return <Einstellungen />;
    case 'mehr': return <Mehr />;
  }
}

export function App() {
  const { ready, authStep, theme, systemDark, winW, screen, email, toastMsg } = useStore();
  const s = useStore();

  useEffect(() => { s.init(); /* eslint-disable-next-line */ }, []);
  useEffect(() => {
    const mm = matchMedia('(prefers-color-scheme: dark)');
    const onMM = () => s.setSystemDark(mm.matches);
    const onResize = () => s.setWinW(window.innerWidth);
    mm.addEventListener('change', onMM);
    window.addEventListener('resize', onResize);
    return () => { mm.removeEventListener('change', onMM); window.removeEventListener('resize', onResize); };
    // eslint-disable-next-line
  }, []);

  const themeAttr = theme === 'system' ? (systemDark ? 'dark' : 'light') : theme;
  const isDesktop = winW >= 1024;
  const isMobile = !isDesktop;
  const scrName: Screen = (isDesktop && screen === 'mehr') ? 'einstellungen' : screen;
  const initial = (email[0] || 'D').toUpperCase();
  const loggedIn = authStep === 'in';

  const rootStyle = { '--accent': ACCENT } as React.CSSProperties;

  const toast = toastMsg ? (
    <div style={{ position: 'fixed', left: '50%', bottom: 96, transform: 'translateX(-50%)', background: 'var(--text)', color: 'var(--bg)', padding: '11px 18px', borderRadius: 12, fontSize: 13.5, fontWeight: 600, boxShadow: 'var(--shadow-lg)', zIndex: 70, animation: 'tb-fadeup .25s ease', maxWidth: 'min(90vw,480px)', textAlign: 'center' }}>{toastMsg}</div>
  ) : null;

  if (!ready) {
    return <div data-tb-root="1" data-theme={themeAttr} style={rootStyle}><div style={{ minHeight: '100vh', background: 'var(--bg)' }} /></div>;
  }

  if (!loggedIn) {
    return (
      <div data-tb-root="1" data-theme={themeAttr} style={rootStyle}>
        <div style={{ minHeight: '100vh', background: 'var(--bg)', color: 'var(--text)' }}>
          <Login />
          {toast}
        </div>
      </div>
    );
  }

  const showBack = isMobile && (scrName === 'fahrzeuge' || scrName === 'einstellungen');

  return (
    <div data-tb-root="1" data-theme={themeAttr} style={rootStyle}>
      <div style={{ minHeight: '100vh', background: 'var(--bg)', color: 'var(--text)' }}>
        <div style={{ display: 'flex', minHeight: '100vh', alignItems: 'stretch' }}>
          {isDesktop && (
            <div style={{ width: 238, flexShrink: 0, background: 'var(--card)', borderRight: '1px solid var(--border)', position: 'sticky', top: 0, height: '100vh', boxSizing: 'border-box', display: 'flex', flexDirection: 'column', padding: '22px 14px 18px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '0 8px' }}>
                <Logo size={34} />
                <div>
                  <div style={{ fontSize: 18, fontWeight: 700, letterSpacing: '-0.01em', lineHeight: 1.2 }}>Tankbuch</div>
                  <div style={{ fontSize: 11, color: 'var(--text3)' }}>Fahrtenbuch fürs Tanken</div>
                </div>
              </div>
              <button className="tb-accent" onClick={() => s.go('erfassen')} style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8, margin: '18px 4px 14px', padding: 11, border: 'none', borderRadius: 11, background: 'var(--accent)', color: 'var(--accent-ink)', fontFamily: 'inherit', fontSize: 14, fontWeight: 700, cursor: 'pointer', boxShadow: 'var(--shadow)' }}>
                <IconPlus size={16} /> Tankvorgang erfassen
              </button>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                <NavItem active={scrName === 'dashboard'} onClick={() => s.go('dashboard')} icon={<IconHome />} label="Dashboard" />
                <NavItem active={scrName === 'verlauf'} onClick={() => s.go('verlauf')} icon={<IconClock />} label="Verlauf" />
                <NavItem active={scrName === 'statistik'} onClick={() => s.go('statistik')} icon={<IconBars />} label="Statistiken" />
                <NavItem active={scrName === 'fahrzeuge'} onClick={() => s.go('fahrzeuge')} icon={<IconCar />} label="Fahrzeuge" />
                <NavItem active={scrName === 'einstellungen'} onClick={() => s.go('einstellungen')} icon={<IconSettings />} label="Einstellungen" />
              </div>
              <div style={{ flex: 1 }} />
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 8px', borderTop: '1px solid var(--border)' }}>
                <div style={{ width: 32, height: 32, borderRadius: '50%', background: 'color-mix(in srgb, var(--accent) 18%, var(--card))', color: 'var(--accent-text)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 13, flexShrink: 0 }}>{initial}</div>
                <div style={{ flex: 1, minWidth: 0, fontSize: 12.5, color: 'var(--text2)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{email}</div>
                <button className="tb-icon" onClick={s.logout} aria-label="Abmelden" title="Abmelden" style={{ width: 30, height: 30, border: 'none', borderRadius: 8, background: 'none', color: 'var(--text3)', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center' }}><IconLogout size={17} /></button>
              </div>
            </div>
          )}

          <div style={{ flex: 1, minWidth: 0 }}>
            {isMobile && (
              <div style={{ position: 'sticky', top: 0, zIndex: 30, background: 'var(--card)', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 10, padding: '11px 16px' }}>
                {showBack ? (
                  <button onClick={() => s.go('mehr')} aria-label="Zurück" style={{ width: 32, height: 32, border: 'none', borderRadius: 8, background: 'none', color: 'var(--text2)', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', marginLeft: -8 }}><IconChevronLeft size={20} /></button>
                ) : <Logo size={26} />}
                <div style={{ fontSize: 17, fontWeight: 700, letterSpacing: '-0.01em' }}>{TITLES[scrName]}</div>
                <div style={{ flex: 1 }} />
                <button onClick={() => s.go('mehr')} aria-label="Konto" style={{ width: 33, height: 33, borderRadius: '50%', background: 'color-mix(in srgb, var(--accent) 18%, var(--card))', color: 'var(--accent-text)', fontWeight: 700, border: 'none', fontSize: 13.5, cursor: 'pointer', fontFamily: 'inherit' }}>{initial}</button>
              </div>
            )}

            <div style={{ maxWidth: 1060, margin: '0 auto', boxSizing: 'border-box', width: '100%', padding: `clamp(16px,3vw,32px) clamp(14px,3vw,32px) ${isMobile ? '110px' : '48px'} clamp(14px,3vw,32px)` }}>
              <ScreenView name={scrName} />
            </div>
          </div>
        </div>

        {isMobile && (
          <div style={{ position: 'fixed', left: 0, right: 0, bottom: 0, zIndex: 40, background: 'var(--card)', borderTop: '1px solid var(--border)', display: 'grid', gridTemplateColumns: '1fr 1fr 84px 1fr 1fr', alignItems: 'center', padding: '5px 6px calc(5px + env(safe-area-inset-bottom))' }}>
            <TabItem active={scrName === 'dashboard'} onClick={() => s.go('dashboard')} icon={<IconHome size={21} sw={1.9} />} label="Start" />
            <TabItem active={scrName === 'verlauf'} onClick={() => s.go('verlauf')} icon={<IconClock size={21} sw={1.9} />} label="Verlauf" />
            <div style={{ display: 'flex', justifyContent: 'center' }}>
              <button onClick={() => s.go('erfassen')} aria-label="Tankvorgang erfassen" style={{ width: 56, height: 56, borderRadius: '50%', background: 'var(--accent)', color: 'var(--accent-ink)', border: 'none', boxShadow: '0 6px 18px color-mix(in srgb, var(--accent) 45%, transparent)', cursor: 'pointer', marginTop: -26, display: 'flex', alignItems: 'center', justifyContent: 'center' }}><IconPlus size={24} /></button>
            </div>
            <TabItem active={scrName === 'statistik'} onClick={() => s.go('statistik')} icon={<IconBars size={21} sw={1.9} />} label="Statistik" />
            <TabItem active={['mehr', 'fahrzeuge', 'einstellungen'].includes(scrName)} onClick={() => s.go('mehr')} icon={<IconDots size={21} />} label="Mehr" />
          </div>
        )}

        <EditModal />
        <VehicleModal />
        {toast}
      </div>
    </div>
  );
}
