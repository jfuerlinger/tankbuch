import SwiftUI
import Observation

// Zentraler App-Zustand – Port von frontend/src/store.ts (zustand) auf @Observable.

enum AuthStep { case email, code, loggedIn }

enum AppTheme: String, CaseIterable, Identifiable {
    case system, light, dark
    var id: String { rawValue }
    var label: String {
        switch self { case .system: return "System"; case .light: return "Hell"; case .dark: return "Dunkel" }
    }
    var colorScheme: ColorScheme? {
        switch self { case .system: return nil; case .light: return .light; case .dark: return .dark }
    }
}

enum AppTab: Hashable { case dashboard, verlauf, erfassen, statistik, mehr }

enum ScanKind { case pump, tacho }

struct EntryForm {
    var fahrzeugId: UUID?
    var datum: Date = .now
    var liter = ""
    var ppl = ""
    var total = ""
    var km = ""
    var tankstelle = ""
    var notiz = ""
    var voll = true
}

struct VehicleForm {
    var name = ""
    var kz = ""
    var fuel = "Diesel"
    var color = "#3B82F6"
    var startKm = ""
}

@MainActor
@Observable
final class AppStore {
    // Auth
    var ready = false
    var authStep: AuthStep = .email
    var email = ""
    var emailInput = ""
    var codeInput = ""
    var tenantName = "Privat"
    var serverURLInput: String

    // Einstellungen
    var theme: AppTheme {
        didSet { UserDefaults.standard.set(theme.rawValue, forKey: "tb_theme") }
    }

    // Daten
    var vehicles: [Fahrzeug] = []
    var entries: [Tankvorgang] = []
    var busy = false

    // Navigation & Filter
    var tab: AppTab = .dashboard
    var dashVeh: UUID?
    var statVeh: UUID?
    var histVeh: UUID?
    var histRange = "all" // all | 30 | 90 | 365

    // Erfassen-Formular + Trio-Auto-Berechnung
    var form = EntryForm()
    var autoCalc = true
    var editOrder: [String] = []
    var derived: String?

    // Scan-Zustand (Zapfsäule/Tacho)
    var pumpImage: UIImage?
    var pumpDemo = false
    var tachoImage: UIImage?
    var tachoDemo = false
    var scanning: ScanKind?
    var recPump = false
    var recTacho = false

    // Bearbeiten-Sheet
    var editEntry: Tankvorgang?
    var editForm = EntryForm()
    var editOrderE: [String] = []
    var derivedE: String?

    // Fahrzeug-Sheet
    var vehSheet = false
    var vehId: UUID?
    var vehForm = VehicleForm()

    // Toast
    var toastMsg: String?
    private var toastTask: Task<Void, Never>?

    private let api: ApiClient

    init() {
        let defaults = UserDefaults.standard
        // UI-Tests starten immer abgemeldet und mit Standard-Server/-Theme.
        if CommandLine.arguments.contains("-uitest-reset") {
            defaults.removeObject(forKey: "tb_token")
            defaults.removeObject(forKey: "tb_server")
            defaults.removeObject(forKey: "tb_theme")
        }
        let server = defaults.string(forKey: "tb_server") ?? "http://localhost:5072"
        serverURLInput = server
        theme = AppTheme(rawValue: defaults.string(forKey: "tb_theme") ?? "") ?? .system
        api = ApiClient(baseURL: URL(string: server) ?? URL(string: "http://localhost:5072")!,
                        token: defaults.string(forKey: "tb_token"))
    }

    private func setToken(_ token: String?) {
        api.token = token
        if let token { UserDefaults.standard.set(token, forKey: "tb_token") }
        else { UserDefaults.standard.removeObject(forKey: "tb_token") }
    }

    /// Übernimmt die Server-URL aus dem Login-Feld (wie `tb login --api …` in der CLI).
    private func applyServerURL() -> Bool {
        let raw = serverURLInput.trimmingCharacters(in: .whitespaces)
        guard let url = URL(string: raw), url.scheme?.hasPrefix("http") == true else {
            toast("Bitte eine gültige Server-URL angeben (http/https)")
            return false
        }
        api.baseURL = url
        UserDefaults.standard.set(raw, forKey: "tb_server")
        return true
    }

    // ---------- Lifecycle ----------

    func bootstrap() async {
        if api.token != nil {
            do {
                let me = try await api.me()
                email = me.email
                tenantName = me.tenantName
                authStep = .loggedIn
                await refresh()
            } catch {
                setToken(nil)
            }
        }
        ready = true
    }

    // ---------- Auth ----------

