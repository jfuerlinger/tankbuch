import SwiftUI

// Logo: Kraftstoff-Tropfen mit ansteigenden Balken im Negativraum – Nachbau des Web-SVGs (48×48).
struct LogoView: View {
    var size: CGFloat = 34
    var drop: Color = Theme.accent
    var inner: Color = Theme.card

    var body: some View {
        ZStack {
            DropShape().fill(drop)
            bar(x: 15.5, y: 28.5, h: 8.5)
            bar(x: 21.8, y: 24.5, h: 12.5)
            bar(x: 28.1, y: 20.5, h: 16.5)
        }
        .frame(width: size, height: size)
    }

    private func bar(x: CGFloat, y: CGFloat, h: CGFloat) -> some View {
        let s = size / 48
        return RoundedRectangle(cornerRadius: 1.8 * s, style: .continuous)
            .fill(inner)
            .frame(width: 4.4 * s, height: h * s)
            .position(x: (x + 2.2) * s, y: (y + h / 2) * s)
    }
}

struct LogoOutlineView: View {
    var size: CGFloat = 44

    var body: some View {
        DropShape()
            .fill(Theme.card2)
            .overlay(DropShape().stroke(Theme.text3, lineWidth: 1.5))
            .frame(width: size, height: size)
            .opacity(0.9)
    }
}

/// Tropfen-Pfad aus dem Web-SVG (viewBox 0 0 48 48).
struct DropShape: Shape {
    func path(in rect: CGRect) -> Path {
        let s = rect.width / 48
        var p = Path()
        p.move(to: CGPoint(x: 24 * s, y: 3.5 * s))
        p.addCurve(to: CGPoint(x: 9.5 * s, y: 29.5 * s),
                   control1: CGPoint(x: 24 * s, y: 3.5 * s),
                   control2: CGPoint(x: 9.5 * s, y: 19.5 * s))
        p.addCurve(to: CGPoint(x: 24 * s, y: 44 * s),
                   control1: CGPoint(x: 9.5 * s, y: 37.5 * s),
                   control2: CGPoint(x: 16 * s, y: 44 * s))
        p.addCurve(to: CGPoint(x: 38.5 * s, y: 29.5 * s),
                   control1: CGPoint(x: 32 * s, y: 44 * s),
                   control2: CGPoint(x: 38.5 * s, y: 37.5 * s))
        p.addCurve(to: CGPoint(x: 24 * s, y: 3.5 * s),
                   control1: CGPoint(x: 38.5 * s, y: 19.5 * s),
                   control2: CGPoint(x: 24 * s, y: 3.5 * s))
        p.closeSubpath()
        return p
    }
}
