import SwiftUI

// Tankvorgang erfassen – Nachbau von frontend/src/screens/Erfassen.tsx.
struct ErfassenView: View {
    @Environment(AppStore.self) private var store

    var body: some View {
        @Bindable var store = store
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                Text("Per Foto oder manuell – Werte werden vor dem Speichern immer geprüft.")
                    .font(.plex(13.5))
                    .foregroundStyle(Theme.text2)

                if store.vehicles.isEmpty {
                    noVehicleCard
                } else {
                    vehiclePicker
                    scanCards
                    detailsCard
                }
            }
            .padding(16)
        }
        .background(Theme.bg)
        .navigationTitle("Erfassen")
        .navigationBarTitleDisplayMode(.large)
        .scrollDismissesKeyboard(.interactively)
    }

    private var noVehicleCard: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Zuerst ein Fahrzeug anlegen").font(.plex(15, .bold)).foregroundStyle(Theme.text)
            Text("Tankvorgänge werden immer einem Fahrzeug zugeordnet.")
                .font(.plex(13.5)).foregroundStyle(Theme.text2)
            Button("Fahrzeug anlegen") {
                store.tab = .mehr
                store.openVehSheet(nil)
            }
            .buttonStyle(AccentButtonStyle(fontSize: 14))
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .card(padding: 22)
    }

    private var vehiclePicker: some View {
        VStack(alignment: .leading, spacing: 7) {
            Text("Fahrzeug").font(.plex(12, .semibold)).foregroundStyle(Theme.text2)
            ScrollView(.horizontal, showsIndicators: false) {
                HStack(spacing: 8) {
                    ForEach(store.vehicles) { v in
                        ChipButton(label: v.name, active: store.form.fahrzeugId == v.id,
                                   dotColor: Color(hex: v.farbe)) {
                            store.form.fahrzeugId = v.id
                        }
                    }
                }
                .padding(.horizontal, 1)
                .padding(.vertical, 2)
            }
        }
    }

    private var scanCards: some View {
        VStack(spacing: 14) {
            ScanCard(kind: .pump,
                     title: "Zapfsäule scannen",
                     subtitle: "Erkennt Liter & Gesamtpreis",
                     icon: "fuelpump",
                     scanLabel: "Werte werden erkannt …")
            ScanCard(kind: .tacho,
                     title: "Tacho scannen",
                     subtitleNote: "(optional)",
                     subtitle: "Erkennt den Kilometerstand",
                     icon: "gauge.with.needle",
                     scanLabel: "Kilometerstand wird erkannt …")
        }
    }

    private var detailsCard: some View {
        @Bindable var store = store
        return VStack(alignment: .leading, spacing: 14) {
            HStack {
                Text("Details").font(.plex(15, .bold)).foregroundStyle(Theme.text)
                Spacer()
                HStack(spacing: 9) {
                    Text("Auto-Berechnung").font(.plex(12.5, .semibold)).foregroundStyle(Theme.text2)
                    Toggle("", isOn: Binding(get: { store.autoCalc }, set: { _ in store.toggleAutoCalc() }))
                        .labelsHidden()
                }
            }

            VStack(spacing: 12) {
                HStack(alignment: .top, spacing: 12) {
                    trioField("Getankte Liter", "z. B. 45,50", field: "liter", text: store.form.liter)
                    trioField("Preis pro Liter (€/l)", "z. B. 1,589", field: "ppl", text: store.form.ppl)
                }
                HStack(alignment: .top, spacing: 12) {
                    trioField("Gesamtpreis (€)", "z. B. 72,30", field: "total", text: store.form.total)
                    VStack(alignment: .leading, spacing: 6) {
                        FieldLabel(label: "Kilometerstand (km)")
                        TbField(placeholder: "z. B. 48.230",
                                text: $store.form.km,
                                keyboard: .numberPad)
                    }
                }
                HStack(alignment: .top, spacing: 12) {
                    VStack(alignment: .leading, spacing: 6) {
                        FieldLabel(label: "Datum")
                        DatePicker("", selection: $store.form.datum, displayedComponents: .date)
                            .labelsHidden()
                            .datePickerStyle(.compact)
                            .frame(maxWidth: .infinity, alignment: .leading)
                    }
                    VStack(alignment: .leading, spacing: 6) {
                        FieldLabel(label: "Tankstelle", note: "(optional)")
                        TbField(placeholder: "z. B. OMV Wien Nord", text: $store.form.tankstelle)
                    }
                }
                VStack(alignment: .leading, spacing: 6) {
                    FieldLabel(label: "Notiz", note: "(optional)")
                    TbField(placeholder: "z. B. Fahrt in die Steiermark", text: $store.form.notiz)
                }
            }

            Divider().overlay(Theme.border)

            HStack {
                Toggle(isOn: $store.form.voll) {
                    VStack(alignment: .leading, spacing: 1) {
                        Text("Volltankung").font(.plex(14, .semibold)).foregroundStyle(Theme.text)
                        Text("Basis für die Verbrauchsberechnung").font(.plex(12)).foregroundStyle(Theme.text3)
                    }
                }
                Spacer(minLength: 16)
                Button {
                    Task { await store.saveEntry() }
                } label: {
                    HStack(spacing: 8) {
                        Image(systemName: "checkmark").font(.system(size: 13, weight: .bold))
                        Text("Speichern")
                    }
                }
                .buttonStyle(AccentButtonStyle(horizontalPadding: 22))
            }
        }
        .card(padding: 18)
    }

    private func trioField(_ label: String, _ placeholder: String, field: String, text: String) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            FieldLabel(label: label, pill: store.autoCalc && store.derived == field)
            TbField(placeholder: placeholder,
                    text: Binding(get: { text }, set: { store.onTrio(field, $0) }),
                    keyboard: .decimalPad)
        }
    }
}
