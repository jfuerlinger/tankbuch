import SwiftUI

// Mehr – Nachbau von frontend/src/screens/Mehr.tsx (Profil, Fahrzeuge, Einstellungen, Abmelden).
struct MehrView: View {
    @Environment(AppStore.self) private var store

    var body: some View {
        ScrollView {
            VStack(spacing: 14) {
                profileCard

                VStack(spacing: 0) {
                    NavigationLink { FahrzeugeView() } label: {
                        row(icon: "car", label: "Fahrzeuge")
                    }
                    Divider().overlay(Theme.border).padding(.leading, 47)
                    NavigationLink { EinstellungenView() } label: {
                        row(icon: "gearshape", label: "Einstellungen")
                    }
                }
                .background(RoundedRectangle(cornerRadius: 16, style: .continuous).fill(Theme.card))
                .overlay(RoundedRectangle(cornerRadius: 16, style: .continuous).stroke(Theme.border, lineWidth: 1))
                .clipShape(RoundedRectangle(cornerRadius: 16, style: .continuous))
                .shadow(color: .black.opacity(0.05), radius: 6, x: 0, y: 2)

                Button {
                    store.logout()
                } label: {
                    HStack(spacing: 9) {
                        Image(systemName: "rectangle.portrait.and.arrow.right")
                            .font(.system(size: 14, weight: .medium))
                        Text("Abmelden").font(.plex(14, .semibold))
                    }
                    .foregroundStyle(Theme.bad)
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 14)
                    .background(RoundedRectangle(cornerRadius: 14, style: .continuous).fill(Theme.card))
                    .overlay(RoundedRectangle(cornerRadius: 14, style: .continuous).stroke(Theme.border, lineWidth: 1))
                }
                .buttonStyle(.plain)
            }
            .padding(16)
        }
        .background(Theme.bg)
        .navigationTitle("Mehr")
        .navigationBarTitleDisplayMode(.large)
    }

    private var profileCard: some View {
        HStack(spacing: 13) {
            Circle()
                .fill(Theme.accent.mix(0.18, with: Theme.card))
                .frame(width: 44, height: 44)
                .overlay(
                    Text(String(store.email.first ?? "D").uppercased())
                        .font(.plex(17, .bold))
                        .foregroundStyle(Theme.accentText)
                )
            VStack(alignment: .leading, spacing: 2) {
                Text(store.email).font(.plex(14.5, .bold)).foregroundStyle(Theme.text).lineLimit(1)
                Text("Mandant: \(store.tenantName)").font(.plex(12.5)).foregroundStyle(Theme.text3)
            }
            Spacer()
        }
        .card(padding: 16)
    }

    private func row(icon: String, label: String) -> some View {
        HStack(spacing: 12) {
            Image(systemName: icon)
                .font(.system(size: 17))
                .foregroundStyle(Theme.text2)
                .frame(width: 22)
            Text(label).font(.plex(14.5, .semibold)).foregroundStyle(Theme.text)
            Spacer()
            Image(systemName: "chevron.right")
                .font(.system(size: 13, weight: .semibold))
                .foregroundStyle(Theme.text3)
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 15)
        .contentShape(Rectangle())
    }
}
