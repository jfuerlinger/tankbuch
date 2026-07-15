import SwiftUI

// Statistiken – Nachbau von frontend/src/screens/Statistik.tsx.
struct StatistikView: View {
    @Environment(AppStore.self) private var store

    private var k: Kpi { kpis(vehicles: store.vehicles, entries: store.entries, sel: store.statVeh) }
    private var multi: Bool { store.statVeh == nil && store.vehicles.count > 1 }

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                VehChips(sel: store.statVeh) { store.statVeh = $0 }

                if k.count == 0 {
                    EmptyCard(title: "Noch keine Daten",
                              message: "Erfasse Tankvorgänge, um Statistiken zu sehen.")
                } else {
                    kpiGrid
                    if multi { compareCard }
                    charts
                }
            }
            .padding(16)
        }
        .background(Theme.bg)
        .navigationTitle("Statistiken")
        .navigationBarTitleDisplayMode(.large)
        .refreshable { await store.refresh() }
    }

    private var kpiGrid: some View {
        LazyVGrid(columns: [GridItem(.adaptive(minimum: 158), spacing: 12)], spacing: 12) {
            KpiCard(label: "Gesamtliter", value: fmt(k.liters, 0), unit: "l", big: 22)
            KpiCard(label: "Gesamtkosten", value: fmt(k.cost, 2), unit: "€", big: 22)
            KpiCard(label: "Ø Verbrauch", value: fmt(k.verbrauch, 1), unit: "l/100 km", big: 22)
            KpiCard(label: "Ø Preis", value: fmt(k.ppl, 3), unit: "€/l", big: 22)
            KpiCard(label: "Gefahrene Kilometer", value: fmt(k.km, 0), unit: "km", big: 22)
            KpiCard(label: "Kosten pro km", value: fmt(k.costKm.map { $0 * 100 }, 1), unit: "Cent", big: 22)
            KpiCard(label: "Kosten pro Monat", value: fmt(k.costMonth, 2), unit: "€", big: 22)
            KpiCard(label: "Tankvorgänge", value: fmt(Double(k.count), 0), unit: "", big: 22)
        }
    }

    // Fahrzeugvergleich – horizontale Grid-Tabelle wie im Web.
    private var compareCard: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Fahrzeugvergleich")
                .font(.plex(14, .bold))
                .foregroundStyle(Theme.text)
                .padding(.bottom, 6)
            ScrollView(.horizontal, showsIndicators: false) {
                Grid(alignment: .trailing, horizontalSpacing: 14, verticalSpacing: 0) {
                    GridRow {
                        Text("Fahrzeug").gridColumnAlignment(.leading)
                        Text("Ø l/100 km")
                        Text("Ø €/l")
                        Text("Kosten")
                        Text("km")
                        Text("€/km")
                    }
                    .font(.plex(11.5, .semibold))
                    .foregroundStyle(Theme.text3)
                    .padding(.vertical, 9)

                    Divider().gridCellUnsizedAxes(.horizontal).overlay(Theme.border)

                    ForEach(store.vehicles) { v in
                        let kv = kpis(vehicles: store.vehicles, entries: store.entries, sel: v.id)
                        GridRow {
                            HStack(spacing: 8) {
                                Circle().fill(Color(hex: v.farbe)).frame(width: 9, height: 9)
                                Text(v.name).font(.plex(13.5, .semibold)).lineLimit(1)
                            }
                            .gridColumnAlignment(.leading)
                            Text(fmt(kv.verbrauch, 1))
                            Text(fmt(kv.ppl, 3))
                            Text(fmt(kv.cost, 0) + " €")
                            Text(fmt(kv.km, 0))
                            Text(kv.costKm.map { fmt($0 * 100, 1) + " ct" } ?? "—")
                        }
                        .font(.plex(13.5))
                        .monospacedDigit()
                        .foregroundStyle(Theme.text)
                        .padding(.vertical, 11)

                        if v.id != store.vehicles.last?.id {
                            Divider().gridCellUnsizedAxes(.horizontal).overlay(Theme.border)
                        }
                    }
                }
            }
        }
        .card(padding: 16)
    }

    private var charts: some View {
        let legendItems = store.vehicles.map { (color: $0.farbe, label: $0.name) }
        let bars = monthlyCosts(vehicles: store.vehicles, entries: store.entries, sel: store.statVeh)
        return VStack(spacing: 14) {
            ChartCard(title: "Verbrauch", unit: "l/100 km") {
                TbLineChart(points: verbPoints(vehicles: store.vehicles, entries: store.entries, sel: store.statVeh),
                            decimals: 1,
                            empty: "Noch zu wenige Volltankungen für die Verbrauchskurve.")
            } legend: {
                if multi { LegendView(items: legendItems) }
            }
            ChartCard(title: "Kosten pro Monat", unit: "€") {
                TbBarChart(segments: bars.segments, monthOrder: bars.monthOrder,
                           empty: "Noch keine Kosten erfasst.")
            }
            ChartCard(title: "Preis pro Liter", unit: "€/l") {
                TbLineChart(points: pplPoints(vehicles: store.vehicles, entries: store.entries, sel: store.statVeh),
                            decimals: 2,
                            empty: "Noch zu wenige Datenpunkte.")
            }
        }
    }
}
