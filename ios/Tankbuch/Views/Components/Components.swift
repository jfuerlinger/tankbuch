import SwiftUI

// Gemeinsame Bausteine – Gegenstücke zu frontend/src/ui/components.tsx & styles.tsx.

// ---------- Chips ----------

struct ChipButton: View {
    let label: String
    let active: Bool
    var dotColor: Color?
    var compact = false
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack(spacing: 7) {
                if let dotColor {
                    Circle().fill(dotColor).frame(width: 9, height: 9)
                }
                Text(label)
                    .font(.plex(compact ? 12.5 : 13.5, .semibold))
            }
            .padding(.horizontal, compact ? 12 : 14)
            .padding(.vertical, compact ? 6 : 8)
            .foregroundStyle(active ? Theme.text : Theme.text2)
            .background(
                Capsule().fill(active ? (dotColor ?? Theme.accent).mix(0.16, with: Theme.card) : Theme.card)
            )
            .overlay(
                Capsule().stroke(active ? (dotColor ?? Theme.accent) : Theme.border, lineWidth: 1)
            )
        }
        .buttonStyle(.plain)
    }
}

/// „Alle Fahrzeuge" + ein Chip pro Fahrzeug (horizontal scrollbar).
struct VehChips: View {
    @Environment(AppStore.self) private var store
    let sel: UUID?
    let onSelect: (UUID?) -> Void

    var body: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                ChipButton(label: "Alle Fahrzeuge", active: sel == nil) { onSelect(nil) }
                ForEach(store.vehicles) { v in
                    ChipButton(label: v.name, active: sel == v.id, dotColor: Color(hex: v.farbe)) {
                        onSelect(v.id)
                    }
                }
            }
            .padding(.horizontal, 1)
            .padding(.vertical, 2)
        }
    }
}

struct RangeChips: View {
    let sel: String
    let onSelect: (String) -> Void

    private let ranges: [(String, String)] = [
        ("all", "Gesamter Zeitraum"), ("30", "30 Tage"), ("90", "3 Monate"), ("365", "12 Monate"),
    ]

    var body: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                ForEach(ranges, id: \.0) { key, label in
                    ChipButton(label: label, active: sel == key, compact: true) { onSelect(key) }
                }
            }
            .padding(.horizontal, 1)
            .padding(.vertical, 2)
        }
    }
}

// ---------- KPI / Chart-Karten ----------

struct KpiCard: View {
    let label: String
    let value: String
    let unit: String
    var sub: String?
    var big: CGFloat = 23

    var body: some View {
        VStack(alignment: .leading, spacing: 3) {
            Text(label)
                .font(.plex(12, .semibold))
                .foregroundStyle(Theme.text2)
            HStack(alignment: .firstTextBaseline, spacing: 4) {
                Text(value)
                    .font(.plex(big, .bold))
                    .monospacedDigit()
                    .foregroundStyle(Theme.text)
                if !unit.isEmpty {
                    Text(unit)
                        .font(.plex(12.5, .semibold))
                        .foregroundStyle(Theme.text3)
                }
            }
            .lineLimit(1)
            .minimumScaleFactor(0.7)
            if let sub {
                Text(sub)
                    .font(.plex(12))
                    .foregroundStyle(Theme.text3)
                    .lineLimit(1)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(.horizontal, 16)
        .padding(.vertical, 14)
        .background(RoundedRectangle(cornerRadius: 16, style: .continuous).fill(Theme.card))
        .overlay(RoundedRectangle(cornerRadius: 16, style: .continuous).stroke(Theme.border, lineWidth: 1))
        .shadow(color: .black.opacity(0.05), radius: 6, x: 0, y: 2)
    }
}

struct ChartCard<Content: View, Legend: View>: View {
    let title: String
    let unit: String
    @ViewBuilder var content: Content
    @ViewBuilder var legend: Legend

    init(title: String, unit: String,
         @ViewBuilder content: () -> Content,
         @ViewBuilder legend: () -> Legend = { EmptyView() }) {
        self.title = title
        self.unit = unit
        self.content = content()
        self.legend = legend()
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack(alignment: .firstTextBaseline) {
                Text(title).font(.plex(14, .bold)).foregroundStyle(Theme.text)
                Spacer()
                Text(unit).font(.plex(12)).foregroundStyle(Theme.text3)
            }
            content
            legend
        }
        .padding(EdgeInsets(top: 16, leading: 18, bottom: 12, trailing: 18))
        .background(RoundedRectangle(cornerRadius: 16, style: .continuous).fill(Theme.card))
        .overlay(RoundedRectangle(cornerRadius: 16, style: .continuous).stroke(Theme.border, lineWidth: 1))
        .shadow(color: .black.opacity(0.05), radius: 6, x: 0, y: 2)
    }
}

struct LegendView: View {
    let items: [(color: String, label: String)]

    var body: some View {
        FlowLayoutHStack {
            ForEach(items.indices, id: \.self) { i in
                HStack(spacing: 6) {
                    RoundedRectangle(cornerRadius: 2)
                        .fill(Color(hex: items[i].color))
                        .frame(width: 12, height: 3)
                    Text(items[i].label).font(.plex(12)).foregroundStyle(Theme.text2)
                }
            }
        }
        .padding(.top, 4)
    }
}

/// Einfache umbruchsfähige HStack-Alternative für die Legende.
struct FlowLayoutHStack<Content: View>: View {
    @ViewBuilder var content: Content

