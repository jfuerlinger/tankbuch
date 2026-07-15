import SwiftUI

struct RootView: View {
    @Environment(AppStore.self) private var store

    var body: some View {
        @Bindable var store = store
        Group {
            if !store.ready {
                Theme.bg.ignoresSafeArea()
            } else if store.authStep != .loggedIn {
                LoginView()
            } else {
                TabView(selection: $store.tab) {
                    SwiftUI.Tab("Start", systemImage: "house", value: AppTab.dashboard) {
                        NavigationStack { DashboardView() }
                    }
                    SwiftUI.Tab("Verlauf", systemImage: "clock", value: AppTab.verlauf) {
                        NavigationStack { VerlaufView() }
                    }
                    SwiftUI.Tab("Erfassen", systemImage: "plus.circle.fill", value: AppTab.erfassen) {
                        NavigationStack { ErfassenView() }
                    }
                    SwiftUI.Tab("Statistik", systemImage: "chart.bar", value: AppTab.statistik) {
                        NavigationStack { StatistikView() }
                    }
                    SwiftUI.Tab("Mehr", systemImage: "ellipsis", value: AppTab.mehr) {
                        NavigationStack { MehrView() }
                    }
                }
            }
        }
        .tbToast()
    }
}
