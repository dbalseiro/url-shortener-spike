package com.jurisfutura.shortener.shorten

import com.jurisfutura.shortener.redis.RedisConnection
import io.lettuce.core.*
import kotlinx.coroutines.future.await

class ShortenRepository {
    private val redis = RedisConnection.getInstance().async()

    suspend fun save(shortCode: String, longUrl: String, ttlSeconds: Long?) {
        val args = SetArgs()
        if (ttlSeconds != null) {
            args.ex(ttlSeconds)
        }
        redis.set(shortCode, longUrl, args).await()
    }

    suspend fun get(shortCode: String): String? = redis.get(shortCode).await()

    suspend fun exists(shortCode: String): Boolean = redis.exists(shortCode).await() > 0
}