    var body: some View {
        HStack(spacing: 14) { content }
            .frame(maxWidth: .infinity, alignment: .leading)
    }
}

// ---------- Empty-State ----------

struct EmptyCard: View {
    var showLogo = false
    let title: String
    let message: String
    var buttonLabel: String?
    var buttonAction: (() -> Void)?

    var body: some View {
        VStack(spacing: 10) {
            if showLogo { LogoOutlineView() }
            Text(title).font(.plex(16, .bold)).foregroundStyle(Theme.text)
            Text(message)
                .font(.plex(13.5))
                .foregroundStyle(Theme.text2)
                .multilineTextAlignment(.center)
                .frame(maxWidth: 340)
            if let buttonLabel, let buttonAction {
                Button(action: buttonAction) {
                    Text(buttonLabel).font(.plex(14, .bold))
                }
                .buttonStyle(AccentButtonStyle())
                .padding(.top, 8)
            }
        }
        .frame(maxWidth: .infinity)
        .padding(.horizontal, 24)
        .padding(.vertical, 40)
        .background(RoundedRectangle(cornerRadius: 18, style: .continuous).fill(Theme.card))
        .overlay(RoundedRectangle(cornerRadius: 18, style: .continuous).stroke(Theme.border, lineWidth: 1))
        .shadow(color: .black.opacity(0.05), radius: 6, x: 0, y: 2)
    }
}

// ---------- Buttons ----------

/// Amber-Akzentbutton wie `accentBtn` im Web.
struct AccentButtonStyle: ButtonStyle {
    var fontSize: CGFloat = 14.5
    var horizontalPadding: CGFloat = 18
    var verticalPadding: CGFloat = 12

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.plex(fontSize, .bold))
            .foregroundStyle(Theme.accentInk)
            .padding(.horizontal, horizontalPadding)
            .padding(.vertical, verticalPadding)
            .background(RoundedRectangle(cornerRadius: 12, style: .continuous).fill(Theme.accent))
            .opacity(configuration.isPressed ? 0.8 : 1)
            .shadow(color: Theme.accent.opacity(0.3), radius: 6, x: 0, y: 3)
    }
}

/// Dezente Sekundär-Schaltfläche (card2-Hintergrund, Border) wie die „CSV importieren"-Fläche.
struct SoftButtonStyle: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.plex(13.5, .semibold))
            .foregroundStyle(Theme.text)
            .padding(.horizontal, 16)
            .padding(.vertical, 11)
            .background(RoundedRectangle(cornerRadius: 11, style: .continuous).fill(Theme.card2))
            .overlay(RoundedRectangle(cornerRadius: 11, style: .continuous).stroke(Theme.border, lineWidth: 1))
            .opacity(configuration.isPressed ? 0.7 : 1)
    }
}

// ---------- Eingabefelder ----------

/// TextField im Web-Input-Stil (bg-Hintergrund, Border, Radius 10).
struct TbField: View {
    let placeholder: String
    @Binding var text: String
    var keyboard: UIKeyboardType = .default

    var body: some View {
        TextField(placeholder, text: $text)
            .font(.plex(15))
            .monospacedDigit()
            .keyboardType(keyboard)
            .autocorrectionDisabled()
            .textInputAutocapitalization(keyboard == .default ? .sentences : .never)
            .padding(.horizontal, 12)
            .padding(.vertical, 11)
            .background(RoundedRectangle(cornerRadius: 10, style: .continuous).fill(Theme.bg))
            .overlay(RoundedRectangle(cornerRadius: 10, style: .continuous).stroke(Theme.border, lineWidth: 1))
            .foregroundStyle(Theme.text)
    }
}

/// Feld-Label mit optionalem Hinweis und „berechnet"-Pill.
struct FieldLabel: View {
    let label: String
    var note: String?
    var pill = false

    var body: some View {
        HStack(spacing: 7) {
            (Text(label).font(.plex(12, .semibold)).foregroundStyle(Theme.text2)
             + Text(note.map { " \($0)" } ?? "").font(.plex(12)).foregroundStyle(Theme.text3))
            if pill {
                Text("berechnet")
                    .font(.plex(10.5, .bold))
                    .foregroundStyle(Theme.accentText)
                    .padding(.horizontal, 7)
                    .padding(.vertical, 2)
                    .background(Capsule().fill(Theme.accent.opacity(0.15)))
            }
        }
    }
}

// ---------- Toast ----------

struct ToastModifier: ViewModifier {
    @Environment(AppStore.self) private var store

    func body(content: Content) -> some View {
        content.overlay(alignment: .bottom) {
            if let msg = store.toastMsg {
                Text(msg)
                    .font(.plex(13.5, .semibold))
                    .foregroundStyle(Theme.bg)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal, 18)
                    .padding(.vertical, 11)
                    .background(RoundedRectangle(cornerRadius: 12, style: .continuous).fill(Theme.text))
                    .shadow(color: .black.opacity(0.25), radius: 16, x: 0, y: 8)
                    .padding(.horizontal, 24)
                    .padding(.bottom, 76) // über der Tab-Bar, wie im Web (bottom: 96)
                    .transition(.move(edge: .bottom).combined(with: .opacity))
            }
        }
        .animation(.easeOut(duration: 0.25), value: store.toastMsg)
    }
}

extension View {
    func tbToast() -> some View { modifier(ToastModifier()) }
}
