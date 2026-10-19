package com.jurisfutura.shortener.redis

import io.lettuce.core.RedisClient
import io.lettuce.core.api.StatefulRedisConnection

// This is a singleton object that manages the Redis connection. It ensures that
// only one instance of the connection is created and reused throughout the application.
object RedisConnection {
    // Volatile variables to ensure visibility of changes across threads
    @Volatile
    private var client: RedisClient? = null

    @Volatile
    private var connection: StatefulRedisConnection<String, String>? = null

    fun getInstance(): StatefulRedisConnection<String, String> =
        connection ?: synchronized(this) {
            connection ?: createConnection()
        }

    private fun createConnection(): StatefulRedisConnection<String, String> {
        val redisUrl = System.getenv("REDIS_URL") 
            ?: System.getProperty("REDIS_URL")
            ?: "redis://localhost:6379"
        client = RedisClient.create(redisUrl)
        // !! is kinda unsafe, but we just created the client, so it should not be null here.
        return client!!.connect().also { connection = it }
    }

    fun shutdown() {
        connection?.close()
        client?.shutdown()
    }
}