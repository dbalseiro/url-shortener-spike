package com.jurisfutura.shortener.shorten

import kotlinx.serialization.Serializable

@Serializable
data class ShortenRequest(
    val url: String,
    val alias: String? = null,
    val ttlSeconds: Int? = null
)

@Serializable
data class ShortenResponse(
    val code: String,
    val shortUrl: String,
    val url: String,
    // date in milliseconds if present
    val expiresAt: Long? = null
)

@Serializable
sealed class ShortenError {
    @Serializable
    data class InvalidInput(
        val message: String,
        val violations: List<Violation>
    ) : ShortenError()
    @Serializable
    data class AliasTaken(val message: String) : ShortenError()
    @Serializable
    data class RateLimited(val message: String) : ShortenError()
}

// sealed makes "when" exhaustive
@Serializable
sealed class Violation {
    @Serializable
    data class InvalidUrl(val message: String) : Violation()
    @Serializable
    data class InvalidAlias(val message: String) : Violation()
    @Serializable
    data class InvalidTtl(val message: String) : Violation()
}

