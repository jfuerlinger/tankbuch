import Foundation

// HTTP-Client – Spiegel von frontend/src/lib/api.ts. Bearer-Token, camelCase-JSON.

struct ApiError: LocalizedError {
    let message: String
    let status: Int
    var errorDescription: String? { message }
}

final class ApiClient {
    var baseURL: URL
    var token: String?

    init(baseURL: URL, token: String? = nil) {
        self.baseURL = baseURL
        self.token = token
    }

    // ---------- Auth ----------
    func requestCode(email: String) async throws -> RequestCodeResponse {
        try await request("POST", "/api/auth/request-code", json: ["email": email])
    }

    func verify(email: String, code: String) async throws -> VerifyCodeResponse {
        try await request("POST", "/api/auth/verify", json: ["email": email, "code": code])
    }

    func me() async throws -> MeResponse {
        try await request("GET", "/api/auth/me")
    }

    // ---------- Fahrzeuge ----------
    func fahrzeugeList() async throws -> [Fahrzeug] {
        try await request("GET", "/api/fahrzeuge")
    }

    func fahrzeugCreate(_ body: FahrzeugBody) async throws -> Fahrzeug {
        try await request("POST", "/api/fahrzeuge", json: body)
    }

    func fahrzeugUpdate(_ id: UUID, _ body: FahrzeugBody) async throws -> Fahrzeug {
        try await request("PUT", "/api/fahrzeuge/\(id.uuidString.lowercased())", json: body)
    }

    func fahrzeugDelete(_ id: UUID) async throws {
        try await requestVoid("DELETE", "/api/fahrzeuge/\(id.uuidString.lowercased())")
    }

    // ---------- Tankvorgänge ----------
    func tankvorgaengeList() async throws -> [Tankvorgang] {
        try await request("GET", "/api/tankvorgaenge")
    }

    func tankvorgangCreate(_ body: TankvorgangBody) async throws -> Tankvorgang {
        try await request("POST", "/api/tankvorgaenge", json: body)
    }

    func tankvorgangUpdate(_ id: UUID, _ body: TankvorgangBody) async throws -> Tankvorgang {
        try await request("PUT", "/api/tankvorgaenge/\(id.uuidString.lowercased())", json: body)
    }

    func tankvorgangDelete(_ id: UUID) async throws {
        try await requestVoid("DELETE", "/api/tankvorgaenge/\(id.uuidString.lowercased())")
    }

    // ---------- OCR ----------
    func ocrPump(image: Data) async throws -> PumpOcrResult {
        try await request("POST", "/api/ocr/pump", multipart: [.file(name: "image", filename: "pump.jpg", mimeType: "image/jpeg", data: image)])
    }

    func ocrTacho(image: Data) async throws -> TachoOcrResult {
        try await request("POST", "/api/ocr/tacho", multipart: [.file(name: "image", filename: "tacho.jpg", mimeType: "image/jpeg", data: image)])
    }

    // ---------- CSV ----------
    func csvExport() async throws -> (data: Data, filename: String) {
        let (data, response) = try await send(makeRequest("GET", "/api/csv/export"))
        guard response.statusCode < 300 else { throw ApiError(message: "Export fehlgeschlagen", status: response.statusCode) }
        var filename = "tankbuch-export.csv"
        if let cd = response.value(forHTTPHeaderField: "Content-Disposition"),
           let range = cd.range(of: "filename=") {
            filename = String(cd[range.upperBound...])
                .trimmingCharacters(in: CharacterSet(charactersIn: "\"; "))
        }
        return (data, filename)
    }

    func csvImport(file: Data, filename: String) async throws -> CsvImportResult {
        try await request("POST", "/api/csv/import", multipart: [.file(name: "file", filename: filename, mimeType: "text/csv", data: file)])
    }

    // ---------- Intern ----------

    enum MultipartPart {
        case file(name: String, filename: String, mimeType: String, data: Data)
    }

    private func makeRequest(_ method: String, _ path: String) -> URLRequest {
        var req = URLRequest(url: URL(string: path, relativeTo: baseURL)!)
        req.httpMethod = method
        req.timeoutInterval = 120 // OCR über das Vision-Modell kann dauern
        if let token { req.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization") }
        return req
    }

    private func send(_ req: URLRequest) async throws -> (Data, HTTPURLResponse) {
        let data: Data
        let response: URLResponse
        do {
            (data, response) = try await URLSession.shared.data(for: req)
        } catch {
            throw ApiError(message: "Netzwerkfehler: Server nicht erreichbar", status: 0)
        }
        guard let http = response as? HTTPURLResponse else {
            throw ApiError(message: "Ungültige Antwort", status: 0)
        }
        return (data, http)
    }

    private func perform(_ method: String, _ path: String, json: Encodable?, multipart: [MultipartPart]?) async throws -> Data {
        var req = makeRequest(method, path)
        if let json {
            req.setValue("application/json", forHTTPHeaderField: "Content-Type")
            let encoder = JSONEncoder()
            req.httpBody = try encoder.encode(json)
        } else if let multipart {
            let boundary = "tb-\(UUID().uuidString)"
            req.setValue("multipart/form-data; boundary=\(boundary)", forHTTPHeaderField: "Content-Type")
            var body = Data()
            for part in multipart {
                switch part {
                case let .file(name, filename, mimeType, data):
                    body.append(Data("--\(boundary)\r\n".utf8))
                    body.append(Data("Content-Disposition: form-data; name=\"\(name)\"; filename=\"\(filename)\"\r\n".utf8))
                    body.append(Data("Content-Type: \(mimeType)\r\n\r\n".utf8))
                    body.append(data)
                    body.append(Data("\r\n".utf8))
                }
            }
            body.append(Data("--\(boundary)--\r\n".utf8))
            req.httpBody = body
        }

        let (data, http) = try await send(req)
        guard http.statusCode < 300 else { throw Self.parseError(data, status: http.statusCode) }
        return data
    }

    private func request<T: Decodable>(_ method: String, _ path: String, json: Encodable? = nil,
                                       multipart: [MultipartPart]? = nil) async throws -> T {
        let data = try await perform(method, path, json: json, multipart: multipart)
        return try JSONDecoder().decode(T.self, from: data)
    }

    private func requestVoid(_ method: String, _ path: String) async throws {
        _ = try await perform(method, path, json: nil, multipart: nil)
    }

    /// Fehlerformat wie im Web: { errors: {feld: [msg]} } | { message } | { title }
    private static func parseError(_ data: Data, status: Int) -> ApiError {
        var msg = "Fehler (\(status))"
        if let obj = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
            if let errors = obj["errors"] as? [String: [String]] {
                msg = errors.values.flatMap { $0 }.joined(separator: ", ")
            } else if let m = obj["message"] as? String {
                msg = m
            } else if let t = obj["title"] as? String {
                msg = t
            }
        }
        return ApiError(message: msg, status: status)
    }
}
