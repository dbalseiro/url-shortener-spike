# F# URL Shortener Learning Spike - Implementation Plan

## 1. Problem & Goal

You're learning F# and want to scaffold a web project by building a **URL Shortener** service. This spike already has a working **Kotlin** implementation; the F# version will follow the same OpenAPI contract and architecture pattern (functional core + imperative shell) to enable direct comparison of language/framework characteristics.

**Learning Focus**: All aspects of F#
- Functional patterns & immutability
- Type system & discriminated unions  
- Async/concurrency workflows
- Pattern matching
- Hands-on code-writing, not just reading

---

## 2. Approach & Framework Choice

### Why Giraffe + Saturn?

| Aspect | Choice | Rationale |
|--------|--------|-----------|
| **Web Framework** | Giraffe (primary) + Saturn (optional wrapping) | Giraffe teaches functional composition; composable HTTP handlers & pipelines align with F# philosophy |
| **Runtime** | .NET 8+ | Latest stable, cross-platform |
| **HTTP Client** | Giraffe's built-in routing | Native to framework, no extras needed |
| **Redis Client** | StackExchange.Redis | Mature, async-friendly, no F#-specific lock-in |
| **Testing** | xUnit + Expecto/Hedgehog | xUnit for structured tests, Expecto/Hedgehog optional for property-based testing |

### Project Structure

```
url-shortener-spike/
├── fsharp-shortener/              # New F# project root
│   ├── fsharp-shortener.sln       # Solution file
│   ├── src/
│   │   ├── Domain/                # Pure F# types & functions
│   │   │   ├── Domain.fs          # Error union, Request/Response types
│   │   │   ├── Validation.fs      # URL validation, TTL checks
│   │   │   ├── Encoding.fs        # Base62 encoding/decoding
│   │   │   └── Model.fs           # LinkStore abstraction
│   │   ├── Infrastructure/        # I/O layer
│   │   │   ├── Redis.fs           # Redis LinkStore implementation
│   │   │   └── Memory.fs          # In-memory LinkStore (testing)
│   │   ├── Http/                  # HTTP handlers
│   │   │   ├── Handlers.fs        # Route handlers
│   │   │   └── Middleware.fs      # Rate limiter, error mapping
│   │   ├── Program.fs             # Application entry point
│   │   └── *.fsproj               # Project file
│   ├── tests/
│   │   ├── Domain.Tests.fs        # Unit tests for pure functions
│   │   ├── Integration.Tests.fs   # Integration tests (in-memory store)
│   │   └── *.fsproj               # Test project file
│   ├── README.md                  # F# notes & learning outcomes
│   └── Dockerfile                 # Container image

├── docker-compose.yaml            # (Updated to include fsharp-shortener)
├── contract/                      # (Shared OpenAPI spec)
└── kotlin/                        # (Existing Kotlin version)
```

---

## 3. Implementation Phases

### Phase 0: Scaffolding & Setup
**Goal**: Get a working .NET F# project with Giraffe, compiling and serving requests.

**Todos**:
- `fsharp-scaffold`: Create new .NET F# project using `dotnet new` with Giraffe template
- Add dependencies: `Giraffe`, `StackExchange.Redis`, `xUnit`
- Verify: `dotnet build` and `dotnet run` work, server listens on port 8080

**Expected Output**:
- `fsharp-shortener/` directory with `.fsproj`, `Program.fs`, folder structure
- A single `GET /health` endpoint returning `{"status":"ok"}`

**What I did***
Just a simple server
```
dotnet new web -lang F# -o MyService
```

---

### Phase 1: Domain Model (Pure F#)
**Goal**: Implement types and pure functions matching the OpenAPI contract. **No I/O here.**

**Todos**:
- `fsharp-core-domain`: Create domain types
  - Discriminated union: `Error = InvalidUrl | AliasTaken | RateLimited | NotFound`
  - Record types: `ShortenRequest`, `ShortenResponse`, `Link`
  - `Result<'T, Error>` wrapper for error handling
  
