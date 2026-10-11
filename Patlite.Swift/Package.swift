// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "PatliteSwift",
    platforms: [.macOS(.v13)],
    products: [.executable(name: "PatliteSwift", targets: ["PatliteSwift"])],
    targets: [
        .target(name: "PatliteCore"),
        .executableTarget(name: "PatliteSwift", dependencies: ["PatliteCore"],
                          resources: [.copy("Resources/patlite.icns")]),
        .testTarget(name: "PatliteCoreTests", dependencies: ["PatliteCore"])
    ]
)
