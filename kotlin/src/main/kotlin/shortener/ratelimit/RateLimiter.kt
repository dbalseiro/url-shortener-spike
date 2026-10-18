package com.jurisfutura.shortener.ratelimit

import com.jurisfutura.shortener.redis.RedisConnection
import io.lettuce.core.ScriptOutputType
import kotlinx.coroutines.future.await

/**
 * Thread-safe rate limiter using Redis token bucket algorithm.
 *
 * The token bucket algorithm works as follows:
 * - Each client (identified by IP) has a bucket of tokens
 * - Tokens are refilled at a constant rate (tokensPerSecond)
 * - Maximum capacity is maxTokens
 * - Each request consumes 1 token
 * - If no tokens available, request is rate limited
 */
open class RateLimiter(
    private val maxTokens: Int,
    private val tokensPerSecond: Double,
    private val windowSeconds: Long = 60
) {
    private val redis by lazy { RedisConnection.getInstance().async() }

    /**
     * Checks if a request from the given client IP should be allowed.
     * Uses a Lua script to atomically check and update the token bucket.
     * Thread-safe by virtue of Redis atomic operations.
     */
    open suspend fun allowRequest(clientIp: String): Boolean {
        val tokensKey = "rate-limit:tokens:$clientIp"
        val timestampKey = "rate-limit:timestamp:$clientIp"
        val now = System.currentTimeMillis()

        val script = """
            local tokensKey = KEYS[1]
            local timestampKey = KEYS[2]
            local maxTokens = tonumber(ARGV[1])
            local tokensPerSecond = tonumber(ARGV[2])
            local now = tonumber(ARGV[3])
            local windowSeconds = tonumber(ARGV[4])
            
            local lastTimestamp = redis.call('GET', timestampKey)
            local currentTokens = tonumber(redis.call('GET', tokensKey) or maxTokens)
            
            if not lastTimestamp then
                -- First request, initialize with full bucket
                redis.call('SET', tokensKey, maxTokens - 1, 'EX', windowSeconds)
                redis.call('SET', timestampKey, now, 'EX', windowSeconds)
                return 1  -- Allow request
            end
            
            lastTimestamp = tonumber(lastTimestamp)
            local elapsedMs = now - lastTimestamp
            local elapsedSeconds = elapsedMs / 1000.0
            
            -- Calculate tokens to add based on elapsed time
            local tokensToAdd = elapsedSeconds * tokensPerSecond
            local newTokens = math.min(maxTokens, currentTokens + tokensToAdd)
            
            -- Try to consume one token
            if newTokens >= 1 then
                newTokens = newTokens - 1
                redis.call('SET', tokensKey, tostring(newTokens), 'EX', windowSeconds)
                redis.call('SET', timestampKey, now, 'EX', windowSeconds)
                return 1  -- Allow request
            else
                -- Not enough tokens, keep the current tokens
                redis.call('SET', tokensKey, tostring(newTokens), 'EX', windowSeconds)
                return 0  -- Deny request
            end
        """.trimIndent()

        val result = redis.eval<Long>(
            script,
            ScriptOutputType.INTEGER,
            arrayOf(tokensKey, timestampKey),
            maxTokens.toString(),
            tokensPerSecond.toString(),
            now.toString(),
            windowSeconds.toString()
        ).await()

        return result == 1L
    }

    /**
     * Resets the rate limit for a specific client (useful for testing).
     */
    suspend fun reset(clientIp: String) {
        val tokensKey = "rate-limit:tokens:$clientIp"
        val timestampKey = "rate-limit:timestamp:$clientIp"
        redis.del(tokensKey, timestampKey).await()
    }
}





