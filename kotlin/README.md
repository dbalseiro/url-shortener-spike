# shortener

This project was created using the [Ktor Project Generator](https://start.ktor.io).

Here are some useful links to get you started:
 * [Ktor Documentation](https://ktor.io/docs/home.html)
 * [Ktor GitHub page](https://github.com/ktorio/ktor)
 * [Ktor Slack chat](https://app.slack.com/client/T09229ZC6/C0A974TJ9). [Request an invite](https://surveys.jetbrains.com/s3/kotlin-slack-sign-up).


## Features
Here's a list of features included in this project:

| Name | Description |
|------|-------------|
| [Micrometer Metrics](https://start.ktor.io/p/io.ktor/server-metrics-micrometer) | Enables Micrometer metrics in your Ktor server application. |
| [Call Logging](https://start.ktor.io/p/io.ktor/server-call-logging) | Logs client requests |
| [Status Pages](https://start.ktor.io/p/io.ktor/server-status-pages) | Provides exception handling for routes |
| [Content Negotiation](https://start.ktor.io/p/io.ktor/server-content-negotiation) | Provides automatic content conversion according to Content-Type and Accept headers |


## Building & Running
To build or run the project, use one of the following tasks:


| Task | Description |
|------|-------------|
| `./gradlew test`    | Run unit tests (no Redis required)     |
| `./gradlew testIntegration` | Run integration tests (requires Redis) |
| `./gradlew build`   | Build the project |
| `./gradlew run`     | Run the server    |

### Test Organization

Tests are organized into two categories:

- **Unit Tests** (`src/test/kotlin/unit/`): Fast tests that verify business logic without external dependencies. These can run in CI without requiring Redis or other services.
- **Integration Tests** (`src/test/kotlin/integration/`): Tests that interact with the full application stack, including Redis and other services.

Use `./gradlew test` for quick feedback during development, and `./gradlew testIntegration` to verify the complete system.

If the server starts successfully, you'll see the following output:
```
2024-12-04 14:32:45.584 [main] INFO  Application - Application started in 0.303 seconds.
2024-12-04 14:32:45.682 [main] INFO  Application - Responding at http://0.0.0.0:8080
```
