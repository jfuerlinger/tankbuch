import Foundation

// Auswertung – Port von frontend/src/lib/calc.ts (kpis, Serien, computeTrio).
// Der Verbrauch pro Eintrag kommt bereits berechnet von der API (Tankvorgang.verbrauch).

struct Kpi {
    var cost = 0.0
    var liters = 0.0
    var km: Int64 = 0
    var count = 0
    var last: Tankvorgang?
    var verbrauch: Double?
    var ppl: Double?
    var costKm: Double?
    var costMonth: Double?
}

/// Sortierung wie die Web-Vorlage: Kilometerstand aufsteigend, bei Gleichstand Datum.
private func sortByKm(_ a: Tankvorgang, _ b: Tankvorgang) -> Bool {
    a.kilometerstand != b.kilometerstand ? a.kilometerstand < b.kilometerstand : a.datum < b.datum
}

func kpis(vehicles: [Fahrzeug], entries: [Tankvorgang], sel: UUID?) -> Kpi {
    let vs = sel == nil ? vehicles : vehicles.filter { $0.id == sel }
    var k = Kpi()
    var segL = 0.0
    var segKm: Int64 = 0
    var minT: String?
    var maxT: String?

    for v in vs {
        let es = entries.filter { $0.fahrzeugId == v.id }.sorted(by: sortByKm)
        guard let lastEntry = es.last else { continue }
        k.km += max(0, lastEntry.kilometerstand - v.anfangsKilometer)
        var prevFull: Int64?
        var sum = 0.0
        for e in es {
            k.cost += e.gesamtpreis
            k.liters += e.liter
            k.count += 1
            sum += e.liter
            if e.volltankung {
                if let pf = prevFull, e.kilometerstand > pf {
                    segL += sum
                    segKm += e.kilometerstand - pf
                }
                prevFull = e.kilometerstand
                sum = 0
            }
            if k.last == nil || e.datum > k.last!.datum { k.last = e }
            if minT == nil || e.datum < minT! { minT = e.datum }
            if maxT == nil || e.datum > maxT! { maxT = e.datum }
        }
    }

    var months = 1.0
    if let minT, let maxT, let d0 = dateFromISO(minT), let d1 = dateFromISO(maxT) {
        months = max(1, d1.timeIntervalSince(d0) / 86400 / 30.44)
    }
    k.verbrauch = segKm > 0 ? (segL / Double(segKm)) * 100 : nil
    k.ppl = k.liters > 0 ? k.cost / k.liters : nil
    k.costKm = k.km > 0 ? k.cost / Double(k.km) : nil
    k.costMonth = k.count > 0 ? k.cost / months : nil
    return k
}

// ---------- Diagramm-Daten (Swift Charts) ----------

struct LinePoint: Identifiable {
    let id = UUID()
    let series: String
    let colorHex: String
    let date: Date
    let value: Double
}

/// Verbrauchskurve: nur Einträge mit berechnetem Verbrauch (Volltankung → Volltankung).
func verbPoints(vehicles: [Fahrzeug], entries: [Tankvorgang], sel: UUID?) -> [LinePoint] {
    linePoints(vehicles: vehicles, entries: entries, sel: sel) { e in e.verbrauch }
}

func pplPoints(vehicles: [Fahrzeug], entries: [Tankvorgang], sel: UUID?) -> [LinePoint] {
    linePoints(vehicles: vehicles, entries: entries, sel: sel) { e in e.preisProLiter }
}

private func linePoints(vehicles: [Fahrzeug], entries: [Tankvorgang], sel: UUID?,
                        value: (Tankvorgang) -> Double?) -> [LinePoint] {
    let vs = sel == nil ? vehicles : vehicles.filter { $0.id == sel }
    var pts: [LinePoint] = []
    for v in vs {
        for e in entries where e.fahrzeugId == v.id {
            guard let val = value(e), let d = dateFromISO(e.datum) else { continue }
            pts.append(LinePoint(series: v.name, colorHex: v.farbe, date: d, value: val))
        }
    }
    return pts.sorted { $0.date < $1.date }
}

struct MonthCost: Identifiable {
    let id = UUID()
    let monthKey: String   // "2026-07"
    let label: String      // "Juli" kurz, z. B. "Jul."
    let series: String
    let colorHex: String
    let value: Double
    var isTop = false      // oberstes Segment → trägt das Summen-Label
    var total = 0.0
}

/// Kosten pro Monat, gestapelt nach Fahrzeug – letzte 9 Monate wie die Web-Vorlage.
func monthlyCosts(vehicles: [Fahrzeug], entries: [Tankvorgang], sel: UUID?) -> (segments: [MonthCost], monthOrder: [String]) {
    let es = entries.filter { sel == nil || $0.fahrzeugId == sel }
    guard !es.isEmpty else { return ([], []) }

    var map: [String: [UUID: Double]] = [:]
    for e in es {
        let key = String(e.datum.prefix(7))
        map[key, default: [:]][e.fahrzeugId, default: 0] += e.gesamtpreis
    }
    let keys = map.keys.sorted().suffix(9)

    let monthFormatter = DateFormatter()
    monthFormatter.locale = Locale(identifier: "de_AT")
    monthFormatter.dateFormat = "MMM"

    var segments: [MonthCost] = []
    var order: [String] = []
    for key in keys {
        let label = dateFromISO(key + "-15").map { monthFormatter.string(from: $0) } ?? key
        order.append(label)
        let byVeh = map[key]!
        let total = byVeh.values.reduce(0, +)
        var monthSegs: [MonthCost] = []
        for v in vehicles {
            guard let val = byVeh[v.id], val > 0 else { continue }
            monthSegs.append(MonthCost(monthKey: key, label: label, series: v.name,
                                       colorHex: v.farbe, value: val, total: total))
        }
        if !monthSegs.isEmpty { monthSegs[monthSegs.count - 1].isTop = true }
        segments.append(contentsOf: monthSegs)
    }
    return (segments, order)
}

// ---------- Trio-Auto-Berechnung (Liter · €/l · Gesamtpreis) ----------

struct Trio {
    var liter: String
    var ppl: String
    var total: String
}

/// Port von computeTrio: Die zwei zuletzt bearbeiteten Felder bestimmen das dritte.
func computeTrio(_ input: Trio, order: [String]) -> (trio: Trio, derived: String?) {
    let fields = ["liter", "ppl", "total"]
    guard order.count >= 2 else { return (input, nil) }
    let derived = fields.first { !order.contains($0) }

    func get(_ f: String) -> String {
        switch f { case "liter": return input.liter; case "ppl": return input.ppl; default: return input.total }
    }
    guard let a = parseDe(get(order[0])), a > 0,
          let b = parseDe(get(order[1])), b > 0 else { return (input, derived) }

    var v: [String: Double] = [:]
    v[order[0]] = a
    v[order[1]] = b

    var out: String?
    switch derived {
    case "ppl" where (v["liter"] ?? 0) > 0: out = fmt(v["total"]! / v["liter"]!, 3)
    case "total": out = fmt(v["liter"]! * v["ppl"]!, 2)
    case "liter" where (v["ppl"] ?? 0) > 0: out = fmt(v["total"]! / v["ppl"]!, 2)
    default: break
    }

    var result = input
    if let out, let derived {
        switch derived {
        case "liter": result.liter = out
        case "ppl": result.ppl = out
        default: result.total = out
        }
    }
    return (result, derived)
}
