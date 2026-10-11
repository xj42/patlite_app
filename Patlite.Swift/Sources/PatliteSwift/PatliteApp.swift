import AppKit
import PatliteCore
import SwiftUI

@main
struct PatliteApp: App {
    @StateObject private var controller = Controller()

    init() {
        NSApplication.shared.setActivationPolicy(.regular)
        // A signed .app keeps resources in Contents/Resources. SwiftPM's
        // generated accessor only searches the bundle root and build directory.
        let resources: Bundle
        if let url = Bundle.main.url(forResource: "PatliteSwift_PatliteSwift", withExtension: "bundle"),
           let bundle = Bundle(url: url) {
            resources = bundle
        } else {
            resources = Bundle.module
        }
        if let url = resources.url(forResource: "patlite", withExtension: "icns") {
            NSApplication.shared.applicationIconImage = NSImage(contentsOf: url)
        }
    }

    var body: some Scene {
        Window("Patlite Controller", id: "controller") {
            ControllerView(controller: controller)
                .preferredColorScheme(.light)
                .onAppear { NSApplication.shared.activate(ignoringOtherApps: true) }
        }
        .defaultSize(width: 320, height: 520)
        .windowResizability(.contentMinSize)
    }
}

struct ControllerView: View {
    @ObservedObject var controller: Controller
    private let tierPaths: [WritableKeyPath<PatliteCore.Pattern, LightColour>] = [
        \.tier1, \.tier2, \.tier3, \.tier4, \.tier5
    ]

    var body: some View {
        VStack(spacing: 12) {
            connectionControls
            tierControls
            logView
            actionControls
        }
        .font(.system(size: 12))
        .controlSize(.small)
        .textFieldStyle(.roundedBorder)
        .padding(12)
        .frame(minWidth: 320, minHeight: 480)
    }

    private var connectionControls: some View {
        HStack(alignment: .top, spacing: 16) {
            VStack(alignment: .leading, spacing: 3) {
                fieldLabel("IP / Host:")
                TextField("", text: $controller.host)
                    .accessibilityLabel("IP address or hostname")
                fieldLabel("Flash:").padding(.top, 5)
                Picker("Flash", selection: $controller.selected.flash) {
                    ForEach(Flash.allCases, id: \.self) { Text($0.label).tag($0) }
                }.labelsHidden()
                Text("Current: \(controller.applied.flash.label)").font(.system(size: 10))
            }
            VStack(alignment: .leading, spacing: 3) {
                fieldLabel("Port:")
                TextField("", text: $controller.port)
                    .frame(width: 64).accessibilityLabel("UDP port")
                fieldLabel("Buzzer:").padding(.top, 5)
                Picker("Buzzer", selection: $controller.selected.buzzer) {
                    ForEach(Buzzer.allCases, id: \.self) { Text($0.label).tag($0) }
                }.labelsHidden()
                Text("Current: \(controller.applied.buzzer.label)").font(.system(size: 10))
            }.frame(width: 112)
        }
    }

    private var tierControls: some View {
        VStack(spacing: 6) {
            ForEach(0..<5) { index in
                HStack(spacing: 5) {
                    Text("Tier \(index + 1)").frame(width: 34, alignment: .leading)
                    Picker("Tier \(index + 1) colour", selection: $controller.selected[dynamicMember: tierPaths[index]]) {
                        ForEach(LightColour.allCases, id: \.self) { colour in
                            Label {
                                Text(colour.label)
                            } icon: {
                                Image(systemName: "circle.fill").foregroundStyle(colour.swatch)
                            }.tag(colour)
                        }
                    }.labelsHidden().frame(width: 100)
                    TierIndicator(colour: controller.applied.tiers[index], flash: controller.applied.flash)
                    Button(LightPreset.all[index].name) {
                        Task { await controller.sendPreset(LightPreset.all[index]) }
                    }
                    .frame(maxWidth: .infinity)
                    .disabled(!controller.canSend)
                }
            }
        }
    }

    private var logView: some View {
        ScrollViewReader { proxy in
            ScrollView {
                LazyVStack(alignment: .leading, spacing: 3) {
                    ForEach(controller.logs) { line in
                        Text(line.text).frame(maxWidth: .infinity, alignment: .leading).id(line.id)
                    }
                }.padding(8)
            }
            .background(Color(white: 0.07))
            .foregroundStyle(Color(white: 0.87))
            .clipShape(RoundedRectangle(cornerRadius: 5))
            .onChange(of: controller.logs.last?.id) { id in
                if let id { proxy.scrollTo(id, anchor: .bottom) }
            }
        }.frame(minHeight: 120)
    }

    private var actionControls: some View {
        HStack {
            Text("Busy: \(controller.isBusy ? "True" : "False")")
            if controller.isBusy { ProgressView().controlSize(.mini) }
            Spacer()
            Button("Off") { Task { await controller.off() } }
                .disabled(!controller.canSend)
            Button("Send") { Task { await controller.send() } }
                .keyboardShortcut(.return, modifiers: [])
                .disabled(!controller.canSend)
        }
    }

    private func fieldLabel(_ text: String) -> some View {
        Text(text).font(.system(size: 10, weight: .semibold))
    }
}

struct TierIndicator: View {
    let colour: LightColour
    let flash: Flash

    var body: some View {
        TimelineView(.animation(minimumInterval: 0.35, paused: flash == .off)) { context in
            let dim = flash == .on && Int(context.date.timeIntervalSinceReferenceDate / 0.35) % 2 == 1
            Circle().fill(colour.swatch)
                .overlay(Circle().stroke(Color.black, lineWidth: 0.5))
                .opacity(dim ? 0.25 : 1)
        }
        .frame(width: 20, height: 20)
        .accessibilityLabel("Last sent colour: \(colour.label), flash \(flash.label)")
    }
}

extension LightColour {
    var swatch: Color {
        switch self {
        case .off: return .gray
        case .red: return .red
        case .amber: return .orange
        case .lemon: return .yellow
        case .green: return Color(red: 0, green: 0.5, blue: 0)
        case .skyBlue: return Color(red: 0.53, green: 0.81, blue: 0.92)
        case .blue: return .blue
        case .purple: return .purple
        case .pink: return Color(red: 1, green: 0.75, blue: 0.8)
        case .white: return .white
        }
    }
}
