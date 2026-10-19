package com.jurisfutura

import io.lettuce.core.RedisClient
import kotlinx.coroutines.delay
import kotlinx.coroutines.runBlocking
import org.testcontainers.containers.GenericContainer
import org.testcontainers.utility.DockerImageName

/**
 * Manages a Redis test container lifecycle.
 * Automatically starts/stops Redis for integration tests.
 */
object RedisContainer {
    private var container: GenericContainer<*>? = null
    private var redisUrl: String? = null

    fun start() {
        val redisContainer = GenericContainer(DockerImageName.parse("redis:7-alpine"))
            .withExposedPorts(6379)

        redisContainer.start()
        container = redisContainer

        val port = redisContainer.getMappedPort(6379)
        redisUrl = "redis://localhost:$port"
        System.setProperty("REDIS_URL", redisUrl)
        
        waitForRedis(redisUrl)
    }

    private fun waitForRedis(url: String?) {
        if (url == null) return
        runBlocking {
            repeat(10) {
                try {
                    val client = RedisClient.create(url)
                    val conn = client.connect()
                    conn.close()
                    client.shutdown()
                    return@runBlocking
                } catch (e: Exception) {
                    delay(200)
                }
            }
        }
    }

    fun stop() {
        try {
            container?.stop()
            container?.close()
        } catch (e: Exception) {
            // Already stopped
        }
        container = null
        redisUrl = null
        System.clearProperty("REDIS_URL")
    }

    fun clear() {
        val url = redisUrl ?: return
        try {
            val client = RedisClient.create(url)
            val conn = client.connect()
            conn.async().flushall()
            conn.close()
            client.shutdown()
        } catch (e: Exception) {
            // Redis not available
        }
    }
}
