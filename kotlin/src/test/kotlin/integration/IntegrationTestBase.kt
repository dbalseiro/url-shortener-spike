package com.jurisfutura

import org.junit.BeforeClass
import org.junit.AfterClass
import kotlin.test.BeforeTest

abstract class IntegrationTestBase {
    companion object {
        private val initialized = lazy {
            System.setProperty("REDIS_TEST_MODE", "true")
            RedisContainer.start()
            // Add shutdown hook to stop container when JVM exits
            Runtime.getRuntime().addShutdownHook(Thread {
                RedisContainer.stop()
            })
            true
        }

        fun ensureInitialized() {
            initialized.value
        }
    }

    init {
        ensureInitialized()
    }

    @BeforeTest
    fun setup() {
        RedisContainer.clear()
    }
}

