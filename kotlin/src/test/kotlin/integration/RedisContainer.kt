package com.jurisfutura

import org.testcontainers.containers.GenericContainer
import org.testcontainers.utility.DockerImageName

/**
 * Manages a Redis test container lifecycle.
 * Automatically starts/stops Redis for integration tests.
 */
object RedisContainer {
    private var container: GenericContainer<*>? = null

    fun start() {
        if (container?.isRunning == true) {
            return
        }

        val redisContainer = GenericContainer(DockerImageName.parse("redis:7-alpine"))
            .withExposedPorts(6379)

        redisContainer.start()
        container = redisContainer

        val port = redisContainer.getMappedPort(6379)
        val redisUrl = "redis://localhost:$port"
        System.setProperty("REDIS_URL", redisUrl)
    }

    fun stop() {
        container?.stop()
        container = null
        System.clearProperty("REDIS_URL")
    }
}
