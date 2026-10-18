package com.jurisfutura.shortener.shorten

import com.jurisfutura.shortener.redis.RedisConnection
import io.lettuce.core.*
import kotlinx.coroutines.future.await

// In Kotlin, classes are FINAL by default (unlike Java), meaning they cannot be inherited.
// We made this class "open" to allow test code to create MockShortenRepository as a subclass.
//
// BETTER ALTERNATIVE (not done here to keep it simple):
// Extract an interface (ShortenRepositoryPort) and have both ShortenRepository and MockShortenRepository
// implement it. This allows testing without forcing the production class to be open.
open class ShortenRepository {
    private val redis = RedisConnection.getInstance().async()

    open suspend fun save(shortCode: String, longUrl: String, ttlSeconds: Long?) {
        val args = SetArgs()
        if (ttlSeconds != null) {
            args.ex(ttlSeconds)
        }
        redis.set(shortCode, longUrl, args).await()
    }

    open suspend fun get(shortCode: String): String? = redis.get(shortCode).await()

    open suspend fun exists(shortCode: String): Boolean = redis.exists(shortCode).await() > 0
}