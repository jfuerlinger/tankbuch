import { useMemo } from 'react';
import { useStore } from '../store';
import { fmt } from '../lib/format';
import { kpis, verbMap, buildLine, buildBars, lineSeriesVerb, lineSeriesPpl } from '../lib/calc';
import { VehChips, KpiCard, ChartCard, Legend, KPI_GRID, CHART_GRID, EMPTY_CARD } from '../ui/components';
import { LineChartView, BarChartView } from '../ui/Charts';
import { card, h1 } from '../ui/styles';

export function Statistik() {
  const { vehicles, entries, statVeh } = useStore();
  const setSel = useStore((s) => s.setSel);

  const d = useMemo(() => {
    const k = kpis(vehicles, entries, statVeh);
    const vm = verbMap(vehicles, entries);
    const multi = statVeh === 'all' && vehicles.length > 1;
    return {
      k, multi,
      stVerb: buildLine(lineSeriesVerb(vehicles, entries, statVeh, vm), 1),
      stCost: buildBars(vehicles, entries, statVeh),
      stPpl: buildLine(lineSeriesPpl(vehicles, entries, statVeh), 2),
      compareRows: multi ? vehicles.map((v) => {
        const kv = kpis(vehicles, entries, v.id);
        return {
          dot: v.farbe, name: v.name, verb: fmt(kv.verbrauch, 1), ppl: fmt(kv.ppl, 3),
          cost: fmt(kv.cost, 0) + ' €', km: fmt(kv.km, 0),
          costKm: kv.costKm != null ? fmt(kv.costKm * 100, 1) + ' ct' : '—',
        };
      }) : [],
    };
  }, [vehicles, entries, statVeh]);

  const { k } = d;
  const hasData = k.count > 0;
  const legendItems = vehicles.map((v) => ({ color: v.farbe, label: v.name }));

  return (
    <div style={{ animation: 'tb-in .25s ease', display: 'flex', flexDirection: 'column', gap: 16 }}>
      <h1 style={h1}>Statistiken</h1>
      <VehChips sel={statVeh} onSelect={(id) => setSel('statVeh', id)} />

      {!hasData ? (
        <div style={EMPTY_CARD}>
          <div style={{ fontSize: 15, fontWeight: 700 }}>Noch keine Daten</div>
          <div style={{ fontSize: 13.5, color: 'var(--text2)' }}>Erfasse Tankvorgänge, um Statistiken zu sehen.</div>
        </div>
      ) : (
        <>
          <div style={KPI_GRID}>
            <KpiCard big={22} label="Gesamtliter" value={fmt(k.liters, 0)} unit="l" />
            <KpiCard big={22} label="Gesamtkosten" value={fmt(k.cost, 2)} unit="€" />
            <KpiCard big={22} label="Ø Verbrauch" value={fmt(k.verbrauch, 1)} unit="l/100 km" />
            <KpiCard big={22} label="Ø Preis" value={fmt(k.ppl, 3)} unit="€/l" />
            <KpiCard big={22} label="Gefahrene Kilometer" value={fmt(k.km, 0)} unit="km" />
            <KpiCard big={22} label="Kosten pro km" value={fmt(k.costKm != null ? k.costKm * 100 : null, 1)} unit="Cent" />
            <KpiCard big={22} label="Kosten pro Monat" value={fmt(k.costMonth, 2)} unit="€" />
            <KpiCard big={22} label="Tankvorgänge" value={fmt(k.count, 0)} unit="" />
          </div>

          {d.multi && (
            <div style={{ ...card, padding: '8px 18px 14px', overflowX: 'auto' }}>
              <div style={{ fontWeight: 700, fontSize: 14, padding: '10px 0 2px' }}>Fahrzeugvergleich</div>
              <div style={{ minWidth: 560 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1.7fr 1fr 1fr 1fr 1fr 1fr', gap: 10, padding: '9px 0', borderBottom: '1px solid var(--border)', fontSize: 11.5, fontWeight: 600, color: 'var(--text3)' }}>
                  <span>Fahrzeug</span><span style={{ textAlign: 'right' }}>Ø l/100 km</span><span style={{ textAlign: 'right' }}>Ø €/l</span><span style={{ textAlign: 'right' }}>Kosten</span><span style={{ textAlign: 'right' }}>km</span><span style={{ textAlign: 'right' }}>€/km</span>
                </div>
                {d.compareRows.map((cr, i) => (
                  <div key={i} style={{ display: 'grid', gridTemplateColumns: '1.7fr 1fr 1fr 1fr 1fr 1fr', gap: 10, padding: '11px 0', borderBottom: '1px solid var(--border)', fontSize: 13.5, fontVariantNumeric: 'tabular-nums', alignItems: 'center' }}>
                    <span style={{ display: 'flex', alignItems: 'center', gap: 8, fontWeight: 600, minWidth: 0 }}>
                      <span style={{ width: 9, height: 9, borderRadius: '50%', background: cr.dot, flexShrink: 0, display: 'inline-block' }} />
                      <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{cr.name}</span>
                    </span>
                    <span style={{ textAlign: 'right' }}>{cr.verb}</span><span style={{ textAlign: 'right' }}>{cr.ppl}</span><span style={{ textAlign: 'right' }}>{cr.cost}</span><span style={{ textAlign: 'right' }}>{cr.km}</span><span style={{ textAlign: 'right' }}>{cr.costKm}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          <div style={CHART_GRID}>
            <ChartCard title="Verbrauch" unit="l/100 km" legend={d.multi ? <Legend items={legendItems} /> : undefined}>
              <LineChartView chart={d.stVerb} empty="Noch zu wenige Volltankungen für die Verbrauchskurve." />
            </ChartCard>
            <ChartCard title="Kosten pro Monat" unit="€">
              <BarChartView chart={d.stCost} empty="Noch keine Kosten erfasst." />
            </ChartCard>
            <ChartCard title="Preis pro Liter" unit="€/l">
              <LineChartView chart={d.stPpl} empty="Noch zu wenige Datenpunkte." />
            </ChartCard>
          </div>
        </>
      )}
    </div>
  );
}
