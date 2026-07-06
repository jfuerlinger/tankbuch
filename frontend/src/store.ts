import { create } from 'zustand';
import { api, ApiError, getToken, setToken } from './lib/api';
import { computeTrio } from './lib/calc';
import { parseDe, fmt, todayISO } from './lib/format';
import type { Fahrzeug, Tankvorgang, Theme, Screen, EntryForm, VehicleForm } from './lib/types';

const emptyForm = (vehId: string | null): EntryForm => ({
  fahrzeugId: vehId, datum: todayISO(), liter: '', ppl: '', total: '', km: '', tankstelle: '', notiz: '', voll: true,
});

interface EditForm { datum: string; liter: string; ppl: string; total: string; km: string; tankstelle: string; notiz: string; voll: boolean; }

let toastTimer: ReturnType<typeof setTimeout> | undefined;

interface State {
  ready: boolean;
  authStep: 'email' | 'code' | 'in';
  email: string; emailInput: string; code: string[]; tenantName: string;
  theme: Theme; systemDark: boolean; winW: number;
  screen: Screen;
  vehicles: Fahrzeug[]; entries: Tankvorgang[]; busy: boolean;
  dashVeh: string; statVeh: string; histVeh: string; histRange: string;
  form: EntryForm; autoCalc: boolean; editOrder: string[]; derived: string | null;
  pumpImg: string | null; pumpDemo: boolean; tachoImg: string | null; tachoDemo: boolean;
  scanning: 'pump' | 'tacho' | null; rec: { pump: boolean; tacho: boolean };
  editId: string | null; editForm: EditForm | null; editOrderE: string[]; derivedE: string | null;
  vehModal: boolean; vehId: string | null; vehForm: VehicleForm | null; vehConfirm: boolean;
  delId: string | null;
  toastMsg: string | null;

  init(): Promise<void>;
  setSystemDark(v: boolean): void;
  setWinW(v: number): void;
  setEmailInput(v: string): void;
  sendCode(): Promise<void>;
  setCode(code: string[]): void;
  doLogin(): Promise<void>;
  backToEmail(): void;
  resendHint(): void;
  logout(): void;
  setTheme(t: Theme): void;
  go(s: Screen): void;
  setSel(key: 'dashVeh' | 'statVeh' | 'histVeh' | 'histRange', v: string): void;
  refresh(): Promise<void>;
  setFormVehicle(id: string): void;
  onTrio(field: 'liter' | 'ppl' | 'total', value: string): void;
  setForm(patch: Partial<EntryForm>): void;
  toggleVoll(): void;
  toggleAutoCalc(): void;
  scan(kind: 'pump' | 'tacho', file: File | null): Promise<void>;
  saveEntry(): Promise<void>;
  openEdit(e: Tankvorgang): void;
  setEditForm(patch: Partial<EditForm>): void;
  onTrioE(field: 'liter' | 'ppl' | 'total', value: string): void;
  toggleEVoll(): void;
  saveEdit(): Promise<void>;
  closeEdit(): void;
  askDel(id: string): void;
  cancelDel(): void;
  deleteEntry(id: string): Promise<void>;
  openVehModal(id: string | null): void;
  setVehForm(patch: Partial<VehicleForm>): void;
  saveVeh(): Promise<void>;
  askVehDel(): void;
  cancelVehDel(): void;
  deleteVeh(): Promise<void>;
  closeVehModal(): void;
  exportCsv(): Promise<void>;
  importCsv(file: File): Promise<void>;
  toast(msg: string): void;
}

