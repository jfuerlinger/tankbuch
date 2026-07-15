import SwiftUI

// Anmeldung – Nachbau von frontend/src/screens/Login.tsx: E-Mail → 6-stelliger Code.
// Zusätzlich (wie die CLI): konfigurierbare Server-URL, da Aspire dynamische Ports vergibt.
struct LoginView: View {
    @Environment(AppStore.self) private var store
    @FocusState private var codeFocused: Bool
    @State private var serverExpanded = false

    var body: some View {
        @Bindable var store = store
        ZStack {
            Theme.bg.ignoresSafeArea()
            ScrollView {
                VStack(spacing: 16) {
                    Spacer(minLength: 60)

                    VStack(spacing: 26) {
                        VStack(spacing: 8) {
                            LogoView(size: 60)
                            Text("Tankbuch").font(.plex(25, .bold)).foregroundStyle(Theme.text)
                            Text("Das digitale Fahrtenbuch fürs Tanken")
                                .font(.plex(14)).foregroundStyle(Theme.text2)
                        }

                        if store.authStep == .email {
                            emailStep
                        } else {
                            codeStep
                        }
                    }
                    .padding(EdgeInsets(top: 32, leading: 28, bottom: 32, trailing: 28))
                    .background(RoundedRectangle(cornerRadius: 20, style: .continuous).fill(Theme.card))
                    .overlay(RoundedRectangle(cornerRadius: 20, style: .continuous).stroke(Theme.border, lineWidth: 1))
                    .shadow(color: .black.opacity(0.06), radius: 8, x: 0, y: 3)
                    .frame(maxWidth: 400)

                    Text("Prototyp: Es wird keine echte E-Mail versendet.\nDer Demo-Code erscheint nach „Code senden“.")
                        .font(.plex(12.5))
                        .foregroundStyle(Theme.text3)
                        .multilineTextAlignment(.center)
                        .frame(maxWidth: 380)

                    Spacer(minLength: 40)
                }
                .padding(24)
                .frame(maxWidth: .infinity)
            }
        }
    }

    // ---------- Schritt 1: E-Mail ----------

    private var emailStep: some View {
        @Bindable var store = store
        return VStack(alignment: .leading, spacing: 14) {
            VStack(alignment: .leading, spacing: 6) {
                Text("E-Mail-Adresse").font(.plex(12, .semibold)).foregroundStyle(Theme.text2)
                TextField("name@beispiel.at", text: $store.emailInput)
                    .font(.plex(15))
                    .keyboardType(.emailAddress)
                    .textContentType(.emailAddress)
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled()
                    .submitLabel(.send)
                    .onSubmit { Task { await store.sendCode() } }
                    .padding(.horizontal, 13)
                    .padding(.vertical, 12)
                    .background(RoundedRectangle(cornerRadius: 11, style: .continuous).fill(Theme.bg))
                    .overlay(RoundedRectangle(cornerRadius: 11, style: .continuous).stroke(Theme.border, lineWidth: 1))
            }

            Button {
                Task { await store.sendCode() }
            } label: {
                Text("Code senden")
                    .font(.plex(15, .bold))
                    .frame(maxWidth: .infinity)
            }
            .buttonStyle(AccentButtonStyle(fontSize: 15, verticalPadding: 13))

            DisclosureGroup(isExpanded: $serverExpanded) {
                VStack(alignment: .leading, spacing: 6) {
                    TextField("http://localhost:5072", text: $store.serverURLInput)
                        .font(.plex(13.5))
                        .keyboardType(.URL)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                        .padding(.horizontal, 12)
                        .padding(.vertical, 10)
                        .background(RoundedRectangle(cornerRadius: 10, style: .continuous).fill(Theme.bg))
                        .overlay(RoundedRectangle(cornerRadius: 10, style: .continuous).stroke(Theme.border, lineWidth: 1))
                    Text("API-Adresse aus dem Aspire-Dashboard (oder DevTunnel-URL).")
                        .font(.plex(11.5)).foregroundStyle(Theme.text3)
                }
                .padding(.top, 8)
            } label: {
                Text("Server")
                    .font(.plex(12.5, .semibold))
                    .foregroundStyle(Theme.text3)
            }
            .tint(Theme.text3)
        }
    }

    // ---------- Schritt 2: Code ----------

    private var codeStep: some View {
        @Bindable var store = store
        return VStack(spacing: 16) {
            (Text("Wir haben einen 6-stelligen Code an\n").font(.plex(13.5)).foregroundStyle(Theme.text2)
             + Text(store.emailInput.trimmingCharacters(in: .whitespaces).isEmpty ? "—" : store.emailInput.trimmingCharacters(in: .whitespaces))
                .font(.plex(13.5, .bold)).foregroundStyle(Theme.text)
             + Text(" gesendet.").font(.plex(13.5)).foregroundStyle(Theme.text2))
                .multilineTextAlignment(.center)

            // 6 Code-Kästchen; dahinter ein unsichtbares Textfeld (robust, Paste/OTP-Autofill inklusive)
            ZStack {
                TextField("", text: $store.codeInput)
                    .keyboardType(.numberPad)
                    .textContentType(.oneTimeCode)
                    .focused($codeFocused)
                    .opacity(0.02)
                    .onChange(of: store.codeInput) { _, newValue in
                        let digits = String(newValue.filter(\.isNumber).prefix(6))
                        if digits != newValue { store.codeInput = digits }
                        if digits.count == 6 {
                            Task {
                                try? await Task.sleep(for: .milliseconds(220))
                                await store.doLogin()
                            }
                        }
                    }

                HStack(spacing: 8) {
                    ForEach(0..<6, id: \.self) { i in
                        let chars = Array(store.codeInput)
                        Text(i < chars.count ? String(chars[i]) : " ")
                            .font(.plex(22, .bold))
                            .monospacedDigit()
                            .foregroundStyle(Theme.text)
                            .frame(width: 44, height: 54)
                            .background(RoundedRectangle(cornerRadius: 11, style: .continuous).fill(Theme.bg))
                            .overlay(RoundedRectangle(cornerRadius: 11, style: .continuous)
                                .stroke(i == store.codeInput.count && codeFocused ? Theme.accent : Theme.border,
                                        lineWidth: 1.5))
                    }
                }
                .contentShape(Rectangle())
                .onTapGesture { codeFocused = true }
            }
            .frame(height: 54)

            Button {
                Task { await store.doLogin() }
            } label: {
                Text("Anmelden")
                    .font(.plex(15, .bold))
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 13)
                    .foregroundStyle(store.codeInput.count == 6 ? Theme.accentInk : Theme.text3)
                    .background(RoundedRectangle(cornerRadius: 12, style: .continuous)
                        .fill(store.codeInput.count == 6 ? Theme.accent : Theme.card2))
            }
            .buttonStyle(.plain)

            HStack(spacing: 18) {
                Button("Zurück") { store.backToEmail() }
                    .font(.plex(13, .semibold))
                    .foregroundStyle(Theme.text2)
                Button("Code erneut senden") { store.resendHint() }
                    .font(.plex(13, .semibold))
                    .foregroundStyle(Theme.accentText)
            }
        }
        .onAppear { codeFocused = true }
    }
}
