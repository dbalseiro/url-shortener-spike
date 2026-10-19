package com.jurisfutura.shortener

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


data class RateLimiterCfg(
    val tokensPerSecond: Int = 5,
    val maxTokens: Int = 10,
    val windowSeconds: Int = 60
) {
    companion object {
        fun fromConfig(config: Map<String, Any?>): RateLimiterCfg {
            val def = RateLimiterCfg()
            val tokensPerSecond = config.int("tokensPerSecond") ?: def.tokensPerSecond
            val maxTokens = config.int("maxTokens") ?: def.maxTokens
            val windowSeconds = config.int("windowSeconds") ?: def.windowSeconds
            return RateLimiterCfg(tokensPerSecond, maxTokens, windowSeconds)
        }

        private fun Map<String, Any?>.int(key: String): Int? = (this[key] as? Number)?.toInt()
    }
}

open class RateLimiter(cfg: RateLimiterCfg) {
    open suspend fun isRateLimited(clientIp: String): Boolean = TODO("Not yet implemented")
}