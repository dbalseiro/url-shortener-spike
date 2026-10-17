# Versions

Recorded 2026-10-16 on macOS (aarch64).

## Kotlin track

| Component | Version | Source |
|---|---|---|
| JDK (launcher) | Temurin 25.0.4.1 LTS | `java -version` |
| JDK (compile toolchain) | 21 | `kotlin/build.gradle.kts` (`jvmToolchain(21)`) |
| Gradle | 9.5.1 | `kotlin/gradle/wrapper/gradle-wrapper.properties` |
| Kotlin | 2.4.0 | `kotlin/gradle/libs.versions.toml` |
| Ktor | 3.6.0 | `kotlin/settings.gradle.kts` (`ktor-version-catalog`) |
| Lettuce | 7.8.0.RELEASE | `kotlin/gradle/libs.versions.toml` |
| Arrow | 2.2.3 | `kotlin/gradle/libs.versions.toml` |

Other pinned libraries: kotlinx-coroutines 1.11.0, Logback 1.6.3, Micrometer Prometheus registry 1.17.0, Testcontainers 2.0.5.

## Swift track

| Component | Version | Source |
|---|---|---|
| Swift toolchain | | |
| Hummingbird | | |
| Redis client (choice and why) | | |
