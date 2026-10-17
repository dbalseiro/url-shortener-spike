# Kotlin vs Swift Spike: Functional URL Shortener

**Goal:** Decide which language we move to for backend services by building the *same* small service in both and comparing the results against criteria we agree on beforehand.

**This document covers bootstrapping only:** what to install, how to scaffold, and how to confirm everything works. The actual development is up to the team.

> Version numbers drift. Wherever this doc says "latest stable", check the official site on the day you start and write the versions you used into `docs/versions.md`.

---

## 1. The Spike at a Glance

| | |
|---|---|
| **Service** | URL shortener backed by Redis |
| **Style** | Functional core, imperative shell (pure logic, thin I/O layer) |
| **Timebox** | 1 week per language, ideally different people on each |
| **Output** | Working service + filled-in scorecard + a short recommendation |

### Scope of the service (identical in both languages)

- `POST /shorten` creates a short link (optional alias, optional TTL)
- `GET /{code}` redirects to the original URL
- [OPTIONAL] Rate limiting per client
- [OPTIONAL] Async click analytics that never blocks the redirect
- [OPTIONAL] Health check and Prometheus-style metrics

### Functional design rules (both languages)

1. **Pure core:** URL validation, base62 encoding, TTL decisions, Redis key building, and rate-limit window math are pure functions with no I/O.
2. **Errors as values:** a closed error type (`InvalidUrl`, `AliasTaken`, `RateLimited`, `NotFound`) with a single mapping to HTTP status codes. No exceptions for control flow.
3. **Thin shell:** HTTP handlers and the Redis client only translate between the outside world and the core.
4. **Dependencies as interfaces:** a `LinkStore` abstraction with a real Redis implementation and an in-memory one, so core and service tests run without Docker.
5. **No shared mutable state:** state lives in Redis; anything crossing concurrency boundaries is an immutable message.

---

## 2. Prerequisites (Both Tracks)