    func sendCode() async {
        let em = emailInput.trimmingCharacters(in: .whitespaces)
        guard em.range(of: #"^\S+@\S+\.\S+$"#, options: .regularExpression) != nil else {
            toast("Bitte eine gültige E-Mail-Adresse eingeben")
            return
        }
        guard applyServerURL() else { return }
        do {
            let r = try await api.requestCode(email: em)
            authStep = .code
            codeInput = ""
            toast(r.message)
        } catch {
            toast((error as? ApiError)?.message ?? "Netzwerkfehler")
        }
    }

    func doLogin() async {
        let code = codeInput
        guard code.range(of: #"^\d{6}$"#, options: .regularExpression) != nil else {
            toast("Bitte den 6-stelligen Code eingeben")
            return
        }
        let em = emailInput.trimmingCharacters(in: .whitespaces).isEmpty ? email : emailInput.trimmingCharacters(in: .whitespaces)
        do {
            let r = try await api.verify(email: em, code: code)
            setToken(r.token)
            email = r.email
            tenantName = r.tenantName
            authStep = .loggedIn
            tab = .dashboard
            await refresh()
            toast("Willkommen im Tankbuch!")
        } catch {
            toast((error as? ApiError)?.message ?? "Anmeldung fehlgeschlagen")
        }
    }

    func backToEmail() { authStep = .email }

    func resendHint() { toast("Demo-Code: 123456 – es wird keine echte E-Mail versendet") }

    func logout() {
        setToken(nil)
        authStep = .email
        codeInput = ""
        tab = .dashboard
        vehicles = []
        entries = []
        toast("Abgemeldet")
    }

    // ---------- Daten ----------

    func refresh() async {
        busy = true
        defer { busy = false }
        do {
            async let v = api.fahrzeugeList()
            async let e = api.tankvorgaengeList()
            (vehicles, entries) = try await (v, e)
            if form.fahrzeugId == nil { form.fahrzeugId = vehicles.first?.id }
        } catch {
            if let apiError = error as? ApiError, apiError.status == 401 {
                setToken(nil)
                authStep = .email
            }
        }
    }

    func vehicle(_ id: UUID?) -> Fahrzeug? {
        guard let id else { return nil }
        return vehicles.first { $0.id == id }
    }

    // ---------- Erfassen: Trio & Formular ----------

    func onTrio(_ field: String, _ value: String) {
        switch field {
        case "liter": form.liter = value
        case "ppl": form.ppl = value
        default: form.total = value
        }
        editOrder = ([field] + editOrder.filter { $0 != field }).prefix(2).map { $0 }
        derived = nil
        if autoCalc {
            let r = computeTrio(Trio(liter: form.liter, ppl: form.ppl, total: form.total), order: editOrder)
            form.liter = r.trio.liter
            form.ppl = r.trio.ppl
            form.total = r.trio.total
            derived = r.derived
        }
    }

    func toggleAutoCalc() {
        autoCalc.toggle()
        derived = nil
    }

    // ---------- Scan (Foto-OCR mit simuliertem Fallback – wie store.ts) ----------

    func scan(_ kind: ScanKind, image: UIImage?) async {
        let started = Date()
        let veh = vehicle(form.fahrzeugId)

        switch kind {
        case .pump:
            pumpImage = image
            pumpDemo = image == nil
        case .tacho:
            tachoImage = image
            tachoDemo = image == nil
        }
        scanning = kind

        var pump: (liter: Double, total: Double)?
        var tacho: Int64?
        var meldung: String?

        if let jpeg = image?.jpegData(compressionQuality: 0.85) {
            do {
                switch kind {
                case .pump:
                    let r = try await api.ocrPump(image: jpeg)
                    if let l = r.liter, let t = r.gesamtpreis, l > 0, t > 0 { pump = (l, t) }
                    if r.simuliert { meldung = r.meldung ?? "Bilderkennung nicht erfolgreich" }
                case .tacho:
                    let r = try await api.ocrTacho(image: jpeg)
                    if let km = r.kilometerstand, km > 0 { tacho = km }
                    if r.simuliert { meldung = r.meldung ?? "Bilderkennung nicht erfolgreich" }
                }
            } catch {
                meldung = (error as? ApiError)?.message ?? "Anfrage an die Bilderkennung fehlgeschlagen"
            }
        }

        // Fallback: plausible simulierte Werte (Demo-Button oder fehlgeschlagene Erkennung)
        if kind == .pump, pump == nil {
            let base = veh?.kraftstoffart.lowercased().contains("diesel") == true ? 1.659 : 1.539
            let ppl = ((base + (Double.random(in: 0...1) - 0.5) * 0.08) * 1000).rounded() / 1000
            let liter = ((22 + Double.random(in: 0...1) * 30) * 100).rounded() / 100
            pump = (liter, (liter * ppl * 100).rounded() / 100)
        }
        if kind == .tacho, tacho == nil {
            let es = entries.filter { veh != nil && $0.fahrzeugId == veh!.id }
            let lastKm = es.map(\.kilometerstand).max() ?? (veh?.anfangsKilometer ?? 40000)
            tacho = lastKm + 350 + Int64((Double.random(in: 0...1) * 550).rounded())
        }

        // Mindestdauer der Scan-Animation wie im Web (~1,9 s)
        let elapsed = Date().timeIntervalSince(started)
        if elapsed < 1.9 {
            try? await Task.sleep(for: .seconds(1.9 - elapsed))
        }

        scanning = nil
        let hadRealPhoto = image != nil
        switch kind {
        case .pump:
            guard let pump else { return }
            recPump = true
            form.liter = fmt(pump.liter, 2)
            form.total = fmt(pump.total, 2)
            form.ppl = fmt(pump.total / pump.liter, 3)
            editOrder = ["total", "liter"]
            derived = "ppl"
            toast(meldung != nil && hadRealPhoto
                ? "Erkennung nicht möglich (\(meldung!)) – Schätzwerte, bitte korrigieren"
                : "Erkannt: \(form.liter) l · \(form.total) € – bitte prüfen und bestätigen")
        case .tacho:
            guard let tacho else { return }
            recTacho = true
            form.km = fmt(tacho, 0)
            toast(meldung != nil && hadRealPhoto
                ? "Erkennung nicht möglich (\(meldung!)) – Schätzwert, bitte korrigieren"
                : "Kilometerstand erkannt: \(form.km) km – bitte prüfen")
        }
    }

