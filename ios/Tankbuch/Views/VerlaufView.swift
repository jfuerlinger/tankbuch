import SwiftUI

// Verlauf – Nachbau von frontend/src/screens/Verlauf.tsx mit nativen Swipe-Actions.
struct VerlaufView: View {
    @Environment(AppStore.self) private var store
    @State private var deleteCandidate: Tankvorgang?

    private struct Row: Identifiable {
        let entry: Tankvorgang
        let dot: Color
        let vehName: String
        let verbColor: Color
        let meta: String
        var id: UUID { entry.id }
    }

    private var rows: [Row] {
        let cutoff: String? = store.histRange == "all" ? nil
            : isoString(from: Calendar.current.date(byAdding: .day, value: -(Int(store.histRange) ?? 0), to: .now)!)
        return store.entries
            .filter { store.histVeh == nil || $0.fahrzeugId == store.histVeh }
            .filter { cutoff == nil || $0.datum >= cutoff! }
            .sorted { a, b in
                a.datum != b.datum ? a.datum > b.datum : a.kilometerstand > b.kilometerstand
            }
            .map { e in
                let v = store.vehicle(e.fahrzeugId)
                var verbColor = Theme.text
                if let verb = e.verbrauch, let avg = v?.durchschnittsVerbrauch, avg > 0 {
                    if verb <= avg * 0.97 { verbColor = Theme.good }
                    else if verb >= avg * 1.06 { verbColor = Theme.bad }
                }
                let meta = [e.tankstelle,
                            e.volltankung ? "Volltankung" : "Teilbetankung",
                            e.notiz]
                    .compactMap { $0 }
                    .filter { !$0.isEmpty }
                    .joined(separator: " · ")
                return Row(entry: e,
                           dot: v.map { Color(hex: $0.farbe) } ?? Theme.text3,
                           vehName: v?.name ?? "?",
                           verbColor: verbColor,
                           meta: meta)
            }
    }

    var body: some View {
        List {
            Section {
                VStack(alignment: .leading, spacing: 8) {
                    VehChips(sel: store.histVeh) { store.histVeh = $0 }
                    RangeChips(sel: store.histRange) { store.histRange = $0 }
                }
                .listRowInsets(EdgeInsets(top: 4, leading: 16, bottom: 6, trailing: 16))
                .listRowBackground(Color.clear)
                .listRowSeparator(.hidden)
            }

            if rows.isEmpty {
                Section {
                    EmptyCard(title: "Keine Tankvorgänge gefunden",
                              message: "Passe die Filter an oder erfasse einen neuen Tankvorgang.")
                        .listRowInsets(EdgeInsets(top: 4, leading: 16, bottom: 8, trailing: 16))
                        .listRowBackground(Color.clear)
                        .listRowSeparator(.hidden)
                }
            }

            ForEach(rows) { row in
                entryCard(row)
                    .listRowInsets(EdgeInsets(top: 5, leading: 16, bottom: 5, trailing: 16))
                    .listRowBackground(Color.clear)
                    .listRowSeparator(.hidden)
                    .swipeActions(edge: .trailing, allowsFullSwipe: false) {
                        Button(role: .destructive) {
                            deleteCandidate = row.entry
                        } label: {
                            Label("Löschen", systemImage: "trash")
                        }
                        Button {
                            store.openEdit(row.entry)
                        } label: {
                            Label("Bearbeiten", systemImage: "pencil")
                        }
                        .tint(Theme.accent)
                    }
            }
        }
        .listStyle(.plain)
        .scrollContentBackground(.hidden)
        .background(Theme.bg)
        .navigationTitle("Verlauf")
        .navigationBarTitleDisplayMode(.large)
        .refreshable { await store.refresh() }
        .confirmationDialog("Tankvorgang löschen?",
                            isPresented: Binding(get: { deleteCandidate != nil },
                                                 set: { if !$0 { deleteCandidate = nil } }),
                            titleVisibility: .visible) {
            Button("Löschen", role: .destructive) {
                if let e = deleteCandidate {
                    Task { await store.deleteEntry(e.id) }
                }
                deleteCandidate = nil
            }
            Button("Abbrechen", role: .cancel) { deleteCandidate = nil }
        } message: {
            Text("Der Eintrag wird dauerhaft entfernt.")
        }
        .sheet(item: Binding(get: { store.editEntry }, set: { store.editEntry = $0 })) { _ in
            EditEntrySheet()
        }
    }

