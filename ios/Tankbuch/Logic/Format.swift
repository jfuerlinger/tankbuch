import Foundation

// de-AT-Formatierung – Port von frontend/src/lib/format.ts (fmt / fmtDate / parseDe).

private let deAT = Locale(identifier: "de_AT")

private let numberFormatters: [Int: NumberFormatter] = {
    var dict: [Int: NumberFormatter] = [:]
    for d in 0...3 {
        let f = NumberFormatter()
        f.locale = deAT
        f.numberStyle = .decimal
        f.minimumFractionDigits = d
        f.maximumFractionDigits = d
        dict[d] = f
    }
    return dict
}()

private let isoDateFormatter: DateFormatter = {
    let f = DateFormatter()
    f.locale = Locale(identifier: "en_US_POSIX")
    f.timeZone = .current
    f.dateFormat = "yyyy-MM-dd"
    return f
}()

func fmt(_ n: Double?, _ d: Int) -> String {
    guard let n, n.isFinite else { return "—" }
    let f = numberFormatters[max(0, min(3, d))]!
    return f.string(from: NSNumber(value: n)) ?? "—"
}

func fmt(_ n: Int64?, _ d: Int = 0) -> String {
    guard let n else { return "—" }
    return fmt(Double(n), d)
}

/// "2026-07-08" → "08.07.2026"
func fmtDate(_ iso: String) -> String {
    guard let date = isoDateFormatter.date(from: iso) else { return iso }
    let f = DateFormatter()
    f.locale = deAT
    f.dateFormat = "dd.MM.yyyy"
    return f.string(from: date)
}

/// "1.234,56" / "1234.56" → 1234.56 (Dezimal-Komma bevorzugt, wie die Web-Vorlage)
func parseDe(_ s: String?) -> Double? {
    guard var t = s?.trimmingCharacters(in: .whitespaces), !t.isEmpty else { return nil }
    // Gruppierungs-Leerzeichen entfernen (de-AT formatiert je nach CLDR "54 696").
    t = t.replacingOccurrences(of: #"\s"#, with: "", options: .regularExpression) // inkl. U+00A0/U+202F
    guard !t.isEmpty else { return nil }
    if t.contains(",") {
        t = t.replacingOccurrences(of: ".", with: "").replacingOccurrences(of: ",", with: ".")
    } else if t.range(of: #"^\d{1,3}(\.\d{3})+$"#, options: .regularExpression) != nil {
        // Reine Tausender-Gruppierung ohne Komma (z. B. "54.696" aus fmt(km, 0)):
        // Punkte sind Gruppierung, kein Dezimalpunkt.
        t = t.replacingOccurrences(of: ".", with: "")
    }
    guard let n = Double(t), n.isFinite else { return nil }
    return n
}

func isoString(from date: Date) -> String { isoDateFormatter.string(from: date) }

func dateFromISO(_ iso: String) -> Date? { isoDateFormatter.date(from: iso) }

func todayISO() -> String { isoString(from: Date()) }