    // ---------- Tankvorgang speichern / bearbeiten / löschen ----------

    func saveEntry() async {
        let liter = parseDe(form.liter)
        let total = parseDe(form.total)
        let km = parseDe(form.km)
        let ppl = parseDe(form.ppl)
        guard let fahrzeugId = form.fahrzeugId else { toast("Bitte ein Fahrzeug wählen"); return }
        guard let liter, liter > 0 else { toast("Bitte die getankten Liter angeben"); return }
        guard let total, total > 0 else { toast("Bitte den Gesamtpreis angeben"); return }
        guard let km, km > 0 else { toast("Bitte den Kilometerstand angeben"); return }
        do {
            _ = try await api.tankvorgangCreate(TankvorgangBody(
                fahrzeugId: fahrzeugId,
                datum: isoString(from: form.datum),
                liter: liter,
                preisProLiter: (ppl ?? 0) > 0 ? ppl : nil,
                gesamtpreis: total,
                kilometerstand: Int64(km.rounded()),
                tankstelle: form.tankstelle.trimmingCharacters(in: .whitespaces),
                notiz: form.notiz.trimmingCharacters(in: .whitespaces),
                volltankung: form.voll))
            await refresh()
            form = EntryForm(fahrzeugId: fahrzeugId)
            editOrder = []
            derived = nil
            pumpImage = nil; pumpDemo = false; tachoImage = nil; tachoDemo = false
            recPump = false; recTacho = false
            histVeh = fahrzeugId
            histRange = "all"
            tab = .verlauf
            toast("Tankvorgang gespeichert")
        } catch {
            toast((error as? ApiError)?.message ?? "Speichern fehlgeschlagen")
        }
    }

    func openEdit(_ e: Tankvorgang) {
        editForm = EntryForm(
            fahrzeugId: e.fahrzeugId,
            datum: dateFromISO(e.datum) ?? .now,
            liter: fmt(e.liter, 2),
            ppl: fmt(e.preisProLiter, 3),
            total: fmt(e.gesamtpreis, 2),
            km: fmt(e.kilometerstand, 0),
            tankstelle: e.tankstelle ?? "",
            notiz: e.notiz ?? "",
            voll: e.volltankung)
        editOrderE = []
        derivedE = nil
        editEntry = e
    }

    func onTrioE(_ field: String, _ value: String) {
        switch field {
        case "liter": editForm.liter = value
        case "ppl": editForm.ppl = value
        default: editForm.total = value
        }
        editOrderE = ([field] + editOrderE.filter { $0 != field }).prefix(2).map { $0 }
        derivedE = nil
        if autoCalc {
            let r = computeTrio(Trio(liter: editForm.liter, ppl: editForm.ppl, total: editForm.total), order: editOrderE)
            editForm.liter = r.trio.liter
            editForm.ppl = r.trio.ppl
            editForm.total = r.trio.total
            derivedE = r.derived
        }
    }

