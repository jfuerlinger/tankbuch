import SwiftUI
import Charts

// Swift-Charts-Wrapper im Stil der Web-Diagramme (frontend/src/ui/Charts.tsx):
// Linien 2,4 pt mit Punkten in Fahrzeugfarbe, gestrichelte Y-Gridlines, 3 Ticks.

private let chartHeight: CGFloat = 180

struct TbLineChart: View {
    let points: [LinePoint]
    let decimals: Int
    let empty: String

    private var seriesNames: [String] {
        var seen: Set<String> = []
        return points.compactMap { seen.insert($0.series).inserted ? $0.series : nil }
    }

    private var seriesColors: [Color] {
        var colors: [String: String] = [:]
        for p in points where colors[p.series] == nil { colors[p.series] = p.colorHex }
        return seriesNames.map { Color(hex: colors[$0] ?? "#8B93A1") }
    }

    var body: some View {
        if points.count < 2 {
            ChartEmptyBox(message: empty)
        } else {
            Chart(points) { p in
                LineMark(
                    x: .value("Datum", p.date),
                    y: .value("Wert", p.value),
                    series: .value("Fahrzeug", p.series))
                    .foregroundStyle(by: .value("Fahrzeug", p.series))
                    .lineStyle(StrokeStyle(lineWidth: 2.4, lineCap: .round, lineJoin: .round))
                    .interpolationMethod(.linear)
                PointMark(
                    x: .value("Datum", p.date),
                    y: .value("Wert", p.value))
                    .foregroundStyle(by: .value("Fahrzeug", p.series))
                    .symbolSize(46)
            }
            .chartForegroundStyleScale(domain: seriesNames, range: seriesColors)
            .chartLegend(.hidden)
            .chartYAxis {
                AxisMarks(position: .leading, values: .automatic(desiredCount: 3)) { value in
                    AxisGridLine(stroke: StrokeStyle(lineWidth: 1, dash: [3, 4]))
                        .foregroundStyle(Theme.border)
                    AxisValueLabel {
                        if let v = value.as(Double.self) {
                            Text(fmt(v, decimals)).font(.plex(11)).foregroundStyle(Theme.text3)
                        }
                    }
                }
            }
            .chartXAxis {
                AxisMarks(values: .automatic(desiredCount: 4)) { value in
                    AxisValueLabel {
                        if let d = value.as(Date.self) {
                            Text(d, format: .dateTime.month(.abbreviated).year(.twoDigits))
                                .font(.plex(11)).foregroundStyle(Theme.text3)
                        }
                    }
                }
            }
            .frame(height: chartHeight)
        }
    }
}

struct TbBarChart: View {
    let segments: [MonthCost]
    let monthOrder: [String]
    let empty: String

    var body: some View {
        if segments.isEmpty {
            ChartEmptyBox(message: empty)
        } else {
            Chart(segments) { s in
                BarMark(
                    x: .value("Monat", s.label),
                    y: .value("€", s.value),
                    width: .ratio(0.55))
                    .foregroundStyle(Color(hex: s.colorHex).opacity(0.9))
                    .cornerRadius(3)
                    .annotation(position: .top, spacing: 3) {
                        if s.isTop {
                            Text(fmt(s.total, 0))
                                .font(.plex(10.5, .semibold))
                                .monospacedDigit()
                                .foregroundStyle(Theme.text2)
                        }
                    }
            }
            .chartXScale(domain: monthOrder)
            .chartLegend(.hidden)
            .chartYAxis(.hidden)
            .chartXAxis {
                AxisMarks { value in
                    AxisValueLabel {
                        if let label = value.as(String.self) {
                            Text(label).font(.plex(11)).foregroundStyle(Theme.text3)
                        }
                    }
                }
            }
            .frame(height: chartHeight)
        }
    }
}

struct ChartEmptyBox: View {
    let message: String

    var body: some View {
        Text(message)
            .font(.plex(13))
            .foregroundStyle(Theme.text3)
            .multilineTextAlignment(.center)
            .frame(maxWidth: .infinity)
            .padding(.horizontal, 10)
            .padding(.vertical, 38)
    }
}
