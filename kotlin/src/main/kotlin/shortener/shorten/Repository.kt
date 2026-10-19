package com.jurisfutura.shortener.shorten

import com.jurisfutura.shortener.redis.RedisConnection
import io.lettuce.core.*
import kotlinx.coroutines.future.await

interface ShortenRepository {
    suspend fun save(shortCode: String, longUrl: String, ttlSeconds: Long?)
    suspend fun get(shortCode: String): String?
    suspend fun exists(shortCode: String): Boolean
}

class ShortenRepositoryImpl : ShortenRepository {
    private val redis = RedisConnection.getInstance().async()

    override suspend fun save(shortCode: String, longUrl: String, ttlSeconds: Long?) {
        val args = SetArgs()
        if (ttlSeconds != null) {
            args.ex(ttlSeconds)
        }
        redis.set(shortCode, longUrl, args).await()
    }

    override suspend fun get(shortCode: String): String? = redis.get(shortCode).await()

    override suspend fun exists(shortCode: String): Boolean = redis.exists(shortCode).await() > 0
}