    func saveEdit() async {
        guard let entry = editEntry else { return }
        let liter = parseDe(editForm.liter)
        let total = parseDe(editForm.total)
        let km = parseDe(editForm.km)
        let ppl = parseDe(editForm.ppl)
        guard let liter, liter > 0, let total, total > 0, let km, km > 0 else {
            toast("Bitte alle Pflichtfelder korrekt ausfüllen")
            return
        }
        do {
            _ = try await api.tankvorgangUpdate(entry.id, TankvorgangBody(
                fahrzeugId: nil,
                datum: isoString(from: editForm.datum),
                liter: liter,
                preisProLiter: (ppl ?? 0) > 0 ? ppl : nil,
                gesamtpreis: total,
                kilometerstand: Int64(km.rounded()),
                tankstelle: editForm.tankstelle.trimmingCharacters(in: .whitespaces),
                notiz: editForm.notiz.trimmingCharacters(in: .whitespaces),
                volltankung: editForm.voll))
            await refresh()
            editEntry = nil
            toast("Änderungen gespeichert")
        } catch {
            toast((error as? ApiError)?.message ?? "Speichern fehlgeschlagen")
        }
    }

    func deleteEntry(_ id: UUID) async {
        do {
            try await api.tankvorgangDelete(id)
            await refresh()
            toast("Eintrag gelöscht")
        } catch {
            toast((error as? ApiError)?.message ?? "Löschen fehlgeschlagen")
        }
    }

    // ---------- Fahrzeuge ----------

    func openVehSheet(_ id: UUID?) {
        vehId = id
        if let v = vehicle(id) {
            vehForm = VehicleForm(name: v.name, kz: v.kennzeichen, fuel: v.kraftstoffart,
                                  color: v.farbe, startKm: fmt(v.anfangsKilometer, 0))
        } else {
            vehForm = VehicleForm()
        }
        vehSheet = true
    }

    func saveVeh() async {
        let name = vehForm.name.trimmingCharacters(in: .whitespaces)
        guard !name.isEmpty else { toast("Bitte einen Namen angeben"); return }
        let startKm = Int64(max(0, (parseDe(vehForm.startKm) ?? 0).rounded()))
        let body = FahrzeugBody(name: name,
                                kennzeichen: vehForm.kz.trimmingCharacters(in: .whitespaces),
                                kraftstoffart: vehForm.fuel,
                                farbe: vehForm.color,
                                anfangsKilometer: startKm)
        do {
            if let vehId {
                _ = try await api.fahrzeugUpdate(vehId, body)
                toast("Fahrzeug aktualisiert")
            } else {
                let nv = try await api.fahrzeugCreate(body)
                if form.fahrzeugId == nil { form.fahrzeugId = nv.id }
                toast("Fahrzeug angelegt")
            }
            await refresh()
            vehSheet = false
        } catch {
            toast((error as? ApiError)?.message ?? "Speichern fehlgeschlagen")
        }
    }

    func deleteVeh() async {
        guard let id = vehId else { return }
        do {
            try await api.fahrzeugDelete(id)
            await refresh()
            vehSheet = false
            if dashVeh == id { dashVeh = nil }
            if statVeh == id { statVeh = nil }
            if histVeh == id { histVeh = nil }
            if form.fahrzeugId == id { form.fahrzeugId = vehicles.first(where: { $0.id != id })?.id }
            toast("Fahrzeug und zugehörige Tankvorgänge gelöscht")
        } catch {
            toast((error as? ApiError)?.message ?? "Löschen fehlgeschlagen")
        }
    }

    // ---------- CSV ----------

    /// Lädt den Export und legt ihn als Datei im Temp-Verzeichnis ab (fürs Teilen-Sheet).
    func exportCsv() async -> URL? {
        do {
            let (data, filename) = try await api.csvExport()
            let url = FileManager.default.temporaryDirectory.appendingPathComponent(filename)
            try data.write(to: url, options: .atomic)
            toast("Tankvorgänge als CSV exportiert")
            return url
        } catch {
            toast((error as? ApiError)?.message ?? "Export fehlgeschlagen")
            return nil
        }
    }

    func importCsv(from url: URL) async {
        do {
            let scoped = url.startAccessingSecurityScopedResource()
            defer { if scoped { url.stopAccessingSecurityScopedResource() } }
            let data = try Data(contentsOf: url)
            let r = try await api.csvImport(file: data, filename: url.lastPathComponent)
            await refresh()
            toast(r.meldung)
        } catch {
            toast((error as? ApiError)?.message ?? "Import fehlgeschlagen")
        }
    }

    // ---------- Toast ----------

    func toast(_ msg: String) {
        toastTask?.cancel()
        toastMsg = msg
        toastTask = Task {
            try? await Task.sleep(for: .seconds(3.6))
            if !Task.isCancelled { toastMsg = nil }
        }
    }
}
