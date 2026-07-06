import { useRef } from 'react';
import { useStore } from '../store';
import { Logo } from '../ui/Logo';
import { inputStyle } from '../ui/styles';

export function Login() {
  const { authStep, emailInput, code, tenantName } = useStore();
  const setEmailInput = useStore((s) => s.setEmailInput);
  const sendCode = useStore((s) => s.sendCode);
  const setCode = useStore((s) => s.setCode);
  const doLogin = useStore((s) => s.doLogin);
  const backToEmail = useStore((s) => s.backToEmail);
  const resendHint = useStore((s) => s.resendHint);
  void tenantName;

  const refs = useRef<(HTMLInputElement | null)[]>([]);

  const onDigit = (i: number, val: string) => {
    const d = val.replace(/\D/g, '').slice(-1);
    const next = [...code];
    next[i] = d;
    setCode(next);
    if (d && i < 5) refs.current[i + 1]?.focus();
    if (next.every((x) => x)) setTimeout(doLogin, 220);
  };
  const onKey = (i: number, e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Backspace' && !code[i] && i > 0) refs.current[i - 1]?.focus();
    if (e.key === 'Enter') doLogin();
  };
  const onPaste = (e: React.ClipboardEvent) => {
    const txt = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);
    if (!txt) return;
    e.preventDefault();
    const next = ['', '', '', '', '', ''];
    for (let i = 0; i < txt.length; i++) next[i] = txt[i];
    setCode(next);
    refs.current[Math.min(txt.length, 5)]?.focus();
    if (next.every((x) => x)) setTimeout(doLogin, 220);
  };

  const complete = code.every((x) => x);

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: 24, boxSizing: 'border-box' }}>
      <div style={{ width: '100%', maxWidth: 400, background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 20, boxShadow: 'var(--shadow)', padding: '32px 28px', boxSizing: 'border-box', animation: 'tb-in .3s ease' }}>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8, marginBottom: 26 }}>
          <Logo size={60} />
          <div style={{ fontSize: 25, fontWeight: 700, letterSpacing: '-0.01em' }}>Tankbuch</div>
          <div style={{ fontSize: 14, color: 'var(--text2)', textAlign: 'center' }}>Das digitale Fahrtenbuch fürs Tanken</div>
        </div>

        {authStep === 'email' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div>
              <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--text2)', marginBottom: 6 }}>E-Mail-Adresse</label>
              <input className="tb-input" type="email" value={emailInput}
                onChange={(e) => setEmailInput(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') sendCode(); }}
                placeholder="name@beispiel.at" style={{ ...inputStyle, padding: '12px 13px', borderRadius: 11 }} />
            </div>
            <button className="tb-accent" onClick={sendCode} style={{ width: '100%', padding: 13, border: 'none', borderRadius: 12, background: 'var(--accent)', color: 'var(--accent-ink)', fontFamily: 'inherit', fontSize: 15, fontWeight: 700, cursor: 'pointer' }}>Code senden</button>
          </div>
        )}

        {authStep === 'code' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
            <div style={{ fontSize: 13.5, color: 'var(--text2)', textAlign: 'center' }}>
              Wir haben einen 6-stelligen Code an<br /><b style={{ color: 'var(--text)' }}>{emailInput.trim() || '—'}</b> gesendet.
            </div>
            <div style={{ display: 'flex', gap: 8, justifyContent: 'center' }}>
              {code.map((val, i) => (
                <input key={i} className="tb-input" value={val} ref={(el) => { refs.current[i] = el; }}
                  onChange={(e) => onDigit(i, e.target.value)} onKeyDown={(e) => onKey(i, e)} onPaste={onPaste}
                  inputMode="numeric" maxLength={2} aria-label="Code-Ziffer"
                  style={{ width: 44, height: 54, textAlign: 'center', fontSize: 22, fontWeight: 700, borderRadius: 11, border: '1.5px solid var(--border)', background: 'var(--bg)', color: 'var(--text)', fontFamily: 'inherit', outline: 'none', boxSizing: 'border-box', fontVariantNumeric: 'tabular-nums' }} />
              ))}
            </div>
            <button onClick={doLogin} style={{ width: '100%', padding: 13, border: 'none', borderRadius: 12, background: complete ? 'var(--accent)' : 'var(--card2)', color: complete ? 'var(--accent-ink)' : 'var(--text3)', fontFamily: 'inherit', fontSize: 15, fontWeight: 700, cursor: 'pointer', transition: 'background .2s' }}>Anmelden</button>
            <div style={{ display: 'flex', justifyContent: 'center', gap: 18, fontSize: 13 }}>
              <button onClick={backToEmail} style={{ background: 'none', border: 'none', color: 'var(--text2)', fontFamily: 'inherit', fontSize: 13, fontWeight: 600, cursor: 'pointer', padding: 2 }}>Zurück</button>
              <button onClick={resendHint} style={{ background: 'none', border: 'none', color: 'var(--accent-text)', fontFamily: 'inherit', fontSize: 13, fontWeight: 600, cursor: 'pointer', padding: 2 }}>Code erneut senden</button>
            </div>
          </div>
        )}
      </div>
      <div style={{ marginTop: 16, fontSize: 12.5, color: 'var(--text3)', textAlign: 'center', maxWidth: 380 }}>
        Prototyp: Es wird keine echte E-Mail versendet.<br />Der Demo-Code erscheint nach „Code senden“.
      </div>
    </div>
  );
}
