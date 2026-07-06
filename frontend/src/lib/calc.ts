// Auswertung & Diagramm-Geometrie – 1:1 aus der Design-Vorlage portiert.
import type { Fahrzeug, Tankvorgang } from './types';
import { fmt } from './format';

export interface Kpi {
  cost: number; liters: number; km: number; count: number;
  last: Tankvorgang | null;
  verbrauch: number | null; ppl: number | null; costKm: number | null; costMonth: number | null;
}
export interface SeriesDef { color: string; label: string; pts: { t: number; v: number }[]; }
export interface LineChart {
  series: { color: string; path: string; dots: { x: string; y: string }[] }[];
  yTicks: { y: string; text: string }[];
  xTicks: { x: string; text: string }[];
  w: number; h: number;
}
export interface BarChart {
  bars: { x: string; cx: string; segs: { y: string; h: string; color: string }[]; label: string; topY: string; total: string }[];
  bw: string; w: number; h: number;
}

function sortByKm(a: Tankvorgang, b: Tankvorgang) {
  return a.kilometerstand - b.kilometerstand || (a.datum < b.datum ? -1 : 1);
}

export function verbMap(vehicles: Fahrzeug[], entries: Tankvorgang[]): Record<string, number | null> {
  const map: Record<string, number | null> = {};
  for (const v of vehicles) {
    const es = entries.filter(e => e.fahrzeugId === v.id).sort(sortByKm);
    let prevFull: number | null = null, sum = 0;
    for (const e of es) {
      sum += e.liter;
      if (e.volltankung) {
        map[e.id] = (prevFull != null && e.kilometerstand > prevFull)
          ? (sum / (e.kilometerstand - prevFull)) * 100 : null;
        prevFull = e.kilometerstand; sum = 0;
      } else map[e.id] = null;
    }
  }
  return map;
}

export function kpis(vehicles: Fahrzeug[], entries: Tankvorgang[], sel: string): Kpi {
  const vs = sel === 'all' ? vehicles : vehicles.filter(v => v.id === sel);
  let cost = 0, liters = 0, km = 0, segL = 0, segKm = 0, count = 0;
  let last: Tankvorgang | null = null;
  let minT: string | null = null, maxT: string | null = null;
  for (const v of vs) {
    const es = entries.filter(e => e.fahrzeugId === v.id).sort(sortByKm);
    if (!es.length) continue;
    km += Math.max(0, es[es.length - 1].kilometerstand - v.anfangsKilometer);
    let prevFull: number | null = null, sum = 0;
    for (const e of es) {
      cost += e.gesamtpreis; liters += e.liter; count++; sum += e.liter;
      if (e.volltankung) {
        if (prevFull != null && e.kilometerstand > prevFull) { segL += sum; segKm += e.kilometerstand - prevFull; }
        prevFull = e.kilometerstand; sum = 0;
      }
      if (!last || e.datum > last.datum) last = e;
      if (!minT || e.datum < minT) minT = e.datum;
      if (!maxT || e.datum > maxT) maxT = e.datum;
    }
  }
  const months = minT && maxT
    ? Math.max(1, (new Date(maxT).getTime() - new Date(minT).getTime()) / 86400000 / 30.44) : 1;
  return {
    cost, liters, km, count, last,
    verbrauch: segKm ? (segL / segKm) * 100 : null,
    ppl: liters ? cost / liters : null,
    costKm: km ? cost / km : null,
    costMonth: count ? cost / months : null,
  };
}

export function lineSeriesVerb(vehicles: Fahrzeug[], entries: Tankvorgang[], sel: string, vm: Record<string, number | null>): SeriesDef[] {
  const vs = sel === 'all' ? vehicles : vehicles.filter(v => v.id === sel);
  return vs.map(v => ({
    color: v.farbe, label: v.name,
    pts: entries.filter(e => e.fahrzeugId === v.id && vm[e.id] != null)
      .map(e => ({ t: new Date(e.datum + 'T12:00:00').getTime(), v: vm[e.id] as number })),
  }));
}

export function lineSeriesPpl(vehicles: Fahrzeug[], entries: Tankvorgang[], sel: string): SeriesDef[] {
  const vs = sel === 'all' ? vehicles : vehicles.filter(v => v.id === sel);
  return vs.map(v => ({
    color: v.farbe, label: v.name,
    pts: entries.filter(e => e.fahrzeugId === v.id)
      .map(e => ({ t: new Date(e.datum + 'T12:00:00').getTime(), v: e.preisProLiter })),
  }));
}

