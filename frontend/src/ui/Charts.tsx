import type { LineChart, BarChart } from '../lib/calc';

const emptyBox = (msg: string) => (
  <div style={{ padding: '38px 10px', textAlign: 'center', color: 'var(--text3)', fontSize: 13 }}>{msg}</div>
);

export function LineChartView({ chart, empty }: { chart: LineChart | null; empty: string }) {
  if (!chart) return emptyBox(empty);
  return (
    <svg viewBox="0 0 600 214" style={{ width: '100%', height: 'auto', display: 'block' }}>
      {chart.yTicks.map((tk, i) => (
        <g key={'g' + i}>
          <line x1={48} x2={590} y1={tk.y} y2={tk.y} stroke="var(--border)" strokeDasharray="3 4" />
          <text x={42} y={+tk.y + 4} textAnchor="end" fontSize={11} fill="var(--text3)" style={{ fontVariantNumeric: 'tabular-nums' }}>{tk.text}</text>
        </g>
      ))}
      {chart.xTicks.map((tk, i) => (
        <text key={'x' + i} x={tk.x} y={209} textAnchor="middle" fontSize={11} fill="var(--text3)">{tk.text}</text>
      ))}
      {chart.series.map((s, i) => (
        <g key={'s' + i}>
          <path d={s.path} fill="none" stroke={s.color} strokeWidth={2.4} strokeLinecap="round" strokeLinejoin="round" />
          {s.dots.map((d, j) => (
            <circle key={j} cx={d.x} cy={d.y} r={3.1} fill="var(--card)" stroke={s.color} strokeWidth={2} />
          ))}
        </g>
      ))}
    </svg>
  );
}

export function BarChartView({ chart, empty }: { chart: BarChart | null; empty: string }) {
  if (!chart) return emptyBox(empty);
  return (
    <svg viewBox="0 0 600 214" style={{ width: '100%', height: 'auto', display: 'block' }}>
      {chart.bars.map((b, i) => (
        <g key={i}>
          {b.segs.map((sg, j) => (
            <rect key={j} x={b.x} y={sg.y} width={chart.bw} height={sg.h} rx={3} fill={sg.color} opacity={0.9} />
          ))}
          <text x={b.cx} y={209} textAnchor="middle" fontSize={11} fill="var(--text3)">{b.label}</text>
          <text x={b.cx} y={b.topY} textAnchor="middle" fontSize={10.5} fontWeight={600} fill="var(--text2)" style={{ fontVariantNumeric: 'tabular-nums' }}>{b.total}</text>
        </g>
      ))}
    </svg>
  );
}
