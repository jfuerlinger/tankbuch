import SwiftUI
import UniformTypeIdentifiers

// Einstellungen – Nachbau von frontend/src/screens/Einstellungen.tsx.
struct EinstellungenView: View {
    @Environment(AppStore.self) private var store
    @State private var importerShown = false
    @State private var exportURL: URL?

    var body: some View {
        @Bindable var store = store
        ScrollView {
            VStack(alignment: .leading, spacing: 14) {
                // Darstellung
                VStack(alignment: .leading, spacing: 12) {
                    Text("Darstellung").font(.plex(14, .bold)).foregroundStyle(Theme.text)
                    Picker("Darstellung", selection: $store.theme) {
                        ForEach(AppTheme.allCases) { t in
                            Text(t.label).tag(t)
                        }
                    }
                    .pickerStyle(.segmented)
                    Text("„System“ folgt automatisch dem Hell-/Dunkelmodus deines Geräts.")
                        .font(.plex(12.5)).foregroundStyle(Theme.text3)
                }
                .frame(maxWidth: .infinity, alignment: .leading)
                .card(padding: 17)

                // Daten
                VStack(alignment: .leading, spacing: 12) {
                    Text("Daten").font(.plex(14, .bold)).foregroundStyle(Theme.text)
                    HStack(spacing: 10) {
                        Button {
                            Task { exportURL = await store.exportCsv() }
                        } label: {
                            HStack(spacing: 8) {
                                Image(systemName: "square.and.arrow.down").font(.system(size: 13, weight: .semibold))
                                Text("CSV exportieren")
                            }
                        }
                        .buttonStyle(AccentButtonStyle(fontSize: 13.5, horizontalPadding: 16, verticalPadding: 11))

                        Button {
                            importerShown = true
                        } label: {
                            HStack(spacing: 8) {
                                Image(systemName: "square.and.arrow.up").font(.system(size: 13, weight: .semibold))
                                Text("CSV importieren")
                            }
                        }
                        .buttonStyle(SoftButtonStyle())
                    }
                    Text("Semikolon-getrennt, Dezimal-Komma, UTF-8. Der Export dient zugleich als Backup – der Import stellt Daten wieder her bzw. führt sie zusammen.")
                        .font(.plex(12.5)).foregroundStyle(Theme.text3)
                }
                .frame(maxWidth: .infinity, alignment: .leading)
                .card(padding: 17)

                // Konto
                VStack(alignment: .leading, spacing: 11) {
                    Text("Konto").font(.plex(14, .bold)).foregroundStyle(Theme.text)
                    HStack {
                        Text("Angemeldet als").font(.plex(13.5)).foregroundStyle(Theme.text2)
                        Spacer()
                        Text(store.email).font(.plex(13.5, .semibold)).foregroundStyle(Theme.text).lineLimit(1)
                    }
                    HStack {
                        Text("Mandant").font(.plex(13.5)).foregroundStyle(Theme.text2)
                        Spacer()
                        Text(store.tenantName).font(.plex(13.5, .semibold)).foregroundStyle(Theme.text)
                    }
                    Divider().overlay(Theme.border)
                    Button {
                        store.logout()
                    } label: {
                        HStack(spacing: 9) {
                            Image(systemName: "rectangle.portrait.and.arrow.right")
                                .font(.system(size: 13, weight: .medium))
                            Text("Abmelden").font(.plex(13.5, .semibold))
                        }
                        .foregroundStyle(Theme.bad)
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 11)
                        .overlay(RoundedRectangle(cornerRadius: 11, style: .continuous).stroke(Theme.border, lineWidth: 1))
                    }
                    .buttonStyle(.plain)
                }
                .frame(maxWidth: .infinity, alignment: .leading)
                .card(padding: 17)
            }
            .padding(16)
            .frame(maxWidth: 620)
            .frame(maxWidth: .infinity)
        }
        .background(Theme.bg)
        .navigationTitle("Einstellungen")
        .navigationBarTitleDisplayMode(.large)
        .fileImporter(isPresented: $importerShown,
                      allowedContentTypes: [.commaSeparatedText, .plainText, .text]) { result in
            if case .success(let url) = result {
                Task { await store.importCsv(from: url) }
            }
        }
        .sheet(item: Binding(get: { exportURL.map(ShareFile.init) },
                             set: { exportURL = $0?.url })) { file in
            ShareSheet(items: [file.url])
                .presentationDetents([.medium, .large])
        }
    }
}

private struct ShareFile: Identifiable {
    let url: URL
    var id: URL { url }
}
