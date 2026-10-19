
plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.kotlin.serialization)
    alias(ktorLibs.plugins.ktor)
}

group = "com.jurisfutura"
version = "1.0.0-SNAPSHOT"

application {
    mainClass = "io.ktor.server.netty.EngineMain"
}

kotlin {
    jvmToolchain(21)
}

// Configuration of sourcesets to separate unit tests and integration tests
sourceSets {
    // WHY "named"?
    // ==================
    // The "test" sourceset is BUILT-IN/STANDARD in Gradle
    // It's created automatically when you apply the kotlin-jvm plugin
    // We use "named" to ACCESS and CONFIGURE this existing sourceset
    // "named" avoids creating unused variable references in the DSL
    //
    // By default, Gradle's "test" sourceset looks for tests in:
    //   - src/test/kotlin/
    //   - src/test/java/
    //
    // We're REPLACING those default directories with ONLY unit tests:
    named("test") {
        // Override the default test directories to only include unit tests
        kotlin.setSrcDirs(setOf("src/test/kotlin/unit"))
    }

    // WHY "create"?
    // ===================
    // The "integrationTest" sourceset DOES NOT EXIST by default in Gradle
    // It's a CUSTOM sourceset we need to define from scratch
    // We use "create" to DECLARE and CONFIGURE a new sourceset
    //
    // This new sourceset:
    // - Is completely separate from "test"
    // - Can have its own dependencies configuration
    // - Can have its own source directories
    // - Will be used by our custom "testIntegration" Gradle task
    create("integrationTest") {
        // Integration tests are located in src/test/kotlin/integration/
        kotlin.setSrcDirs(setOf("src/test/kotlin/integration"))

        // Integration tests need access to compiled code from main and test
        // so we add the output of both sourcesets to their classpath
        compileClasspath += sourceSets["main"].output + sourceSets["test"].output
        runtimeClasspath += sourceSets["main"].output + sourceSets["test"].output
    }
}
dependencies {
    implementation(ktorLibs.server.callLogging)
    implementation(ktorLibs.server.config.yaml)
    implementation(ktorLibs.server.contentNegotiation)
    implementation(ktorLibs.server.core)
    implementation(ktorLibs.server.metrics.micrometer)
    implementation(ktorLibs.server.netty)
    implementation(ktorLibs.server.statusPages)
    implementation(ktorLibs.server.forwardedHeader)
    implementation(libs.logback.classic)
    implementation(libs.micrometer.registryPrometheus)

    implementation(ktorLibs.serialization.kotlinx.json)
    implementation(libs.lettuce.core)
    implementation(libs.kotlinx.coroutines.reactive)
    implementation(libs.arrow.core)
    implementation(libs.arrow.fx.coroutines)

    // Define shared dependencies between unit tests and integration tests
    // using a list to avoid code duplication
    val testDeps = listOf(
        kotlin("test"),           // Kotlin testing framework
        ktorLibs.client.contentNegotiation,
        ktorLibs.server.testHost  // Ktor test host to simulate HTTP server
    )
    
    // For each dependency, add it to BOTH: testImplementation and integrationTestImplementation
    // This ensures both unit tests and integration tests have access to these libraries
    testDeps.forEach {
        testImplementation(it)                    // For unit tests (src/test/kotlin/unit/)
        "integrationTestImplementation"(it)       // For integration tests (src/test/kotlin/integration/)
    }
}

// Configuration of compilation dependencies for "integrationTestImplementation"
// This tells Gradle that testImplementation dependencies should also be included
// in integrationTestImplementation, avoiding repeating the same dependencies twice
configurations {
    "integrationTestImplementation" {
        extendsFrom(testImplementation.get())
    }
}

// Custom Gradle task to run ONLY integration tests
// Usage: ./gradlew testIntegration
tasks.register<Test>("testIntegration") {
    // Defines which directories contain the compiled test classes
    testClassesDirs = sourceSets["integrationTest"].output.classesDirs
    
    // Defines the classpath (all libraries and code needed to run the tests)
    // Includes: main code + test dependencies + integration test code
    classpath = sourceSets["integrationTest"].runtimeClasspath
    
    description = "Run integration tests (requires Redis and other services)"
}
