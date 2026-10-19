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
    /**
     * WHAT IS A COMPANION OBJECT?
     * ============================
     *
     * A companion object is Kotlin's way of defining "static" members (like in Java).
     *
     * Key points:
     * 1. SCOPE: Members inside a companion object belong to the CLASS, not to instances
     *    - You call them with: RateLimiterCfg.fromConfig(...)
     *    - NOT: RateLimiterCfg().fromConfig(...)
     *
     * 2. ONE PER CLASS: A class can have only ONE companion object
     *    - If you need more, use a separate object or create extensions
     *
     * 3. FACTORY PATTERN: Commonly used for factory methods
     *    - Example: RateLimiterCfg.fromConfig() creates an instance from a Map
     *    - It's a way to provide alternative constructors
     *
     * 4. PRIVATE ACCESS: Can access private members of the enclosing class
     *    - This allows it to do internal validation/transformation
     *
     * 5. REPLACES JAVA'S STATIC: In Java you'd write:
     *    public static RateLimiterCfg fromConfig(Map<String, ?> config) { ... }
     *    In Kotlin with companion object, it's more organized
     *
     * WHY USE IT HERE?
     * ================
     * We use it to convert configuration data (a Map) into a properly typed
     * RateLimiterCfg object. This is a "smart constructor" or "factory method".
     * Users can call: RateLimiterCfg.fromConfig(configMap)
     */
    companion object {
        fun fromConfig(config: Map<String, Any?>): RateLimiterCfg {
            val def = RateLimiterCfg()
            val tokensPerSecond = config.int("tokensPerSecond") ?: def.tokensPerSecond
            val maxTokens = config.int("maxTokens") ?: def.maxTokens
            val windowSeconds = config.int("windowSeconds") ?: def.windowSeconds
            return RateLimiterCfg(tokensPerSecond, maxTokens, windowSeconds)
        }

        // Helper function (also in companion, so it's private to the class)
        // Safely extracts Int values from a Map
        private fun Map<String, Any?>.int(key: String): Int? = (this[key] as? Number)?.toInt()
    }
}

open class RateLimiter(cfg: RateLimiterCfg) {
    open suspend fun isRateLimited(clientIp: String): Boolean = TODO("Not yet implemented")
}