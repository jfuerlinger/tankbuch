import SwiftUI
import PhotosUI

// Scan-Karte (Zapfsäule/Tacho) – Nachbau der Web-ScanCard inkl. Scan-Linie und „Erkannt"-Badge.
struct ScanCard: View {
    @Environment(AppStore.self) private var store
    let kind: ScanKind
    let title: String
    var subtitleNote: String?
    let subtitle: String
    let icon: String // SF Symbol
    let scanLabel: String

    @State private var cameraShown = false
    @State private var photoItem: PhotosPickerItem?
    @State private var scanPhase = false

    private var image: UIImage? { kind == .pump ? store.pumpImage : store.tachoImage }
    private var demo: Bool { kind == .pump ? store.pumpDemo : store.tachoDemo }
    private var scanning: Bool { store.scanning == kind }
    private var done: Bool { (kind == .pump ? store.recPump : store.recTacho) && !scanning }

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack(spacing: 10) {
                Image(systemName: icon)
                    .font(.system(size: 20, weight: .medium))
                    .foregroundStyle(Theme.accentText)
                VStack(alignment: .leading, spacing: 1) {
                    (Text(title).font(.plex(14, .bold)).foregroundStyle(Theme.text)
                     + Text(subtitleNote.map { " \($0)" } ?? "").font(.plex(14)).foregroundStyle(Theme.text3))
                    Text(subtitle).font(.plex(12)).foregroundStyle(Theme.text3)
                }
            }

            preview

            HStack(spacing: 8) {
                photoSourceButton
                Button("Demo") {
                    Task { await store.scan(kind, image: nil) }
                }
                .font(.plex(13, .semibold))
                .foregroundStyle(Theme.accentText)
                .padding(.horizontal, 6)
            }
        }
        .card(padding: 16)
        .fullScreenCover(isPresented: $cameraShown) {
            CameraPicker { img in
                if let img { Task { await store.scan(kind, image: img) } }
            }
            .ignoresSafeArea()
        }
        .onChange(of: photoItem) { _, item in
            guard let item else { return }
            photoItem = nil
            Task {
                if let data = try? await item.loadTransferable(type: Data.self),
                   let img = UIImage(data: data) {
                    await store.scan(kind, image: img)
                }
            }
        }
    }

    /// „Foto aufnehmen": Kamera direkt wenn vorhanden, sonst (Simulator) Fotobibliothek.
    @ViewBuilder private var photoSourceButton: some View {
        if CameraPicker.isAvailable {
            Menu {
                Button { cameraShown = true } label: { Label("Kamera", systemImage: "camera") }
                photoLibraryPicker
            } label: {
                photoButtonLabel
            }
        } else {
            PhotosPicker(selection: $photoItem, matching: .images) { photoButtonLabel }
        }
    }

    private var photoLibraryPicker: some View {
        PhotosPicker(selection: $photoItem, matching: .images) {
            Label("Fotobibliothek", systemImage: "photo.on.rectangle")
        }
    }

    private var photoButtonLabel: some View {
        HStack(spacing: 7) {
            Image(systemName: "camera")
                .font(.system(size: 13, weight: .semibold))
            Text("Foto aufnehmen").font(.plex(13.5, .semibold))
        }
        .foregroundStyle(Theme.text)
        .frame(maxWidth: .infinity)
        .padding(.vertical, 10)
        .background(RoundedRectangle(cornerRadius: 10, style: .continuous).fill(Theme.card2))
        .overlay(RoundedRectangle(cornerRadius: 10, style: .continuous).stroke(Theme.border, lineWidth: 1))
    }

    private var preview: some View {
        ZStack {
            RoundedRectangle(cornerRadius: 12, style: .continuous).fill(Theme.card2)

            if let image {
                Image(uiImage: image)
                    .resizable()
                    .scaledToFill()
            } else if demo {
                StripedBackground()
                Text("Demo-Foto").font(.plex(12, .semibold)).foregroundStyle(Theme.text3)
            } else {
                VStack(spacing: 6) {
                    Image(systemName: icon)
                        .font(.system(size: 20))
                        .foregroundStyle(Theme.text3)
                    Text("Noch kein Foto").font(.plex(12.5)).foregroundStyle(Theme.text3)
                }
            }

            if scanning {
                Theme.accent.opacity(0.1)
                GeometryReader { geo in
                    Rectangle()
                        .fill(Theme.accent)
                        .frame(height: 3)
                        .shadow(color: Theme.accent, radius: 7)
                        .offset(y: scanPhase ? geo.size.height - 5 : 2)
                        .animation(.easeInOut(duration: 0.8).repeatForever(autoreverses: true), value: scanPhase)
                }
                VStack {
                    Spacer()
                    Text(scanLabel)
                        .font(.plex(12, .semibold))
                        .foregroundStyle(Theme.bg)
                        .padding(.horizontal, 12)
                        .padding(.vertical, 5)
                        .background(Capsule().fill(Theme.text))
                        .padding(.bottom, 10)
                }
            }

            if done {
                VStack {
                    HStack {
                        Spacer()
                        HStack(spacing: 5) {
                            Image(systemName: "checkmark").font(.system(size: 10, weight: .bold))
                            Text("Erkannt").font(.plex(11.5, .bold))
                        }
                        .foregroundStyle(Color(hexValue: 0x0B1B10))
                        .padding(.horizontal, 10)
                        .padding(.vertical, 4)
                        .background(Capsule().fill(Theme.good))
                        .padding(8)
                    }
                    Spacer()
                }
            }
        }
        .aspectRatio(16 / 9, contentMode: .fit)
        .clipShape(RoundedRectangle(cornerRadius: 12, style: .continuous))
        .onAppear { scanPhase = true }
    }
}

/// Diagonale Streifen für den Demo-Platzhalter (repeating-linear-gradient der Web-Vorlage).
struct StripedBackground: View {
    var body: some View {
        Canvas { ctx, size in
            let stripe: CGFloat = 10
            var x: CGFloat = -size.height
            var toggle = false
            while x < size.width + size.height {
                if toggle {
                    var path = Path()
                    path.move(to: CGPoint(x: x, y: size.height))
                    path.addLine(to: CGPoint(x: x + size.height, y: 0))
                    path.addLine(to: CGPoint(x: x + size.height + stripe, y: 0))
                    path.addLine(to: CGPoint(x: x + stripe, y: size.height))
                    path.closeSubpath()
                    ctx.fill(path, with: .color(.gray.opacity(0.12)))
                }
                x += stripe
                toggle.toggle()
            }
        }
    }
}
