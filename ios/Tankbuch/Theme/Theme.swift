import SwiftUI
import UIKit

// Design-Tokens – 1:1 aus der Web-Vorlage (frontend/src/index.css), hell/dunkel dynamisch.
enum Theme {
    static let bg      = dyn(0xF6F7F9, 0x0F1115)
    static let card    = dyn(0xFFFFFF, 0x1A1D24)
    static let card2   = dyn(0xEFF1F4, 0x242833)
    static let border  = dyn(0xE5E7EB, 0x2A2E37)
    static let text    = dyn(0x151922, 0xF2F4F8)
    static let text2   = dyn(0x5B6472, 0x9AA3B0)
    static let text3   = dyn(0x8B93A1, 0x6B7482)
    static let accent  = Color(hexValue: 0xF59E0B)
    static let accentInk = Color(hexValue: 0x231903)
    // color-mix(accent 58 %, #3A2600) bzw. color-mix(accent 72 %, #FFFFFF) – vorberechnet.
    static let accentText = dyn(0xA66C06, 0xF8B94F)
    static let data1   = dyn(0x0D9488, 0x2DD4BF)
    static let data2   = dyn(0x3B82F6, 0x60A5FA)
    static let good    = dyn(0x15803D, 0x4ADE80)
    static let bad     = dyn(0xDC2626, 0xF87171)

    static func dyn(_ light: UInt32, _ dark: UInt32) -> Color {
        Color(UIColor { tc in
            tc.userInterfaceStyle == .dark ? UIColor(hexValue: dark) : UIColor(hexValue: light)
        })
    }
}

extension UIColor {
    convenience init(hexValue v: UInt32) {
        self.init(
            red: CGFloat((v >> 16) & 0xFF) / 255,
            green: CGFloat((v >> 8) & 0xFF) / 255,
            blue: CGFloat(v & 0xFF) / 255,
            alpha: 1)
    }
}

extension Color {
    init(hexValue v: UInt32) { self.init(UIColor(hexValue: v)) }

    /// Fahrzeugfarben kommen als "#RRGGBB"-Strings von der API.
    init(hex: String) {
        var s = hex.trimmingCharacters(in: .whitespaces)
        if s.hasPrefix("#") { s.removeFirst() }
        let v = UInt32(s, radix: 16) ?? 0x8B93A1
        self.init(hexValue: v)
    }

    /// Entspricht color-mix(in srgb, self N %, other) aus der Web-Vorlage.
    func mix(_ fraction: Double, with other: Color) -> Color {
        let a = UIColor(self), b = UIColor(other)
        return Color(UIColor { tc in
            var ar: CGFloat = 0, ag: CGFloat = 0, ab: CGFloat = 0, aa: CGFloat = 0
            var br: CGFloat = 0, bg: CGFloat = 0, bb: CGFloat = 0, ba: CGFloat = 0
            a.resolvedColor(with: tc).getRed(&ar, green: &ag, blue: &ab, alpha: &aa)
            b.resolvedColor(with: tc).getRed(&br, green: &bg, blue: &bb, alpha: &ba)
            let f = CGFloat(fraction)
            return UIColor(red: ar * f + br * (1 - f), green: ag * f + bg * (1 - f),
                           blue: ab * f + bb * (1 - f), alpha: aa * f + ba * (1 - f))
        })
    }
}

// IBM Plex Sans – gebündelt (siehe Resources/Fonts), Gewichte wie im Web (400/500/600/700).
enum PlexWeight: String {
    case regular = "IBMPlexSans"
    case medium = "IBMPlexSans-Medium"
    case semibold = "IBMPlexSans-SemiBold"
    case bold = "IBMPlexSans-Bold"
}

extension Font {
    static func plex(_ size: CGFloat, _ weight: PlexWeight = .regular) -> Font {
        .custom(weight.rawValue, size: size)
    }
}

// Karten-Stil wie die Web-Card (radius 16, Border, weicher Schatten).
struct CardModifier: ViewModifier {
    var padding: CGFloat
    var radius: CGFloat

    func body(content: Content) -> some View {
        content
            .padding(padding)
            .background(RoundedRectangle(cornerRadius: radius, style: .continuous).fill(Theme.card))
            .overlay(RoundedRectangle(cornerRadius: radius, style: .continuous).stroke(Theme.border, lineWidth: 1))
            .compositingGroup()
            .shadow(color: .black.opacity(0.06), radius: 7, x: 0, y: 3)
    }
}

extension View {
    func card(padding: CGFloat = 16, radius: CGFloat = 16) -> some View {
        modifier(CardModifier(padding: padding, radius: radius))
    }
}
