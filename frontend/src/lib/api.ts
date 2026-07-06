import type {
  Fahrzeug, Tankvorgang, VerifyCodeResponse, RequestCodeResponse,
  PumpOcrResult, TachoOcrResult, CsvImportResult,
} from './types';

// Relative Pfade: im Dev-Server leitet die Vite-Proxy-Konfiguration /api/* an das Backend
// weiter (vite.config.ts), im Docker-Compose-Deployment übernimmt das YARP der
// PublishAsStaticWebsite-Ressource dieselbe Weiterleitung (siehe AppHost.cs).
const BASE = '';

let token: string | null = localStorage.getItem('tb_token');
export const getToken = () => token;
export function setToken(t: string | null) {
  token = t;
  if (t) localStorage.setItem('tb_token', t);
  else localStorage.removeItem('tb_token');
}

export class ApiError extends Error {
  status: number;
  constructor(message: string, status: number) { super(message); this.status = status; }
}

async function req<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {};
  if (token) headers['Authorization'] = 'Bearer ' + token;
  let payload: BodyInit | undefined;
  if (body instanceof FormData) payload = body;
  else if (body !== undefined) { headers['Content-Type'] = 'application/json'; payload = JSON.stringify(body); }

  const res = await fetch(BASE + path, { method, headers, body: payload });
  if (!res.ok) {
    let msg = res.statusText || 'Fehler';
    try {
      const j = await res.json();
      if (j.errors) msg = (Object.values(j.errors) as string[][]).flat().join(', ');
      else msg = j.message || j.title || msg;
    } catch { /* keine JSON-Antwort */ }
    throw new ApiError(msg, res.status);
  }
  if (res.status === 204) return undefined as T;
  const ct = res.headers.get('content-type') || '';
  return ct.includes('application/json') ? res.json() : (await res.text()) as unknown as T;
}

export const api = {
  auth: {
    requestCode: (email: string) => req<RequestCodeResponse>('POST', '/api/auth/request-code', { email }),
    verify: (email: string, code: string) => req<VerifyCodeResponse>('POST', '/api/auth/verify', { email, code }),
    me: () => req<{ email: string; tenantId: string; tenantName: string }>('GET', '/api/auth/me'),
  },
  fahrzeuge: {
    list: () => req<Fahrzeug[]>('GET', '/api/fahrzeuge'),
    create: (b: { name: string; kennzeichen: string; kraftstoffart: string; farbe: string; anfangsKilometer: number }) =>
      req<Fahrzeug>('POST', '/api/fahrzeuge', b),
    update: (id: string, b: { name: string; kennzeichen: string; kraftstoffart: string; farbe: string; anfangsKilometer: number }) =>
      req<Fahrzeug>('PUT', `/api/fahrzeuge/${id}`, b),
    remove: (id: string) => req<void>('DELETE', `/api/fahrzeuge/${id}`),
  },
  tankvorgaenge: {
    list: (p?: { fahrzeugId?: string; tage?: number }) => {
      const q = new URLSearchParams();
      if (p?.fahrzeugId) q.set('fahrzeugId', p.fahrzeugId);
      if (p?.tage) q.set('tage', String(p.tage));
      const qs = q.toString();
      return req<Tankvorgang[]>('GET', '/api/tankvorgaenge' + (qs ? '?' + qs : ''));
    },
    create: (b: Record<string, unknown>) => req<Tankvorgang>('POST', '/api/tankvorgaenge', b),
    update: (id: string, b: Record<string, unknown>) => req<Tankvorgang>('PUT', `/api/tankvorgaenge/${id}`, b),
    remove: (id: string) => req<void>('DELETE', `/api/tankvorgaenge/${id}`),
  },
  ocr: {
    pump: (file: Blob) => { const f = new FormData(); f.append('image', file); return req<PumpOcrResult>('POST', '/api/ocr/pump', f); },
    tacho: (file: Blob) => { const f = new FormData(); f.append('image', file); return req<TachoOcrResult>('POST', '/api/ocr/tacho', f); },
  },
  csv: {
    async export(): Promise<{ blob: Blob; filename: string }> {
      const headers: Record<string, string> = {};
      if (token) headers['Authorization'] = 'Bearer ' + token;
      const res = await fetch(BASE + '/api/csv/export', { headers });
      if (!res.ok) throw new ApiError('Export fehlgeschlagen', res.status);
      const cd = res.headers.get('content-disposition') || '';
      const m = cd.match(/filename="?([^"]+)"?/);
      return { blob: await res.blob(), filename: m ? m[1] : 'tankbuch-export.csv' };
    },
    import: (file: Blob) => { const f = new FormData(); f.append('file', file); return req<CsvImportResult>('POST', '/api/csv/import', f); },
  },
};
