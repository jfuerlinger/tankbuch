import SwiftUI

@main
struct TankbuchApp: App {
    @State private var store = AppStore()

    var body: some Scene {
        WindowGroup {
            RootView()
                .environment(store)
                .tint(Theme.accent)
                .preferredColorScheme(store.theme.colorScheme)
                .task { await store.bootstrap() }
        }
    }
}
