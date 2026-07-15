import SwiftUI

// Fahrzeuge – Nachbau von frontend/src/screens/Fahrzeuge.tsx + VehicleModal.tsx.
struct FahrzeugeView: View {
    @Environment(AppStore.self) private var store

    var body: some View {
        @Bindable var store = store
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                if store.vehicles.isEmpty {
                    EmptyCard(title: "Noch keine Fahrzeuge",
                              message: "Lege dein erstes Fahrzeug an, um Tankvorgänge zu erfassen.")
                }

                LazyVGrid(columns: [GridItem(.adaptive(minimum: 290), spacing: 14)], spacing: 14) {
                    ForEach(store.vehicles) { v in
                        vehicleCard(v)
                    }
                }
            }
            .padding(16)
        }
        .background(Theme.bg)
        .navigationTitle("Fahrzeuge")
        .navigationBarTitleDisplayMode(.large)
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Button {
                    store.openVehSheet(nil)
                } label: {
                    Label("Fahrzeug anlegen", systemImage: "plus")
                }
            }
        }
        .sheet(isPresented: $store.vehSheet) { VehicleSheet() }
        .refreshable { await store.refresh() }
    }

    private func vehicleCard(_ v: Fahrzeug) -> some View {
        VStack(alignment: .leading, spacing: 13) {
            HStack(spacing: 12) {
                RoundedRectangle(cornerRadius: 12, style: .continuous)
                    .fill(Color(hex: v.farbe).mix(0.16, with: Theme.card))
                    .frame(width: 44, height: 44)
                    .overlay(
                        Image(systemName: "car")
                            .font(.system(size: 20, weight: .medium))
                            .foregroundStyle(Color(hex: v.farbe))
                    )
                VStack(alignment: .leading, spacing: 3) {
                    Text(v.name).font(.plex(15.5, .bold)).foregroundStyle(Theme.text).lineLimit(1)
                    HStack(spacing: 8) {
                        Text(v.kennzeichen.isEmpty ? "—" : v.kennzeichen)
                            .font(.plex(11.5, .bold))
                            .kerning(0.8)
                            .foregroundStyle(Theme.text2)
                            .padding(.horizontal, 7)
                            .padding(.vertical, 1)
                            .overlay(RoundedRectangle(cornerRadius: 5).stroke(Theme.text3, lineWidth: 1.5))
                        Text(v.kraftstoffart).font(.plex(12)).foregroundStyle(Theme.text3)
                    }
                }
                Spacer()
                Button {
                    store.openVehSheet(v.id)
                } label: {
                    Image(systemName: "pencil")
                        .font(.system(size: 15))
                        .foregroundStyle(Theme.text3)
                        .frame(width: 32, height: 32)
                }
                .buttonStyle(.plain)
            }

            LazyVGrid(columns: [GridItem(.flexible(), spacing: 9), GridItem(.flexible())], spacing: 9) {
                miniStat("Kilometerstand", fmt(v.aktuellerKilometerstand, 0) + " km")
                miniStat("Ø Verbrauch", v.durchschnittsVerbrauch.map { fmt($0, 1) + " l/100 km" } ?? "—")
                miniStat("Gesamtkosten", fmt(v.gesamtkosten, 2) + " €")
                miniStat("Tankvorgänge", String(v.anzahlTankvorgaenge))
            }
        }
        .card(padding: 17)
    }

    private func miniStat(_ label: String, _ value: String) -> some View {
        VStack(alignment: .leading, spacing: 1) {
            Text(label).font(.plex(11, .semibold)).foregroundStyle(Theme.text3)
            Text(value).font(.plex(14.5, .bold)).monospacedDigit().foregroundStyle(Theme.text)
                .lineLimit(1).minimumScaleFactor(0.8)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(.horizontal, 12)
        .padding(.vertical, 9)
        .background(RoundedRectangle(cornerRadius: 10, style: .continuous).fill(Theme.card2))
    }
}

