import { useMemo } from 'react';
import { useStore } from '../store';
import { fmt, fmtDate } from '../lib/format';
import { kpis, verbMap, buildLine, buildBars, lineSeriesVerb, lineSeriesPpl } from '../lib/calc';
import { VehChips, KpiCard, ChartCard, Legend, KPI_GRID, CHART_GRID } from '../ui/components';
import { LineChartView, BarChartView } from '../ui/Charts';
import { LogoOutline } from '../ui/Logo';
import { IconPlus } from '../ui/icons';
import { accentBtn, h1 } from '../ui/styles';

export function Dashboard() {
  const { vehicles, entries, dashVeh } = useStore();
  const go = useStore((s) => s.go);
  const setSel = useStore((s) => s.setSel);

  const data = useMemo(() => {
    const k = kpis(vehicles, entries, dashVeh);
    const vm = verbMap(vehicles, entries);
    const veh = (id: string) => vehicles.find((v) => v.id === id);
    return {
      k,
      chVerb: buildLine(lineSeriesVerb(vehicles, entries, dashVeh, vm), 1),
      chCost: buildBars(vehicles, entries, dashVeh),
      chPpl: buildLine(lineSeriesPpl(vehicles, entries, dashVeh), 2),
      lastVehName: k.last ? veh(k.last.fahrzeugId)?.name ?? '' : '',
    };
  }, [vehicles, entries, dashVeh]);

  const { k } = data;
  const hasData = k.count > 0;
  const multi = dashVeh === 'all' && vehicles.length > 1;
  const todayLong = new Date().toLocaleDateString('de-AT', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  const legendItems = vehicles.map((v) => ({ color: v.farbe, label: v.name }));

  return (
    <div style={{ animation: 'tb-in .25s ease', display: 'flex', flexDirection: 'column', gap: 18 }}>
      <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <h1 style={h1}>Übersicht</h1>
          <div style={{ color: 'var(--text2)', fontSize: 13.5, marginTop: 2 }}>{todayLong}</div>
        </div>
        <button className="tb-accent" onClick={() => go('erfassen')} style={accentBtn}>
          <IconPlus size={16} /> Tankvorgang erfassen
        </button>
      </div>

      <VehChips sel={dashVeh} onSelect={(id) => setSel('dashVeh', id)} />

      {!hasData ? (
        <div style={{ background: 'var(--card)', border: '1px solid var(--border)', borderRadius: 18, boxShadow: 'var(--shadow)', padding: '44px 24px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10, textAlign: 'center' }}>
          <LogoOutline />
          <div style={{ fontSize: 16, fontWeight: 700 }}>Noch keine Tankvorgänge</div>
          <div style={{ fontSize: 13.5, color: 'var(--text2)', maxWidth: 340 }}>
            {vehicles.length ? 'Erfasse deinen ersten Tankvorgang – per Foto der Zapfsäule oder manuell.' : 'Lege zuerst ein Fahrzeug an, dann kannst du Tankvorgänge erfassen.'}
          </div>
          <button className="tb-accent" onClick={() => go(vehicles.length ? 'erfassen' : 'fahrzeuge')} style={{ ...accentBtn, marginTop: 8, borderRadius: 11, padding: '11px 18px', fontSize: 14 }}>
            {vehicles.length ? 'Tankvorgang erfassen' : 'Fahrzeug anlegen'}
          </button>
        </div>
      ) : (
        <>
          <div style={KPI_GRID}>
            <KpiCard label="Ø Verbrauch" value={fmt(k.verbrauch, 1)} unit="l/100 km" />
            <KpiCard label="Ø Preis" value={fmt(k.ppl, 3)} unit="€/l" />
            <KpiCard label="Gesamtkosten" value={fmt(k.cost, 2)} unit="€" />
            <KpiCard label="Gefahrene Kilometer" value={fmt(k.km, 0)} unit="km" />
            <KpiCard label="Kosten pro km" value={fmt(k.costKm != null ? k.costKm * 100 : null, 1)} unit="Cent" />
            <KpiCard label="Letzter Tankvorgang"
              value={k.last ? fmtDate(k.last.datum).slice(0, 6) : '—'} unit=""
              sub={k.last ? `${fmt(k.last.liter, 2)} l · ${fmt(k.last.gesamtpreis, 2)} € · ${data.lastVehName}` : null} />
          </div>
          <div style={CHART_GRID}>
            <ChartCard title="Verbrauch" unit="l/100 km" legend={multi ? <Legend items={legendItems} /> : undefined}>
              <LineChartView chart={data.chVerb} empty="Noch zu wenige Volltankungen für die Verbrauchskurve." />
            </ChartCard>
            <ChartCard title="Kosten pro Monat" unit="€">
              <BarChartView chart={data.chCost} empty="Noch keine Kosten erfasst." />
            </ChartCard>
            <ChartCard title="Preis pro Liter" unit="€/l" legend={multi ? <Legend items={legendItems} /> : undefined}>
              <LineChartView chart={data.chPpl} empty="Noch zu wenige Datenpunkte." />
            </ChartCard>
          </div>
        </>
      )}
    </div>
  );
}
