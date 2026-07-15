import XCTest

// E2E-Smoke-Test gegen die laufende API (http://localhost:5072, Seed-Daten aktiv) –
// natives Gegenstück zu den Playwright-E2E-Tests des Web-Frontends.
final class SmokeTests: XCTestCase {

    override func setUpWithError() throws {
        continueAfterFailure = false
    }

    func testLoginErfassenVerlaufStatistikEinstellungen() throws {
        let app = XCUIApplication()
        app.launchArguments = ["-uitest-reset"]
        app.launch()

        // ---------- Login ----------
        let emailField = app.textFields["name@beispiel.at"]
        XCTAssertTrue(emailField.waitForExistence(timeout: 10), "Login-Screen erscheint")
        shot(app, "01-login")

        emailField.tap()
        emailField.typeText("demo@tankbuch.at")
        app.buttons["Code senden"].tap()

        let anmelden = app.buttons["Anmelden"]
        XCTAssertTrue(anmelden.waitForExistence(timeout: 10), "Code-Schritt erscheint")
        shot(app, "02-code")
        app.typeText("123456") // verstecktes Codefeld ist fokussiert; Auto-Login nach 6 Ziffern

        // ---------- Dashboard ----------
        let startTab = app.tabBars.buttons["Start"]
        XCTAssertTrue(startTab.waitForExistence(timeout: 15), "Nach dem Login erscheint die Tab-Bar")
        XCTAssertTrue(app.staticTexts["Ø Verbrauch"].waitForExistence(timeout: 10), "KPIs mit Seed-Daten")
        shot(app, "03-dashboard")

        // ---------- Verlauf ----------
        app.tabBars.buttons["Verlauf"].tap()
        XCTAssertTrue(app.buttons["Alle Fahrzeuge"].waitForExistence(timeout: 5))
        shot(app, "04-verlauf")

        // ---------- Erfassen (Demo-Scan Zapfsäule + Tacho, dann speichern) ----------
        app.tabBars.buttons["Erfassen"].tap()
        let demoButtons = app.buttons.matching(NSPredicate(format: "label == 'Demo'"))
        XCTAssertTrue(demoButtons.element(boundBy: 0).waitForExistence(timeout: 5))

        demoButtons.element(boundBy: 0).tap() // Zapfsäule
        XCTAssertTrue(app.staticTexts["Erkannt"].waitForExistence(timeout: 10), "Pump-Scan liefert Werte")

        demoButtons.element(boundBy: 1).tap() // Tacho
        let erkanntBadges = app.staticTexts.matching(NSPredicate(format: "label == 'Erkannt'"))
        let bothRecognized = NSPredicate(format: "count == 2")
        expectation(for: bothRecognized, evaluatedWith: erkanntBadges)
        waitForExpectations(timeout: 10)
        shot(app, "05-erfassen")

        app.buttons["Speichern"].firstMatch.tap()

        // Speichern wechselt zum Verlauf; der neue Eintrag (heutiges Datum) ist oben.
        let today = DateFormatter()
        today.dateFormat = "dd.MM.yyyy"
        let newEntry = app.staticTexts[today.string(from: .now)]
        XCTAssertTrue(newEntry.waitForExistence(timeout: 10), "Neuer Tankvorgang erscheint im Verlauf")
        // Regression: "54.696" (Tausenderpunkt) darf nicht als 54,696 → 55 km gespeichert werden.
        XCTAssertFalse(app.staticTexts["55 km"].exists, "Kilometerstand mit Tausenderpunkt korrekt geparst")
        shot(app, "06-verlauf-neuer-eintrag")

        // ---------- Statistik ----------
        app.tabBars.buttons["Statistik"].tap()
        XCTAssertTrue(app.staticTexts["Gesamtliter"].waitForExistence(timeout: 5))
        shot(app, "07-statistik")

        // ---------- Mehr → Fahrzeuge ----------
        app.tabBars.buttons["Mehr"].tap()
        XCTAssertTrue(app.buttons["Fahrzeuge"].waitForExistence(timeout: 5))
        app.buttons["Fahrzeuge"].tap()
        XCTAssertTrue(app.staticTexts["Kilometerstand"].firstMatch.waitForExistence(timeout: 5), "Fahrzeugkarten sichtbar")
        shot(app, "08-fahrzeuge")
        app.navigationBars.buttons.firstMatch.tap() // zurück

        // ---------- Einstellungen: Theme umschalten ----------
        XCTAssertTrue(app.buttons["Einstellungen"].waitForExistence(timeout: 5))
        app.buttons["Einstellungen"].tap()
        XCTAssertTrue(app.staticTexts["Darstellung"].waitForExistence(timeout: 5))
        shot(app, "09-einstellungen")

        app.buttons["Dunkel"].tap()
        app.tabBars.buttons["Start"].tap()
        XCTAssertTrue(app.staticTexts["Ø Verbrauch"].waitForExistence(timeout: 5))
        shot(app, "10-dashboard-dunkel")

        // Theme zurück auf System (der Mehr-Tab zeigt noch die Einstellungen-Ansicht)
        app.tabBars.buttons["Mehr"].tap()
        if !app.buttons["System"].waitForExistence(timeout: 2) {
            app.buttons["Einstellungen"].tap()
        }
        XCTAssertTrue(app.buttons["System"].waitForExistence(timeout: 5))
        app.buttons["System"].tap()
    }

    private func shot(_ app: XCUIApplication, _ name: String) {
        let attachment = XCTAttachment(screenshot: app.screenshot())
        attachment.name = name
        attachment.lifetime = .keepAlways
        add(attachment)
    }
}
