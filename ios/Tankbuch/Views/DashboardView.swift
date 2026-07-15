import SwiftUI

// Übersicht – Nachbau von frontend/src/screens/Dashboard.tsx.
struct DashboardView: View {
    @Environment(AppStore.self) private var store

    private var k: Kpi { kpis(vehicles: store.vehicles, entries: store.entries, sel: store.dashVeh) }
    private var multi: Bool { store.dashVeh == nil && store.vehicles.count > 1 }

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 18) {
                header
                VehChips(sel: store.dashVeh) { store.dashVeh = $0 }

                if k.count == 0 {
                    EmptyCard(
                        showLogo: true,
                        title: "Noch keine Tankvorgänge",
                        message: store.vehicles.isEmpty
                            ? "Lege zuerst ein Fahrzeug an, dann kannst du Tankvorgänge erfassen."
                            : "Erfasse deinen ersten Tankvorgang – per Foto der Zapfsäule oder manuell.",
                        buttonLabel: store.vehicles.isEmpty ? "Fahrzeug anlegen" : "Tankvorgang erfassen") {
                            if store.vehicles.isEmpty {
                                store.tab = .mehr
                                store.openVehSheet(nil)
                            } else {
                                store.tab = .erfassen
                            }
                        }
                } else {
                    kpiGrid
                    charts
                }
            }
            .padding(16)
        }
        .background(Theme.bg)
        .navigationTitle("Übersicht")
        .navigationBarTitleDisplayMode(.large)
        .refreshable { await store.refresh() }
    }

    private var header: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(Date.now.formatted(.dateTime.weekday(.wide).day().month(.wide).year()
                .locale(Locale(identifier: "de_AT"))))
                .font(.plex(13.5))
                .foregroundStyle(Theme.text2)
            Button {
                store.tab = .erfassen
            } label: {
                HStack(spacing: 8) {
                    Image(systemName: "plus").font(.system(size: 14, weight: .bold))
                    Text("Tankvorgang erfassen")
                }
            }
            .buttonStyle(AccentButtonStyle(fontSize: 14))
        }
    }

    private var kpiGrid: some View {
        LazyVGrid(columns: [GridItem(.adaptive(minimum: 158), spacing: 12)], spacing: 12) {
            KpiCard(label: "Ø Verbrauch", value: fmt(k.verbrauch, 1), unit: "l/100 km")
            KpiCard(label: "Ø Preis", value: fmt(k.ppl, 3), unit: "€/l")
            KpiCard(label: "Gesamtkosten", value: fmt(k.cost, 2), unit: "€")
            KpiCard(label: "Gefahrene Kilometer", value: fmt(k.km, 0), unit: "km")
            KpiCard(label: "Kosten pro km", value: fmt(k.costKm.map { $0 * 100 }, 1), unit: "Cent")
            KpiCard(label: "Letzter Tankvorgang",
                    value: k.last.map { String(fmtDate($0.datum).prefix(6)) } ?? "—",
                    unit: "",
                    sub: k.last.map { last in
                        let veh = store.vehicle(last.fahrzeugId)?.name ?? ""
                        return "\(fmt(last.liter, 2)) l · \(fmt(last.gesamtpreis, 2)) € · \(veh)"
                    })
        }
    }

    private var charts: some View {
        let legendItems = store.vehicles.map { (color: $0.farbe, label: $0.name) }
        let bars = monthlyCosts(vehicles: store.vehicles, entries: store.entries, sel: store.dashVeh)
        return VStack(spacing: 14) {
            ChartCard(title: "Verbrauch", unit: "l/100 km") {
                TbLineChart(points: verbPoints(vehicles: store.vehicles, entries: store.entries, sel: store.dashVeh),
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
                TbLineChart(points: pplPoints(vehicles: store.vehicles, entries: store.entries, sel: store.dashVeh),
                            decimals: 2,
                            empty: "Noch zu wenige Datenpunkte.")
            } legend: {
                if multi { LegendView(items: legendItems) }
            }
        }
    }
}