| Tool | Why | Check |
|---|---|---|
| Git | Version control | `git --version` |
| Docker + Docker Compose | Redis, later Postgres and the app images | `docker --version && docker compose version` |
| `redis-cli` | Poking at keys while developing | `redis-cli --version` |
| [k6](https://k6.io) | Load testing the redirect path | `k6 version` |
| `curl` + `jq` | Manual API testing | `curl --version && jq --version` |
| `just` or `make` | One-command tasks (optional but recommended) | `just --version` |

macOS (Homebrew):

```bash
brew install git redis k6 jq just
brew install --cask docker
```

Linux: use your package manager for `git`, `redis-tools`, `jq`, `just`, and follow the official Docker and k6 install guides.

---

## 3. Repository Layout

One repo, one folder per language, shared assets at the top level so both implementations are held to the same contract.

```
url-shortener-spike/
├── README.md
├── docker-compose.yml        # Redis (and later Postgres), shared by both tracks
├── Justfile                  # common tasks: up, down, load-test
├── contract/
│   └── openapi.yaml          # the API both services must implement
├── loadtest/
│   └── redirect.js           # same k6 script against both services
├── docs/
│   ├── versions.md           # exact tool/library versions used
│   ├── key-schema.md         # Redis key design (agree on this first)
│   └── scorecard.md          # evaluation criteria and results
├── kotlin/                   # Kotlin track
└── swift/                    # Swift track
```

Bootstrap it:

```bash
mkdir url-shortener-spike && cd url-shortener-spike
git init
mkdir -p contract loadtest docs kotlin swift
```

### Shared infrastructure: `docker-compose.yml`

```yaml
services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5
```

Verify:

```bash
docker compose up -d redis
redis-cli ping        # expect: PONG
```

### Write the contract first

Before any code, agree on `contract/openapi.yaml`: endpoints, request/response bodies, status codes, and error shapes. Both services implement this same contract, which keeps the comparison fair and lets the same k6 script and curl examples work against either.

---

## 4. Kotlin Track Bootstrap

### 4.1 Install the toolchain

Use [SDKMAN!](https://sdkman.io) to manage JDK and build tools:

```bash
curl -s "https://get.sdkman.io" | bash
source "$HOME/.sdkman/bin/sdkman-init.sh"

sdk list java                 # pick the latest LTS Temurin build
sdk install java <version>-tem
sdk install gradle
sdk install kotlin            # optional, only for the standalone compiler/REPL
```

IDE: **IntelliJ IDEA** (Community edition is enough).

Verify:

```bash
java -version
gradle --version
```

### 4.2 Scaffold the project

The Ktor project generator works like Spring Initializr: pick options in a form, download a ready-to-build Gradle project.

1. Open **[start.ktor.io](https://start.ktor.io)**.
2. Set **Project artifact** (e.g. `com.jurisfutura.shortener`).
3. Click **Configure** and set **Build System** to *Gradle* with *Kotlin* as the build script language (the generator may show these as two separate options), and **Engine** to *Netty*. Pick YAML for **Configuration**, or in-code if you prefer.
4. Add these plugins (routing itself is built in):
   - Content Negotiation + kotlinx.serialization: JSON request and response bodies
   - Status Pages: one place to map the closed error type to HTTP status codes
   - Call Logging: request logs
   - Micrometer Metrics: the generator also adds the Prometheus registry and a sample scrape route in `Monitoring.kt`
5. Click **Download**, unzip so `build.gradle.kts` sits directly in `kotlin/`, then:

   ```bash
   cd kotlin
   ./gradlew build
   ./gradlew run
   curl http://0.0.0.0:8080
   ```

### 4.3 Add the remaining dependencies

The generator already added call logging, status pages, content negotiation, Micrometer and the Prometheus registry. What is still missing:

| Library | Purpose |
|---|---|
| kotlinx.serialization (compiler plugin + JSON) | `ContentNegotiation` is installed but has no JSON converter yet |
| [Lettuce](https://github.com/redis/lettuce) + `kotlinx-coroutines-reactive` | Redis client with coroutine support |
| [Arrow](https://arrow-kt.io) (`arrow-core`, `arrow-fx-coroutines`) | `Either`, `Raise`, `Resource` for functional error handling |
| [Testcontainers](https://testcontainers.com) | Integration tests against a real Redis (needs Docker running) |

Tests already use `kotlin("test")` (JUnit under the hood), so Kotest is optional.

**Quick way: one line per dependency in `kotlin/build.gradle.kts`.**
Replace `<version>` with the latest stable version from each project's releases page or Maven Central, and record them in `docs/versions.md`.

```kotlin
plugins {
    // ... existing entries ...
    kotlin("plugin.serialization") version "2.4.0"   // same version as the Kotlin plugin
}

dependencies {
    // ... existing entries ...
    implementation(ktorLibs.serialization.kotlinx.json)   // Ktor module: no version needed
    implementation("io.lettuce:lettuce-core:<version>")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-reactive:<version>")
    implementation("io.arrow-kt:arrow-core:<version>")
    implementation("io.arrow-kt:arrow-fx-coroutines:<version>")

    testImplementation("org.testcontainers:testcontainers:<version>")
}
```

Then reload Gradle (IDE sync or `./gradlew build`). This is enough for a one-week spike.

**Recommended if the code will live on: a version catalog.**
Versions live in one place, accessors are type-safe, and tools like Renovate can update them. The project has two catalogs:

| Catalog | Defined in | Holds |
|---|---|---|
| `ktorLibs` | `kotlin/settings.gradle.kts` (`io.ktor:ktor-version-catalog`) | Every Ktor module. Versions are managed by Ktor, so never pin them. |
| `libs` | `kotlin/gradle/libs.versions.toml` | Everything else (ours to pin). |

*Catalog step 1: declare versions, libraries and the plugin in `kotlin/gradle/libs.versions.toml`.* Replace `<latest>` as above.

```toml
[versions]
kotlin = "2.4.0"
logback = "1.5.37"
prometheus = "1.17.0"
lettuce = "<latest>"
coroutines = "<latest>"
arrow = "<latest>"
testcontainers = "<latest>"

[libraries]
logback-classic = { module = "ch.qos.logback:logback-classic", version.ref = "logback" }
micrometer-registryPrometheus = { module = "io.micrometer:micrometer-registry-prometheus", version.ref = "prometheus" }
lettuce-core = { module = "io.lettuce:lettuce-core", version.ref = "lettuce" }
kotlinx-coroutines-reactive = { module = "org.jetbrains.kotlinx:kotlinx-coroutines-reactive", version.ref = "coroutines" }
arrow-core = { module = "io.arrow-kt:arrow-core", version.ref = "arrow" }
arrow-fxCoroutines = { module = "io.arrow-kt:arrow-fx-coroutines", version.ref = "arrow" }
testcontainers = { module = "org.testcontainers:testcontainers", version.ref = "testcontainers" }

[plugins]
kotlin-jvm = { id = "org.jetbrains.kotlin.jvm", version.ref = "kotlin" }
kotlin-serialization = { id = "org.jetbrains.kotlin.plugin.serialization", version.ref = "kotlin" }
```

Keep the existing entries; only the `lettuce`, `coroutines`, `arrow`, `testcontainers` versions, the last five `[libraries]` lines and the `kotlin-serialization` plugin are new. Dashes in a key become dots in the accessor (`lettuce-core` becomes `libs.lettuce.core`).

*Catalog step 2: use them in `kotlin/build.gradle.kts` (instead of the inline version above).*

```kotlin
plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.kotlin.serialization)   // new
    alias(ktorLibs.plugins.ktor)
}

dependencies {
    // ... existing entries ...
    implementation(ktorLibs.serialization.kotlinx.json)   // new
    implementation(libs.lettuce.core)                      // new
    implementation(libs.kotlinx.coroutines.reactive)       // new
    implementation(libs.arrow.core)                        // new
    implementation(libs.arrow.fxCoroutines)                // new

    testImplementation(kotlin("test"))
    testImplementation(ktorLibs.server.testHost)
    testImplementation(libs.testcontainers)                // new
}
```

**Either way, enable JSON in `kotlin/src/main/kotlin/Serialization.kt`.**

```kotlin
import io.ktor.serialization.kotlinx.json.json

fun Application.configureSerialization() {
    install(ContentNegotiation) {
        json()
    }
}
```

**Align the metrics route with the contract.** `Monitoring.kt` serves the scrape data at `/metrics-micrometer`; change it to `/metrics` to match `contract/openapi.yaml`.

**Check that everything resolves.**

```bash
cd kotlin
./gradlew build
./gradlew dependencies --configuration runtimeClasspath | grep -E "lettuce|arrow|kotlinx-serialization"
```

### 4.4 Verify the bootstrap

Definition of done for the Kotlin bootstrap:

- [ ] `./gradlew build` succeeds on a clean checkout
- [ ] `./gradlew run` starts the server and `curl localhost:8080/health` returns 200
- [ ] A trivial test runs green via `./gradlew test`
- [ ] A throwaway snippet connects to the Compose Redis through Lettuce and does `SET` / `GET`
- [ ] An Arrow `either { ... }` block compiles and a test exercises `raise(...)`
- [ ] `docs/versions.md` lists JDK, Gradle, Kotlin, Ktor, Lettuce, Arrow

---

## 5. Swift Track Bootstrap

### 5.1 Install the toolchain

- **macOS:** install the latest stable **Xcode** from the App Store, then `xcode-select --install`.
- **Linux (and optionally macOS):** install [swiftly](https://www.swift.org/install/), the official toolchain manager:

```bash
swiftly install latest
swiftly use latest
```

Verify (we want the Swift 6 language mode for strict concurrency checking):

```bash
swift --version
```

IDE: Xcode, or **VS Code** with the official Swift extension. Both work for a server project.

### 5.2 Scaffold the project

Option A: start from the Hummingbird template (check the [Hummingbird docs](https://docs.hummingbird.codes) for the current instructions, since the template flow changes occasionally).

Option B: bare SwiftPM, which is more transparent and fine for a spike:

```bash
cd swift
swift package init --type executable --name Shortener
```

Then declare dependencies in `Package.swift`.

### 5.3 Dependencies

| Package | Purpose |
|---|---|
| [Hummingbird](https://github.com/hummingbird-project/hummingbird) | HTTP server (lighter than Vapor for this use case) |
| A Redis/Valkey client: [valkey-swift](https://github.com/valkey-io/valkey-swift) or [RediStack](https://github.com/swift-server/RediStack) | **Check maintenance status and Swift 6 / async-await support before choosing.** Record the decision in `docs/versions.md`; this is a finding in itself. |
| [swift-log](https://github.com/apple/swift-log) + [swift-metrics](https://github.com/apple/swift-metrics) | Structured logging and metrics facade |
| Swift Testing (bundled with recent toolchains) or XCTest | Unit tests |

Enable Swift 6 language mode in `Package.swift` so data-race checks are errors rather than warnings:

```swift
// swift-tools-version: 6.0
```

### 5.4 Verify the bootstrap

Definition of done for the Swift bootstrap:

- [ ] `swift build` succeeds on a clean checkout
- [ ] `swift run` starts the server and `curl localhost:8080/health` returns 200
- [ ] A trivial test runs green via `swift test`
- [ ] A throwaway snippet connects to the Compose Redis and does `SET` / `GET`
- [ ] A small `Result`/typed-throws function compiles, with a test covering both paths
- [ ] `docs/versions.md` lists Swift toolchain, Hummingbird, and the Redis client

---

## 6. Agree on These Before Writing Features

Spend the first hour of the spike on these decisions, written into `docs/`:

1. **Redis key schema** (`docs/key-schema.md`), for example:
   - `link:{code}` → target URL (with TTL)
   - `counter:next-id` → atomic counter if using base62-of-counter
   - `clicks:{code}` → click count (or a stream, if using Redis Streams)
   - `ratelimit:{clientId}` → rate-limit state
2. **Code generation strategy:** atomic counter + base62 vs random + `SET NX`. Both are worth trying; decide which each track starts with.
3. **Error taxonomy and HTTP mapping:** the closed set of errors and their status codes.
4. **Rate-limit algorithm:** fixed window, sliding window, or token bucket. Model it as a pure function `(state, now) -> (decision, newState)`.

---

## 7. Suggested Milestones (For the Team to Execute)

| # | Milestone | Notes |
|---|---|---|
| 1 | Pure core + unit tests | No Redis, no HTTP. The best place to feel each language's FP ergonomics. |
| 2 | `LinkStore` interface + in-memory implementation | Service tests run without Docker. |
| 3 | Redis implementation + integration tests | Testcontainers (Kotlin) or Compose Redis (Swift). |
| 4 | HTTP layer | Handlers map core results and errors to the contract. |
| 5 | Rate limiting | Pure algorithm, shell persists state. |
| 6 | Async analytics | Coroutines/Flow (Kotlin) vs async/await and actors (Swift). |
| 7 | Metrics, health, Dockerfile | Record image size and startup time. |
| 8 | Load test and scorecard | Same k6 script against both. |

Stretch: Postgres as source of truth with Redis as cache-aside.

---

## 8. Evaluation Scorecard

Agree on the criteria and weights **before** anyone writes code, then fill this in at the end. Put it in `docs/scorecard.md`.

| Criterion | Weight | Kotlin | Swift | Notes |
|---|---|---|---|---|
| Time to a working vertical slice | | | | |
| Library maturity and gaps hit | | | | |
| FP ergonomics (error composition, immutability, exhaustiveness) | | | | |
| Concurrency model clarity and safety | | | | |
| Testability of the core without mocks | | | | |
| Error messages and tooling (IDE, debugger, build times) | | | | |
| Performance: redirect p50/p99 latency, throughput | | | | |
| Resource usage: memory, startup time, image size | | | | |
| Fit with existing stack and team skills | | | | |
| Hiring and community support | | | | |

**Quantitative data to capture:** cold start time, idle and loaded memory, container image size, k6 p50/p95/p99 and requests per second, clean build time, incremental build time.

**Qualitative data to capture:** where each person got stuck, what they had to write themselves, how readable the code is to someone who didn't write it.

---

## 9. Ground Rules for a Fair Comparison

- Same contract, same Redis key schema, same load test.
- Same machine class for benchmarks, and run them more than once.
- Note who built which track, and how experienced they were with that language.
- Record friction as it happens (a running log in `docs/`) instead of reconstructing it afterward.
- The result is a recommendation with evidence, not a vote.

---

## 10. Useful Commands (Fill In as You Go)

```just
# Justfile
up:
    docker compose up -d

down:
    docker compose down

load-test url="http://localhost:8080":
    k6 run -e BASE_URL={{url}} loadtest/redirect.js
```