// Fahrzeug anlegen/bearbeiten – Sheet-Gegenstück zum VehicleModal.
struct VehicleSheet: View {
    @Environment(AppStore.self) private var store
    @State private var confirmDelete = false

    private static let swatches = ["#3B82F6", "#2DD4BF", "#F59E0B", "#A78BFA", "#F472B6", "#34D399", "#FB923C", "#94A3B8"]
    private static let fuels = ["Diesel", "Super 95", "Super Plus 98", "Premium Diesel", "LPG", "CNG"]

    var body: some View {
        @Bindable var store = store
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 12) {
                    VStack(alignment: .leading, spacing: 6) {
                        FieldLabel(label: "Name")
                        TbField(placeholder: "z. B. Škoda Octavia Combi", text: $store.vehForm.name)
                    }
                    HStack(alignment: .top, spacing: 11) {
                        VStack(alignment: .leading, spacing: 6) {
                            FieldLabel(label: "Kennzeichen")
                            TbField(placeholder: "z. B. W-123 AB", text: $store.vehForm.kz)
                        }
                        VStack(alignment: .leading, spacing: 6) {
                            FieldLabel(label: "Kraftstoffart")
                            Picker("Kraftstoffart", selection: $store.vehForm.fuel) {
                                ForEach(Self.fuels, id: \.self) { Text($0).tag($0) }
                            }
                            .pickerStyle(.menu)
                            .tint(Theme.text)
                            .frame(maxWidth: .infinity, alignment: .leading)
                            .padding(.horizontal, 4)
                            .padding(.vertical, 4)
                            .background(RoundedRectangle(cornerRadius: 10, style: .continuous).fill(Theme.bg))
                            .overlay(RoundedRectangle(cornerRadius: 10, style: .continuous).stroke(Theme.border, lineWidth: 1))
                        }
                    }
                    VStack(alignment: .leading, spacing: 6) {
                        FieldLabel(label: "Anfangs-Kilometerstand")
                        TbField(placeholder: "z. B. 41.250", text: $store.vehForm.startKm, keyboard: .numberPad)
                    }
                    VStack(alignment: .leading, spacing: 7) {
                        FieldLabel(label: "Farbe")
                        HStack(spacing: 9) {
                            ForEach(Self.swatches, id: \.self) { hex in
                                Button {
                                    store.vehForm.color = hex
                                } label: {
                                    Circle()
                                        .fill(Color(hex: hex))
                                        .frame(width: 30, height: 30)
                                        .overlay(
                                            Circle().stroke(store.vehForm.color == hex ? Color(hex: hex) : Theme.border,
                                                            lineWidth: store.vehForm.color == hex ? 2.5 : 1)
                                                .padding(store.vehForm.color == hex ? -4 : 0)
                                        )
                                }
                                .buttonStyle(.plain)
                            }
                        }
                    }

                    if store.vehId != nil {
                        Divider().overlay(Theme.border).padding(.top, 8)
                        Button("Fahrzeug löschen …", role: .destructive) {
                            confirmDelete = true
                        }
                        .font(.plex(13, .semibold))
                        .padding(.top, 4)
                    }
                }
                .padding(20)
            }
            .background(Theme.bg)
            .navigationTitle(store.vehId != nil ? "Fahrzeug bearbeiten" : "Fahrzeug anlegen")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen") { store.vehSheet = false }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Speichern") { Task { await store.saveVeh() } }
                        .fontWeight(.bold)
                }
            }
            .confirmationDialog("Fahrzeug wirklich löschen?",
                                isPresented: $confirmDelete, titleVisibility: .visible) {
                Button("Ja, löschen", role: .destructive) {
                    Task { await store.deleteVeh() }
                }
                Button("Abbrechen", role: .cancel) {}
            } message: {
                Text("Alle Tankvorgänge dieses Fahrzeugs werden entfernt.")
            }
        }
        .presentationDetents([.medium, .large])
        .presentationDragIndicator(.visible)
    }
}