    private func entryCard(_ row: Row) -> some View {
        let e = row.entry
        return VStack(alignment: .leading, spacing: 7) {
            HStack(spacing: 9) {
                Circle().fill(row.dot).frame(width: 10, height: 10)
                Text(fmtDate(e.datum)).font(.plex(14.5, .bold)).monospacedDigit().foregroundStyle(Theme.text)
                Text(row.vehName).font(.plex(13)).foregroundStyle(Theme.text3)
                Spacer()
                if let verb = e.verbrauch {
                    Text("\(fmt(verb, 1)) l/100 km")
                        .font(.plex(12.5, .bold))
                        .monospacedDigit()
                        .foregroundStyle(row.verbColor)
                        .padding(.horizontal, 10)
                        .padding(.vertical, 3)
                        .background(Capsule().fill(Theme.card2))
                }
            }
            HStack(spacing: 16) {
                stat(fmt(e.liter, 2), "l")
                stat(fmt(e.gesamtpreis, 2), "€")
                stat(fmt(e.preisProLiter, 3), "€/l")
                stat(fmt(e.kilometerstand, 0), "km")
            }
            if !row.meta.isEmpty {
                Text(row.meta).font(.plex(13)).foregroundStyle(Theme.text3).lineLimit(1)
            }
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 13)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(RoundedRectangle(cornerRadius: 14, style: .continuous).fill(Theme.card))
        .overlay(RoundedRectangle(cornerRadius: 14, style: .continuous).stroke(Theme.border, lineWidth: 1))
        .shadow(color: .black.opacity(0.04), radius: 5, x: 0, y: 2)
    }

    private func stat(_ value: String, _ unit: String) -> some View {
        (Text(value).font(.plex(13.5, .semibold)).foregroundStyle(Theme.text)
         + Text(" \(unit)").font(.plex(13.5)).foregroundStyle(Theme.text2))
            .monospacedDigit()
    }
}

// Bearbeiten-Sheet – Gegenstück zu frontend/src/modals/EditModal.tsx.
struct EditEntrySheet: View {
    @Environment(AppStore.self) private var store

    var body: some View {
        @Bindable var store = store
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 12) {
                    VStack(alignment: .leading, spacing: 6) {
                        FieldLabel(label: "Datum")
                        DatePicker("", selection: $store.editForm.datum, displayedComponents: .date)
                            .labelsHidden()
                    }
                    HStack(alignment: .top, spacing: 11) {
                        trioField("Liter", field: "liter", text: store.editForm.liter)
                        trioField("Preis pro Liter (€/l)", field: "ppl", text: store.editForm.ppl)
                    }
                    HStack(alignment: .top, spacing: 11) {
                        trioField("Gesamtpreis (€)", field: "total", text: store.editForm.total)
                        VStack(alignment: .leading, spacing: 6) {
                            FieldLabel(label: "Kilometerstand")
                            TbField(placeholder: "", text: $store.editForm.km, keyboard: .numberPad)
                        }
                    }
                    VStack(alignment: .leading, spacing: 6) {
                        FieldLabel(label: "Tankstelle")
                        TbField(placeholder: "", text: $store.editForm.tankstelle)
                    }
                    VStack(alignment: .leading, spacing: 6) {
                        FieldLabel(label: "Notiz")
                        TbField(placeholder: "", text: $store.editForm.notiz)
                    }
                    Toggle(isOn: $store.editForm.voll) {
                        Text("Volltankung").font(.plex(14, .semibold)).foregroundStyle(Theme.text)
                    }
                    .padding(.top, 4)
                }
                .padding(20)
            }
            .background(Theme.bg)
            .navigationTitle("Tankvorgang bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Abbrechen") { store.editEntry = nil }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Speichern") { Task { await store.saveEdit() } }
                        .fontWeight(.bold)
                }
            }
        }
        .presentationDetents([.medium, .large])
        .presentationDragIndicator(.visible)
    }

    private func trioField(_ label: String, field: String, text: String) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            FieldLabel(label: label, pill: store.autoCalc && store.derivedE == field)
            TbField(placeholder: "",
                    text: Binding(get: { text }, set: { store.onTrioE(field, $0) }),
                    keyboard: .decimalPad)
        }
    }
}