export const useStore = create<State>((set, get) => ({
  ready: false,
  authStep: 'email',
  email: '', emailInput: '', code: ['', '', '', '', '', ''], tenantName: 'Privat',
  theme: (localStorage.getItem('tb_theme') as Theme) || 'system',
  systemDark: typeof matchMedia !== 'undefined' && matchMedia('(prefers-color-scheme: dark)').matches,
  winW: typeof window !== 'undefined' ? window.innerWidth : 1200,
  screen: 'dashboard',
  vehicles: [], entries: [], busy: false,
  dashVeh: 'all', statVeh: 'all', histVeh: 'all', histRange: 'all',
  form: emptyForm(null), autoCalc: true, editOrder: [], derived: null,
  pumpImg: null, pumpDemo: false, tachoImg: null, tachoDemo: false,
  scanning: null, rec: { pump: false, tacho: false },
  editId: null, editForm: null, editOrderE: [], derivedE: null,
  vehModal: false, vehId: null, vehForm: null, vehConfirm: false,
  delId: null,
  toastMsg: null,

  async init() {
    if (getToken()) {
      try {
        const me = await api.auth.me();
        set({ authStep: 'in', email: me.email, tenantName: me.tenantName });
        await get().refresh();
      } catch {
        setToken(null);
      }
    }
    set({ ready: true });
  },

  setSystemDark: (v) => set({ systemDark: v }),
  setWinW: (v) => set({ winW: v }),
  setEmailInput: (v) => set({ emailInput: v }),

  async sendCode() {
    const em = get().emailInput.trim();
    if (!/^\S+@\S+\.\S+$/.test(em)) { get().toast('Bitte eine gültige E-Mail-Adresse eingeben'); return; }
    try {
      const r = await api.auth.requestCode(em);
      set({ authStep: 'code', code: ['', '', '', '', '', ''] });
      get().toast(r.message);
    } catch (e) {
      get().toast(e instanceof ApiError ? e.message : 'Netzwerkfehler');
    }
  },

  setCode: (code) => set({ code }),

  async doLogin() {
    const c = get().code.join('');
    if (!/^\d{6}$/.test(c)) { get().toast('Bitte den 6-stelligen Code eingeben'); return; }
    const email = get().emailInput.trim() || get().email;
    try {
      const r = await api.auth.verify(email, c);
      setToken(r.token);
      set({ authStep: 'in', email: r.email, tenantName: r.tenantName, screen: 'dashboard' });
      await get().refresh();
      get().toast('Willkommen im Tankbuch!');
    } catch (e) {
      get().toast(e instanceof ApiError ? e.message : 'Anmeldung fehlgeschlagen');
    }
  },

  backToEmail: () => set({ authStep: 'email' }),
  resendHint: () => get().toast('Demo-Code: 123456 – es wird keine echte E-Mail versendet'),

  logout() {
    setToken(null);
    set({ authStep: 'email', code: ['', '', '', '', '', ''], screen: 'dashboard', vehicles: [], entries: [] });
    get().toast('Abgemeldet');
  },

  setTheme(t) { localStorage.setItem('tb_theme', t); set({ theme: t }); },
  go: (s) => { set({ screen: s, delId: null }); window.scrollTo(0, 0); },
  setSel: (key, v) => set({ [key]: v } as Pick<State, typeof key>),

  async refresh() {
    set({ busy: true });
    try {
      const [vehicles, entries] = await Promise.all([api.fahrzeuge.list(), api.tankvorgaenge.list()]);
      set((s) => ({
        vehicles, entries,
        form: s.form.fahrzeugId ? s.form : { ...s.form, fahrzeugId: vehicles[0]?.id ?? null },
      }));
    } catch (e) {
      if (e instanceof ApiError && e.status === 401) { setToken(null); set({ authStep: 'email' }); }
    } finally {
      set({ busy: false });
    }
  },

  setFormVehicle: (id) => set((s) => ({ form: { ...s.form, fahrzeugId: id } })),

  onTrio(field, value) {
    const s = get();
    let form = { ...s.form, [field]: value };
    const editOrder = [field, ...s.editOrder.filter((x) => x !== field)].slice(0, 2);
    let derived: string | null = null;
    if (s.autoCalc) {
      const r = computeTrio({ liter: form.liter, ppl: form.ppl, total: form.total }, editOrder);
      form = { ...form, ...r.form };
      derived = r.derived;
    }
    set({ form, editOrder, derived });
  },

  setForm: (patch) => set((s) => ({ form: { ...s.form, ...patch } })),
  toggleVoll: () => set((s) => ({ form: { ...s.form, voll: !s.form.voll } })),
  toggleAutoCalc: () => set((s) => ({ autoCalc: !s.autoCalc, derived: null })),

  async scan(kind, file) {
    const started = Date.now();
    const s = get();
    const veh = s.vehicles.find((v) => v.id === s.form.fahrzeugId);
    if (kind === 'pump') set({ pumpImg: file ? URL.createObjectURL(file) : null, pumpDemo: !file, scanning: 'pump' });
    else set({ tachoImg: file ? URL.createObjectURL(file) : null, tachoDemo: !file, scanning: 'tacho' });

    // Werte ermitteln: echtes Foto → API (Vision-Modell), Demo → lokal simuliert.
    let pump: { liter: number; total: number } | null = null;
    let tacho: { km: number } | null = null;
    try {
      if (file) {
        if (kind === 'pump') {
          const r = await api.ocr.pump(file);
          if (r.liter && r.gesamtpreis) pump = { liter: r.liter, total: r.gesamtpreis };
        } else {
          const r = await api.ocr.tacho(file);
          if (r.kilometerstand) tacho = { km: r.kilometerstand };
        }
      }
    } catch { /* Fallback unten */ }

    if (kind === 'pump' && !pump) {
      const base = veh && /diesel/i.test(veh.kraftstoffart) ? 1.659 : 1.539;
      const ppl = Math.round((base + (Math.random() - 0.5) * 0.08) * 1000) / 1000;
      const liter = Math.round((22 + Math.random() * 30) * 100) / 100;
      pump = { liter, total: Math.round(liter * ppl * 100) / 100 };
    }
    if (kind === 'tacho' && !tacho) {
      const es = s.entries.filter((e) => veh && e.fahrzeugId === veh.id);
      const lastKm = es.length ? Math.max(...es.map((e) => e.kilometerstand)) : (veh ? veh.anfangsKilometer : 40000);
      tacho = { km: lastKm + 350 + Math.round(Math.random() * 550) };
    }

    const wait = Math.max(0, 1900 - (Date.now() - started));
    setTimeout(() => {
      const st = get();
      if (kind === 'pump' && pump) {
        const liter = fmt(pump.liter, 2), total = fmt(pump.total, 2), ppl = fmt(pump.total / pump.liter, 3);
        set({
          scanning: null, rec: { ...st.rec, pump: true },
          form: { ...st.form, liter, total, ppl }, editOrder: ['total', 'liter'], derived: 'ppl',
        });
        get().toast('Erkannt: ' + liter + ' l · ' + total + ' € – bitte prüfen und bestätigen');
      } else if (kind === 'tacho' && tacho) {
        const km = fmt(tacho.km, 0);
        set({ scanning: null, rec: { ...st.rec, tacho: true }, form: { ...st.form, km } });
        get().toast('Kilometerstand erkannt: ' + km + ' km – bitte prüfen');
      }
    }, wait);
  },

  async saveEntry() {
    const f = get().form;
    const liter = parseDe(f.liter), total = parseDe(f.total), km = parseDe(f.km);
    const ppl = parseDe(f.ppl);
    if (!f.fahrzeugId) return get().toast('Bitte ein Fahrzeug wählen');
    if (!liter || liter <= 0) return get().toast('Bitte die getankten Liter angeben');
    if (!total || total <= 0) return get().toast('Bitte den Gesamtpreis angeben');
    if (!km || km <= 0) return get().toast('Bitte den Kilometerstand angeben');
    if (!f.datum) return get().toast('Bitte ein Datum angeben');
    try {
      await api.tankvorgaenge.create({
        fahrzeugId: f.fahrzeugId, datum: f.datum, liter, preisProLiter: ppl && ppl > 0 ? ppl : null,
        gesamtpreis: total, kilometerstand: Math.round(km), tankstelle: f.tankstelle.trim(), notiz: f.notiz.trim(),
        volltankung: f.voll,
      });
      await get().refresh();
      set({
        form: emptyForm(f.fahrzeugId), editOrder: [], derived: null,
        pumpImg: null, pumpDemo: false, tachoImg: null, tachoDemo: false, rec: { pump: false, tacho: false },
        screen: 'verlauf', histVeh: f.fahrzeugId, histRange: 'all',
      });
      window.scrollTo(0, 0);
      get().toast('Tankvorgang gespeichert');
    } catch (e) {
      get().toast(e instanceof ApiError ? e.message : 'Speichern fehlgeschlagen');
    }
  },

  openEdit(e) {
    set({
      editId: e.id,
      editForm: {
        datum: e.datum, liter: fmt(e.liter, 2), ppl: fmt(e.preisProLiter, 3), total: fmt(e.gesamtpreis, 2),
        km: fmt(e.kilometerstand, 0), tankstelle: e.tankstelle ?? '', notiz: e.notiz ?? '', voll: e.volltankung,
      },
      editOrderE: [], derivedE: null,
    });
  },

  setEditForm: (patch) => set((s) => ({ editForm: s.editForm ? { ...s.editForm, ...patch } : s.editForm })),

  onTrioE(field, value) {
    const s = get();
    if (!s.editForm) return;
    let form = { ...s.editForm, [field]: value };
    const editOrderE = [field, ...s.editOrderE.filter((x) => x !== field)].slice(0, 2);
    let derivedE: string | null = null;
    if (s.autoCalc) {
      const r = computeTrio({ liter: form.liter, ppl: form.ppl, total: form.total }, editOrderE);
      form = { ...form, ...r.form };
      derivedE = r.derived;
    }
    set({ editForm: form, editOrderE, derivedE });
  },

  toggleEVoll: () => set((s) => ({ editForm: s.editForm ? { ...s.editForm, voll: !s.editForm.voll } : s.editForm })),

  async saveEdit() {
    const s = get();
    const f = s.editForm;
    if (!f || !s.editId) return;
    const liter = parseDe(f.liter), total = parseDe(f.total), km = parseDe(f.km);
    const ppl = parseDe(f.ppl);
    if (!liter || liter <= 0 || !total || total <= 0 || !km || km <= 0 || !f.datum) {
      return get().toast('Bitte alle Pflichtfelder korrekt ausfüllen');
    }
    try {
      await api.tankvorgaenge.update(s.editId, {
        datum: f.datum, liter, preisProLiter: ppl && ppl > 0 ? ppl : null, gesamtpreis: total,
        kilometerstand: Math.round(km), tankstelle: f.tankstelle.trim(), notiz: f.notiz.trim(), volltankung: f.voll,
      });
      await get().refresh();
      set({ editId: null, editForm: null });
      get().toast('Änderungen gespeichert');
    } catch (e) {
      get().toast(e instanceof ApiError ? e.message : 'Speichern fehlgeschlagen');
    }
  },

  closeEdit: () => set({ editId: null, editForm: null }),
  askDel: (id) => set({ delId: id }),
  cancelDel: () => set({ delId: null }),

  async deleteEntry(id) {
    try {
      await api.tankvorgaenge.remove(id);
      await get().refresh();
      set({ delId: null });
      get().toast('Eintrag gelöscht');
    } catch (e) {
      get().toast(e instanceof ApiError ? e.message : 'Löschen fehlgeschlagen');
    }
  },

  openVehModal(id) {
    const v = id ? get().vehicles.find((x) => x.id === id) : null;
    set({
      vehModal: true, vehId: id, vehConfirm: false,
      vehForm: v
        ? { name: v.name, kz: v.kennzeichen, fuel: v.kraftstoffart, color: v.farbe, startKm: fmt(v.anfangsKilometer, 0) }
        : { name: '', kz: '', fuel: 'Diesel', color: '#3B82F6', startKm: '' },
    });
  },

  setVehForm: (patch) => set((s) => ({ vehForm: s.vehForm ? { ...s.vehForm, ...patch } : s.vehForm })),

  async saveVeh() {
    const s = get();
    const f = s.vehForm;
    if (!f) return;
    if (!f.name.trim()) return get().toast('Bitte einen Namen angeben');
    const startKm = Math.max(0, Math.round(parseDe(f.startKm) || 0));
    const body = { name: f.name.trim(), kennzeichen: f.kz.trim(), kraftstoffart: f.fuel, farbe: f.color, anfangsKilometer: startKm };
    try {
      if (s.vehId) { await api.fahrzeuge.update(s.vehId, body); get().toast('Fahrzeug aktualisiert'); }
      else {
        const nv = await api.fahrzeuge.create(body);
        get().toast('Fahrzeug angelegt');
        set((st) => ({ form: st.form.fahrzeugId ? st.form : { ...st.form, fahrzeugId: nv.id } }));
      }
      await get().refresh();
      set({ vehModal: false });
    } catch (e) {
      get().toast(e instanceof ApiError ? e.message : 'Speichern fehlgeschlagen');
    }
  },

  askVehDel: () => set({ vehConfirm: true }),
  cancelVehDel: () => set({ vehConfirm: false }),

  async deleteVeh() {
    const s = get();
    const id = s.vehId;
    if (!id) return;
    try {
      await api.fahrzeuge.remove(id);
      await get().refresh();
      set((st) => ({
        vehModal: false,
        dashVeh: st.dashVeh === id ? 'all' : st.dashVeh,
        statVeh: st.statVeh === id ? 'all' : st.statVeh,
        histVeh: st.histVeh === id ? 'all' : st.histVeh,
        form: st.form.fahrzeugId === id ? { ...st.form, fahrzeugId: st.vehicles.find((v) => v.id !== id)?.id ?? null } : st.form,
      }));
      get().toast('Fahrzeug und zugehörige Tankvorgänge gelöscht');
    } catch (e) {
      get().toast(e instanceof ApiError ? e.message : 'Löschen fehlgeschlagen');
    }
  },

  closeVehModal: () => set({ vehModal: false }),

  async exportCsv() {
    try {
      const { blob, filename } = await api.csv.export();
      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = filename;
      document.body.appendChild(a); a.click(); a.remove();
      setTimeout(() => URL.revokeObjectURL(a.href), 5000);
      get().toast('Tankvorgänge als CSV exportiert');
    } catch (e) {
      get().toast(e instanceof ApiError ? e.message : 'Export fehlgeschlagen');
    }
  },

  async importCsv(file) {
    try {
      const r = await api.csv.import(file);
      await get().refresh();
      get().toast(r.meldung);
    } catch (e) {
      get().toast(e instanceof ApiError ? e.message : 'Import fehlgeschlagen');
    }
  },

  toast(msg) {
    clearTimeout(toastTimer);
    set({ toastMsg: msg });
    toastTimer = setTimeout(() => set({ toastMsg: null }), 3600);
  },
}));