- `fsharp-validation-errors`: Implement validation pipeline
  - `validateUrl : string -> Result<Uri, Violation list>`
  - `validateAlias : string option -> Result<string option, Violation>`
  - `validateTtl : int option -> Result<DateTimeOffset option, Violation>`
  - Violation types: `InvalidUrl`, `InvalidAlias`, `InvalidTtl`
  - Compose validators and collect all violations (not short-circuit)

- Implement utility functions (pure):
  - `base62Encode : int64 -> string`
  - `base62Decode : string -> int64`
  - `generateCode : unit -> string` (using timestamp + random)
  - `mapErrorToStatus : Error -> HttpStatusCode` (error → HTTP)

**Expected Output**:
- `Domain/Domain.fs`: Type definitions
- `Domain/Validation.fs`: Validators
- `Domain/Encoding.fs`: Base62 functions
- No external dependencies (except F# stdlib)

**Learning Checkpoints**:
- How discriminated unions model domain errors
- Immutability by default (all types are immutable)
- `Result` monad for error handling chains
- Pattern matching in validators

---

### Phase 2: Storage Abstraction (Redis)
**Goal**: Define a storage interface and implement Redis + in-memory versions.

**Todos**:
- `fsharp-redis-abstraction`: Define `LinkStore` interface
  - `type LinkStore = { save: Link -> Async<Result<unit, Error>>; get: string -> Async<Result<Link option, Error>>; ... }`
  - Or use F# interfaces for OOP-style if preferred

- Create `Redis.fs` implementation
  - Connect to Redis on `localhost:6379` (from docker-compose)
  - `save`: Serialize `Link`, set key with TTL, handle `AliasTaken` collision
  - `get`: Fetch key, deserialize, check expiry
  - **Learning**: Async workflows, error handling in I/O, Redis commands

- Create `Memory.fs` implementation (for testing)
  - Use `ConcurrentDictionary` or `MailboxProcessor` to hold links
  - Same signature as Redis version
  - **Learning**: Immutable data structures, actor-style async

**Expected Output**:
- `Domain/Model.fs`: `Link` type and `LinkStore` interface
- `Infrastructure/Redis.fs`: Redis implementation
- `Infrastructure/Memory.fs`: In-memory implementation

**Dependencies**:
- Depends on: `fsharp-core-domain` (type definitions)

**Learning Checkpoints**:
- F# `async { }` computation expressions
- Interface design in F#
- Error handling across async boundaries
- Testing seam: easy to swap implementations

---

### Phase 3: HTTP Handlers (Giraffe routing)
**Goal**: Wire domain + storage to HTTP endpoints per OpenAPI spec.

**Todos**:
- `fsharp-http-handlers`: Implement route handlers
  - `POST /shorten`: Extract request → validate → call domain → save to store → return 201 + response
  - `GET /{code}`: Extract code → call store → return 302 redirect or 404
  - `GET /health`: Return `{"status":"ok"}`
  - `GET /metrics`: Return Prometheus-format metrics (stub for now)
  - **Learning**: Giraffe's `choose` and handler composition, JSON serialization (Giraffe.Serialization)

- `Middleware.fs`: Error mapping
  - Catch domain `Error` and map to HTTP status + JSON error response
  - Match on error discriminated union → (statusCode, errorBody)
  - **Learning**: Higher-order functions, error translation

- `Program.fs`: Wire it all up
  - Initialize Redis store (or memory for testing)
  - Configure Giraffe router with all handlers
  - Set up error-handling middleware
  - **Learning**: Dependency injection pattern in F#

**Expected Output**:
- `Http/Handlers.fs`: Route handlers
- `Http/Middleware.fs`: Error mapping
- `Program.fs`: Wired application

**Dependencies**:
- Depends on: `fsharp-redis-abstraction`, `fsharp-validation-errors`

**Learning Checkpoints**:
- Function composition: handlers are functions that compose
- Giraffe's `choose` combinator (functional routing)
- Async error handling in route handlers
- JSON serialization/deserialization

---

### Phase 4: Rate Limiting (Async + Redis)
**Goal**: Implement per-client rate limiting middleware.

**Todos**:
- `fsharp-rate-limiting`: Add rate limiter
  - Extract client IP from request headers
  - Check Redis counter for IP: `rate:limit:{ip}` with TTL
  - If over limit, return 429 + `Retry-After` header
  - Else increment counter and proceed
  - **Learning**: Advanced async patterns, Redis operations in sequence, header handling

- Integrate into middleware pipeline
- Test with concurrent requests

**Expected Output**:
- `Http/Middleware.fs`: Rate limiter implementation
- Updated `Program.fs` to apply rate limiter

**Dependencies**:
- Depends on: `fsharp-http-handlers`

**Learning Checkpoints**:
- Redis atomic operations (INCR, EXPIRE) from F#
- Request header parsing
- Async sequencing (do! in async blocks)

---

### Phase 5: Testing (Unit + Integration)
**Goal**: Comprehensive tests ensuring behavior matches OpenAPI spec.

**Todos**:
- `fsharp-testing-unit`: Unit tests for pure functions
  - Test validators: valid/invalid URLs, aliases, TTLs
  - Test base62 encode/decode round-trip
  - Test error mapping to HTTP status codes
  - Use xUnit facts and theories
  - **Learning**: Property-based testing (optional: FsCheck for quickcheck-style tests)

- `fsharp-testing-integration`: Integration tests
  - Spin up in-memory `LinkStore`
  - Make HTTP requests to running server (TestServer)
  - Test full request-response cycle: create → retrieve → redirect
  - Test error cases: invalid URL, alias taken, not found, rate limit
  - **Learning**: Test framework patterns, async test setup/teardown

**Expected Output**:
- `tests/Domain.Tests.fs`: Unit test suite
- `tests/Integration.Tests.fs`: Integration test suite
- `.fsproj` test project wired up

**Dependencies**:
- Depends on: `fsharp-core-domain`, `fsharp-http-handlers`

**Learning Checkpoints**:
- xUnit and async test patterns
- Test fixtures and setup/teardown
- Mocking and seams in F#
- Assertion libraries (fluent assertions, custom matchers)

---

### Phase 6: Containerization & Compose
**Goal**: Build Docker image and add to docker-compose.

**Todos**:
- `fsharp-docker-compose`: Create Dockerfile
  - Multi-stage build: build stage on `mcr.microsoft.com/dotnet/sdk:8`, runtime stage on `mcr.microsoft.com/dotnet/runtime:8`
  - Copy built binaries to runtime stage
  - EXPOSE 8080
  - CMD ["dotnet", "fsharp-shortener.dll"]

- Update repo's `docker-compose.yaml`
  - Add `fsharp-shortener` service
  - Ensure Redis is shared and both Kotlin + F# can connect
  - Expose F# service on port 8081 (or alternate)

- Verify: `docker compose up` starts both Kotlin and F# services

**Expected Output**:
- `fsharp-shortener/Dockerfile`
- Updated `docker-compose.yaml`

**Dependencies**:
- Depends on: `fsharp-http-handlers` (runnable app)

**Learning Checkpoints**:
- .NET multi-stage Docker builds
- Container networking, environment variables

---

### Phase 7: Documentation & Learning Reflection
**Goal**: Capture learning, compare to Kotlin, write README.

**Todos**:
- `fsharp-docs-learning`: Write `README.md`
  - How to build, run, test
  - Key F# patterns used in this spike (discriminated unions, async workflows, pattern matching)
  - Trade-offs vs. Kotlin (verbosity, type safety, functional idioms)
  - Pain points and delights
  - Recommendations for future F# projects in this team
  
- Update parent `docs/versions.md` with F# / .NET versions used

**Expected Output**:
- `fsharp-shortener/README.md`
- Updated `docs/versions.md`

---

## 4. Success Criteria

### Code Quality
- ✅ All types are immutable (F# default)
- ✅ Pure domain functions have no side effects
- ✅ All error cases modeled as discriminated unions (no exceptions for control flow)
- ✅ Handlers are composable functions
- ✅ No shared mutable state outside Redis

### Behavior
- ✅ Implements all 4 endpoints from OpenAPI spec
- ✅ Validates requests per spec (URL, alias, TTL)
- ✅ Returns correct HTTP status codes (201, 302, 400, 404, 409, 429)
- ✅ Rate limiting works (per-IP, Redis-backed)
- ✅ Expired links return 404

### Testing
- ✅ Unit tests pass: validators, encoding, error mapping
- ✅ Integration tests pass: all endpoints work end-to-end
- ✅ No Docker required for tests (use in-memory store)

### Learning
- ✅ Code demonstrates F# strengths: immutability, pattern matching, async, discriminated unions
- ✅ README explains patterns and trade-offs
- ✅ Next developer can extend this without confusion

---

## 5. Technology Summary

| Component | Technology | Why |
|-----------|------------|-----|
| Runtime | .NET 8 | Latest stable, cross-platform |
| Language | F# 8.0 | Full feature set, learning goal |
| Web Framework | Giraffe | Functional, composable routing |
| Redis Client | StackExchange.Redis | Async-friendly, mature |
| Testing | xUnit + Expecto | xUnit structure, Expecto for assertions |
| Serialization | System.Text.Json (Giraffe default) | Built-in, performant |
| Build | dotnet CLI | Standard .NET tooling |
| Container | Docker + multi-stage | Lean runtime image |

---

## 6. Dependency Graph (Execution Order)

```
fsharp-scaffold
├── fsharp-core-domain
│   ├── fsharp-validation-errors
│   ├── fsharp-testing-unit
│   └── fsharp-redis-abstraction
│       └── fsharp-http-handlers
│           ├── fsharp-rate-limiting
│           ├── fsharp-testing-integration
│           └── fsharp-docker-compose
│               └── fsharp-docs-learning
```

**Parallelizable**:
- Unit tests can start as soon as core domain is done
- Integration tests wait for handlers
- Docker/docs wait for everything else

---

## 7. Milestones & Checkpoints

| Milestone | Todos | Learning Goal | Checkpoint |
|-----------|-------|----------------|------------|
| **Day 1** | Scaffold | F# syntax, project structure | `dotnet run` works, `/health` responds |
| **Day 2** | Domain | Discriminated unions, pattern matching, immutability | All validators compile, tests pass |
| **Day 3** | Storage | Async workflows, interface design, error handling | In-memory store works, Redis connects |
| **Day 4** | HTTP + Handlers | Giraffe routing, composition, JSON serialization | All endpoints respond, match OpenAPI spec |
| **Day 5** | Rate Limiting | Advanced async, middleware, Redis ops | Rate limiter blocks after threshold |
| **Day 6** | Tests | Test frameworks, test patterns, property-based testing | 100% test pass rate, >80% code coverage |
| **Day 7** | Docker + Docs | Multi-stage builds, comparison to Kotlin, reflection | Container runs, README captures learning |

---

## 8. Notes for You

1. **Hands-on emphasis**: Every phase has you writing code, not just reading examples.
2. **Comparison to Kotlin**: As you build each piece in F#, mentally note how it differs from the Kotlin version—type system, error handling, async model, developer experience.
3. **Testing seam**: The in-memory `LinkStore` is a critical design choice; it means you never need Docker for testing, and it teaches interface-driven design.
4. **Functional-first**: Resist the urge to use mutable state or exceptions for control flow. Model everything as types and let F#'s type system guide you.
5. **Async is pervasive**: Redis calls, file I/O, and HTTP handlers all use F# `async`. This is a core learning area.
6. **Giraffe is lightweight**: It's easy to compose but requires you to think functionally about routing and error handling.

---

## Ready to Start?

Phase 0 (Scaffolding) is your entry point. Once the dotnet CLI is installed and project compiles, move to Phase 1 (Domain). Each phase is self-contained but depends on earlier ones.

Let me know when you want to begin! 🚀