export function buildLine(seriesDefs: SeriesDef[], dec: number): LineChart | null {
  const defs = seriesDefs.filter(s => s.pts.length);
  const all = defs.flatMap(s => s.pts);
  if (all.length < 2) return null;
  const w = 600, h = 214, l = 48, r = 10, t = 14, b = 30;
  let x0 = Math.min(...all.map(p => p.t)), x1 = Math.max(...all.map(p => p.t));
  if (x1 === x0) x1 = x0 + 1;
  let y0 = Math.min(...all.map(p => p.v)), y1 = Math.max(...all.map(p => p.v));
  const pad = (y1 - y0) * 0.2 || y0 * 0.08 || 1;
  y0 -= pad; y1 += pad; if (y0 < 0) y0 = 0;
  const X = (v: number) => l + ((v - x0) / (x1 - x0)) * (w - l - r);
  const Y = (v: number) => t + (1 - (v - y0) / (y1 - y0)) * (h - t - b);
  const series = defs.map(s => {
    const pts = [...s.pts].sort((a, c) => a.t - c.t);
    return {
      color: s.color,
      path: pts.map((p, i) => (i ? 'L' : 'M') + X(p.t).toFixed(1) + ' ' + Y(p.v).toFixed(1)).join(' '),
      dots: pts.map(p => ({ x: X(p.t).toFixed(1), y: Y(p.v).toFixed(1) })),
    };
  });
  const yTicks = [0, 0.5, 1].map(f => { const v = y0 + f * (y1 - y0); return { y: Y(v).toFixed(1), text: fmt(v, dec) }; });
  const xTicks = [];
  for (let i = 0; i < 4; i++) {
    const tt = x0 + (i / 3) * (x1 - x0);
    xTicks.push({ x: X(tt).toFixed(1), text: new Date(tt).toLocaleDateString('de-AT', { month: 'short', year: '2-digit' }) });
  }
  return { series, yTicks, xTicks, w, h };
}

export function buildBars(vehicles: Fahrzeug[], entries: Tankvorgang[], sel: string): BarChart | null {
  const es = entries.filter(e => sel === 'all' || e.fahrzeugId === sel);
  if (!es.length) return null;
  const map = new Map<string, Record<string, number>>();
  for (const e of es) {
    const k = e.datum.slice(0, 7);
    if (!map.has(k)) map.set(k, {});
    const m = map.get(k)!;
    m[e.fahrzeugId] = (m[e.fahrzeugId] || 0) + e.gesamtpreis;
  }
  const keys = [...map.keys()].sort().slice(-9);
  const w = 600, h = 214, t = 30, b = 30, l = 10, r = 10;
  const totals = keys.map(k => Object.values(map.get(k)!).reduce((a, c) => a + c, 0));
  const max = Math.max(...totals) || 1;
  const slot = (w - l - r) / keys.length;
  const bw = Math.min(46, slot * 0.55);
  const bars = keys.map((k, i) => {
    const cx = l + slot * i + slot / 2;
    let yCur = h - b;
    const segs = [];
    for (const v of vehicles) {
      const val = map.get(k)![v.id];
      if (!val) continue;
      const hh = Math.max(1.5, (val / max) * (h - t - b));
      yCur -= hh;
      segs.push({ y: yCur.toFixed(1), h: hh.toFixed(1), color: v.farbe });
    }
    const d = new Date(k + '-15T12:00:00');
    return {
      x: (cx - bw / 2).toFixed(1), cx: cx.toFixed(1), segs,
      label: d.toLocaleDateString('de-AT', { month: 'short' }),
      topY: (yCur - 7).toFixed(1), total: fmt(totals[i], 0),
    };
  });
  return { bars, bw: bw.toFixed(1), w, h };
}

// Trio-Auto-Berechnung – 1:1 aus der Design-Vorlage (computeTrio).
export function computeTrio(
  form: { liter: string; ppl: string; total: string },
  order: string[],
): { form: { liter: string; ppl: string; total: string }; derived: string | null } {
  const trio = ['liter', 'ppl', 'total'] as const;
  if (order.length < 2) return { form, derived: null };
  const derived = trio.find(f => !order.includes(f)) ?? null;
  const parse = (s: string) => {
    const t = s.trim();
    if (!t) return null;
    const v = parseFloat(t.includes(',') ? t.replace(/\./g, '').replace(',', '.') : t);
    return isFinite(v) ? v : null;
  };
  const a = parse(form[order[0] as 'liter']); const b = parse(form[order[1] as 'liter']);
  if (a == null || b == null || a <= 0 || b <= 0) return { form, derived };
  const v: Record<string, number | null> = { liter: null, ppl: null, total: null };
  v[order[0]] = a; v[order[1]] = b;
  let out: string | null = null;
  if (derived === 'ppl' && (v.liter ?? 0) > 0) out = fmt((v.total as number) / (v.liter as number), 3);
  else if (derived === 'total') out = fmt((v.liter as number) * (v.ppl as number), 2);
  else if (derived === 'liter' && (v.ppl ?? 0) > 0) out = fmt((v.total as number) / (v.ppl as number), 2);
  if (out != null && derived) form = { ...form, [derived]: out };
  return { form, derived };
}
